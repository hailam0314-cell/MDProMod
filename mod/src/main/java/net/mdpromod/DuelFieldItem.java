package net.mdpromod;

import net.minecraft.core.BlockPos;
import net.minecraft.network.chat.Component;
import net.minecraft.world.InteractionResult;
import net.minecraft.world.entity.EntityType;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.Item.Properties;
import net.minecraft.world.item.context.UseOnContext;
import net.minecraft.world.level.Level;

public class DuelFieldItem extends Item {
    public DuelFieldItem(Properties properties) {
        super(properties);
    }

    @Override
    public String getDescriptionId(net.minecraft.world.item.ItemStack stack) {
        DuelFieldTheme theme = DuelFieldTheme.read(stack.getTag());
        return theme == DuelFieldTheme.CLASSIC ? super.getDescriptionId(stack) : getDescriptionId() + "." + theme.id;
    }

    public InteractionResult useOn(UseOnContext context) {
        Level level = context.getLevel();
        if (!level.isClientSide) {
            BlockPos pos = context.getClickedPos().relative(context.getClickedFace());
            DuelFieldEntity field = (DuelFieldEntity)((EntityType)ModEntities.DUEL_FIELD.get()).create(level);
            if (field != null) {
                float yaw = snapYaw(context.getPlayer() == null ? 0.0F : context.getPlayer().getYRot() + 180.0F);
                DuelFieldTheme theme = DuelAppearance.of(context.getPlayer()).fieldOr(DuelFieldTheme.read(context.getItemInHand().getTag()));
                field.moveTo(pos.getX() + 0.5, pos.getY() + theme.placementHeight, pos.getZ() + 0.5, yaw, 0.0F);
                if (context.getPlayer() != null) {
                    field.setPlacer(context.getPlayer().getUUID());
                }

                field.setTheme(theme);
                level.addFreshEntity(field);
                if (context.getPlayer() != null) {
                    context.getPlayer().displayClientMessage(Component.literal("决斗盘已放置：16 x 14 方块，" + DuelFieldLayout.zoneCount() + " 个区域（把卡盒右键上来放卡组）"), false);
                    if (!context.getPlayer().isCreative()) {
                        context.getItemInHand().shrink(1);
                    }
                }
            }
        }

        return InteractionResult.sidedSuccess(level.isClientSide);
    }

    public static float snapYaw(float yaw) {
        return Math.round(yaw / 90.0F) * 90.0F;
    }
}
