package net.mdpromod;

import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;
import java.util.function.Supplier;
import net.minecraft.network.FriendlyByteBuf;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.level.Level;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.fml.DistExecutor;
import net.minecraftforge.network.NetworkDirection;
import net.minecraftforge.network.PacketDistributor;
import net.minecraftforge.network.NetworkEvent.Context;
import net.minecraftforge.network.NetworkRegistry.ChannelBuilder;
import net.minecraftforge.network.PacketDistributor.TargetPoint;
import net.minecraftforge.network.simple.SimpleChannel;
import org.slf4j.LoggerFactory;

public final class ModNetwork {
    private static final String PROTOCOL = "8";
    public static final SimpleChannel CHANNEL = ChannelBuilder.named(ResourceLocation.fromNamespaceAndPath("mdpromod", "main"))
        .networkProtocolVersion(() -> "8")
        .clientAcceptedVersions("8"::equals)
        .serverAcceptedVersions("8"::equals)
        .simpleChannel();
    public static final int CARD_MENU_SUMMON = 0;
    public static final int CARD_MENU_SET = 1;
    public static final int CARD_MENU_ACTIVATE = 2;
    public static final int CARD_MENU_ATTACK = 3;
    public static final int CARD_MENU_POSITION = 4;
    public static final int CARD_MENU_SPSUMMON = 5;

    private ModNetwork() {
    }

    public static void register() {
        int id = 0;
        CHANNEL.messageBuilder(ModNetwork.CardCaseAction.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.CardCaseAction::encode)
            .decoder(ModNetwork.CardCaseAction::decode)
            .consumerMainThread(ModNetwork.CardCaseAction::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.CardCaseReplace.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.CardCaseReplace::encode)
            .decoder(ModNetwork.CardCaseReplace::decode)
            .consumerMainThread(ModNetwork.CardCaseReplace::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.CardCaseSync.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.CardCaseSync::encode)
            .decoder(ModNetwork.CardCaseSync::decode)
            .consumerMainThread(ModNetwork.CardCaseSync::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.CardCaseOpen.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.CardCaseOpen::encode)
            .decoder(ModNetwork.CardCaseOpen::decode)
            .consumerMainThread(ModNetwork.CardCaseOpen::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.HandSync.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.HandSync::encode)
            .decoder(ModNetwork.HandSync::decode)
            .consumerMainThread(ModNetwork.HandSync::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.OpenCard.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.OpenCard::encode)
            .decoder(ModNetwork.OpenCard::decode)
            .consumerMainThread(ModNetwork.OpenCard::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.Anim.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.Anim::encode)
            .decoder(ModNetwork.Anim::decode)
            .consumerMainThread(ModNetwork.Anim::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.DuelAnimation.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.DuelAnimation::encode)
            .decoder(ModNetwork.DuelAnimation::decode)
            .consumerMainThread(ModNetwork.DuelAnimation::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.DuelAnimationClear.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.DuelAnimationClear::encode)
            .decoder(ModNetwork.DuelAnimationClear::decode)
            .consumerMainThread(ModNetwork.DuelAnimationClear::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.CardKnowledge.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.CardKnowledge::encode)
            .decoder(ModNetwork.CardKnowledge::decode)
            .consumerMainThread(ModNetwork.CardKnowledge::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.CardActions.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.CardActions::encode)
            .decoder(ModNetwork.CardActions::decode)
            .consumerMainThread(ModNetwork.CardActions::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.CardAct.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.CardAct::encode)
            .decoder(ModNetwork.CardAct::decode)
            .consumerMainThread(ModNetwork.CardAct::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.PlaceHint.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.PlaceHint::encode)
            .decoder(ModNetwork.PlaceHint::decode)
            .consumerMainThread(ModNetwork.PlaceHint::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.SelectHint.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.SelectHint::encode)
            .decoder(ModNetwork.SelectHint::decode)
            .consumerMainThread(ModNetwork.SelectHint::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.PileHint.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.PileHint::encode)
            .decoder(ModNetwork.PileHint::decode)
            .consumerMainThread(ModNetwork.PileHint::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.SummonEffect.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.SummonEffect::encode)
            .decoder(ModNetwork.SummonEffect::decode)
            .consumerMainThread(ModNetwork.SummonEffect::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.PileView.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.PileView::encode)
            .decoder(ModNetwork.PileView::decode)
            .consumerMainThread(ModNetwork.PileView::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.PileAct.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.PileAct::encode)
            .decoder(ModNetwork.PileAct::decode)
            .consumerMainThread(ModNetwork.PileAct::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.DecisionPrompt.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.DecisionPrompt::encode)
            .decoder(ModNetwork.DecisionPrompt::decode)
            .consumerMainThread(ModNetwork.DecisionPrompt::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.DecisionAnswer.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.DecisionAnswer::encode)
            .decoder(ModNetwork.DecisionAnswer::decode)
            .consumerMainThread(ModNetwork.DecisionAnswer::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.ChainState.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.ChainState::encode)
            .decoder(ModNetwork.ChainState::decode)
            .consumerMainThread(ModNetwork.ChainState::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.ChainChoice.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.ChainChoice::encode)
            .decoder(ModNetwork.ChainChoice::decode)
            .consumerMainThread(ModNetwork.ChainChoice::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.ChainChoose.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.ChainChoose::encode)
            .decoder(ModNetwork.ChainChoose::decode)
            .consumerMainThread(ModNetwork.ChainChoose::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.SelectList.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.SelectList::encode)
            .decoder(ModNetwork.SelectList::decode)
            .consumerMainThread(ModNetwork.SelectList::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.SelectChoose.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.SelectChoose::encode)
            .decoder(ModNetwork.SelectChoose::decode)
            .consumerMainThread(ModNetwork.SelectChoose::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.UnselectList.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.UnselectList::encode)
            .decoder(ModNetwork.UnselectList::decode)
            .consumerMainThread(ModNetwork.UnselectList::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.UnselectPick.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.UnselectPick::encode)
            .decoder(ModNetwork.UnselectPick::decode)
            .consumerMainThread(ModNetwork.UnselectPick::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.PositionList.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.PositionList::encode)
            .decoder(ModNetwork.PositionList::decode)
            .consumerMainThread(ModNetwork.PositionList::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.PositionAnswer.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.PositionAnswer::encode)
            .decoder(ModNetwork.PositionAnswer::decode)
            .consumerMainThread(ModNetwork.PositionAnswer::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.SortList.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.SortList::encode)
            .decoder(ModNetwork.SortList::decode)
            .consumerMainThread(ModNetwork.SortList::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.SortOrder.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.SortOrder::encode)
            .decoder(ModNetwork.SortOrder::decode)
            .consumerMainThread(ModNetwork.SortOrder::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.OptionList.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.OptionList::encode)
            .decoder(ModNetwork.OptionList::decode)
            .consumerMainThread(ModNetwork.OptionList::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.OptionAnswer.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.OptionAnswer::encode)
            .decoder(ModNetwork.OptionAnswer::decode)
            .consumerMainThread(ModNetwork.OptionAnswer::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.DuelSound.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.DuelSound::encode)
            .decoder(ModNetwork.DuelSound::decode)
            .consumerMainThread(ModNetwork.DuelSound::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.MusicSettingsOpen.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.MusicSettingsOpen::encode)
            .decoder(ModNetwork.MusicSettingsOpen::decode)
            .consumerMainThread(ModNetwork.MusicSettingsOpen::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.MusicSettingsAction.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.MusicSettingsAction::encode)
            .decoder(ModNetwork.MusicSettingsAction::decode)
            .consumerMainThread(ModNetwork.MusicSettingsAction::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.AiDeckOpen.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.AiDeckOpen::encode)
            .decoder(ModNetwork.AiDeckOpen::decode)
            .consumerMainThread(ModNetwork.AiDeckOpen::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.AiDeckSelect.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.AiDeckSelect::encode)
            .decoder(ModNetwork.AiDeckSelect::decode)
            .consumerMainThread(ModNetwork.AiDeckSelect::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.CardMarks.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.CardMarks::encode)
            .decoder(ModNetwork.CardMarks::decode)
            .consumerMainThread(ModNetwork.CardMarks::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.FieldAction.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.FieldAction::encode)
            .decoder(ModNetwork.FieldAction::decode)
            .consumerMainThread(ModNetwork.FieldAction::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.FieldClick.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.FieldClick::encode)
            .decoder(ModNetwork.FieldClick::decode)
            .consumerMainThread(ModNetwork.FieldClick::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.PromptResend.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.PromptResend::encode)
            .decoder(ModNetwork.PromptResend::decode)
            .consumerMainThread(ModNetwork.PromptResend::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.AnnounceList.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.AnnounceList::encode)
            .decoder(ModNetwork.AnnounceList::decode)
            .consumerMainThread(ModNetwork.AnnounceList::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.AnnounceAnswer.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.AnnounceAnswer::encode)
            .decoder(ModNetwork.AnnounceAnswer::decode)
            .consumerMainThread(ModNetwork.AnnounceAnswer::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.RpsPrompt.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.RpsPrompt::encode)
            .decoder(ModNetwork.RpsPrompt::decode)
            .consumerMainThread(ModNetwork.RpsPrompt::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.RpsPick.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.RpsPick::encode)
            .decoder(ModNetwork.RpsPick::decode)
            .consumerMainThread(ModNetwork.RpsPick::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.TpPrompt.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.TpPrompt::encode)
            .decoder(ModNetwork.TpPrompt::decode)
            .consumerMainThread(ModNetwork.TpPrompt::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.TpPick.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.TpPick::encode)
            .decoder(ModNetwork.TpPick::decode)
            .consumerMainThread(ModNetwork.TpPick::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.EffectChoices.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.EffectChoices::encode)
            .decoder(ModNetwork.EffectChoices::decode)
            .consumerMainThread(ModNetwork.EffectChoices::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.EffectPick.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.EffectPick::encode)
            .decoder(ModNetwork.EffectPick::decode)
            .consumerMainThread(ModNetwork.EffectPick::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.SumChoice.class, id++, NetworkDirection.PLAY_TO_CLIENT)
            .encoder(ModNetwork.SumChoice::encode)
            .decoder(ModNetwork.SumChoice::decode)
            .consumerMainThread(ModNetwork.SumChoice::handle)
            .add();
        CHANNEL.messageBuilder(ModNetwork.SumAnswer.class, id++, NetworkDirection.PLAY_TO_SERVER)
            .encoder(ModNetwork.SumAnswer::encode)
            .decoder(ModNetwork.SumAnswer::decode)
            .consumerMainThread(ModNetwork.SumAnswer::handle)
            .add();
    }

