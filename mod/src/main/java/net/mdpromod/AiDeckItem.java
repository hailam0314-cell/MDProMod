package net.mdpromod;

import java.nio.file.Path;
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

public class AiDeckItem extends Item {
    public static final String TAG_KEY = "AiDeck";
    public static final String TAG_LABEL = "AiDeckLabel";
    public static final String DEFAULT_KEY = "AI_BlueEyes";

    public AiDeckItem(Properties properties) {
        super(properties);
    }

    public InteractionResultHolder<ItemStack> use(Level level, Player player, InteractionHand hand) {
        ItemStack stack = player.getItemInHand(hand);
        if (level.isClientSide) {
            return InteractionResultHolder.success(stack);
        }

        if (player instanceof ServerPlayer serverPlayer) {
            Path root = DuelSessionManager.runtimeRoot();
            ModNetwork.sendTo(serverPlayer, ModNetwork.AiDeckOpen.of(getKey(stack), AiDeckCatalog.entries(root), root));
        }

        return InteractionResultHolder.consume(stack);
    }

    public Component getName(ItemStack stack) {
        return Component.translatable("item.mdpromod.ai_deck").copy().append(Component.literal(" · " + getLabel(stack)));
    }

    public void appendHoverText(ItemStack stack, Level level, List<Component> tooltip, TooltipFlag flag) {
        tooltip.add(Component.literal("人机：" + getLabel(stack)).withStyle(ChatFormatting.GRAY));
        String description = getDescription(stack);
        if (!description.isBlank()) {
            tooltip.add(Component.literal(description).withStyle(ChatFormatting.DARK_GRAY));
        }

        tooltip.add(Component.literal("右键换人机；把它放到决斗盘远半场开始对战").withStyle(ChatFormatting.DARK_GRAY));
    }

    public static String getKey(ItemStack stack) {
        if (stack != null && stack.getItem() instanceof AiDeckItem) {
            CompoundTag tag = stack.getTag();
            if (tag != null && tag.contains("AiDeck")) {
                String key = tag.getString("AiDeck");
                return key.isBlank() ? "AI_BlueEyes" : key;
            } else {
                return "AI_BlueEyes";
            }
        } else {
            return "AI_BlueEyes";
        }
    }

    public static String getLabel(ItemStack stack) {
        if (stack != null && stack.getItem() instanceof AiDeckItem) {
            CompoundTag tag = stack.getTag();
            if (tag != null && tag.contains("AiDeckLabel") && !tag.getString("AiDeckLabel").isBlank()) {
                return tag.getString("AiDeckLabel");
            }
        }

        String key = getKey(stack);
        return key.startsWith("AI_") ? key.substring(3) : key;
    }

    public static String getDescription(ItemStack stack) {
        if (stack != null && stack.getItem() instanceof AiDeckItem) {
            CompoundTag tag = stack.getTag();
            if (tag != null && tag.contains("AiDeckDesc")) {
                return tag.getString("AiDeckDesc");
            }
        }

        return "";
    }

    public static void setSelection(ItemStack stack, String key, String label, String description) {
        if (stack != null && stack.getItem() instanceof AiDeckItem) {
            CompoundTag tag = stack.getOrCreateTag();
            tag.putString("AiDeck", key != null && !key.isBlank() ? key : "AI_BlueEyes");
            tag.putString("AiDeckLabel", label == null ? "" : label);
            tag.putString("AiDeckDesc", description == null ? "" : description);
        }
    }

    public static ItemStack find(Player player) {
        if (player == null) {
            return null;
        }

        if (player.getMainHandItem().getItem() instanceof AiDeckItem) {
            return player.getMainHandItem();
        }

        if (player.getOffhandItem().getItem() instanceof AiDeckItem) {
            return player.getOffhandItem();
        }

        for (int i = 0; i < player.getInventory().getContainerSize(); i++) {
            ItemStack stack = player.getInventory().getItem(i);
            if (stack.getItem() instanceof AiDeckItem) {
                return stack;
            }
        }

        return null;
    }
}
