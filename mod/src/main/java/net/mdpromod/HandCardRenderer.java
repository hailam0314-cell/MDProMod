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
        return CardBacks.texture(entity.cardBack());
    }

    public void render(HandCardEntity entity, float entityYaw, float partialTicks, PoseStack poseStack, MultiBufferSource buffer, int packedLight) {
        var screen = Minecraft.getInstance().screen;
        if (screen instanceof DuelControlScreen || screen instanceof SelectCardScreen selection && selection.returnPanel() != null) {
            DuelFieldEntity field = ClientHandBridge.field();
            if (field != null && entity.belongsTo(field.getUUID(), ClientHandBridge.snapshot().seat())) return;
        }
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
            ResourceLocation front = code > 0 ? CardTextureCache.get(code) : getTextureLocation(entity);
            poseStack.translate(0.0, 0.4F, 0.0);
            ClientDuelAnimationRenderer.drawCard(poseStack, buffer, front, getTextureLocation(entity), 0.55F, 0.8F, packedLight);
            poseStack.popPose();
            super.render(entity, entityYaw, partialTicks, poseStack, buffer, packedLight);
        }
    }
}
