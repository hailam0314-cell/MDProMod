package net.mdpromod;

import com.mojang.brigadier.arguments.IntegerArgumentType;
import com.mojang.brigadier.arguments.StringArgumentType;
import com.mojang.brigadier.CommandDispatcher;
import com.mojang.brigadier.exceptions.CommandSyntaxException;
import net.minecraft.commands.CommandSourceStack;
import net.minecraft.commands.Commands;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.event.RegisterCommandsEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;

import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;

final class DuelCommands {
    /** 玩家卡组目录（{@code config/mdpromod/decks}）：模组资源目录是只读的，玩家的东西放配置目录。 */
    private static Path deckDir() {
        return ModPaths.deckDir();
    }

    private final DuelSessionManager sessions;

    DuelCommands(DuelSessionManager sessions) {
        this.sessions = sessions;
    }

    @SubscribeEvent
    public void register(RegisterCommandsEvent event) {
        CommandDispatcher<CommandSourceStack> dispatcher = event.getDispatcher();
        dispatcher.register(Commands.literal("mdpromod")
                .then(Commands.literal("anim")
                        .then(Commands.literal("demo").executes(context -> animationDemo(context.getSource())))
                        .then(Commands.literal("stop").executes(context -> stop(context.getSource()))))
                .then(Commands.literal("duel")
                        .then(Commands.literal("start").executes(context -> start(context.getSource())))
                        .then(Commands.literal("smoke").executes(context -> smoke(context.getSource())))
                        .then(Commands.literal("prompt").executes(context -> prompt(context.getSource())))
                        .then(Commands.literal("respond")
                                .then(Commands.argument("value", IntegerArgumentType.integer())
                                        .executes(context -> respond(context.getSource(),
                                                IntegerArgumentType.getInteger(context, "value")))))
                        .then(Commands.literal("respondbytes")
                                .then(Commands.argument("hex", StringArgumentType.word())
                                        .executes(context -> respondBytes(context.getSource(),
                                                StringArgumentType.getString(context, "hex")))))
                        .then(Commands.literal("places")
                                .executes(context -> places(context.getSource())))
                        .then(Commands.literal("place")
                                .then(Commands.argument("index", IntegerArgumentType.integer(0))
                                        .executes(context -> place(context.getSource(),
                                                IntegerArgumentType.getInteger(context, "index")))))
                        .then(Commands.literal("stop").executes(context -> stop(context.getSource())))
                        .then(Commands.literal("stats").executes(context -> stats(context.getSource())))
                        .then(Commands.literal("status").executes(context -> status(context.getSource()))))
                .then(Commands.literal("card")
                        .then(Commands.literal("give")
                                .then(Commands.argument("code", IntegerArgumentType.integer(0))
                                        .executes(context -> giveCard(context.getSource(),
                                                IntegerArgumentType.getInteger(context, "code"), 1))
                                        .then(Commands.argument("count", IntegerArgumentType.integer(1, 64))
                                                .executes(context -> giveCard(context.getSource(),
                                                        IntegerArgumentType.getInteger(context, "code"),
                                                        IntegerArgumentType.getInteger(context, "count"))))))
                        .then(Commands.literal("spawn")
                                .then(Commands.argument("code", IntegerArgumentType.integer(0))
                                        .executes(context -> spawnCard(context.getSource(),
                                                IntegerArgumentType.getInteger(context, "code"))))))
                .then(Commands.literal("field")
                        .then(Commands.literal("place")
                                .executes(context -> placeField(context.getSource())))
                        .then(Commands.literal("bind")
                                .executes(context -> bindField(context.getSource())))
                        .then(Commands.literal("info")
                                .executes(context -> fieldInfo(context.getSource()))))
                .then(Commands.literal("hand")
                        .then(Commands.literal("test")
                                .executes(context -> handTest(context.getSource())))
                        .then(Commands.literal("reveal")
                                .executes(context -> handReveal(context.getSource())))
                        .then(Commands.literal("clear")
                                .executes(context -> handClear(context.getSource()))))
                .then(Commands.literal("case")
                        .then(Commands.literal("place")
                                .executes(context -> placeCase(context.getSource())))
                        .then(Commands.literal("give")
                                .executes(context -> giveCase(context.getSource(), null))
                                .then(Commands.argument("name", StringArgumentType.word())
                                        .executes(context -> giveCase(context.getSource(),
                                                StringArgumentType.getString(context, "name")))))
                        .then(Commands.literal("export")
                                .executes(context -> exportCase(context.getSource())))
                        .then(Commands.literal("load")
                                .then(Commands.argument("name", StringArgumentType.word())
                                        .executes(context -> loadCase(context.getSource(),
                                                StringArgumentType.getString(context, "name")))))
                        .then(Commands.literal("info")
                                .executes(context -> caseInfo(context.getSource())))));
    }

