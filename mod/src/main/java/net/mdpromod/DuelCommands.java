package net.mdpromod;

import com.mojang.brigadier.CommandDispatcher;
import com.mojang.brigadier.arguments.IntegerArgumentType;
import com.mojang.brigadier.arguments.StringArgumentType;
import com.mojang.brigadier.builder.LiteralArgumentBuilder;
import com.mojang.brigadier.builder.RequiredArgumentBuilder;
import com.mojang.brigadier.exceptions.CommandSyntaxException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;
import net.minecraft.commands.CommandSourceStack;
import net.minecraft.commands.Commands;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.EntityType;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.level.ItemLike;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.event.RegisterCommandsEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;

final class DuelCommands {
    private final DuelSessionManager sessions;

    private static Path deckDir() {
        return ModPaths.deckDir();
    }

    DuelCommands(DuelSessionManager sessions) {
        this.sessions = sessions;
    }

    @SubscribeEvent
    public void register(RegisterCommandsEvent event) {
        CommandDispatcher<CommandSourceStack> dispatcher = event.getDispatcher();
        dispatcher.register(
            (LiteralArgumentBuilder)((LiteralArgumentBuilder)((LiteralArgumentBuilder)((LiteralArgumentBuilder)((LiteralArgumentBuilder)((LiteralArgumentBuilder)Commands.literal(
                                        "mdpromod"
                                    )
                                    .then(
                                        ((LiteralArgumentBuilder)Commands.literal("anim")
                                                .then(
                                                    Commands.literal("demo").executes(context -> this.animationDemo((CommandSourceStack)context.getSource()))
                                                ))
                                            .then(Commands.literal("stop").executes(context -> this.stop((CommandSourceStack)context.getSource())))
                                    ))
                                .then(
                                    ((LiteralArgumentBuilder)((LiteralArgumentBuilder)((LiteralArgumentBuilder)((LiteralArgumentBuilder)((LiteralArgumentBuilder)((LiteralArgumentBuilder)((LiteralArgumentBuilder)((LiteralArgumentBuilder)((LiteralArgumentBuilder)Commands.literal(
                                                                                "duel"
                                                                            )
                                                                            .then(
                                                                                Commands.literal("start")
                                                                                    .executes(context -> this.start((CommandSourceStack)context.getSource()))
                                                                            ))
                                                                        .then(
                                                                            Commands.literal("smoke")
                                                                                .executes(context -> this.smoke((CommandSourceStack)context.getSource()))
                                                                        ))
                                                                    .then(
                                                                        Commands.literal("prompt")
                                                                            .executes(context -> this.prompt((CommandSourceStack)context.getSource()))
                                                                    ))
                                                                .then(
                                                                    Commands.literal("respond")
                                                                        .then(
                                                                            Commands.argument("value", IntegerArgumentType.integer())
                                                                                .executes(
                                                                                    context -> this.respond(
                                                                                        (CommandSourceStack)context.getSource(),
                                                                                        IntegerArgumentType.getInteger(context, "value")
                                                                                    )
                                                                                )
                                                                        )
                                                                ))
                                                            .then(
                                                                Commands.literal("respondbytes")
                                                                    .then(
                                                                        Commands.argument("hex", StringArgumentType.word())
                                                                            .executes(
                                                                                context -> this.respondBytes(
                                                                                    (CommandSourceStack)context.getSource(),
                                                                                    StringArgumentType.getString(context, "hex")
                                                                                )
                                                                            )
                                                                    )
                                                            ))
                                                        .then(
                                                            Commands.literal("places")
                                                                .executes(context -> this.places((CommandSourceStack)context.getSource()))
                                                        ))
                                                    .then(
                                                        Commands.literal("place")
                                                            .then(
                                                                Commands.argument("index", IntegerArgumentType.integer(0))
                                                                    .executes(
                                                                        context -> this.place(
                                                                            (CommandSourceStack)context.getSource(),
                                                                            IntegerArgumentType.getInteger(context, "index")
                                                                        )
                                                                    )
                                                            )
                                                    ))
                                                .then(Commands.literal("stop").executes(context -> this.stop((CommandSourceStack)context.getSource()))))
                                            .then(Commands.literal("stats").executes(context -> this.stats((CommandSourceStack)context.getSource()))))
                                        .then(Commands.literal("status").executes(context -> this.status((CommandSourceStack)context.getSource())))
                                ))
                            .then(
                                ((LiteralArgumentBuilder)Commands.literal("card")
                                        .then(
                                            Commands.literal("give")
                                                .then(
                                                    ((RequiredArgumentBuilder)Commands.argument("code", IntegerArgumentType.integer(0))
                                                            .executes(
                                                                context -> this.giveCard(
                                                                    (CommandSourceStack)context.getSource(), IntegerArgumentType.getInteger(context, "code"), 1
                                                                )
                                                            ))
                                                        .then(
                                                            Commands.argument("count", IntegerArgumentType.integer(1, 64))
                                                                .executes(
                                                                    context -> this.giveCard(
                                                                        (CommandSourceStack)context.getSource(),
                                                                        IntegerArgumentType.getInteger(context, "code"),
                                                                        IntegerArgumentType.getInteger(context, "count")
                                                                    )
                                                                )
                                                        )
                                                )
                                        ))
                                    .then(
                                        Commands.literal("spawn")
                                            .then(
                                                Commands.argument("code", IntegerArgumentType.integer(0))
                                                    .executes(
                                                        context -> this.spawnCard(
                                                            (CommandSourceStack)context.getSource(), IntegerArgumentType.getInteger(context, "code")
                                                        )
                                                    )
                                            )
                                    )
                            ))
                        .then(
                            ((LiteralArgumentBuilder)((LiteralArgumentBuilder)Commands.literal("field")
                                        .then(Commands.literal("place").executes(context -> this.placeField((CommandSourceStack)context.getSource()))))
                                    .then(Commands.literal("bind").executes(context -> this.bindField((CommandSourceStack)context.getSource()))))
                                .then(Commands.literal("info").executes(context -> this.fieldInfo((CommandSourceStack)context.getSource())))
                        ))
                    .then(
                        ((LiteralArgumentBuilder)((LiteralArgumentBuilder)Commands.literal("hand")
                                    .then(Commands.literal("test").executes(context -> this.handTest((CommandSourceStack)context.getSource()))))
                                .then(Commands.literal("reveal").executes(context -> this.handReveal((CommandSourceStack)context.getSource()))))
                            .then(Commands.literal("clear").executes(context -> this.handClear((CommandSourceStack)context.getSource())))
                    ))
                .then(
                    ((LiteralArgumentBuilder)((LiteralArgumentBuilder)((LiteralArgumentBuilder)((LiteralArgumentBuilder)Commands.literal("case")
                                        .then(Commands.literal("place").executes(context -> this.placeCase((CommandSourceStack)context.getSource()))))
                                    .then(
                                        ((LiteralArgumentBuilder)Commands.literal("give")
                                                .executes(context -> this.giveCase((CommandSourceStack)context.getSource(), null)))
                                            .then(
                                                Commands.argument("name", StringArgumentType.word())
                                                    .executes(
                                                        context -> this.giveCase(
                                                            (CommandSourceStack)context.getSource(), StringArgumentType.getString(context, "name")
                                                        )
                                                    )
                                            )
                                    ))
                                .then(Commands.literal("export").executes(context -> this.exportCase((CommandSourceStack)context.getSource()))))
                            .then(
                                Commands.literal("load")
                                    .then(
                                        Commands.argument("name", StringArgumentType.word())
                                            .executes(
                                                context -> this.loadCase((CommandSourceStack)context.getSource(), StringArgumentType.getString(context, "name"))
                                            )
                                    )
                            ))
                        .then(Commands.literal("info").executes(context -> this.caseInfo((CommandSourceStack)context.getSource())))
                )
        );
    }