    public static void sendToServer(Object message) {
        CHANNEL.sendToServer(message);
    }

    public static void sendTo(ServerPlayer player, Object message) {
        CHANNEL.send(PacketDistributor.PLAYER.with(() -> player), message);
    }

    public static void sendNear(Level level, Vec3 at, double radius, Object message) {
        CHANNEL.send(PacketDistributor.NEAR.with(() -> new TargetPoint(at.x, at.y, at.z, radius, level.dimension())), message);
    }

    private static void writeStrings(FriendlyByteBuf buffer, String[] values) {
        buffer.writeVarInt(values.length);

        for (String value : values) {
            buffer.writeUtf(value == null ? "" : value, 256);
        }
    }

    private static String[] readStrings(FriendlyByteBuf buffer) {
        String[] values = new String[buffer.readVarInt()];

        for (int i = 0; i < values.length; i++) {
            values[i] = buffer.readUtf(256);
        }

        return values;
    }

    public record AiDeckOpen(String selected, String[] keys, String[] labels, String[] descriptions, int[] counts, int[] cards) {
        public static ModNetwork.AiDeckOpen of(String selected, List<AiDeckCatalog.Entry> entries, Path root) {
            int size = entries.size();
            String[] keys = new String[size];
            String[] labels = new String[size];
            String[] descriptions = new String[size];
            int[] counts = new int[size * 3];
            List<Integer> cards = new ArrayList<>();

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
                    LoggerFactory.getLogger("mdpromod").warn("读人机卡组失败 {}: {}", entry.key(), error.toString());
                }
            }

            int[] flat = new int[cards.size()];

            for (int i = 0; i < flat.length; i++) {
                flat[i] = cards.get(i);
            }