    private int start(CommandSourceStack source) {
        ServerPlayer player = source.getEntity() instanceof ServerPlayer serverPlayer
                ? serverPlayer : null;
        String deck0 = "starter.ydk";
        String label = "starter.ydk（内置测试卡组）";
        if (player != null) {
            DuelFieldEntity linkedField = nearest(player, DuelFieldEntity.class, 32.0);
            // Prefer the deck physically placed on the nearby field.  The
            // card case may have been returned to the inventory after setup,
            // and using it here would silently start a different deck.
            DeckData fieldDeck = linkedField == null ? null : sessions.boundDeck(linkedField, "p1");
            if (fieldDeck != null) {
                List<String> problems = DeckRules.problems(fieldDeck);
                if (!problems.isEmpty()) {
                    source.sendFailure(Component.literal(
                            "决斗盘上的卡组「" + fieldDeck.name() + "」还不能用于决斗："
                                    + String.join("；", problems)));
                    return 0;
                }
                try {
                    Path exported = CardCaseItem.exportYdk(fieldDeck, deckDir());
                    deck0 = exported.toAbsolutePath().toString();
                    label = fieldDeck.name() + "（决斗盘近半场：主 " + fieldDeck.mainDeck().size()
                            + " / 额外 " + fieldDeck.extraDeck().size() + " / 副 "
                            + fieldDeck.sideDeck().size() + "）";
                } catch (Exception ex) {
                    source.sendFailure(Component.literal("卡组导出失败：" + ex.getMessage()));
                    return 0;
                }
            }
            ItemStack stack = CardCaseItem.findCase(player);
            boolean deckReady = false;
            if (fieldDeck != null) {
                deckReady = true;
            } else if (stack != null) {
                DeckData deck = CardCaseItem.getDeck(stack);
                List<String> problems = DeckRules.problems(deck);
                if (!problems.isEmpty()) {
                    source.sendFailure(Component.literal(
                            "卡盒「" + deck.name() + "」还不能用于决斗：" + String.join("；", problems)));
                    return 0;
                }
                try {
                    Path exported = CardCaseItem.exportYdk(deck, deckDir());
                    deck0 = exported.toAbsolutePath().toString();
                    label = deck.name() + "（主 " + deck.mainDeck().size() + " / 额外 "
                            + deck.extraDeck().size() + " / 副 " + deck.sideDeck().size() + "）";
                    deckReady = true;
                } catch (Exception ex) {
                    source.sendFailure(Component.literal("卡组导出失败：" + ex.getMessage()));
                    return 0;
                }
            } else if (sessions.pendingDeck() != null) {
                DeckData pending = sessions.pendingDeck();
                List<String> problems = DeckRules.problems(pending);
                if (!problems.isEmpty()) {
                    source.sendFailure(Component.literal(
                            "放在决斗盘上的卡组「" + pending.name() + "」还不能用于决斗："
                                    + String.join("；", problems)));
                    return 0;
                }
                try {
                    Path exported = CardCaseItem.exportYdk(pending, deckDir());
                    deck0 = exported.toAbsolutePath().toString();
                    label = pending.name() + "（放在决斗盘上的卡组：主 " + pending.mainDeck().size()
                            + " / 额外 " + pending.extraDeck().size() + "）";
                    deckReady = true;
                } catch (Exception ex) {
                    source.sendFailure(Component.literal("卡组导出失败：" + ex.getMessage()));
                    return 0;
                }
            }
            if (!deckReady) {
                source.sendFailure(Component.literal(
                        "没有可用的卡组：背包里没有卡盒，也没有用卡盒右键放下的卡组"));
                return 0;
            }
        }
        try {
            String deckName = deck0;
            String summary = label;
            sessions.start(source.getServer(), deckName, "starter.ydk");
            String fieldNote = "";
            if (player != null) {
                DuelFieldEntity field = nearest(player, DuelFieldEntity.class, 32.0);
                if (field != null) {
                    sessions.linkField(source.getServer(), field, player);
                    field.setGameStarted(true);
                    fieldNote = "；已绑定附近的决斗盘，抽卡/墓地/除外会实时更新";
                }
            }
            String note = fieldNote;
            source.sendSuccess(() -> Component.literal("决斗开始，使用卡组：" + summary + note), true);
            return 1;
        } catch (Exception ex) {
            source.sendFailure(Component.literal("MDProMod duel start failed: " + ex.getMessage()));
            return 0;
        }
    }