    private int start(CommandSourceStack source) {
        ServerPlayer player = source.getEntity() instanceof ServerPlayer serverPlayer ? serverPlayer : null;
        String deck0 = "starter.ydk";
        String label = "starter.ydk（内置测试卡组）";
        if (player != null) {
            DuelFieldEntity linkedField = nearest(player, DuelFieldEntity.class, 32.0);
            DeckData fieldDeck = linkedField == null ? null : this.sessions.boundDeck(linkedField, "p1");
            if (fieldDeck != null) {
                List<String> problems = DeckRules.problems(fieldDeck);
                if (!problems.isEmpty()) {
                    source.sendFailure(Component.literal("决斗盘上的卡组「" + fieldDeck.name() + "」还不能用于决斗：" + String.join("；", problems)));
                    return 0;
                }

                try {
                    Path exported = CardCaseItem.exportYdk(fieldDeck, deckDir());
                    deck0 = exported.toAbsolutePath().toString();
                    label = fieldDeck.name()
                        + "（决斗盘近半场：主 "
                        + fieldDeck.mainDeck().size()
                        + " / 额外 "
                        + fieldDeck.extraDeck().size()
                        + " / 副 "
                        + fieldDeck.sideDeck().size()
                        + "）";
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
                    source.sendFailure(Component.literal("卡盒「" + deck.name() + "」还不能用于决斗：" + String.join("；", problems)));
                    return 0;
                }

                try {
                    Path exported = CardCaseItem.exportYdk(deck, deckDir());
                    deck0 = exported.toAbsolutePath().toString();
                    label = deck.name() + "（主 " + deck.mainDeck().size() + " / 额外 " + deck.extraDeck().size() + " / 副 " + deck.sideDeck().size() + "）";
                    deckReady = true;
                } catch (Exception ex) {
                    source.sendFailure(Component.literal("卡组导出失败：" + ex.getMessage()));
                    return 0;
                }
            } else if (this.sessions.pendingDeck() != null) {
                DeckData pending = this.sessions.pendingDeck();
                List<String> problems = DeckRules.problems(pending);
                if (!problems.isEmpty()) {
                    source.sendFailure(Component.literal("放在决斗盘上的卡组「" + pending.name() + "」还不能用于决斗：" + String.join("；", problems)));
                    return 0;
                }

                try {
                    Path exported = CardCaseItem.exportYdk(pending, deckDir());
                    deck0 = exported.toAbsolutePath().toString();
                    label = pending.name() + "（放在决斗盘上的卡组：主 " + pending.mainDeck().size() + " / 额外 " + pending.extraDeck().size() + "）";
                    deckReady = true;
                } catch (Exception ex) {
                    source.sendFailure(Component.literal("卡组导出失败：" + ex.getMessage()));
                    return 0;
                }
            }

            if (!deckReady) {
                source.sendFailure(Component.literal("没有可用的卡组：背包里没有卡盒，也没有用卡盒右键放下的卡组"));
                return 0;
            }
        }

        try {
            String deckName = deck0;
            String summary = label;
            this.sessions.start(source.getServer(), deckName, "starter.ydk");
            String fieldNote = "";
            if (player != null) {
                DuelFieldEntity field = nearest(player, DuelFieldEntity.class, 32.0);
                if (field != null) {
                    this.sessions.linkField(source.getServer(), field, player);
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
        DuelFieldEntity field = nearest(player, DuelFieldEntity.class, 48.0);
        if (field == null) {
            source.sendFailure(Component.literal("先放置决斗盘，再运行动画演示"));
            return 0;
        }

        try {
            this.sessions.animationDemo(player, field);
            source.sendSuccess(() -> Component.literal("开始动画演示：抽卡、检索展示、召唤、盖放、攻击、破坏、素材、效果与回收；/mdpromod anim stop 可清除"), false);
            return 1;
        } catch (Exception ex) {
            source.sendFailure(Component.literal(ex.getMessage()));
            return 0;
        }
    }

    private int smoke(CommandSourceStack source) {
        try {
            this.sessions.start(source.getServer(), "starter.ydk", "starter.ydk", true);
            source.sendSuccess(() -> Component.literal("MDProMod auto smoke duel started; use /mdpromod duel status to inspect it"), true);
            return 1;
        } catch (Exception ex) {
            source.sendFailure(Component.literal("MDProMod duel smoke failed: " + ex.getMessage()));
            return 0;
        }
    }

    private int stop(CommandSourceStack source) {
        this.sessions.stop();
        source.sendSuccess(() -> Component.literal("MDProMod duel stopped"), true);
        return 1;
    }

    private int prompt(CommandSourceStack source) {
        String summary = this.sessions.promptSummary();
        String responseKind = this.sessions.promptResponseKind();
        source.sendSuccess(() -> Component.literal("MDProMod prompt: " + summary + " [responseKind=" + responseKind + "]"), false);
        return 1;
    }

    private int respond(CommandSourceStack source, int value) {
        try {
            this.sessions.respond(value);
            source.sendSuccess(() -> Component.literal("MDProMod integer response sent: " + value), true);
            return 1;
        } catch (Exception ex) {
            source.sendFailure(Component.literal("MDProMod response failed: " + ex.getMessage()));
            return 0;
        }
    }

    private int respondBytes(CommandSourceStack source, String hex) {
        if (hex != null && hex.matches("[0-9a-fA-F]+") && (hex.length() & 1) == 0) {
            try {
                this.sessions.respondBytes(hex);
                source.sendSuccess(() -> Component.literal("MDProMod byte response sent: " + hex), true);
                return 1;
            } catch (Exception ex) {
                source.sendFailure(Component.literal("MDProMod byte response failed: " + ex.getMessage()));
                return 0;
            }
        } else {
            source.sendFailure(Component.literal("MDProMod response bytes must be an even-length hex string"));
            return 0;
        }
    }

    private int status(CommandSourceStack source) {
        String summary = this.sessions.statusSummary();
        source.sendSuccess(() -> Component.literal("MDProMod duel: " + summary), false);
        return this.sessions.isRunning() ? 1 : 0;
    }

    private int stats(CommandSourceStack source) {
        source.sendSuccess(() -> Component.literal("MDProMod 对局统计：" + this.sessions.statsSummary()), false);
        return this.sessions.isRunning() ? 1 : 0;
    }

    private int handTest(CommandSourceStack source) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        DuelFieldEntity field = nearest(player, DuelFieldEntity.class, 48.0);
        if (field == null) {
            source.sendFailure(Component.literal("附近 48 格内没有决斗盘；先放一个决斗盘、空手右键绑定，再执行这条命令"));
            return 0;
        } else {
            int[] test = new int[]{89631139, 14558127, 83764718, 29401950, 55144522, 32807846};
            this.sessions.linkField(source.getServer(), field, player);
            this.sessions.previewHand(player, test);
            this.sessions.sendHandSnapshot(player, test, true, 0);
            source.sendSuccess(() -> Component.literal("已生成 " + test.length + " 张手牌实体（己方一侧）+ 屏幕 HUD；左键选中、右键看大图"), true);
            return 1;
        }
    }

    private int handClear(CommandSourceStack source) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        this.sessions.clearPreviewHand();
        this.sessions.sendHandSnapshot(player, new int[0], false, 0);
        source.sendSuccess(() -> Component.literal("已清空手牌（HUD 与世界实体）"), false);
        return 1;
    }

    private int handReveal(CommandSourceStack source) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        List<HandCardEntity> cards = player.serverLevel().getEntitiesOfClass(HandCardEntity.class, player.getBoundingBox().inflate(48.0));
        List<HandCardEntity> opponents = new ArrayList<>();

        for (HandCardEntity card : cards) {
            if (card.side() == 1) {
                opponents.add(card);
            }
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
        source.sendSuccess(() -> Component.literal((target ? "已公开" : "已收回公开") + "对手手牌 " + count + " 张"), true);
        return 1;
    }

    private int places(CommandSourceStack source) {
        String options = this.sessions.placeOptions();
        source.sendSuccess(() -> Component.literal("MDProMod places: " + options), false);
        return 1;
    }

    private int place(CommandSourceStack source, int index) {
        try {
            this.sessions.respondPlace(index);
            source.sendSuccess(() -> Component.literal("MDProMod place response sent: " + index), true);
            return 1;
        } catch (Exception ex) {
            source.sendFailure(Component.literal("MDProMod place response failed: " + ex.getMessage()));
            return 0;
        }
    }

    private int giveCard(CommandSourceStack source, int code, int count) throws CommandSyntaxException {
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
        String name = info == null ? "#" + code : info.name();
        source.sendSuccess(() -> Component.literal("Gave " + count + " x " + name), false);
        return 1;
    }

    private int spawnCard(CommandSourceStack source, int code) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        if (!CardRegistry.contains(code)) {
            source.sendFailure(Component.literal("Unknown card code: " + code));
            return 0;
        }

        DuelCardEntity card = (DuelCardEntity)((EntityType)ModEntities.DUEL_CARD.get()).create(player.serverLevel());
        if (card == null) {
            return 0;
        }

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
        DuelFieldEntity field = (DuelFieldEntity)((EntityType)ModEntities.DUEL_FIELD.get()).create(player.serverLevel());
        if (field == null) {
            return 0;
        }

        Vec3 position = player.position().add(player.getLookAngle().scale(5.0));
        field.moveTo(position.x, position.y, position.z, DuelFieldItem.snapYaw(player.getYRot() + 180.0F), 0.0F);
        player.serverLevel().addFreshEntity(field);
        source.sendSuccess(() -> Component.literal("已放置决斗盘：16 x 14 方块，" + DuelFieldLayout.zoneCount() + " 个区域（把卡盒右键到场地即可放卡组）"), false);
        return 1;
    }

