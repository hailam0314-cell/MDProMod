package net.mdpromod;

import net.minecraft.core.BlockPos;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.network.chat.Component;
import net.minecraft.world.InteractionResult;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.TooltipFlag;
import net.minecraft.world.item.context.UseOnContext;
import net.minecraft.world.level.Level;

import java.util.List;

public class DuelCardItem extends Item {
    public DuelCardItem(Properties properties) {
        super(properties);
    }

    public int getCardCode(ItemStack stack) {
        CompoundTag tag = stack.getTag();
        return tag != null && tag.contains("CardCode") ? tag.getInt("CardCode") : 0;
    }

    public static ItemStack createStack(int code, int count) {
        ItemStack stack = new ItemStack(ModItems.DUEL_CARD_ITEM.get(), count);
        stack.getOrCreateTag().putInt("CardCode", code);
        return stack;
    }

    @Override
    public Component getName(ItemStack stack) {
        int code = getCardCode(stack);
        return CardRegistry.get(code)
                .<Component>map(info -> Component.literal(info.name()))
                .orElseGet(() -> super.getName(stack));
    }

    @Override
    public void appendHoverText(ItemStack stack, Level level,
                                List<Component> tooltip, TooltipFlag flag) {
        int code = getCardCode(stack);
        CardRegistry.get(code).ifPresent(info -> {
            tooltip.add(Component.literal("#" + info.code() + "  " + info.typeLabel()));
            if (info.isMonster()) {
                tooltip.add(Component.literal(
                        "ATK " + info.atk() + " / DEF " + info.def()
                                + " / LV " + info.level()));
            }
        });
    }

    @Override
    public InteractionResult useOn(UseOnContext context) {
        Level level = context.getLevel();
        if (!level.isClientSide) {
            BlockPos pos = context.getClickedPos().relative(context.getClickedFace());
            DuelCardEntity card = ModEntities.DUEL_CARD.get().create(level);
            if (card != null) {
                card.setCardCode(getCardCode(context.getItemInHand()));
                card.setFaceUp(true);
                card.setDefense(false);
                float yaw = context.getPlayer() == null ? 0.0F : context.getPlayer().getYRot();
                card.moveTo(pos.getX() + 0.5, pos.getY() + 0.05, pos.getZ() + 0.5, yaw, 0.0F);
                level.addFreshEntity(card);
                if (context.getPlayer() != null && !context.getPlayer().isCreative()) {
                    context.getItemInHand().shrink(1);
                }
            }
        }
        return InteractionResult.sidedSuccess(level.isClientSide);
    }
}
