package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.math.Axis;
import net.minecraft.client.Minecraft;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.entity.EntityRenderer;
import net.minecraft.client.renderer.entity.EntityRendererProvider.Context;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.util.Mth;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public class HandCardRenderer extends EntityRenderer<HandCardEntity> {
    private static final float CARD_W = 0.55F;
    private static final float CARD_H = 0.8F;

    public HandCardRenderer(Context context) {
        super(context);
    }

    public ResourceLocation getTextureLocation(HandCardEntity entity) {
        return CardTextureCache.CARD_BACK;
    }

    public void render(HandCardEntity entity, float entityYaw, float partialTicks, PoseStack poseStack, MultiBufferSource buffer, int packedLight) {
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

            float bob = Mth.sin((entity.tickCount + partialTicks) * 0.07F) * 0.035F;
            poseStack.translate(0.0, bob, 0.0);
            LocalPlayer viewer = Minecraft.getInstance().player;
            int code = viewer != null && entity.faceVisibleTo(viewer) ? ClientAnimBridge.privateCardCode(entity.getId(), entity.getCardCode()) : 0;
            ResourceLocation front = code > 0 ? CardTextureCache.get(code) : CardTextureCache.CARD_BACK;
            poseStack.translate(0.0, 0.4F, 0.0);
            ClientDuelAnimationRenderer.drawCard(poseStack, buffer, front, 0.55F, 0.8F, packedLight);
            poseStack.popPose();
            super.render(entity, entityYaw, partialTicks, poseStack, buffer, packedLight);
        }
    }
}