            return new ModNetwork.AiDeckOpen(selected, keys, labels, descriptions, counts, flat);
        }

        static void encode(ModNetwork.AiDeckOpen m, FriendlyByteBuf b) {
            b.writeUtf(m.selected == null ? "" : m.selected, 64);
            ModNetwork.writeStrings(b, m.keys);
            ModNetwork.writeStrings(b, m.labels);
            ModNetwork.writeStrings(b, m.descriptions);
            b.writeVarIntArray(m.counts);
            b.writeVarIntArray(m.cards);
        }

        static ModNetwork.AiDeckOpen decode(FriendlyByteBuf b) {
            return new ModNetwork.AiDeckOpen(
                b.readUtf(64), ModNetwork.readStrings(b), ModNetwork.readStrings(b), ModNetwork.readStrings(b), b.readVarIntArray(), b.readVarIntArray()
            );
        }

        static void handle(ModNetwork.AiDeckOpen m, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientAiDeckBridge.open(m)));
            ctx.setPacketHandled(true);
        }
    }

    public record AiDeckSelect(String key, String label, String description) {
        static void encode(ModNetwork.AiDeckSelect m, FriendlyByteBuf b) {
            b.writeUtf(m.key == null ? "" : m.key, 64);
            b.writeUtf(m.label == null ? "" : m.label, 64);
            b.writeUtf(m.description == null ? "" : m.description, 256);
        }

        static ModNetwork.AiDeckSelect decode(FriendlyByteBuf b) {
            return new ModNetwork.AiDeckSelect(b.readUtf(64), b.readUtf(64), b.readUtf(256));
        }

        static void handle(ModNetwork.AiDeckSelect m, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    ItemStack stack = AiDeckItem.find(player);
                    if (stack != null) {
                        AiDeckItem.setSelection(stack, m.key, m.label, m.description);
                        player.inventoryMenu.broadcastChanges();
                    }

                    player.displayClientMessage(Component.literal("人机卡组已设为：" + (m.label != null && !m.label.isBlank() ? m.label : m.key)), true);
                }
            });
            ctx.setPacketHandled(true);
        }
    }

    public record Anim(int entityId, String kind, double fromX, double fromY, double fromZ, double toX, double toY, double toZ, int durationMs, int rollDegrees) {
        static void encode(ModNetwork.Anim message, FriendlyByteBuf buffer) {
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

        static ModNetwork.Anim decode(FriendlyByteBuf buffer) {
            return new ModNetwork.Anim(
                buffer.readVarInt(),
                buffer.readUtf(16),
                buffer.readDouble(),
                buffer.readDouble(),
                buffer.readDouble(),
                buffer.readDouble(),
                buffer.readDouble(),
                buffer.readDouble(),
                buffer.readVarInt(),
                buffer.readVarInt()
            );
        }

        static void handle(ModNetwork.Anim message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientAnimBridge.apply(message)));
            ctx.setPacketHandled(true);
        }
    }

    public record AnnounceAnswer(int value) {
        static void encode(ModNetwork.AnnounceAnswer message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.value);
        }

        static ModNetwork.AnnounceAnswer decode(FriendlyByteBuf buffer) {
            return new ModNetwork.AnnounceAnswer(buffer.readVarInt());
        }

        static void handle(ModNetwork.AnnounceAnswer message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions(player).answerAnnounce(player, message.value());
                }

                ctx.setPacketHandled(true);
            });
        }
    }

    public record AnnounceList(String kind, int pick, int[] values, String hint) {
        static void encode(ModNetwork.AnnounceList message, FriendlyByteBuf buffer) {
            buffer.writeUtf(message.kind == null ? "" : message.kind, 16);
            buffer.writeVarInt(message.pick);
            buffer.writeVarIntArray(message.values);
            buffer.writeUtf(message.hint == null ? "" : message.hint, 200);
        }

        static ModNetwork.AnnounceList decode(FriendlyByteBuf buffer) {
            return new ModNetwork.AnnounceList(buffer.readUtf(16), buffer.readVarInt(), buffer.readVarIntArray(), buffer.readUtf(200));
        }

        static void handle(ModNetwork.AnnounceList message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientPacketScreens.openAnnounce(message)));
            ctx.setPacketHandled(true);
        }
    }

    public record CardAct(int entityId, int kind) {
        static void encode(ModNetwork.CardAct message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.entityId);
            buffer.writeVarInt(message.kind);
        }

        static ModNetwork.CardAct decode(FriendlyByteBuf buffer) {
            return new ModNetwork.CardAct(buffer.readVarInt(), buffer.readVarInt());
        }

        static void handle(ModNetwork.CardAct message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions(player).performCardAction(player, message.entityId(), message.kind());
                }

                ctx.setPacketHandled(true);
            });
        }
    }

    public record CardActions(int entityId, int mask) {
        static void encode(ModNetwork.CardActions message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.entityId);
            buffer.writeVarInt(message.mask);
        }

        static ModNetwork.CardActions decode(FriendlyByteBuf buffer) {
            return new ModNetwork.CardActions(buffer.readVarInt(), buffer.readVarInt());
        }

        static void handle(ModNetwork.CardActions message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientCardMenu.open(message.entityId(), message.mask())));
            ctx.setPacketHandled(true);
        }
    }

    public record CardCaseAction(int action, int zone, int code, String name) {
        static void encode(ModNetwork.CardCaseAction message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.action);
            buffer.writeVarInt(message.zone);
            buffer.writeVarInt(message.code);
            buffer.writeUtf(message.name == null ? "" : message.name, 64);
        }

        static ModNetwork.CardCaseAction decode(FriendlyByteBuf buffer) {
            return new ModNetwork.CardCaseAction(buffer.readVarInt(), buffer.readVarInt(), buffer.readVarInt(), buffer.readUtf(64));
        }

        static void handle(ModNetwork.CardCaseAction message, Supplier<Context> context) {
            Context ctx = context.get();
            ServerPlayer player = ctx.getSender();
            ctx.enqueueWork(() -> {
                if (player != null) {
                    ItemStack stack = CardCaseItem.findCase(player);
                    if (stack != null) {
                        DeckData deck = CardCaseItem.getDeck(stack);
                        int zone = Math.max(0, Math.min(2, message.zone));
                        switch (message.action) {
                            case 0:
                            {
                                List<Integer> list = switch (zone) {
                                    case 1 -> deck.extraDeck();
                                    case 2 -> deck.sideDeck();
                                    default -> deck.mainDeck();
                                };
                                list.add(message.code);
                                break;
                            }
                            case 1:
                            {
                                List<Integer> list = switch (zone) {
                                    case 1 -> deck.extraDeck();
                                    case 2 -> deck.sideDeck();
                                    default -> deck.mainDeck();
                                };
                                list.remove(Integer.valueOf(message.code));
                                break;
                            }
                            case 2:
                                deck.setName(message.name);
                            case 3:
                            default:
                                break;
                            case 4:
                                try {
                                    Path path = DeckYdk.write(deck, ModPaths.deckDir());
                                    player.displayClientMessage(Component.literal("已导出卡组：" + path), false);
                                } catch (Exception ex) {
                                    player.displayClientMessage(Component.literal("导出失败：" + ex.getMessage()), false);
                                }
                        }

                        CardCaseItem.setDeck(stack, deck);
                        player.inventoryMenu.broadcastChanges();
                        ModNetwork.sendTo(player, ModNetwork.CardCaseSync.of(deck));
                    }
                }
            });
            ctx.setPacketHandled(true);
        }
    }

    public record CardCaseOpen(String name, int[] main, int[] extra, int[] side) {
        public static ModNetwork.CardCaseOpen of(DeckData deck) {
            return new ModNetwork.CardCaseOpen(deck.name(), deck.mainArray(), deck.extraArray(), deck.sideArray());
        }

        static void encode(ModNetwork.CardCaseOpen message, FriendlyByteBuf buffer) {
            buffer.writeUtf(message.name == null ? "" : message.name, 64);
            buffer.writeVarIntArray(message.main);
            buffer.writeVarIntArray(message.extra);
            buffer.writeVarIntArray(message.side);
        }

        static ModNetwork.CardCaseOpen decode(FriendlyByteBuf buffer) {
            return new ModNetwork.CardCaseOpen(buffer.readUtf(64), buffer.readVarIntArray(), buffer.readVarIntArray(), buffer.readVarIntArray());
        }

        static void handle(ModNetwork.CardCaseOpen message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientCardCaseBridge.openScreen(message)));
            ctx.setPacketHandled(true);
        }
    }

    public record CardCaseReplace(String name, int[] main, int[] extra, int[] side) {
        static void encode(ModNetwork.CardCaseReplace message, FriendlyByteBuf buffer) {
            buffer.writeUtf(message.name == null ? "" : message.name, 64);
            buffer.writeVarIntArray(message.main);
            buffer.writeVarIntArray(message.extra);
            buffer.writeVarIntArray(message.side);
        }

        static ModNetwork.CardCaseReplace decode(FriendlyByteBuf buffer) {
            return new ModNetwork.CardCaseReplace(buffer.readUtf(64), buffer.readVarIntArray(), buffer.readVarIntArray(), buffer.readVarIntArray());
        }

        static void handle(ModNetwork.CardCaseReplace message, Supplier<Context> context) {
            Context ctx = context.get();
            ServerPlayer player = ctx.getSender();
            ctx.enqueueWork(() -> {
                if (player != null) {
                    ItemStack stack = CardCaseItem.findCase(player);
                    if (stack != null) {
                        DeckData deck = DeckData.of(message.name, message.main, message.extra, message.side);
                        CardCaseItem.setDeck(stack, deck);
                        player.inventoryMenu.broadcastChanges();
                        ModNetwork.sendTo(player, ModNetwork.CardCaseSync.of(deck));
                    }
                }
            });
            ctx.setPacketHandled(true);
        }
    }

    public record CardCaseSync(String name, int[] main, int[] extra, int[] side) {
        public static ModNetwork.CardCaseSync of(DeckData deck) {
            return new ModNetwork.CardCaseSync(deck.name(), deck.mainArray(), deck.extraArray(), deck.sideArray());
        }

        public DeckData toDeck() {
            return DeckData.of(this.name, this.main, this.extra, this.side);
        }

        static void encode(ModNetwork.CardCaseSync message, FriendlyByteBuf buffer) {
            buffer.writeUtf(message.name == null ? "" : message.name, 64);
            buffer.writeVarIntArray(message.main);
            buffer.writeVarIntArray(message.extra);
            buffer.writeVarIntArray(message.side);
        }

        static ModNetwork.CardCaseSync decode(FriendlyByteBuf buffer) {
            String name = buffer.readUtf(64);
            int[] main = buffer.readVarIntArray();
            int[] extra = buffer.readVarIntArray();
            int[] side = buffer.readVarIntArray();
            return new ModNetwork.CardCaseSync(name, main, extra, side);
        }

        static void handle(ModNetwork.CardCaseSync message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientCardCaseBridge.applySync(message)));
            ctx.setPacketHandled(true);
        }
    }

    public record CardKnowledge(int entityId, int cardCode) {
        static void encode(ModNetwork.CardKnowledge m, FriendlyByteBuf b) {
            b.writeVarInt(m.entityId);
            b.writeVarInt(m.cardCode);
        }

        static ModNetwork.CardKnowledge decode(FriendlyByteBuf b) {
            return new ModNetwork.CardKnowledge(b.readVarInt(), b.readVarInt());
        }

        static void handle(ModNetwork.CardKnowledge m, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientAnimBridge.applyCardKnowledge(m.entityId, m.cardCode)));
            ctx.setPacketHandled(true);
        }
    }

    public record CardMarks(int[] chain, int[] target, int[] chainAddrs, int[] targetAddrs) {
        static void encode(ModNetwork.CardMarks message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.chain);
            buffer.writeVarIntArray(message.target);
            buffer.writeVarIntArray(message.chainAddrs);
            buffer.writeVarIntArray(message.targetAddrs);
        }

        static ModNetwork.CardMarks decode(FriendlyByteBuf buffer) {
            return new ModNetwork.CardMarks(buffer.readVarIntArray(), buffer.readVarIntArray(), buffer.readVarIntArray(), buffer.readVarIntArray());
        }

        static void handle(ModNetwork.CardMarks message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(
                () -> DistExecutor.unsafeRunWhenOn(
                    Dist.CLIENT, () -> () -> ClientCardMarkers.apply(message.chain(), message.target(), message.chainAddrs(), message.targetAddrs())
                )
            );
            ctx.setPacketHandled(true);
        }
    }

    public record ChainChoice(int[] codes, boolean forced, String timing, boolean opening, int[] addrs, int viewerSeat, String[] effectTexts) {
        public ChainChoice(int[] codes, boolean forced, String timing, boolean opening, int[] addrs, int viewerSeat) {
            this(codes, forced, timing, opening, addrs, viewerSeat, new String[0]);
        }

        static void encode(ModNetwork.ChainChoice message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.codes);
            buffer.writeBoolean(message.forced);
            buffer.writeUtf(message.timing == null ? "" : message.timing, 192);
            buffer.writeBoolean(message.opening);
            buffer.writeVarIntArray(message.addrs);
            buffer.writeVarInt(message.viewerSeat);
            String[] texts = message.effectTexts == null ? new String[0] : message.effectTexts;
            buffer.writeVarInt(texts.length);

            for (String text : texts) {
                buffer.writeUtf(text == null ? "" : text, 2048);
            }
        }

        static ModNetwork.ChainChoice decode(FriendlyByteBuf buffer) {
            int[] codes = buffer.readVarIntArray();
            boolean forced = buffer.readBoolean();
            String timing = buffer.readUtf(192);
            boolean opening = buffer.readBoolean();
            int[] addrs = buffer.readVarIntArray();
            int viewerSeat = buffer.readVarInt();
            int count = buffer.readVarInt();
            String[] texts = new String[count];

            for (int i = 0; i < count; i++) {
                texts[i] = buffer.readUtf(2048);
            }

            return new ModNetwork.ChainChoice(codes, forced, timing, opening, addrs, viewerSeat, texts);
        }

        static void handle(ModNetwork.ChainChoice message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientPacketScreens.openChainChoice(message)));
            ctx.setPacketHandled(true);
        }
    }

    public record ChainChoose(int index) {
        static void encode(ModNetwork.ChainChoose message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.index + 1);
        }

        static ModNetwork.ChainChoose decode(FriendlyByteBuf buffer) {
            return new ModNetwork.ChainChoose(buffer.readVarInt() - 1);
        }

        static void handle(ModNetwork.ChainChoose message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions(player).answerChain(player, message.index());
                }

                ctx.setPacketHandled(true);
            });
        }
    }

    public record ChainState(int fieldEntityId, int[] codes, int resolving) {
        static void encode(ModNetwork.ChainState message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.fieldEntityId);
            buffer.writeVarIntArray(message.codes);
            buffer.writeVarInt(message.resolving);
        }

        static ModNetwork.ChainState decode(FriendlyByteBuf buffer) {
            return new ModNetwork.ChainState(buffer.readVarInt(), buffer.readVarIntArray(), buffer.readVarInt());
        }

        static void handle(ModNetwork.ChainState message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(
                () -> DistExecutor.unsafeRunWhenOn(
                    Dist.CLIENT, () -> () -> ClientChainState.apply(message.fieldEntityId(), message.codes(), message.resolving())
                )
            );
            ctx.setPacketHandled(true);
        }
    }

    public record DecisionAnswer(int value) {
        static void encode(ModNetwork.DecisionAnswer message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.value);
        }

        static ModNetwork.DecisionAnswer decode(FriendlyByteBuf buffer) {
            return new ModNetwork.DecisionAnswer(buffer.readVarInt());
        }

        static void handle(ModNetwork.DecisionAnswer message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions(player).answerDecision(player, message.value());
                }

                ctx.setPacketHandled(true);
            });
        }
    }

    public record DecisionPrompt(int function, String cardName, String question) {
        static void encode(ModNetwork.DecisionPrompt message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.function);
            buffer.writeUtf(message.cardName == null ? "" : message.cardName, 128);
            buffer.writeUtf(message.question == null ? "" : message.question, 256);
        }

        static ModNetwork.DecisionPrompt decode(FriendlyByteBuf buffer) {
            return new ModNetwork.DecisionPrompt(buffer.readVarInt(), buffer.readUtf(128), buffer.readUtf(256));
        }

        static void handle(ModNetwork.DecisionPrompt message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientPacketScreens.openDecision(message)));
            ctx.setPacketHandled(true);
        }
    }

    public record DuelAnimation(
        UUID sessionId,
        int fieldId,
        long eventId,
        String kind,
        int cardCode,
        boolean faceUp,
        Vec3 from,
        Vec3 to,
        float fromYaw,
        float toYaw,
        float fromPitch,
        float toPitch,
        boolean fromDefense,
        boolean toDefense,
        int durationMs,
        int delayMs,
        int hideEntityId,
        float fieldYaw
    ) {
        static void encode(ModNetwork.DuelAnimation m, FriendlyByteBuf b) {
            b.writeUUID(m.sessionId);
            b.writeVarInt(m.fieldId);
            b.writeLong(m.eventId);
            b.writeUtf(m.kind, 32);
            b.writeVarInt(m.cardCode);
            b.writeBoolean(m.faceUp);
            writePoint(b, m.from);
            writePoint(b, m.to);
            b.writeFloat(m.fromYaw);
            b.writeFloat(m.toYaw);
            b.writeFloat(m.fromPitch);
            b.writeFloat(m.toPitch);
            b.writeBoolean(m.fromDefense);
            b.writeBoolean(m.toDefense);
            b.writeVarInt(m.durationMs);
            b.writeVarInt(m.delayMs);
            b.writeVarInt(m.hideEntityId);
            b.writeFloat(m.fieldYaw);
        }

        static ModNetwork.DuelAnimation decode(FriendlyByteBuf b) {
            return new ModNetwork.DuelAnimation(
                b.readUUID(),
                b.readVarInt(),
                b.readLong(),
                b.readUtf(32),
                b.readVarInt(),
                b.readBoolean(),
                readPoint(b),
                readPoint(b),
                b.readFloat(),
                b.readFloat(),
                b.readFloat(),
                b.readFloat(),
                b.readBoolean(),
                b.readBoolean(),
                b.readVarInt(),
                b.readVarInt(),
                b.readVarInt(),
                b.readFloat()
            );
        }

        private static void writePoint(FriendlyByteBuf b, Vec3 p) {
            b.writeDouble(p.x);
            b.writeDouble(p.y);
            b.writeDouble(p.z);
        }

        private static Vec3 readPoint(FriendlyByteBuf b) {
            return new Vec3(b.readDouble(), b.readDouble(), b.readDouble());
        }

        static void handle(ModNetwork.DuelAnimation m, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientAnimBridge.apply(m)));
            ctx.setPacketHandled(true);
        }
    }

    public record DuelAnimationClear(UUID sessionId) {
        static void encode(ModNetwork.DuelAnimationClear m, FriendlyByteBuf b) {
            b.writeUUID(m.sessionId);
        }

        static ModNetwork.DuelAnimationClear decode(FriendlyByteBuf b) {
            return new ModNetwork.DuelAnimationClear(b.readUUID());
        }

        static void handle(ModNetwork.DuelAnimationClear m, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> {
                ClientAnimBridge.clearSession(m.sessionId);
                ClientDuelAudio.clearSession(m.sessionId);
            }));
            ctx.setPacketHandled(true);
        }
    }

    public record DuelSound(
        UUID sessionId, int fieldId, String key, Vec3 at, int delayMs, int durationMs, float volume, float pitch, boolean loop, boolean stop
    ) {
        public static ModNetwork.DuelSound once(UUID session, int fieldId, String key, Vec3 at) {
            return new ModNetwork.DuelSound(session, fieldId, key, at, 0, 0, 1.0F, 1.0F, false, false);
        }

        public static ModNetwork.DuelSound bgm(UUID session, int fieldId, int delayMs) {
            return new ModNetwork.DuelSound(session, fieldId, "BGM_DUEL", Vec3.ZERO, delayMs, 0, 1.0F, 1.0F, true, false);
        }

        public static ModNetwork.DuelSound stop(UUID session, int fieldId, String key) {
            return new ModNetwork.DuelSound(session, fieldId, key, Vec3.ZERO, 0, 0, 1.0F, 1.0F, false, true);
        }

        static void encode(ModNetwork.DuelSound m, FriendlyByteBuf b) {
            b.writeUUID(m.sessionId);
            b.writeVarInt(m.fieldId);
            b.writeUtf(m.key, 192);
            b.writeDouble(m.at.x);
            b.writeDouble(m.at.y);
            b.writeDouble(m.at.z);
            b.writeVarInt(m.delayMs);
            b.writeVarInt(m.durationMs);
            b.writeFloat(m.volume);
            b.writeFloat(m.pitch);
            b.writeBoolean(m.loop);
            b.writeBoolean(m.stop);
        }

        static ModNetwork.DuelSound decode(FriendlyByteBuf b) {
            return new ModNetwork.DuelSound(
                b.readUUID(),
                b.readVarInt(),
                b.readUtf(192),
                new Vec3(b.readDouble(), b.readDouble(), b.readDouble()),
                b.readVarInt(),
                b.readVarInt(),
                b.readFloat(),
                b.readFloat(),
                b.readBoolean(),
                b.readBoolean()
            );
        }

        static void handle(ModNetwork.DuelSound m, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientDuelAudio.play(m)));
            ctx.setPacketHandled(true);
        }
    }

    public record EffectChoices(long seq, int code, int[] values, String[] labels) {
        static void encode(ModNetwork.EffectChoices message, FriendlyByteBuf buffer) {
            buffer.writeLong(message.seq);
            buffer.writeVarInt(message.code);
            buffer.writeVarIntArray(message.values);
            buffer.writeVarInt(message.labels.length);

            for (String label : message.labels) {
                buffer.writeUtf(label, 2048);
            }
        }

        static ModNetwork.EffectChoices decode(FriendlyByteBuf buffer) {
            long seq = buffer.readLong();
            int code = buffer.readVarInt();
            int[] values = buffer.readVarIntArray();
            int count = buffer.readVarInt();
            if (count >= 0 && count <= 256) {
                String[] labels = new String[count];

                for (int i = 0; i < count; i++) {
                    labels[i] = buffer.readUtf(2048);
                }

                return new ModNetwork.EffectChoices(seq, code, values, labels);
            } else {
                throw new IllegalArgumentException("Too many effects");
            }
        }

        static void handle(ModNetwork.EffectChoices message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientPacketScreens.openEffects(message)));
            ctx.setPacketHandled(true);
        }
    }

    public record EffectPick(long seq, int value) {
        static void encode(ModNetwork.EffectPick message, FriendlyByteBuf buffer) {
            buffer.writeLong(message.seq);
            buffer.writeInt(message.value);
        }

        static ModNetwork.EffectPick decode(FriendlyByteBuf buffer) {
            return new ModNetwork.EffectPick(buffer.readLong(), buffer.readInt());
        }

        static void handle(ModNetwork.EffectPick message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                if (ctx.getSender() != null) {
                    MDProMod.sessions(ctx.getSender()).answerEffect(ctx.getSender(), message.seq, message.value);
                }
            });
            ctx.setPacketHandled(true);
        }
    }

    public record FieldAction(int action, int value) {
        static void encode(ModNetwork.FieldAction message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.action);
            buffer.writeVarInt(message.value);
        }

        static ModNetwork.FieldAction decode(FriendlyByteBuf buffer) {
            return new ModNetwork.FieldAction(buffer.readVarInt(), buffer.readVarInt());
        }

        static void handle(ModNetwork.FieldAction message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions(player).handleFieldAction(player, message.action(), message.value());
                }

                ctx.setPacketHandled(true);
            });
        }
    }

    public record FieldClick(String controlId) {
        static void encode(ModNetwork.FieldClick message, FriendlyByteBuf buffer) {
            buffer.writeUtf(message.controlId == null ? "" : message.controlId, 64);
        }

        static ModNetwork.FieldClick decode(FriendlyByteBuf buffer) {
            return new ModNetwork.FieldClick(buffer.readUtf(64));
        }

        static void handle(ModNetwork.FieldClick message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.duels().handleFieldClick(player, message.controlId());
                }

                ctx.setPacketHandled(true);
            });
        }
    }

    public record HandSync(int[] cards, boolean active, boolean yourTurn) {
        static void encode(ModNetwork.HandSync message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.cards);
            buffer.writeBoolean(message.active);
            buffer.writeBoolean(message.yourTurn);
        }

        static ModNetwork.HandSync decode(FriendlyByteBuf buffer) {
            return new ModNetwork.HandSync(buffer.readVarIntArray(), buffer.readBoolean(), buffer.readBoolean());
        }

        static void handle(ModNetwork.HandSync message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientHandBridge.apply(message)));
            ctx.setPacketHandled(true);
        }
    }

    public record MusicSettingsAction(String moment, String track) {
        static void encode(ModNetwork.MusicSettingsAction m, FriendlyByteBuf b) {
            b.writeUtf(m.moment == null ? "" : m.moment, 32);
            b.writeUtf(m.track == null ? "" : m.track, 192);
        }

        static ModNetwork.MusicSettingsAction decode(FriendlyByteBuf b) {
            return new ModNetwork.MusicSettingsAction(b.readUtf(32), b.readUtf(192));
        }

        static void handle(ModNetwork.MusicSettingsAction m, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null && DuelBgmRules.isMoment(m.moment)) {
                    ItemStack stack = MusicSettingsItem.findSettings(player);
                    if (stack != null) {
                        MusicSettingsItem.setTrack(stack, m.moment, m.track);
                        player.inventoryMenu.broadcastChanges();
                    }

                    player.displayClientMessage(Component.literal(DuelBgmRules.momentLabel(m.moment) + " 音乐已设为：" + DuelBgmTracks.label(m.track)), true);
                }
            });
            ctx.setPacketHandled(true);
        }
    }

    public record MusicSettingsOpen(String[] selected, String[] keys, String[] labels, boolean open) {
        public static ModNetwork.MusicSettingsOpen screen(String[] selected, String[] keys, String[] labels) {
            return new ModNetwork.MusicSettingsOpen(selected, keys, labels, true);
        }

        public static ModNetwork.MusicSettingsOpen sync(String[] selected) {
            return new ModNetwork.MusicSettingsOpen(selected, new String[0], new String[0], false);
        }

        static void encode(ModNetwork.MusicSettingsOpen m, FriendlyByteBuf b) {
            b.writeVarInt(m.selected.length);

            for (String value : m.selected) {
                b.writeUtf(value == null ? "" : value, 192);
            }

            b.writeVarInt(m.keys.length);

            for (String key : m.keys) {
                b.writeUtf(key, 192);
            }

            b.writeVarInt(m.labels.length);

            for (String label : m.labels) {
                b.writeUtf(label, 256);
            }

            b.writeBoolean(m.open);
        }

        static ModNetwork.MusicSettingsOpen decode(FriendlyByteBuf b) {
            String[] selected = new String[b.readVarInt()];

            for (int i = 0; i < selected.length; i++) {
                selected[i] = b.readUtf(192);
            }

            String[] keys = new String[b.readVarInt()];

            for (int i = 0; i < keys.length; i++) {
                keys[i] = b.readUtf(192);
            }

            String[] labels = new String[b.readVarInt()];

            for (int i = 0; i < labels.length; i++) {
                labels[i] = b.readUtf(256);
            }

            return new ModNetwork.MusicSettingsOpen(selected, keys, labels, b.readBoolean());
        }

        static void handle(ModNetwork.MusicSettingsOpen m, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientMusicSettingsBridge.open(m)));
            ctx.setPacketHandled(true);
        }
    }

    public record OpenCard(int code) {
        static void encode(ModNetwork.OpenCard message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.code);
        }

        static ModNetwork.OpenCard decode(FriendlyByteBuf buffer) {
            return new ModNetwork.OpenCard(buffer.readVarInt());
        }

        static void handle(ModNetwork.OpenCard message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientHandBridge.openCard(message.code)));
            ctx.setPacketHandled(true);
        }
    }

    public record OptionAnswer(int value) {
        static void encode(ModNetwork.OptionAnswer message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.value);
        }

        static ModNetwork.OptionAnswer decode(FriendlyByteBuf buffer) {
            return new ModNetwork.OptionAnswer(buffer.readVarInt());
        }

        static void handle(ModNetwork.OptionAnswer message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions(player).answerOption(player, message.value());
                }

                ctx.setPacketHandled(true);
            });
        }
    }

    public record OptionList(int[] values, String[] labels) {
        static void encode(ModNetwork.OptionList message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.values);
            buffer.writeVarInt(message.labels.length);

            for (String label : message.labels) {
                buffer.writeUtf(label, 2048);
            }
        }

        static ModNetwork.OptionList decode(FriendlyByteBuf buffer) {
            int[] values = buffer.readVarIntArray();
            String[] labels = new String[buffer.readVarInt()];

            for (int i = 0; i < labels.length; i++) {
                labels[i] = buffer.readUtf(2048);
            }

            return new ModNetwork.OptionList(values, labels);
        }

        static void handle(ModNetwork.OptionList message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientPacketScreens.openOptions(message)));
            ctx.setPacketHandled(true);
        }
    }

    public record PileAct(int location, int sequence, boolean summon) {
        static void encode(ModNetwork.PileAct message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.location);
            buffer.writeVarInt(message.sequence);
            buffer.writeBoolean(message.summon);
        }

        static ModNetwork.PileAct decode(FriendlyByteBuf buffer) {
            return new ModNetwork.PileAct(buffer.readVarInt(), buffer.readVarInt(), buffer.readBoolean());
        }

        static void handle(ModNetwork.PileAct message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions(player).performPileAct(player, message.location(), message.sequence(), message.summon());
                }

                ctx.setPacketHandled(true);
            });
        }
    }

    public record PileHint(int fieldEntityId, int[] zones) {
        static void encode(ModNetwork.PileHint message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.fieldEntityId);
            buffer.writeVarIntArray(message.zones);
        }

        static ModNetwork.PileHint decode(FriendlyByteBuf buffer) {
            return new ModNetwork.PileHint(buffer.readVarInt(), buffer.readVarIntArray());
        }

        static void handle(ModNetwork.PileHint message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientPileHints.apply(message.fieldEntityId(), message.zones())));
            ctx.setPacketHandled(true);
        }
    }

    public record PileView(int location, int[] activateSeqs, int[] activateCodes, int[] summonSeqs, int[] summonCodes, int[] allCodes, int hidden, UUID fieldId) {
        public PileView(int location, int[] activateSeqs, int[] activateCodes, int[] summonSeqs, int[] summonCodes, int[] allCodes, int hidden) {
            this(location, activateSeqs, activateCodes, summonSeqs, summonCodes, allCodes, hidden, null);
        }

        static void encode(ModNetwork.PileView message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.location);
            buffer.writeVarIntArray(message.activateSeqs);
            buffer.writeVarIntArray(message.activateCodes);
            buffer.writeVarIntArray(message.summonSeqs);
            buffer.writeVarIntArray(message.summonCodes);
            buffer.writeVarIntArray(message.allCodes);
            buffer.writeVarInt(message.hidden);
            buffer.writeBoolean(message.fieldId != null);
            if (message.fieldId != null) {
                buffer.writeUUID(message.fieldId);
            }
        }

        static ModNetwork.PileView decode(FriendlyByteBuf buffer) {
            int location = buffer.readVarInt();
            int[] activations = buffer.readVarIntArray();
            int[] activateCodes = buffer.readVarIntArray();
            int[] summons = buffer.readVarIntArray();
            int[] summonCodes = buffer.readVarIntArray();
            int[] all = buffer.readVarIntArray();
            int hidden = buffer.readVarInt();
            UUID fieldId = buffer.readBoolean() ? buffer.readUUID() : null;
            return new ModNetwork.PileView(location, activations, activateCodes, summons, summonCodes, all, hidden, fieldId);
        }

        static void handle(ModNetwork.PileView message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientPacketScreens.openPile(message)));
            ctx.setPacketHandled(true);
        }
    }

    public record PlaceHint(int fieldEntityId, int[] places) {
        static void encode(ModNetwork.PlaceHint message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.fieldEntityId);
            buffer.writeVarIntArray(message.places);
        }

        static ModNetwork.PlaceHint decode(FriendlyByteBuf buffer) {
            return new ModNetwork.PlaceHint(buffer.readVarInt(), buffer.readVarIntArray());
        }

        static void handle(ModNetwork.PlaceHint message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientPlaceHints.apply(message.fieldEntityId(), message.places())));
            ctx.setPacketHandled(true);
        }
    }

    public record PositionAnswer(int value) {
        static void encode(ModNetwork.PositionAnswer message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.value);
        }

        static ModNetwork.PositionAnswer decode(FriendlyByteBuf buffer) {
            return new ModNetwork.PositionAnswer(buffer.readVarInt());
        }

        static void handle(ModNetwork.PositionAnswer message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions(player).answerPosition(player, message.value());
                }

                ctx.setPacketHandled(true);
            });
        }
    }

    public record PositionList(int[] values, String[] labels) {
        static void encode(ModNetwork.PositionList message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.values);
            buffer.writeVarInt(message.labels.length);

            for (String label : message.labels) {
                buffer.writeUtf(label, 32);
            }
        }

        static ModNetwork.PositionList decode(FriendlyByteBuf buffer) {
            int[] values = buffer.readVarIntArray();
            String[] labels = new String[buffer.readVarInt()];

            for (int i = 0; i < labels.length; i++) {
                labels[i] = buffer.readUtf(32);
            }

            return new ModNetwork.PositionList(values, labels);
        }

        static void handle(ModNetwork.PositionList message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientPacketScreens.openPosition(message)));
            ctx.setPacketHandled(true);
        }
    }

    public record PromptResend() {
        static void encode(ModNetwork.PromptResend message, FriendlyByteBuf buffer) {
        }

        static ModNetwork.PromptResend decode(FriendlyByteBuf buffer) {
            return new ModNetwork.PromptResend();
        }

        static void handle(ModNetwork.PromptResend message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions(player).requestPromptResend(player);
                }

                ctx.setPacketHandled(true);
            });
        }
    }

    public record RpsPick(int hand) {
        static void encode(ModNetwork.RpsPick message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.hand);
        }

        static ModNetwork.RpsPick decode(FriendlyByteBuf buffer) {
            return new ModNetwork.RpsPick(buffer.readVarInt());
        }

        static void handle(ModNetwork.RpsPick message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions(player).answerRps(player, message.hand());
                }

                ctx.setPacketHandled(true);
            });
        }
    }

    public record RpsPrompt() {
        static void encode(ModNetwork.RpsPrompt message, FriendlyByteBuf buffer) {
        }

        static ModNetwork.RpsPrompt decode(FriendlyByteBuf buffer) {
            return new ModNetwork.RpsPrompt();
        }

        static void handle(ModNetwork.RpsPrompt message, Supplier<Context> context) {
            Context ctx = context.get();
            ModLog.info("RpsPrompt received on client");
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientPacketScreens.openRps()));
            ctx.setPacketHandled(true);
        }
    }

    public record SelectChoose(int[] indices) {
        static void encode(ModNetwork.SelectChoose message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.indices);
        }

        static ModNetwork.SelectChoose decode(FriendlyByteBuf buffer) {
            return new ModNetwork.SelectChoose(buffer.readVarIntArray());
        }

        static void handle(ModNetwork.SelectChoose message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions(player).answerSelect(player, message.indices());
                }

                ctx.setPacketHandled(true);
            });
        }
    }

    public record SelectHint(int[] selectable, int[] selected) {
        static void encode(ModNetwork.SelectHint message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.selectable);
            buffer.writeVarIntArray(message.selected);
        }

        static ModNetwork.SelectHint decode(FriendlyByteBuf buffer) {
            return new ModNetwork.SelectHint(buffer.readVarIntArray(), buffer.readVarIntArray());
        }

        static void handle(ModNetwork.SelectHint message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientCardSelect.apply(message.selectable(), message.selected())));
            ctx.setPacketHandled(true);
        }
    }

    public record SelectList(int[] codes, int min, int max, int[] addrs, int viewerSeat, String hint, boolean cancelable, boolean fieldFirst) {
        public SelectList(int[] codes, int min, int max, int[] addrs, int viewerSeat) {
            this(codes, min, max, addrs, viewerSeat, "");
        }

        public SelectList(int[] codes, int min, int max, int[] addrs, int viewerSeat, String hint) {
            this(codes, min, max, addrs, viewerSeat, hint, false, false);
        }

        static void encode(ModNetwork.SelectList message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.codes);
            buffer.writeVarInt(message.min);
            buffer.writeVarInt(message.max);
            buffer.writeVarIntArray(message.addrs);
            buffer.writeVarInt(message.viewerSeat);
            buffer.writeUtf(message.hint == null ? "" : message.hint, 256);
            buffer.writeBoolean(message.cancelable);
            buffer.writeBoolean(message.fieldFirst);
        }

        static ModNetwork.SelectList decode(FriendlyByteBuf buffer) {
            return new ModNetwork.SelectList(
                buffer.readVarIntArray(),
                buffer.readVarInt(),
                buffer.readVarInt(),
                buffer.readVarIntArray(),
                buffer.readVarInt(),
                buffer.readUtf(256),
                buffer.readBoolean(),
                buffer.readBoolean()
            );
        }

        static void handle(ModNetwork.SelectList message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientPacketScreens.openSelectCard(message)));
            ctx.setPacketHandled(true);
        }
    }

    public record SortList(int[] codes) {
        static void encode(ModNetwork.SortList message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.codes);
        }

        static ModNetwork.SortList decode(FriendlyByteBuf buffer) {
            return new ModNetwork.SortList(buffer.readVarIntArray());
        }

        static void handle(ModNetwork.SortList message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientPacketScreens.openSortCard(message)));
            ctx.setPacketHandled(true);
        }
    }

    public record SortOrder(int[] order) {
        static void encode(ModNetwork.SortOrder message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.order);
        }

        static ModNetwork.SortOrder decode(FriendlyByteBuf buffer) {
            return new ModNetwork.SortOrder(buffer.readVarIntArray());
        }

        static void handle(ModNetwork.SortOrder message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions(player).answerSort(player, message.order());
                }

                ctx.setPacketHandled(true);
            });
        }
    }

    public record SumAnswer(long seq, int[] chosen) {
        static void encode(ModNetwork.SumAnswer message, FriendlyByteBuf buffer) {
            buffer.writeLong(message.seq);
            buffer.writeVarIntArray(message.chosen == null ? new int[0] : message.chosen);
        }

        static ModNetwork.SumAnswer decode(FriendlyByteBuf buffer) {
            return new ModNetwork.SumAnswer(buffer.readLong(), buffer.readVarIntArray());
        }

        static void handle(ModNetwork.SumAnswer message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                if (ctx.getSender() != null) {
                    MDProMod.sessions(ctx.getSender()).answerSum(ctx.getSender(), message.seq, message.chosen);
                }
            });
            ctx.setPacketHandled(true);
        }
    }

    public record SumChoice(
        long seq,
        boolean overFlow,
        int target,
        int min,
        int max,
        int viewerSeat,
        int[] mustCodes,
        int[] mustAddrs,
        int[] mustLevel1,
        int[] mustLevel2,
        int[] codes,
        int[] addrs,
        int[] level1,
        int[] level2
    ) {
        static void encode(ModNetwork.SumChoice message, FriendlyByteBuf buffer) {
            buffer.writeLong(message.seq);
            buffer.writeBoolean(message.overFlow);
            buffer.writeVarInt(message.target);
            buffer.writeVarInt(message.min);
            buffer.writeVarInt(message.max);
            buffer.writeVarInt(message.viewerSeat);
            buffer.writeVarIntArray(message.mustCodes);
            buffer.writeVarIntArray(message.mustAddrs);
            buffer.writeVarIntArray(message.mustLevel1);
            buffer.writeVarIntArray(message.mustLevel2);
            buffer.writeVarIntArray(message.codes);
            buffer.writeVarIntArray(message.addrs);
            buffer.writeVarIntArray(message.level1);
            buffer.writeVarIntArray(message.level2);
        }

        static ModNetwork.SumChoice decode(FriendlyByteBuf buffer) {
            return new ModNetwork.SumChoice(
                buffer.readLong(),
                buffer.readBoolean(),
                buffer.readVarInt(),
                buffer.readVarInt(),
                buffer.readVarInt(),
                buffer.readVarInt(),
                buffer.readVarIntArray(),
                buffer.readVarIntArray(),
                buffer.readVarIntArray(),
                buffer.readVarIntArray(),
                buffer.readVarIntArray(),
                buffer.readVarIntArray(),
                buffer.readVarIntArray(),
                buffer.readVarIntArray()
            );
        }

        static void handle(ModNetwork.SumChoice message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientPacketScreens.openSumChoice(message)));
            ctx.setPacketHandled(true);
        }
    }

    public record SummonEffect(int fieldEntityId, int kind, int phase, int code, int materialCount, double x, double y, double z, int durationMs) {
        static void encode(ModNetwork.SummonEffect message, FriendlyByteBuf buffer) {
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

        static ModNetwork.SummonEffect decode(FriendlyByteBuf buffer) {
            return new ModNetwork.SummonEffect(
                buffer.readVarInt(),
                buffer.readVarInt(),
                buffer.readVarInt(),
                buffer.readVarInt(),
                buffer.readVarInt(),
                buffer.readDouble(),
                buffer.readDouble(),
                buffer.readDouble(),
                buffer.readVarInt()
            );
        }

        static void handle(ModNetwork.SummonEffect message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> {
                if (message.phase() == 2) {
                    ClientCutinEffects.play(message);
                } else {
                    ClientSummonEffects.play(message);
                }
            }));
            ctx.setPacketHandled(true);
        }
    }

    public record TpPick(boolean first) {
        static void encode(ModNetwork.TpPick message, FriendlyByteBuf buffer) {
            buffer.writeBoolean(message.first);
        }

        static ModNetwork.TpPick decode(FriendlyByteBuf buffer) {
            return new ModNetwork.TpPick(buffer.readBoolean());
        }

        static void handle(ModNetwork.TpPick message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions(player).answerTp(player, message.first());
                }

                ctx.setPacketHandled(true);
            });
        }
    }

    public record TpPrompt() {
        static void encode(ModNetwork.TpPrompt message, FriendlyByteBuf buffer) {
        }

        static ModNetwork.TpPrompt decode(FriendlyByteBuf buffer) {
            return new ModNetwork.TpPrompt();
        }

        static void handle(ModNetwork.TpPrompt message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientPacketScreens.openTp()));
            ctx.setPacketHandled(true);
        }
    }

    public record UnselectList(
        int[] selectCodes, int[] chosenCodes, int min, int max, boolean finishable, int[] selectAddrs, int[] chosenAddrs, int viewerSeat, String hint
    ) {
        public UnselectList(int[] selectCodes, int[] chosenCodes, int min, int max, boolean finishable, int[] selectAddrs, int[] chosenAddrs, int viewerSeat) {
            this(selectCodes, chosenCodes, min, max, finishable, selectAddrs, chosenAddrs, viewerSeat, "");
        }

        static void encode(ModNetwork.UnselectList message, FriendlyByteBuf buffer) {
            buffer.writeVarIntArray(message.selectCodes);
            buffer.writeVarIntArray(message.chosenCodes);
            buffer.writeVarInt(message.min);
            buffer.writeVarInt(message.max);
            buffer.writeBoolean(message.finishable);
            buffer.writeVarIntArray(message.selectAddrs);
            buffer.writeVarIntArray(message.chosenAddrs);
            buffer.writeVarInt(message.viewerSeat);
            buffer.writeUtf(message.hint == null ? "" : message.hint, 256);
        }

        static ModNetwork.UnselectList decode(FriendlyByteBuf buffer) {
            return new ModNetwork.UnselectList(
                buffer.readVarIntArray(),
                buffer.readVarIntArray(),
                buffer.readVarInt(),
                buffer.readVarInt(),
                buffer.readBoolean(),
                buffer.readVarIntArray(),
                buffer.readVarIntArray(),
                buffer.readVarInt(),
                buffer.readUtf(256)
            );
        }

        static void handle(ModNetwork.UnselectList message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> DistExecutor.unsafeRunWhenOn(Dist.CLIENT, () -> () -> ClientPacketScreens.openUnselectCard(message)));
            ctx.setPacketHandled(true);
        }
    }

    public record UnselectPick(int index) {
        static void encode(ModNetwork.UnselectPick message, FriendlyByteBuf buffer) {
            buffer.writeVarInt(message.index + 1);
        }

        static ModNetwork.UnselectPick decode(FriendlyByteBuf buffer) {
            return new ModNetwork.UnselectPick(buffer.readVarInt() - 1);
        }

        static void handle(ModNetwork.UnselectPick message, Supplier<Context> context) {
            Context ctx = context.get();
            ctx.enqueueWork(() -> {
                ServerPlayer player = ctx.getSender();
                if (player != null) {
                    MDProMod.sessions(player).unselectPick(player, message.index());
                }

                ctx.setPacketHandled(true);
            });
        }
    }
}