    private int animationDemo(CommandSourceStack source) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        DuelFieldEntity field = nearest(player, DuelFieldEntity.class, 48);
        if (field == null) {
            source.sendFailure(Component.literal("先放置决斗盘，再运行动画演示")); return 0;
        }
        try {
            sessions.animationDemo(player, field);
            source.sendSuccess(() -> Component.literal("开始动画演示：抽卡、检索展示、召唤、盖放、攻击、破坏、素材、效果与回收；/mdpromod anim stop 可清除"), false);
            return 1;
        } catch (Exception ex) {
            source.sendFailure(Component.literal(ex.getMessage())); return 0;
        }
    }

    private int smoke(CommandSourceStack source) {
        try {
            sessions.start(source.getServer(), "starter.ydk", "starter.ydk", true);
            source.sendSuccess(() -> Component.literal(
                    "MDProMod auto smoke duel started; use /mdpromod duel status to inspect it"), true);
            return 1;
        } catch (Exception ex) {
            source.sendFailure(Component.literal("MDProMod duel smoke failed: " + ex.getMessage()));
            return 0;
        }
    }

    private int stop(CommandSourceStack source) {
        sessions.stop();
        source.sendSuccess(() -> Component.literal("MDProMod duel stopped"), true);
        return 1;
    }

    private int prompt(CommandSourceStack source) {
        String summary = sessions.promptSummary();
        String responseKind = sessions.promptResponseKind();
        source.sendSuccess(() -> Component.literal(
                "MDProMod prompt: " + summary + " [responseKind=" + responseKind + "]"), false);
        return 1;
    }

    private int respond(CommandSourceStack source, int value) {
        try {
            sessions.respond(value);
            source.sendSuccess(() -> Component.literal("MDProMod integer response sent: " + value), true);
            return 1;
        } catch (Exception ex) {
            source.sendFailure(Component.literal("MDProMod response failed: " + ex.getMessage()));
            return 0;
        }
    }

    private int respondBytes(CommandSourceStack source, String hex) {
        if (hex == null || !hex.matches("[0-9a-fA-F]+") || (hex.length() & 1) != 0) {
            source.sendFailure(Component.literal("MDProMod response bytes must be an even-length hex string"));
            return 0;
        }
        try {
            sessions.respondBytes(hex);
            source.sendSuccess(() -> Component.literal("MDProMod byte response sent: " + hex), true);
            return 1;
        } catch (Exception ex) {
            source.sendFailure(Component.literal("MDProMod byte response failed: " + ex.getMessage()));
            return 0;
        }
    }

    private int status(CommandSourceStack source) {
        String summary = sessions.statusSummary();
        source.sendSuccess(() -> Component.literal("MDProMod duel: " + summary), false);
        return sessions.isRunning() ? 1 : 0;
    }

    private int stats(CommandSourceStack source) {
        source.sendSuccess(() -> Component.literal("MDProMod 对局统计：" + sessions.statsSummary()), false);
        return sessions.isRunning() ? 1 : 0;
    }

    private int handTest(CommandSourceStack source) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        DuelFieldEntity field = nearest(player, DuelFieldEntity.class, 48.0);
        if (field == null) {
            source.sendFailure(Component.literal(
                    "附近 48 格内没有决斗盘；先放一个决斗盘、空手右键绑定，再执行这条命令"));
            return 0;
        }
        int[] test = {89631139, 14558127, 83764718, 29401950, 55144522, 32807846};
        sessions.linkField(source.getServer(), field, player);
        sessions.previewHand(player, test);
        sessions.sendHandSnapshot(player, test, true, 0);
        source.sendSuccess(() -> Component.literal(
                "已生成 " + test.length + " 张手牌实体（己方一侧）+ 屏幕 HUD；左键选中、右键看大图"), true);
        return 1;
    }

    private int handClear(CommandSourceStack source) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        sessions.clearPreviewHand();
        sessions.sendHandSnapshot(player, new int[0], false, 0);
        source.sendSuccess(() -> Component.literal("已清空手牌（HUD 与世界实体）"), false);
        return 1;
    }

    /** 调试：把对手侧的手牌实体全部亮出 / 收回。 */
    private int handReveal(CommandSourceStack source) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        java.util.List<HandCardEntity> cards = player.serverLevel().getEntitiesOfClass(
                HandCardEntity.class, player.getBoundingBox().inflate(48.0));
        java.util.List<HandCardEntity> opponents = new java.util.ArrayList<>();
        for (HandCardEntity card : cards) {
            if (card.side() == 1) opponents.add(card);
        }
        if (opponents.isEmpty()) {
            source.sendFailure(Component.literal("附近没有对手侧的手牌实体"));
            return 0;
        }
        boolean target = !opponents.get(0).isRevealed();
        for (HandCardEntity card : opponents) {
            card.setRevealed(target);
        }
        int count = opponents.size();
        source.sendSuccess(() -> Component.literal(
                (target ? "已公开" : "已收回公开") + "对手手牌 " + count + " 张"), true);
        return 1;
    }

    private int places(CommandSourceStack source) {
        String options = sessions.placeOptions();
        source.sendSuccess(() -> Component.literal("MDProMod places: " + options), false);
        return 1;
    }

    private int place(CommandSourceStack source, int index) {
        try {
            sessions.respondPlace(index);
            source.sendSuccess(() -> Component.literal(
                    "MDProMod place response sent: " + index), true);
            return 1;
        } catch (Exception ex) {
            source.sendFailure(Component.literal(
                    "MDProMod place response failed: " + ex.getMessage()));
            return 0;
        }
    }

    private int giveCard(CommandSourceStack source, int code, int count)
            throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        if (!CardRegistry.contains(code)) {
            source.sendFailure(Component.literal("Unknown card code: " + code));
            return 0;
        }
        ItemStack stack = DuelCardItem.createStack(code, count);
        if (!player.getInventory().add(stack)) {
            player.drop(stack, false);
        }
        CardInfo info = CardRegistry.get(code).orElse(null);
        String name = info == null ? ("#" + code) : info.name();
        source.sendSuccess(() -> Component.literal("Gave " + count + " x " + name), false);
        return 1;
    }

    private int spawnCard(CommandSourceStack source, int code)
            throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        if (!CardRegistry.contains(code)) {
            source.sendFailure(Component.literal("Unknown card code: " + code));
            return 0;
        }
        DuelCardEntity card = ModEntities.DUEL_CARD.get().create(player.serverLevel());
        if (card == null) return 0;
        Vec3 position = player.position().add(player.getLookAngle().scale(2.0));
        card.setCardCode(code);
        card.setFaceUp(true);
        card.setDefense(false);
        card.moveTo(position.x, position.y, position.z, player.getYRot(), 0.0F);
        player.serverLevel().addFreshEntity(card);
        source.sendSuccess(() -> Component.literal("Spawned card #" + code), false);
        return 1;
    }

    private int placeField(CommandSourceStack source) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        DuelFieldEntity field = ModEntities.DUEL_FIELD.get().create(player.serverLevel());
        if (field == null) return 0;
        Vec3 position = player.position().add(player.getLookAngle().scale(5.0));
        field.moveTo(position.x, position.y, position.z,
                DuelFieldItem.snapYaw(player.getYRot() + 180.0F), 0.0F);
        player.serverLevel().addFreshEntity(field);
        source.sendSuccess(() -> Component.literal(
                "已放置决斗盘：16 x 14 方块，" + DuelFieldLayout.zoneCount()
                        + " 个区域（把卡盒右键到场地即可放卡组）"), false);
        return 1;
    }

    private int bindField(CommandSourceStack source) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        DuelFieldEntity field = nearest(player, DuelFieldEntity.class, 32.0);
        if (field == null) {
            source.sendFailure(Component.literal("附近没有决斗盘"));
            return 0;
        }
        ItemStack stack = CardCaseItem.findCase(player);
        if (stack == null) {
            source.sendFailure(Component.literal("背包里没有卡盒"));
            return 0;
        }
        DeckData deck = CardCaseItem.getDeck(stack);
        java.util.List<String> problems = DeckRules.problems(deck);
        if (!problems.isEmpty()) {
            source.sendFailure(Component.literal("卡组还不能放上场：" + String.join("；", problems)));
            return 0;
        }
        String side = field.playerSide(player);
        field.bindDeck(side, deck);
        sessions.setPendingDeck(field, side, deck);
        source.sendSuccess(() -> Component.literal("已把卡组「" + deck.name() + "」放到"
                + ("p1".equals(side) ? "近半场" : "远半场")
                + "：主 " + deck.mainDeck().size() + " / 额外 " + deck.extraDeck().size()), false);
        return 1;
    }

    private int placeCase(CommandSourceStack source) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        CardCaseEntity cardCase = ModEntities.CARD_CASE.get().create(player.serverLevel());
        if (cardCase == null) return 0;
        Vec3 position = player.position().add(player.getLookAngle().scale(1.5));
        cardCase.moveTo(position.x, position.y, position.z,
                player.getYRot() + 180.0F, 0.0F);
        player.serverLevel().addFreshEntity(cardCase);
        source.sendSuccess(() -> Component.literal("Placed card case"), false);
        return 1;
    }

    private int fieldInfo(CommandSourceStack source) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        DuelFieldEntity field = nearest(player, DuelFieldEntity.class, 24.0);
        if (field == null) {
            source.sendFailure(Component.literal("No duel field nearby"));
            return 0;
        }
        source.sendSuccess(() -> Component.literal(
                "决斗盘 " + field.blockPosition() + "，区域 " + DuelFieldLayout.zoneCount()
                        + "，近半场 " + (field.hasDeck("p1") ? field.deckName("p1")
                                + "（剩余 " + field.remainingCount("p1") + "/" + field.mainCount("p1")
                                + "，墓地 " + field.graveyardCount("p1")
                                + "，除外 " + field.banishedCount("p1") + "）" : "空")
                        + "，远半场 " + (field.hasDeck("p2") ? field.deckName("p2")
                                + "（剩余 " + field.remainingCount("p2") + "/" + field.mainCount("p2")
                                + "，墓地 " + field.graveyardCount("p2")
                                + "，除外 " + field.banishedCount("p2") + "）" : "空")), false);
        return 1;
    }

    private int caseInfo(CommandSourceStack source) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        ItemStack stack = CardCaseItem.findCase(player);
        if (stack != null) {
            DeckData deck = CardCaseItem.getDeck(stack);
            List<String> problems = DeckRules.problems(deck);
            source.sendSuccess(() -> Component.literal("卡盒「" + deck.name() + "」 " + deck.summary()
                    + (problems.isEmpty() ? " · 合法" : " · " + String.join("；", problems))), false);
            return 1;
        }
        CardCaseEntity cardCase = nearest(player, CardCaseEntity.class, 24.0);
        if (cardCase == null) {
            source.sendFailure(Component.literal("背包或附近都没有卡盒"));
            return 0;
        }
        source.sendSuccess(() -> Component.literal(
                "Card case: " + cardCase.getDeckData().summary()), false);
        return 1;
    }

    private int giveCase(CommandSourceStack source, String deckName)
            throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        ItemStack stack = new ItemStack(ModItems.CARD_CASE_ITEM.get());
        if (deckName != null && !deckName.isBlank()) {
            Path path = findDeck(deckName);
            if (path == null) {
                source.sendFailure(Component.literal("找不到卡组：" + deckName + " (.ydk)"));
                return 0;
            }
            try {
                CardCaseItem.setDeck(stack, DeckData.fromYdk(path));
            } catch (Exception ex) {
                source.sendFailure(Component.literal("卡组读取失败：" + ex.getMessage()));
                return 0;
            }
        } else {
            CardCaseItem.setDeck(stack, new DeckData());
        }
        if (!player.getInventory().add(stack)) {
            player.drop(stack, false);
        }
        DeckData deck = CardCaseItem.getDeck(stack);
        source.sendSuccess(() -> Component.literal("已获得卡盒：" + deck.summary()
                + "（右键打开编辑器）"), false);
        return 1;
    }

    private int exportCase(CommandSourceStack source) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        ItemStack stack = CardCaseItem.findCase(player);
        if (stack == null) {
            source.sendFailure(Component.literal("背包里没有卡盒"));
            return 0;
        }
        DeckData deck = CardCaseItem.getDeck(stack);
        try {
            Path exported = CardCaseItem.exportYdk(deck, deckDir());
            source.sendSuccess(() -> Component.literal("已导出：" + exported), false);
            return 1;
        } catch (Exception ex) {
            source.sendFailure(Component.literal("导出失败：" + ex.getMessage()));
            return 0;
        }
    }

    private int loadCase(CommandSourceStack source, String name)
            throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        CardCaseEntity cardCase = nearest(player, CardCaseEntity.class, 24.0);
        if (cardCase == null) {
            source.sendFailure(Component.literal("No card case nearby"));
            return 0;
        }
        Path path = findDeck(name);
        if (path == null) {
            source.sendFailure(Component.literal(
                    "Deck not found: " + name + " (.ydk)"));
            return 0;
        }
        try {
            DeckData data = DeckData.fromYdk(path);
            cardCase.setDeckData(data);
            source.sendSuccess(() -> Component.literal(
                    "Loaded deck: " + data.summary()), false);
            return 1;
        } catch (Exception ex) {
            source.sendFailure(Component.literal(
                    "Deck load failed: " + ex.getMessage()));
            return 0;
        }
    }

    private static Path findDeck(String name) {
        String fileName = name.endsWith(".ydk") ? name : name + ".ydk";
        // 只找玩家自己的卡组目录（config/mdpromod/decks）和当前工作目录：
        // 以前还会去翻 D:\MyCardLibrary\mdpro3\Deck，那是开发机上 MDPro3 的安装目录，
        // 模组现在要能脱离它单独运行，所以不再有这个候选。
        Path[] candidates = {
                deckDir().resolve(fileName),
                Path.of(fileName),
        };
        for (Path path : candidates) {
            if (Files.isRegularFile(path)) return path;
        }
        return null;
    }

    private static <T extends Entity> T nearest(
            ServerPlayer player, Class<T> type, double radius) {
        List<T> entities = player.serverLevel().getEntitiesOfClass(
                type, player.getBoundingBox().inflate(radius));
        T best = null;
        double bestDistance = Double.MAX_VALUE;
        for (T entity : entities) {
            double distance = entity.distanceToSqr(player);
            if (distance < bestDistance) {
                bestDistance = distance;
                best = entity;
            }
        }
        return best;
    }
}
