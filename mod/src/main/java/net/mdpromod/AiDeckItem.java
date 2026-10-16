package net.mdpromod;

import net.minecraft.ChatFormatting;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.InteractionResultHolder;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.TooltipFlag;
import net.minecraft.world.level.Level;

import java.nio.file.Path;
import java.util.List;

/**
 * 「人机卡组」物品：选一个 WindBot 人机，把它放到决斗盘的**远半场**就能和它开打。
 *
 * 选择存在物品自己的 NBT 里（键 + 中文标签），所以物品名不依赖运行时目录，
 * 客户端也能直接显示。右键打开的界面是**只读**的：只能换人机、看卡组，不能增删卡。
 */
public class AiDeckItem extends Item {

    public static final String TAG_KEY = "AiDeck";
    public static final String TAG_LABEL = "AiDeckLabel";
    /** 没选过时的默认人机。 */
    public static final String DEFAULT_KEY = "AI_BlueEyes";

    public AiDeckItem(Properties properties) {
        super(properties);
    }

    @Override
    public InteractionResultHolder<ItemStack> use(Level level, Player player, InteractionHand hand) {
        ItemStack stack = player.getItemInHand(hand);
        if (level.isClientSide) return InteractionResultHolder.success(stack);
        if (player instanceof ServerPlayer serverPlayer) {
            Path root = DuelSessionManager.runtimeRoot();
            ModNetwork.sendTo(serverPlayer, ModNetwork.AiDeckOpen.of(
                    getKey(stack), AiDeckCatalog.entries(root), root));
        }
        return InteractionResultHolder.consume(stack);
    }

    @Override
    public Component getName(ItemStack stack) {
        return Component.translatable("item.mdpromod.ai_deck")
                .copy().append(Component.literal(" · " + getLabel(stack)));
    }

    @Override
    public void appendHoverText(ItemStack stack, Level level,
                                List<Component> tooltip, TooltipFlag flag) {
        tooltip.add(Component.literal("人机：" + getLabel(stack)).withStyle(ChatFormatting.GRAY));
        String description = getDescription(stack);
        if (!description.isBlank()) {
            tooltip.add(Component.literal(description).withStyle(ChatFormatting.DARK_GRAY));
        }
        tooltip.add(Component.literal("右键换人机；把它放到决斗盘远半场开始对战")
                .withStyle(ChatFormatting.DARK_GRAY));
    }

    public static String getKey(ItemStack stack) {
        if (stack == null || !(stack.getItem() instanceof AiDeckItem)) return DEFAULT_KEY;
        var tag = stack.getTag();
        if (tag == null || !tag.contains(TAG_KEY)) return DEFAULT_KEY;
        String key = tag.getString(TAG_KEY);
        return key.isBlank() ? DEFAULT_KEY : key;
    }

    public static String getLabel(ItemStack stack) {
        if (stack != null && stack.getItem() instanceof AiDeckItem) {
            var tag = stack.getTag();
            if (tag != null && tag.contains(TAG_LABEL) && !tag.getString(TAG_LABEL).isBlank()) {
                return tag.getString(TAG_LABEL);
            }
        }
        String key = getKey(stack);
        return key.startsWith("AI_") ? key.substring(3) : key;
    }

    public static String getDescription(ItemStack stack) {
        if (stack != null && stack.getItem() instanceof AiDeckItem) {
            var tag = stack.getTag();
            if (tag != null && tag.contains("AiDeckDesc")) return tag.getString("AiDeckDesc");
        }
        return "";
    }

    public static void setSelection(ItemStack stack, String key, String label, String description) {
        if (stack == null || !(stack.getItem() instanceof AiDeckItem)) return;
        var tag = stack.getOrCreateTag();
        tag.putString(TAG_KEY, key == null || key.isBlank() ? DEFAULT_KEY : key);
        tag.putString(TAG_LABEL, label == null ? "" : label);
        tag.putString("AiDeckDesc", description == null ? "" : description);
    }

    /** 先在双手中找，再翻一遍背包。 */
    public static ItemStack find(Player player) {
        if (player == null) return null;
        if (player.getMainHandItem().getItem() instanceof AiDeckItem) return player.getMainHandItem();
        if (player.getOffhandItem().getItem() instanceof AiDeckItem) return player.getOffhandItem();
        for (int i = 0; i < player.getInventory().getContainerSize(); i++) {
            ItemStack stack = player.getInventory().getItem(i);
            if (stack.getItem() instanceof AiDeckItem) return stack;
        }
        return null;
    }
}
