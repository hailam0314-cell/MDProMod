package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.math.Axis;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.entity.EntityRenderer;
import net.minecraft.client.renderer.entity.EntityRendererProvider;
import net.minecraft.resources.ResourceLocation;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public class DuelCardRenderer extends EntityRenderer<DuelCardEntity> {
    public DuelCardRenderer(EntityRendererProvider.Context context) {
        super(context);
    }

    @Override
    public ResourceLocation getTextureLocation(DuelCardEntity entity) {
        return CardTextureCache.CARD_BACK;
    }

    @Override
    public void render(DuelCardEntity entity, float entityYaw, float partialTicks,
                       PoseStack poseStack, MultiBufferSource buffer, int packedLight) {
        if (ClientAnimBridge.shouldHide(entity)) return;
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
        ResourceLocation texture = entity.isFaceUp()
                ? CardTextureCache.get(entity.getCardCode(), entity.isDisabled())
                : CardTextureCache.CARD_BACK;
        poseStack.translate(0.0, 0.03, 0.0);
        poseStack.mulPose(Axis.XP.rotationDegrees(entity.isFaceUp() ? -90.0F : 90.0F));
        ClientDuelAnimationRenderer.drawCard(poseStack, buffer, texture,
                13.75F / 16.0F, 20.0F / 16.0F, packedLight);
        poseStack.popPose();
        super.render(entity, entityYaw, partialTicks, poseStack, buffer, packedLight);
    }
}
