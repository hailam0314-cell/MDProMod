package net.mdpromod;

import java.util.List;
import net.minecraft.ChatFormatting;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.InteractionResultHolder;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.TooltipFlag;
import net.minecraft.world.item.Item.Properties;
import net.minecraft.world.level.Level;

public final class MusicSettingsItem extends Item {
    private static final String TAG_PREFIX = "Music.";
    private static final String TAG_LEGACY = "MusicTrack";

    public MusicSettingsItem(Properties properties) {
        super(properties);
    }

    public InteractionResultHolder<ItemStack> use(Level level, Player player, InteractionHand hand) {
        ItemStack stack = player.getItemInHand(hand);
        if (level.isClientSide) {
            return InteractionResultHolder.success(stack);
        }

        if (player instanceof ServerPlayer serverPlayer) {
            ModNetwork.sendTo(serverPlayer, ModNetwork.MusicSettingsOpen.screen(snapshot(stack), DuelBgmTracks.keys(), DuelBgmTracks.labels()));
        }

        return InteractionResultHolder.consume(stack);
    }

    public Component getName(ItemStack stack) {
        Component base = Component.translatable("item.mdpromod.music_settings");
        String duel = getTrack(stack, "duel");
        return base.copy().append(Component.literal(" · " + DuelBgmTracks.label(duel)));
    }

    public void appendHoverText(ItemStack stack, Level level, List<Component> tooltip, TooltipFlag flag) {
        tooltip.add(Component.literal("各时机的背景音乐：").withStyle(ChatFormatting.GRAY));

        for (String moment : DuelBgmRules.MOMENTS) {
            tooltip.add(
                Component.literal("  " + DuelBgmRules.momentLabel(moment) + "：" + DuelBgmTracks.label(getTrack(stack, moment)))
                    .withStyle(ChatFormatting.DARK_GRAY)
            );
        }

        tooltip.add(Component.literal("右键选择（共 " + DuelBgmTracks.size() + " 首，可试听）").withStyle(ChatFormatting.DARK_GRAY));
    }

    public static String getTrack(ItemStack stack, String moment) {
        if (stack != null && stack.getItem() instanceof MusicSettingsItem && DuelBgmRules.isMoment(moment)) {
            CompoundTag tag = stack.getTag();
            if (tag != null && tag.contains("Music." + moment)) {
                return DuelBgmRules.localChoice(tag.getString("Music." + moment));
            } else {
                return "duel".equals(moment) && tag != null && tag.contains("MusicTrack")
                    ? DuelBgmRules.localChoice(tag.getString("MusicTrack"))
                    : DuelBgmRules.defaultTrack(moment);
            }
        } else {
            return DuelBgmRules.defaultTrack(moment);
        }
    }

    public static void setTrack(ItemStack stack, String moment, String track) {
        if (stack != null && stack.getItem() instanceof MusicSettingsItem && DuelBgmRules.isMoment(moment)) {
            CompoundTag tag = stack.getOrCreateTag();
            tag.putString("Music." + moment, DuelBgmRules.localChoice(track));
            tag.remove("MusicTrack");
        }
    }

    public static String[] snapshot(ItemStack stack) {
        String[] out = new String[DuelBgmRules.MOMENTS.size()];

        for (int i = 0; i < out.length; i++) {
            out[i] = getTrack(stack, DuelBgmRules.MOMENTS.get(i));
        }

        return out;
    }

    public static ItemStack findSettings(Player player) {
        if (player == null) {
            return null;
        }

        if (player.getMainHandItem().getItem() instanceof MusicSettingsItem) {
            return player.getMainHandItem();
        }

        if (player.getOffhandItem().getItem() instanceof MusicSettingsItem) {
            return player.getOffhandItem();
        }

        for (int i = 0; i < player.getInventory().getContainerSize(); i++) {
            ItemStack stack = player.getInventory().getItem(i);
            if (stack.getItem() instanceof MusicSettingsItem) {
                return stack;
            }
        }

        return null;
    }
}
