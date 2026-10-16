package net.mdpromod;

import net.minecraft.network.FriendlyByteBuf;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.item.ItemStack;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.fml.DistExecutor;
import net.minecraftforge.network.NetworkDirection;
import net.minecraftforge.network.NetworkEvent;
import net.minecraftforge.network.NetworkRegistry;
import net.minecraftforge.network.PacketDistributor;
import net.minecraftforge.network.simple.SimpleChannel;

import java.util.function.Supplier;

public final class ModNetwork {
    private static final String PROTOCOL = "3";

    public static final SimpleChannel CHANNEL = NetworkRegistry.ChannelBuilder
            .named(ResourceLocation.fromNamespaceAndPath(MDProMod.MODID, "main"))
            .networkProtocolVersion(() -> PROTOCOL)
            .clientAcceptedVersions(PROTOCOL::equals)
            .serverAcceptedVersions(PROTOCOL::equals)
            .simpleChannel();

    private ModNetwork() {
    }

    public static void register() {
        int id = 0;
        CHANNEL.messageBuilder(CardCaseAction.class, id++, NetworkDirection.PLAY_TO_SERVER)
                .encoder(CardCaseAction::encode)
                .decoder(CardCaseAction::decode)
                .consumerMainThread(CardCaseAction::handle)
                .add();
        CHANNEL.messageBuilder(CardCaseSync.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(CardCaseSync::encode)
                .decoder(CardCaseSync::decode)
                .consumerMainThread(CardCaseSync::handle)
                .add();
        CHANNEL.messageBuilder(CardCaseOpen.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(CardCaseOpen::encode)
                .decoder(CardCaseOpen::decode)
                .consumerMainThread(CardCaseOpen::handle)
                .add();
        CHANNEL.messageBuilder(HandSync.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(HandSync::encode)
                .decoder(HandSync::decode)
                .consumerMainThread(HandSync::handle)
                .add();
        CHANNEL.messageBuilder(OpenCard.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(OpenCard::encode)
                .decoder(OpenCard::decode)
                .consumerMainThread(OpenCard::handle)
                .add();
        CHANNEL.messageBuilder(Anim.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(Anim::encode)
                .decoder(Anim::decode)
                .consumerMainThread(Anim::handle)
                .add();
        CHANNEL.messageBuilder(DuelAnimation.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(DuelAnimation::encode).decoder(DuelAnimation::decode)
                .consumerMainThread(DuelAnimation::handle).add();
        CHANNEL.messageBuilder(DuelAnimationClear.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(DuelAnimationClear::encode).decoder(DuelAnimationClear::decode)
                .consumerMainThread(DuelAnimationClear::handle).add();
        CHANNEL.messageBuilder(CardKnowledge.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(CardKnowledge::encode).decoder(CardKnowledge::decode)
                .consumerMainThread(CardKnowledge::handle).add();
        CHANNEL.messageBuilder(CardActions.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(CardActions::encode).decoder(CardActions::decode)
                .consumerMainThread(CardActions::handle).add();
        CHANNEL.messageBuilder(CardAct.class, id++, NetworkDirection.PLAY_TO_SERVER)
                .encoder(CardAct::encode).decoder(CardAct::decode)
                .consumerMainThread(CardAct::handle).add();
        CHANNEL.messageBuilder(PlaceHint.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(PlaceHint::encode).decoder(PlaceHint::decode)
                .consumerMainThread(PlaceHint::handle).add();
        CHANNEL.messageBuilder(SelectHint.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(SelectHint::encode).decoder(SelectHint::decode)
                .consumerMainThread(SelectHint::handle).add();
        CHANNEL.messageBuilder(PileHint.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(PileHint::encode).decoder(PileHint::decode)
                .consumerMainThread(PileHint::handle).add();
        CHANNEL.messageBuilder(SummonEffect.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(SummonEffect::encode).decoder(SummonEffect::decode)
                .consumerMainThread(SummonEffect::handle).add();
        CHANNEL.messageBuilder(PileView.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(PileView::encode).decoder(PileView::decode)
                .consumerMainThread(PileView::handle).add();
        CHANNEL.messageBuilder(PileAct.class, id++, NetworkDirection.PLAY_TO_SERVER)
                .encoder(PileAct::encode).decoder(PileAct::decode)
                .consumerMainThread(PileAct::handle).add();
        CHANNEL.messageBuilder(DecisionPrompt.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(DecisionPrompt::encode).decoder(DecisionPrompt::decode)
                .consumerMainThread(DecisionPrompt::handle).add();
        CHANNEL.messageBuilder(DecisionAnswer.class, id++, NetworkDirection.PLAY_TO_SERVER)
                .encoder(DecisionAnswer::encode).decoder(DecisionAnswer::decode)
                .consumerMainThread(DecisionAnswer::handle).add();
        CHANNEL.messageBuilder(ChainState.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(ChainState::encode).decoder(ChainState::decode)
                .consumerMainThread(ChainState::handle).add();
        CHANNEL.messageBuilder(ChainChoice.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(ChainChoice::encode).decoder(ChainChoice::decode)
                .consumerMainThread(ChainChoice::handle).add();
        CHANNEL.messageBuilder(ChainChoose.class, id++, NetworkDirection.PLAY_TO_SERVER)
                .encoder(ChainChoose::encode).decoder(ChainChoose::decode)
                .consumerMainThread(ChainChoose::handle).add();
        CHANNEL.messageBuilder(SelectList.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(SelectList::encode).decoder(SelectList::decode)
                .consumerMainThread(SelectList::handle).add();
        CHANNEL.messageBuilder(SelectChoose.class, id++, NetworkDirection.PLAY_TO_SERVER)
                .encoder(SelectChoose::encode).decoder(SelectChoose::decode)
                .consumerMainThread(SelectChoose::handle).add();
        CHANNEL.messageBuilder(UnselectList.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(UnselectList::encode).decoder(UnselectList::decode)
                .consumerMainThread(UnselectList::handle).add();
        CHANNEL.messageBuilder(UnselectPick.class, id++, NetworkDirection.PLAY_TO_SERVER)
                .encoder(UnselectPick::encode).decoder(UnselectPick::decode)
                .consumerMainThread(UnselectPick::handle).add();
        CHANNEL.messageBuilder(PositionList.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(PositionList::encode).decoder(PositionList::decode)
                .consumerMainThread(PositionList::handle).add();
        CHANNEL.messageBuilder(PositionAnswer.class, id++, NetworkDirection.PLAY_TO_SERVER)
                .encoder(PositionAnswer::encode).decoder(PositionAnswer::decode)
                .consumerMainThread(PositionAnswer::handle).add();
        CHANNEL.messageBuilder(SortList.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(SortList::encode).decoder(SortList::decode)
                .consumerMainThread(SortList::handle).add();
        CHANNEL.messageBuilder(SortOrder.class, id++, NetworkDirection.PLAY_TO_SERVER)
                .encoder(SortOrder::encode).decoder(SortOrder::decode)
                .consumerMainThread(SortOrder::handle).add();
        CHANNEL.messageBuilder(OptionList.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(OptionList::encode).decoder(OptionList::decode)
                .consumerMainThread(OptionList::handle).add();
        CHANNEL.messageBuilder(OptionAnswer.class, id++, NetworkDirection.PLAY_TO_SERVER)
                .encoder(OptionAnswer::encode).decoder(OptionAnswer::decode)
                .consumerMainThread(OptionAnswer::handle).add();
        CHANNEL.messageBuilder(DuelSound.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(DuelSound::encode).decoder(DuelSound::decode)
                .consumerMainThread(DuelSound::handle).add();
        CHANNEL.messageBuilder(MusicSettingsOpen.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(MusicSettingsOpen::encode).decoder(MusicSettingsOpen::decode)
                .consumerMainThread(MusicSettingsOpen::handle).add();
        CHANNEL.messageBuilder(MusicSettingsAction.class, id++, NetworkDirection.PLAY_TO_SERVER)
                .encoder(MusicSettingsAction::encode).decoder(MusicSettingsAction::decode)
                .consumerMainThread(MusicSettingsAction::handle).add();
        CHANNEL.messageBuilder(AiDeckOpen.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(AiDeckOpen::encode).decoder(AiDeckOpen::decode)
                .consumerMainThread(AiDeckOpen::handle).add();
        CHANNEL.messageBuilder(AiDeckSelect.class, id++, NetworkDirection.PLAY_TO_SERVER)
                .encoder(AiDeckSelect::encode).decoder(AiDeckSelect::decode)
                .consumerMainThread(AiDeckSelect::handle).add();
        CHANNEL.messageBuilder(CardMarks.class, id++, NetworkDirection.PLAY_TO_CLIENT)
                .encoder(CardMarks::encode).decoder(CardMarks::decode)
                .consumerMainThread(CardMarks::handle).add();
        CHANNEL.messageBuilder(FieldAction.class, id++, NetworkDirection.PLAY_TO_SERVER)
                .encoder(FieldAction::encode).decoder(FieldAction::decode)
                .consumerMainThread(FieldAction::handle).add();
        CHANNEL.messageBuilder(FieldClick.class, id++, NetworkDirection.PLAY_TO_SERVER)
                .encoder(FieldClick::encode).decoder(FieldClick::decode)
                .consumerMainThread(FieldClick::handle).add();
    }

    public static void sendToServer(Object message) {
        CHANNEL.sendToServer(message);
    }

    public static void sendTo(ServerPlayer player, Object message) {
        CHANNEL.send(PacketDistributor.PLAYER.with(() -> player), message);
    }

    /** 发给附近所有玩家（决斗盘周围的观众也能看到演出）。 */
    public static void sendNear(net.minecraft.world.level.Level level, net.minecraft.world.phys.Vec3 at,
                                double radius, Object message) {
        CHANNEL.send(PacketDistributor.NEAR.with(() ->
                new PacketDistributor.TargetPoint(at.x, at.y, at.z, radius, level.dimension())), message);
    }

    /** 客户端 -> 服务端：编辑动作。action 0 加卡 / 1 减卡 / 2 改名 / 3 请求同步。 */
    public record CardCaseAction(int action, int zone, int code, String name) {
        static void encode(CardCaseAction message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.action);
            buffer.writeVarInt(message.zone);
            buffer.writeVarInt(message.code);
            buffer.writeUtf(message.name == null ? "" : message.name, 64);
        }

        static CardCaseAction decode(FriendlyByteBuf buffer) {
            return new CardCaseAction(buffer.readVarInt(), buffer.readVarInt(),
                    buffer.readVarInt(), buffer.readUtf(64));
        }

        static void handle(CardCaseAction message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ServerPlayer player = ctx.getSender();
            ctx.enqueueWork(() -> {
                if (player == null) return;
                ItemStack stack = CardCaseItem.findCase(player);
                if (stack == null) return;
                DeckData deck = CardCaseItem.getDeck(stack);
                int zone = Math.max(0, Math.min(2, message.zone));
                switch (message.action) {
                    case 0 -> {
                        var list = switch (zone) {
                            case 1 -> deck.extraDeck();
                            case 2 -> deck.sideDeck();
                            default -> deck.mainDeck();
                        };
                        list.add(message.code);
                    }
                    case 1 -> {
                        var list = switch (zone) {
                            case 1 -> deck.extraDeck();
                            case 2 -> deck.sideDeck();
                            default -> deck.mainDeck();
                        };
                        list.remove(Integer.valueOf(message.code));
                    }
                    case 2 -> deck.setName(message.name);
                    case 4 -> {
                        try {
                            java.nio.file.Path path = DeckYdk.write(deck, ModPaths.deckDir());
                            player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                                    "已导出卡组：" + path), false);
                        } catch (Exception ex) {
                            player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                                    "导出失败：" + ex.getMessage()), false);
                        }
                    }
                    default -> { }
                }
                CardCaseItem.setDeck(stack, deck);
                player.inventoryMenu.broadcastChanges();
                sendTo(player, CardCaseSync.of(deck));
            });
            ctx.setPacketHandled(true);
        }
    }

    /** 服务端 -> 客户端：整份卡组快照。 */
    public record CardCaseSync(String name, int[] main, int[] extra, int[] side) {
        public static CardCaseSync of(DeckData deck) {
            return new CardCaseSync(deck.name(), deck.mainArray(), deck.extraArray(), deck.sideArray());
        }

        public DeckData toDeck() {
            return DeckData.of(name, main, extra, side);
        }

        static void encode(CardCaseSync message, FriendlyByteBuf buffer) {
            buffer.writeUtf(message.name == null ? "" : message.name, 64);
            buffer.writeVarIntArray(message.main);
            buffer.writeVarIntArray(message.extra);
            buffer.writeVarIntArray(message.side);
        }

        static CardCaseSync decode(FriendlyByteBuf buffer) {
            String name = buffer.readUtf(64);
            int[] main = buffer.readVarIntArray();
            int[] extra = buffer.readVarIntArray();
            int[] side = buffer.readVarIntArray();
            return new CardCaseSync(name, main, extra, side);
        }

        static void handle(CardCaseSync message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientCardCaseBridge.applySync(message)));
            ctx.setPacketHandled(true);
        }
    }

    /** 服务端 -> 客户端：打开卡组编辑器（携带当前卡组）。 */
    public record CardCaseOpen(String name, int[] main, int[] extra, int[] side) {
        public static CardCaseOpen of(DeckData deck) {
            return new CardCaseOpen(deck.name(), deck.mainArray(), deck.extraArray(), deck.sideArray());
        }

        static void encode(CardCaseOpen message, FriendlyByteBuf buffer) {
            buffer.writeUtf(message.name == null ? "" : message.name, 64);
            buffer.writeVarIntArray(message.main);
            buffer.writeVarIntArray(message.extra);
            buffer.writeVarIntArray(message.side);
        }

        static CardCaseOpen decode(FriendlyByteBuf buffer) {
            return new CardCaseOpen(buffer.readUtf(64), buffer.readVarIntArray(),
                    buffer.readVarIntArray(), buffer.readVarIntArray());
        }

        static void handle(CardCaseOpen message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientCardCaseBridge.openScreen(message)));
            ctx.setPacketHandled(true);
        }
    }

    /** 服务端 -> 客户端：手牌快照（HUD 用）。 */
    public record HandSync(int[] cards, boolean active, int turnPlayer) {
        static void encode(HandSync message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.cards);
            buffer.writeBoolean(message.active);
            buffer.writeVarInt(message.turnPlayer);
        }

        static HandSync decode(FriendlyByteBuf buffer) {
            return new HandSync(buffer.readVarIntArray(), buffer.readBoolean(), buffer.readVarInt());
        }

        static void handle(HandSync message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientHandBridge.apply(message)));
            ctx.setPacketHandled(true);
        }
    }

    /** 服务端 -> 客户端：核心给出若干选项，选一个。 */
    public record OptionList(int[] values, String[] labels) {
        static void encode(OptionList message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.values);
            buffer.writeVarInt(message.labels.length);
            for (String label : message.labels) buffer.writeUtf(label, 96);
        }

        static OptionList decode(FriendlyByteBuf buffer) {
            int[] values = buffer.readVarIntArray();
            String[] labels = new String[buffer.readVarInt()];
            for (int i = 0; i < labels.length; i++) labels[i] = buffer.readUtf(96);
            return new OptionList(values, labels);
        }

        static void handle(OptionList message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientPacketScreens.openOptions(message)));
            ctx.setPacketHandled(true);
        }
    }

    /** 客户端 -> 服务端：选中的选项值。 */
    public record OptionAnswer(int value) {
        static void encode(OptionAnswer message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.value);
        }

        static OptionAnswer decode(FriendlyByteBuf buffer) {
            return new OptionAnswer(buffer.readVarInt());
        }

        static void handle(OptionAnswer message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) MDProMod.sessions().answerOption(player, message.value());
                ctx.setPacketHandled(true);
            });
        }
    }

    /** 服务端 -> 客户端：核心要求给这几张卡排序（卡组顶/底、回收顺序等）。 */
    public record SortList(int[] codes) {
        static void encode(SortList message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.codes);
        }

        static SortList decode(FriendlyByteBuf buffer) {
            return new SortList(buffer.readVarIntArray());
        }

        static void handle(SortList message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientPacketScreens.openSortCard(message)));
            ctx.setPacketHandled(true);
        }
    }

    /** 客户端 -> 服务端：排好的顺序（值是原始下标的新排列）。 */
    public record SortOrder(int[] order) {
        static void encode(SortOrder message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.order);
        }

        static SortOrder decode(FriendlyByteBuf buffer) {
            return new SortOrder(buffer.readVarIntArray());
        }

        static void handle(SortOrder message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) MDProMod.sessions().answerSort(player, message.order());
                ctx.setPacketHandled(true);
            });
        }
    }

    /** 服务端 -> 客户端：融合素材那种"一张一张选 + 完成"的界面数据。 */
    public record UnselectList(int[] selectCodes, int[] chosenCodes, int min, int max, boolean finishable,
                               int[] selectAddrs, int[] chosenAddrs) {
        static void encode(UnselectList message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.selectCodes);
            buffer.writeVarIntArray(message.chosenCodes);
            buffer.writeVarInt(message.min);
            buffer.writeVarInt(message.max);
            buffer.writeBoolean(message.finishable);
            buffer.writeVarIntArray(message.selectAddrs);
            buffer.writeVarIntArray(message.chosenAddrs);
        }

        static UnselectList decode(FriendlyByteBuf buffer) {
            return new UnselectList(buffer.readVarIntArray(), buffer.readVarIntArray(),
                    buffer.readVarInt(), buffer.readVarInt(), buffer.readBoolean(),
                    buffer.readVarIntArray(), buffer.readVarIntArray());
        }

        static void handle(UnselectList message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientPacketScreens.openUnselectCard(message)));
            ctx.setPacketHandled(true);
        }
    }

    /** 客户端 -> 服务端：点了第 index 张（合并列表下标），或者 -1 = 完成。 */
    public record UnselectPick(int index) {
        static void encode(UnselectPick message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.index + 1);
        }

        static UnselectPick decode(FriendlyByteBuf buffer) {
            return new UnselectPick(buffer.readVarInt() - 1);
        }

        static void handle(UnselectPick message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) MDProMod.sessions().unselectPick(player, message.index());
                ctx.setPacketHandled(true);
            });
        }
    }

    /** 服务端 -> 客户端：选表示形式（攻击/守备）。 */
    public record PositionList(int[] values, String[] labels) {
        static void encode(PositionList message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.values);
            buffer.writeVarInt(message.labels.length);
            for (String label : message.labels) buffer.writeUtf(label, 32);
        }

        static PositionList decode(FriendlyByteBuf buffer) {
            int[] values = buffer.readVarIntArray();
            String[] labels = new String[buffer.readVarInt()];
            for (int i = 0; i < labels.length; i++) labels[i] = buffer.readUtf(32);
            return new PositionList(values, labels);
        }

        static void handle(PositionList message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientPacketScreens.openPosition(message)));
            ctx.setPacketHandled(true);
        }
    }

    /** 客户端 -> 服务端：选好的表示形式。 */
    public record PositionAnswer(int value) {
        static void encode(PositionAnswer message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.value);
        }

        static PositionAnswer decode(FriendlyByteBuf buffer) {
            return new PositionAnswer(buffer.readVarInt());
        }

        static void handle(PositionAnswer message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) MDProMod.sessions().answerPosition(player, message.value());
                ctx.setPacketHandled(true);
            });
        }
    }

    /** 服务端 -> 客户端：核心在等选卡，把候选列出来让玩家点（卡组/额外卡组的卡没有世界实体）。 */
    /** {@code addrs} 与 {@code codes} 一一对应：那张卡在哪（界面悬停时显示）。 */
    public record SelectList(int[] codes, int min, int max, int[] addrs) {
        static void encode(SelectList message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.codes);
            buffer.writeVarInt(message.min);
            buffer.writeVarInt(message.max);
            buffer.writeVarIntArray(message.addrs);
        }

        static SelectList decode(FriendlyByteBuf buffer) {
            return new SelectList(buffer.readVarIntArray(), buffer.readVarInt(), buffer.readVarInt(),
                    buffer.readVarIntArray());
        }

        static void handle(SelectList message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientPacketScreens.openSelectCard(message)));
            ctx.setPacketHandled(true);
        }
    }

    /** 客户端 -> 服务端：列表界面选好的下标（按候选顺序）。 */
    public record SelectChoose(int[] indices) {
        static void encode(SelectChoose message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.indices);
        }

        static SelectChoose decode(FriendlyByteBuf buffer) {
            return new SelectChoose(buffer.readVarIntArray());
        }

        static void handle(SelectChoose message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) MDProMod.sessions().answerSelect(player, message.indices());
                ctx.setPacketHandled(true);
            });
        }
    }

    /**
     * 服务端 -> 客户端：核心在问"要不要接连锁、接哪张"。
     *
     * <p>{@code timing} 是"为什么现在问你"的一句话，如「〈卡〉的效果发动了，是否连锁？」。
     * {@code opening} = 链上还什么都没有，这次问的是"要不要发动"（转阶段的诱发效果那种），
     * 窗口的措辞和按钮跟着变。{@code addrs} 是候选项的打包地址，用来给候选卡描标记。
     */
    public record ChainChoice(int[] codes, boolean forced, String timing, boolean opening, int[] addrs) {
        static void encode(ChainChoice message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.codes);
            buffer.writeBoolean(message.forced);
            buffer.writeUtf(message.timing == null ? "" : message.timing, 192);
            buffer.writeBoolean(message.opening);
            buffer.writeVarIntArray(message.addrs);
        }

        static ChainChoice decode(FriendlyByteBuf buffer) {
            return new ChainChoice(buffer.readVarIntArray(), buffer.readBoolean(), buffer.readUtf(192),
                    buffer.readBoolean(), buffer.readVarIntArray());
        }

        static void handle(ChainChoice message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientPacketScreens.openChainChoice(message)));
            ctx.setPacketHandled(true);
        }
    }

    /** 客户端 -> 服务端：选了第几张接连锁（-1 = 放弃）。 */
    public record ChainChoose(int index) {
        static void encode(ChainChoose message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.index + 1);
        }

        static ChainChoose decode(FriendlyByteBuf buffer) {
            return new ChainChoose(buffer.readVarInt() - 1);
        }

        static void handle(ChainChoose message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) MDProMod.sessions().answerChain(player, message.index());
                ctx.setPacketHandled(true);
            });
        }
    }

    /** 服务端 -> 客户端：当前连锁（对双方可见）。codes 按链号顺序，resolving = 正在解决的链号。 */
    public record ChainState(int fieldEntityId, int[] codes, int resolving) {
        static void encode(ChainState message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.fieldEntityId);
            buffer.writeVarIntArray(message.codes);
            buffer.writeVarInt(message.resolving);
        }

        static ChainState decode(FriendlyByteBuf buffer) {
            return new ChainState(buffer.readVarInt(), buffer.readVarIntArray(), buffer.readVarInt());
        }

        static void handle(ChainState message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientChainState.apply(message.fieldEntityId(),
                            message.codes(), message.resolving())));
            ctx.setPacketHandled(true);
        }
    }

    /**
     * 服务端 -> 客户端：卡面标记（对双方可见）。
     * chain/target 是世界实体 id（世界里描边框用），chainAddrs/targetAddrs 是同一批卡的
     * 打包地址（选卡界面给清单描边用）。只在集合变化时发；都空表示清掉全部标记。
     */
    public record CardMarks(int[] chain, int[] target, int[] chainAddrs, int[] targetAddrs) {
        static void encode(CardMarks message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.chain);
            buffer.writeVarIntArray(message.target);
            buffer.writeVarIntArray(message.chainAddrs);
            buffer.writeVarIntArray(message.targetAddrs);
        }

        static CardMarks decode(FriendlyByteBuf buffer) {
            return new CardMarks(buffer.readVarIntArray(), buffer.readVarIntArray(),
                    buffer.readVarIntArray(), buffer.readVarIntArray());
        }

        static void handle(CardMarks message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientCardMarkers.apply(message.chain(), message.target(),
                            message.chainAddrs(), message.targetAddrs())));
            ctx.setPacketHandled(true);
        }
    }

    /**
     * 客户端 -> 服务端：决斗盘菜单上的按钮。action 0 = 投降，1 = 换时点（value = 模式序号）。
     * 菜单本身在客户端就地开关（点确认键），不用服务端往返——省得"点了没反应"。
     */
    public record FieldAction(int action, int value) {
        static void encode(FieldAction message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.action);
            buffer.writeVarInt(message.value);
        }

        static FieldAction decode(FriendlyByteBuf buffer) {
            return new FieldAction(buffer.readVarInt(), buffer.readVarInt());
        }

        static void handle(FieldAction message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions().handleFieldAction(player, message.action(), message.value());
                }
                ctx.setPacketHandled(true);
            });
        }
    }

    /**
     * 客户端 -> 服务端：按了决斗盘上的某个控件（{@code p1_confirm} / {@code p1_phase_ep} 这类）。
     *
     * <p>命中判定放在客户端做（用的是"此刻"的视线，比服务端那一 tick 的视角准），
     * 服务端再校验这个控件是不是他的、现在能不能按，然后执行——左右键都走这条。
     */
    public record FieldClick(String controlId) {
        static void encode(FieldClick message, FriendlyByteBuf buffer) {
            buffer.writeUtf(message.controlId == null ? "" : message.controlId, 64);
        }

        static FieldClick decode(FriendlyByteBuf buffer) {
            return new FieldClick(buffer.readUtf(64));
        }

        static void handle(FieldClick message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions().handleFieldClick(player, message.controlId());
                }
                ctx.setPacketHandled(true);
            });
        }
    }

    /** 服务端 -> 客户端：核心在等一个"是和否"的决定（诱发效果、确认等）。 */
    public record DecisionPrompt(int function, String cardName, String question) {
        static void encode(DecisionPrompt message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.function);
            buffer.writeUtf(message.cardName == null ? "" : message.cardName, 128);
            buffer.writeUtf(message.question == null ? "" : message.question, 256);
        }

        static DecisionPrompt decode(FriendlyByteBuf buffer) {
            return new DecisionPrompt(buffer.readVarInt(), buffer.readUtf(128), buffer.readUtf(256));
        }

        static void handle(DecisionPrompt message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientPacketScreens.openDecision(message)));
            ctx.setPacketHandled(true);
        }
    }

    /** 客户端 -> 服务端：玩家按了"是 / 否"。 */
    public record DecisionAnswer(int value) {
        static void encode(DecisionAnswer message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.value);
        }

        static DecisionAnswer decode(FriendlyByteBuf buffer) {
            return new DecisionAnswer(buffer.readVarInt());
        }

        static void handle(DecisionAnswer message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) MDProMod.sessions().answerDecision(player, message.value());
                ctx.setPacketHandled(true);
            });
        }
    }

    /** 服务端 -> 客户端：打开某个卡堆（额外/墓地/除外）的交互界面。 */
    public record PileView(int location, int[] actionSeqs, int[] actionCodes, int[] allCodes) {
        static void encode(PileView message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.location);
            buffer.writeVarIntArray(message.actionSeqs);
            buffer.writeVarIntArray(message.actionCodes);
            buffer.writeVarIntArray(message.allCodes);
        }

        static PileView decode(FriendlyByteBuf buffer) {
            return new PileView(buffer.readVarInt(), buffer.readVarIntArray(),
                    buffer.readVarIntArray(), buffer.readVarIntArray());
        }

        static void handle(PileView message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientPacketScreens.openPile(message)));
            ctx.setPacketHandled(true);
        }
    }

    /** 客户端 -> 服务端：在卡堆界面里按了"特殊召唤/发动"。 */
    public record PileAct(int location, int sequence) {
        static void encode(PileAct message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.location);
            buffer.writeVarInt(message.sequence);
        }

        static PileAct decode(FriendlyByteBuf buffer) {
            return new PileAct(buffer.readVarInt(), buffer.readVarInt());
        }

        static void handle(PileAct message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions().performPileAct(player, message.location(), message.sequence());
                }
                ctx.setPacketHandled(true);
            });
        }
    }

    /** 服务端 -> 客户端：这三个卡堆现在有"可特殊召唤 / 可发动"的卡，要发光。 */
    /**
     * 召唤演出。kind：0 融合 / 1 同调 / 2 超量 / 3 连接 / 4 仪式 / 5 灵摆；
     * phase：0 = 素材吸入（材料被吸向场地中央），1 = 正式召唤（中央大演出 + 类型细节）；
     * code 是被召唤怪兽的卡号（客户端用它查等级/连接数等），materialCount 是素材数量。
     */
    public record SummonEffect(int fieldEntityId, int kind, int phase, int code, int materialCount,
                               double x, double y, double z, int durationMs) {
        static void encode(SummonEffect message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.fieldEntityId);
            buffer.writeVarInt(message.kind);
            buffer.writeVarInt(message.phase);
            buffer.writeVarInt(message.code);
            buffer.writeVarInt(message.materialCount);
            buffer.writeDouble(message.x);
            buffer.writeDouble(message.y);
            buffer.writeDouble(message.z);
            buffer.writeVarInt(message.durationMs);
        }

        static SummonEffect decode(FriendlyByteBuf buffer) {
            return new SummonEffect(buffer.readVarInt(), buffer.readVarInt(), buffer.readVarInt(),
                    buffer.readVarInt(), buffer.readVarInt(),
                    buffer.readDouble(), buffer.readDouble(), buffer.readDouble(), buffer.readVarInt());
        }

        static void handle(SummonEffect message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> {
                // phase 2 = 场地中央的王牌 Spine 演出。保留场地 ID，按同场地法阵排期。
                if (message.phase() == 2) {
                    ClientCutinEffects.play(message);
                } else {
                    ClientSummonEffects.play(message);
                }
            }));
            ctx.setPacketHandled(true);
        }
    }

    public record PileHint(int fieldEntityId, int[] zones) {
        static void encode(PileHint message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.fieldEntityId);
            buffer.writeVarIntArray(message.zones);
        }

        static PileHint decode(FriendlyByteBuf buffer) {
            return new PileHint(buffer.readVarInt(), buffer.readVarIntArray());
        }

        static void handle(PileHint message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientPileHints.apply(message.fieldEntityId(), message.zones())));
            ctx.setPacketHandled(true);
        }
    }

    /** 动作菜单的按钮种类（位掩码用）。 */
    public static final int CARD_MENU_SUMMON = 0;
    public static final int CARD_MENU_SET = 1;
    public static final int CARD_MENU_ACTIVATE = 2;
    public static final int CARD_MENU_ATTACK = 3;
    public static final int CARD_MENU_POSITION = 4;
    public static final int CARD_MENU_SPSUMMON = 5;

    /** 服务端 -> 客户端：点开某张卡后，它现在能做的动作（位掩码，0 = 什么都不行）。 */
    public record CardActions(int entityId, int mask) {
        static void encode(CardActions message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.entityId);
            buffer.writeVarInt(message.mask);
        }

        static CardActions decode(FriendlyByteBuf buffer) {
            return new CardActions(buffer.readVarInt(), buffer.readVarInt());
        }

        static void handle(CardActions message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientCardMenu.open(message.entityId(), message.mask())));
            ctx.setPacketHandled(true);
        }
    }

    /** 客户端 -> 服务端：玩家在动作菜单里点了某个按钮。 */
    public record CardAct(int entityId, int kind) {
        static void encode(CardAct message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.entityId);
            buffer.writeVarInt(message.kind);
        }

        static CardAct decode(FriendlyByteBuf buffer) {
            return new CardAct(buffer.readVarInt(), buffer.readVarInt());
        }

        static void handle(CardAct message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions().performCardAction(player, message.entityId(), message.kind());
                }
                ctx.setPacketHandled(true);
            });
        }
    }

    /** 服务端 -> 客户端：核心正在等落点，这些都是可以选的格子
     *  （address = controller&lt;&lt;16 | location&lt;&lt;8 | sequence）。 */
    public record PlaceHint(int fieldEntityId, int[] places) {
        static void encode(PlaceHint message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.fieldEntityId);
            buffer.writeVarIntArray(message.places);
        }

        static PlaceHint decode(FriendlyByteBuf buffer) {
            return new PlaceHint(buffer.readVarInt(), buffer.readVarIntArray());
        }

        static void handle(PlaceHint message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientPlaceHints.apply(message.fieldEntityId(), message.places())));
            ctx.setPacketHandled(true);
        }
    }

    /** 服务端 -> 客户端：核心正在等选卡（弃牌等）。只有该玩家能看到这些发光标记。 */
    public record SelectHint(int[] selectable, int[] selected) {
        static void encode(SelectHint message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.selectable);
            buffer.writeVarIntArray(message.selected);
        }

        static SelectHint decode(FriendlyByteBuf buffer) {
            return new SelectHint(buffer.readVarIntArray(), buffer.readVarIntArray());
        }

        static void handle(SelectHint message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientCardSelect.apply(message.selectable(), message.selected())));
            ctx.setPacketHandled(true);
        }
    }

    /** 服务端 -> 客户端：打开某张卡的大图（手牌实体右键）。 */
    public record OpenCard(int code) {
        static void encode(OpenCard message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.code);
        }

        static OpenCard decode(FriendlyByteBuf buffer) {
            return new OpenCard(buffer.readVarInt());
        }

        static void handle(OpenCard message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientHandBridge.openCard(message.code)));
            ctx.setPacketHandled(true);
        }
    }

    /** Spatial duel audio; stop with an empty key also cancels queued/looping session sounds. */
    public record DuelSound(java.util.UUID sessionId, int fieldId, String key,
                            net.minecraft.world.phys.Vec3 at, int delayMs, int durationMs,
                            float volume, float pitch, boolean loop, boolean stop) {
        public static DuelSound once(java.util.UUID session, int fieldId, String key,
                                     net.minecraft.world.phys.Vec3 at) {
            return new DuelSound(session, fieldId, key, at, 0, 0, 1.0F, 1.0F, false, false);
        }
        public static DuelSound bgm(java.util.UUID session, int fieldId, int delayMs) {
            return new DuelSound(session, fieldId, DuelBgmRules.CUE_KEY,
                    net.minecraft.world.phys.Vec3.ZERO, delayMs, 0, 1.0F, 1.0F, true, false);
        }
        public static DuelSound stop(java.util.UUID session, int fieldId, String key) {
            return new DuelSound(session, fieldId, key, net.minecraft.world.phys.Vec3.ZERO,
                    0, 0, 1.0F, 1.0F, false, true);
        }
        static void encode(DuelSound m, FriendlyByteBuf b) {
            b.writeUUID(m.sessionId); b.writeVarInt(m.fieldId); b.writeUtf(m.key, 192);
            b.writeDouble(m.at.x); b.writeDouble(m.at.y); b.writeDouble(m.at.z);
            b.writeVarInt(m.delayMs); b.writeVarInt(m.durationMs);
            b.writeFloat(m.volume); b.writeFloat(m.pitch);
            b.writeBoolean(m.loop); b.writeBoolean(m.stop);
        }
        static DuelSound decode(FriendlyByteBuf b) {
            return new DuelSound(b.readUUID(), b.readVarInt(), b.readUtf(192),
                    new net.minecraft.world.phys.Vec3(b.readDouble(), b.readDouble(), b.readDouble()),
                    b.readVarInt(), b.readVarInt(), b.readFloat(), b.readFloat(),
                    b.readBoolean(), b.readBoolean());
        }
        static void handle(DuelSound m, Supplier<NetworkEvent.Context> context) {
            var ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientDuelAudio.play(m)));
            ctx.setPacketHandled(true);
        }
    }

    /**
     * 服务端 -> 客户端：下发各「时机」当前选的曲目（顺序同 {@link DuelBgmRules#MOMENTS}）
     * 以及全部可选曲目。
     * {@code open=true} 是右键「音乐设置」打开的界面；{@code open=false} 是上线时的只同步。
     */
    public record MusicSettingsOpen(String[] selected, String[] keys, String[] labels, boolean open) {
        public static MusicSettingsOpen screen(String[] selected, String[] keys, String[] labels) {
            return new MusicSettingsOpen(selected, keys, labels, true);
        }

        public static MusicSettingsOpen sync(String[] selected) {
            return new MusicSettingsOpen(selected, new String[0], new String[0], false);
        }

        static void encode(MusicSettingsOpen m, FriendlyByteBuf b) {
            b.writeVarInt(m.selected.length);
            for (String value : m.selected) b.writeUtf(value == null ? "" : value, 192);
            b.writeVarInt(m.keys.length);
            for (String key : m.keys) b.writeUtf(key, 192);
            b.writeVarInt(m.labels.length);
            for (String label : m.labels) b.writeUtf(label, 256);
            b.writeBoolean(m.open);
        }

        static MusicSettingsOpen decode(FriendlyByteBuf b) {
            String[] selected = new String[b.readVarInt()];
            for (int i = 0; i < selected.length; i++) selected[i] = b.readUtf(192);
            String[] keys = new String[b.readVarInt()];
            for (int i = 0; i < keys.length; i++) keys[i] = b.readUtf(192);
            String[] labels = new String[b.readVarInt()];
            for (int i = 0; i < labels.length; i++) labels[i] = b.readUtf(256);
            return new MusicSettingsOpen(selected, keys, labels, b.readBoolean());
        }

        static void handle(MusicSettingsOpen m, Supplier<NetworkEvent.Context> context) {
            var ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientMusicSettingsBridge.open(m)));
            ctx.setPacketHandled(true);
        }
    }

    /** 客户端 -> 服务端：给某个「时机」选了一首新曲子，写进「音乐设置」物品的 NBT。 */
    public record MusicSettingsAction(String moment, String track) {
        static void encode(MusicSettingsAction m, FriendlyByteBuf b) {
            b.writeUtf(m.moment == null ? "" : m.moment, 32);
            b.writeUtf(m.track == null ? "" : m.track, 192);
        }

        static MusicSettingsAction decode(FriendlyByteBuf b) {
            return new MusicSettingsAction(b.readUtf(32), b.readUtf(192));
        }

        static void handle(MusicSettingsAction m, Supplier<NetworkEvent.Context> context) {
            var ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player == null || !DuelBgmRules.isMoment(m.moment)) return;
                net.minecraft.world.item.ItemStack stack = MusicSettingsItem.findSettings(player);
                if (stack != null) {
                    MusicSettingsItem.setTrack(stack, m.moment, m.track);
                    // NBT 是就地改的，要让客户端那份物品也跟着更新
                    player.inventoryMenu.broadcastChanges();
                }
                player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                        DuelBgmRules.momentLabel(m.moment) + " 音乐已设为："
                                + DuelBgmTracks.label(m.track)), true);
            });
            ctx.setPacketHandled(true);
        }
    }

    /**
     * 服务端 -> 客户端：人机卡组列表 + 当前选择 + 每份卡组的卡（只读预览）。
     * {@code counts} 每条 3 个（主/额外/副），{@code cards} 是各卡组按同样顺序拼起来的卡号。
     */
    public record AiDeckOpen(String selected, String[] keys, String[] labels, String[] descriptions,
                             int[] counts, int[] cards) {
        public static AiDeckOpen of(String selected, java.util.List<AiDeckCatalog.Entry> entries,
                                    java.nio.file.Path root) {
            int size = entries.size();
            String[] keys = new String[size];
            String[] labels = new String[size];
            String[] descriptions = new String[size];
            int[] counts = new int[size * 3];
            java.util.List<Integer> cards = new java.util.ArrayList<>();
            for (int i = 0; i < size; i++) {
                AiDeckCatalog.Entry entry = entries.get(i);
                keys[i] = entry.key();
                labels[i] = entry.label();
                descriptions[i] = entry.description();
                try {
                    DeckData deck = AiDeckCatalog.deck(root, entry.key());
                    counts[i * 3] = deck.mainDeck().size();
                    counts[i * 3 + 1] = deck.extraDeck().size();
                    counts[i * 3 + 2] = deck.sideDeck().size();
                    cards.addAll(deck.mainDeck());
                    cards.addAll(deck.extraDeck());
                    cards.addAll(deck.sideDeck());
                } catch (Exception error) {
                    org.slf4j.LoggerFactory.getLogger("mdpromod")
                            .warn("读人机卡组失败 {}: {}", entry.key(), error.toString());
                }
            }
            int[] flat = new int[cards.size()];
            for (int i = 0; i < flat.length; i++) flat[i] = cards.get(i);
            return new AiDeckOpen(selected, keys, labels, descriptions, counts, flat);
        }

        static void encode(AiDeckOpen m, FriendlyByteBuf b) {
            b.writeUtf(m.selected == null ? "" : m.selected, 64);
            writeStrings(b, m.keys);
            writeStrings(b, m.labels);
            writeStrings(b, m.descriptions);
            b.writeVarIntArray(m.counts);
            b.writeVarIntArray(m.cards);
        }

        static AiDeckOpen decode(FriendlyByteBuf b) {
            return new AiDeckOpen(b.readUtf(64), readStrings(b), readStrings(b), readStrings(b),
                    b.readVarIntArray(), b.readVarIntArray());
        }

        static void handle(AiDeckOpen m, Supplier<NetworkEvent.Context> context) {
            var ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientAiDeckBridge.open(m)));
            ctx.setPacketHandled(true);
        }
    }

    /** 客户端 -> 服务端：换了要打的人机，写进「人机卡组」物品的 NBT。 */
    public record AiDeckSelect(String key, String label, String description) {
        static void encode(AiDeckSelect m, FriendlyByteBuf b) {
            b.writeUtf(m.key == null ? "" : m.key, 64);
            b.writeUtf(m.label == null ? "" : m.label, 64);
            b.writeUtf(m.description == null ? "" : m.description, 256);
        }

        static AiDeckSelect decode(FriendlyByteBuf b) {
            return new AiDeckSelect(b.readUtf(64), b.readUtf(64), b.readUtf(256));
        }

        static void handle(AiDeckSelect m, Supplier<NetworkEvent.Context> context) {
            var ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player == null) return;
                net.minecraft.world.item.ItemStack stack = AiDeckItem.find(player);
                if (stack != null) {
                    AiDeckItem.setSelection(stack, m.key, m.label, m.description);
                    player.inventoryMenu.broadcastChanges();
                }
                player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                        "人机卡组已设为：" + (m.label == null || m.label.isBlank() ? m.key : m.label)), true);
            });
            ctx.setPacketHandled(true);
        }
    }

    private static void writeStrings(FriendlyByteBuf buffer, String[] values) {
        buffer.writeVarInt(values.length);
        for (String value : values) buffer.writeUtf(value == null ? "" : value, 256);
    }

    private static String[] readStrings(FriendlyByteBuf buffer) {
        String[] values = new String[buffer.readVarInt()];
        for (int i = 0; i < values.length; i++) values[i] = buffer.readUtf(256);
        return values;
    }

    /** A privacy-filtered moving card, independent of the source entity's lifetime. */
    public record DuelAnimation(java.util.UUID sessionId, int fieldId, long eventId,
                                String kind, int cardCode, boolean faceUp,
                                net.minecraft.world.phys.Vec3 from, net.minecraft.world.phys.Vec3 to,
                                float fromYaw, float toYaw, float fromPitch, float toPitch,
                                boolean fromDefense, boolean toDefense,
                                int durationMs, int delayMs, int hideEntityId, float fieldYaw) {
        static void encode(DuelAnimation m, FriendlyByteBuf b) {
            b.writeUUID(m.sessionId); b.writeVarInt(m.fieldId); b.writeLong(m.eventId);
            b.writeUtf(m.kind, 32); b.writeVarInt(m.cardCode); b.writeBoolean(m.faceUp);
            writePoint(b, m.from); writePoint(b, m.to);
            b.writeFloat(m.fromYaw); b.writeFloat(m.toYaw);
            b.writeFloat(m.fromPitch); b.writeFloat(m.toPitch);
            b.writeBoolean(m.fromDefense); b.writeBoolean(m.toDefense);
            b.writeVarInt(m.durationMs); b.writeVarInt(m.delayMs);
            b.writeVarInt(m.hideEntityId); b.writeFloat(m.fieldYaw);
        }
        static DuelAnimation decode(FriendlyByteBuf b) {
            return new DuelAnimation(b.readUUID(), b.readVarInt(), b.readLong(), b.readUtf(32),
                    b.readVarInt(), b.readBoolean(), readPoint(b), readPoint(b),
                    b.readFloat(), b.readFloat(), b.readFloat(), b.readFloat(),
                    b.readBoolean(), b.readBoolean(), b.readVarInt(), b.readVarInt(),
                    b.readVarInt(), b.readFloat());
        }
        private static void writePoint(FriendlyByteBuf b, net.minecraft.world.phys.Vec3 p) {
            b.writeDouble(p.x); b.writeDouble(p.y); b.writeDouble(p.z);
        }
        private static net.minecraft.world.phys.Vec3 readPoint(FriendlyByteBuf b) {
            return new net.minecraft.world.phys.Vec3(b.readDouble(), b.readDouble(), b.readDouble());
        }
        static void handle(DuelAnimation m, Supplier<NetworkEvent.Context> context) {
            var ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientAnimBridge.apply(m)));
            ctx.setPacketHandled(true);
        }
    }

    public record DuelAnimationClear(java.util.UUID sessionId) {
        static void encode(DuelAnimationClear m, FriendlyByteBuf b) { b.writeUUID(m.sessionId); }
        static DuelAnimationClear decode(FriendlyByteBuf b) { return new DuelAnimationClear(b.readUUID()); }
        static void handle(DuelAnimationClear m, Supplier<NetworkEvent.Context> context) {
            var ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> {
                        ClientAnimBridge.clearSession(m.sessionId);
                        ClientDuelAudio.clearSession(m.sessionId);
                    }));
            ctx.setPacketHandled(true);
        }
    }

    /** Only sent to the owner. Public entity data always contains zero for a hidden card. */
    public record CardKnowledge(int entityId, int cardCode) {
        static void encode(CardKnowledge m, FriendlyByteBuf b) {
            b.writeVarInt(m.entityId); b.writeVarInt(m.cardCode);
        }
        static CardKnowledge decode(FriendlyByteBuf b) {
            return new CardKnowledge(b.readVarInt(), b.readVarInt());
        }
        static void handle(CardKnowledge m, Supplier<NetworkEvent.Context> context) {
            var ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientAnimBridge.applyCardKnowledge(m.entityId, m.cardCode)));
            ctx.setPacketHandled(true);
        }
    }

    /**
     * 服务端 -> 客户端：一次卡牌演出。
     * kind: draw / summon / set；from/to 是世界坐标，客户端按缓动曲线插值渲染。
     */
    public record Anim(int entityId, String kind, double fromX, double fromY, double fromZ,
                       double toX, double toY, double toZ, int durationMs, int rollDegrees) {
        static void encode(Anim message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.entityId);
            buffer.writeUtf(message.kind, 16);
            buffer.writeDouble(message.fromX);
            buffer.writeDouble(message.fromY);
            buffer.writeDouble(message.fromZ);
            buffer.writeDouble(message.toX);
            buffer.writeDouble(message.toY);
            buffer.writeDouble(message.toZ);
            buffer.writeVarInt(message.durationMs);
            buffer.writeVarInt(message.rollDegrees);
        }

        static Anim decode(FriendlyByteBuf buffer) {
            return new Anim(buffer.readVarInt(), buffer.readUtf(16),
                    buffer.readDouble(), buffer.readDouble(), buffer.readDouble(),
                    buffer.readDouble(), buffer.readDouble(), buffer.readDouble(),
                    buffer.readVarInt(), buffer.readVarInt());
        }

        static void handle(Anim message, Supplier<NetworkEvent.Context> context) {
            NetworkEvent.Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT,
                    () -> () -> ClientAnimBridge.apply(message)));
            ctx.setPacketHandled(true);
        }
    }
}