    private int bindField(CommandSourceStack source) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        DuelFieldEntity field = nearest(player, DuelFieldEntity.class, 32.0);
        if (field == null) {
            source.sendFailure(Component.literal("附近没有决斗盘"));
            return 0;
        } else {
            ItemStack stack = CardCaseItem.findCase(player);
            if (stack == null) {
                source.sendFailure(Component.literal("背包里没有卡盒"));
                return 0;
            } else {
                DeckData deck = CardCaseItem.getDeck(stack);
                List<String> problems = DeckRules.problems(deck);
                if (!problems.isEmpty()) {
                    source.sendFailure(Component.literal("卡组还不能放上场：" + String.join("；", problems)));
                    return 0;
                } else {
                    String side = field.playerSide(player);
                    field.bindDeck(side, deck);
                    this.sessions.setPendingDeck(field, side, deck);
                    source.sendSuccess(
                        () -> Component.literal(
                            "已把卡组「"
                                + deck.name()
                                + "」放到"
                                + ("p1".equals(side) ? "近半场" : "远半场")
                                + "：主 "
                                + deck.mainDeck().size()
                                + " / 额外 "
                                + deck.extraDeck().size()
                        ),
                        false
                    );
                    return 1;
                }
            }
        }
    }

    private int placeCase(CommandSourceStack source) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        CardCaseEntity cardCase = (CardCaseEntity)((EntityType)ModEntities.CARD_CASE.get()).create(player.serverLevel());
        if (cardCase == null) {
            return 0;
        }

        Vec3 position = player.position().add(player.getLookAngle().scale(1.5));
        cardCase.moveTo(position.x, position.y, position.z, player.getYRot() + 180.0F, 0.0F);
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
        } else {
            source.sendSuccess(
                () -> Component.literal(
                    "决斗盘 "
                        + field.blockPosition()
                        + "，区域 "
                        + DuelFieldLayout.zoneCount()
                        + "，近半场 "
                        + (
                            field.hasDeck("p1")
                                ? field.deckName("p1")
                                    + "（剩余 "
                                    + field.remainingCount("p1")
                                    + "/"
                                    + field.mainCount("p1")
                                    + "，墓地 "
                                    + field.graveyardCount("p1")
                                    + "，除外 "
                                    + field.banishedCount("p1")
                                    + "）"
                                : "空"
                        )
                        + "，远半场 "
                        + (
                            field.hasDeck("p2")
                                ? field.deckName("p2")
                                    + "（剩余 "
                                    + field.remainingCount("p2")
                                    + "/"
                                    + field.mainCount("p2")
                                    + "，墓地 "
                                    + field.graveyardCount("p2")
                                    + "，除外 "
                                    + field.banishedCount("p2")
                                    + "）"
                                : "空"
                        )
                ),
                false
            );
            return 1;
        }
    }

    private int caseInfo(CommandSourceStack source) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        ItemStack stack = CardCaseItem.findCase(player);
        if (stack != null) {
            DeckData deck = CardCaseItem.getDeck(stack);
            List<String> problems = DeckRules.problems(deck);
            source.sendSuccess(
                () -> Component.literal("卡盒「" + deck.name() + "」 " + deck.summary() + (problems.isEmpty() ? " · 合法" : " · " + String.join("；", problems))),
                false
            );
            return 1;
        } else {
            CardCaseEntity cardCase = nearest(player, CardCaseEntity.class, 24.0);
            if (cardCase == null) {
                source.sendFailure(Component.literal("背包或附近都没有卡盒"));
                return 0;
            } else {
                source.sendSuccess(() -> Component.literal("Card case: " + cardCase.getDeckData().summary()), false);
                return 1;
            }
        }
    }

    private int giveCase(CommandSourceStack source, String deckName) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        ItemStack stack = new ItemStack((ItemLike)ModItems.CARD_CASE_ITEM.get());
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
        source.sendSuccess(() -> Component.literal("已获得卡盒：" + deck.summary() + "（右键打开编辑器）"), false);
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

    private int loadCase(CommandSourceStack source, String name) throws CommandSyntaxException {
        ServerPlayer player = source.getPlayerOrException();
        CardCaseEntity cardCase = nearest(player, CardCaseEntity.class, 24.0);
        if (cardCase == null) {
            source.sendFailure(Component.literal("No card case nearby"));
            return 0;
        }

        Path path = findDeck(name);
        if (path == null) {
            source.sendFailure(Component.literal("Deck not found: " + name + " (.ydk)"));
            return 0;
        }

        try {
            DeckData data = DeckData.fromYdk(path);
            cardCase.setDeckData(data);
            source.sendSuccess(() -> Component.literal("Loaded deck: " + data.summary()), false);
            return 1;
        } catch (Exception ex) {
            source.sendFailure(Component.literal("Deck load failed: " + ex.getMessage()));
            return 0;
        }
    }

    private static Path findDeck(String name) {
        String fileName = name.endsWith(".ydk") ? name : name + ".ydk";
        Path[] candidates = new Path[]{deckDir().resolve(fileName), Path.of(fileName)};

        for (Path path : candidates) {
            if (Files.isRegularFile(path)) {
                return path;
            }
        }

        return null;
    }

    private static <T extends Entity> T nearest(ServerPlayer player, Class<T> type, double radius) {
        List<T> entities = player.serverLevel().getEntitiesOfClass(type, player.getBoundingBox().inflate(radius));
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
