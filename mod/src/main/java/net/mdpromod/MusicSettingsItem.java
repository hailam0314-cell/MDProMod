package net.mdpromod;

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
import net.minecraft.world.level.Level;

import java.util.List;

/**
 * 「音乐设置」物品：每个「时机」可以各选一首曲子，选择存在物品自己的 NBT 里。
 * 服务端只负责保存与下发，具体放哪首由各客户端自己决定。
 */
public final class MusicSettingsItem extends Item {

    /** NBT：{@code Music.<时机> = 曲目键}。 */
    private static final String TAG_PREFIX = "Music.";
    /** 旧版单曲目字段，读的时候当作「决斗」用。 */
    private static final String TAG_LEGACY = "MusicTrack";

    public MusicSettingsItem(Properties properties) {
        super(properties);
    }

    @Override
    public InteractionResultHolder<ItemStack> use(Level level, Player player, InteractionHand hand) {
        ItemStack stack = player.getItemInHand(hand);
        if (level.isClientSide) return InteractionResultHolder.success(stack);
        if (player instanceof ServerPlayer serverPlayer) {
            ModNetwork.sendTo(serverPlayer, ModNetwork.MusicSettingsOpen.screen(
                    snapshot(stack), DuelBgmTracks.keys(), DuelBgmTracks.labels()));
        }
        return InteractionResultHolder.consume(stack);
    }

    @Override
    public Component getName(ItemStack stack) {
        Component base = Component.translatable("item.mdpromod.music_settings");
        String duel = getTrack(stack, DuelBgmRules.MOMENT_DUEL);
        return base.copy().append(Component.literal(" · " + DuelBgmTracks.label(duel)));
    }

    @Override
    public void appendHoverText(ItemStack stack, Level level, List<Component> tooltip, TooltipFlag flag) {
        tooltip.add(Component.literal("各时机的背景音乐：").withStyle(ChatFormatting.GRAY));
        for (String moment : DuelBgmRules.MOMENTS) {
            tooltip.add(Component.literal("  " + DuelBgmRules.momentLabel(moment) + "："
                            + DuelBgmTracks.label(getTrack(stack, moment)))
                    .withStyle(ChatFormatting.DARK_GRAY));
        }
        tooltip.add(Component.literal("右键选择（共 " + DuelBgmTracks.size() + " 首，可试听）")
                .withStyle(ChatFormatting.DARK_GRAY));
    }

    /** 该时机的曲子。没存过给默认；明确选了「不播放」给空串——两者含义不同。 */
    public static String getTrack(ItemStack stack, String moment) {
        if (stack == null || !(stack.getItem() instanceof MusicSettingsItem)
                || !DuelBgmRules.isMoment(moment)) {
            return DuelBgmRules.defaultTrack(moment);
        }
        CompoundTag tag = stack.getTag();
        if (tag != null && tag.contains(TAG_PREFIX + moment)) {
            return DuelBgmRules.localChoice(tag.getString(TAG_PREFIX + moment));
        }
        // 旧存档：那会儿只有一首，就当作「决斗」
        if (DuelBgmRules.MOMENT_DUEL.equals(moment) && tag != null && tag.contains(TAG_LEGACY)) {
            return DuelBgmRules.localChoice(tag.getString(TAG_LEGACY));
        }
        return DuelBgmRules.defaultTrack(moment);
    }

    public static void setTrack(ItemStack stack, String moment, String track) {
        if (stack == null || !(stack.getItem() instanceof MusicSettingsItem)
                || !DuelBgmRules.isMoment(moment)) {
            return;
        }
        CompoundTag tag = stack.getOrCreateTag();
        tag.putString(TAG_PREFIX + moment, DuelBgmRules.localChoice(track));
        tag.remove(TAG_LEGACY);
    }

    /** 按 {@link DuelBgmRules#MOMENTS} 的顺序导出，给界面和同步包用。 */
    public static String[] snapshot(ItemStack stack) {
        String[] out = new String[DuelBgmRules.MOMENTS.size()];
        for (int i = 0; i < out.length; i++) {
            out[i] = getTrack(stack, DuelBgmRules.MOMENTS.get(i));
        }
        return out;
    }

    /** 玩家身上（手上、副手、背包）的那一件；没有返回 null。 */
    public static ItemStack findSettings(Player player) {
        if (player == null) return null;
        if (player.getMainHandItem().getItem() instanceof MusicSettingsItem) return player.getMainHandItem();
        if (player.getOffhandItem().getItem() instanceof MusicSettingsItem) return player.getOffhandItem();
        for (int i = 0; i < player.getInventory().getContainerSize(); i++) {
            ItemStack stack = player.getInventory().getItem(i);
            if (stack.getItem() instanceof MusicSettingsItem) return stack;
        }
        return null;
    }
}
