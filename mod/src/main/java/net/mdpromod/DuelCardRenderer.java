package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.math.Axis;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.entity.EntityRenderer;
import net.minecraft.client.renderer.entity.EntityRendererProvider.Context;
import net.minecraft.resources.ResourceLocation;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public class DuelCardRenderer extends EntityRenderer<DuelCardEntity> {
    public DuelCardRenderer(Context context) {
        super(context);
    }

    public ResourceLocation getTextureLocation(DuelCardEntity entity) {
        return CardBacks.texture(entity.cardBack());
    }

    public void render(DuelCardEntity entity, float entityYaw, float partialTicks, PoseStack poseStack, MultiBufferSource buffer, int packedLight) {
        if (!ClientAnimBridge.shouldHide(entity)) {
            poseStack.pushPose();
            ClientAnimBridge.Transform anim = ClientAnimBridge.renderOffset(entity, partialTicks);
            if (anim != null) {
                poseStack.translate(anim.offset().x, anim.offset().y, anim.offset().z);
            }

            poseStack.mulPose(Axis.YP.rotationDegrees(-entityYaw));
            if (anim != null && anim.roll() != 0.0F) {
                poseStack.mulPose(Axis.ZP.rotationDegrees(anim.roll()));
            }

            if (entity.isDefense()) {
                poseStack.mulPose(Axis.YP.rotationDegrees(90.0F));
            }

            ResourceLocation texture = entity.isFaceUp() ? CardTextureCache.get(entity.getCardCode(), entity.isDisabled()) : getTextureLocation(entity);
            poseStack.translate(0.0, 0.03, 0.0);
            if (entity.pendulumScale() >= 0 && entity.isFaceUp()) {
                ClientPendulumEffects.renderScale(poseStack, buffer, entity, partialTicks);
                poseStack.translate(0, 0.625, 0);
            } else {
                poseStack.mulPose(Axis.XP.rotationDegrees(entity.isFaceUp() ? -90.0F : 90.0F));
            }
            ClientDuelAnimationRenderer.drawCard(poseStack, buffer, texture, getTextureLocation(entity), 0.859375F, 1.25F, packedLight);
            poseStack.popPose();
            super.render(entity, entityYaw, partialTicks, poseStack, buffer, packedLight);
        }
    }
}
