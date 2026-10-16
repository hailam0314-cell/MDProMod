package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.math.Axis;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.entity.EntityRenderer;
import net.minecraft.client.renderer.entity.EntityRendererProvider;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.util.Mth;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

/**
 * 手牌实体渲染：正面（朝本人）是卡图，背面（朝对面）是卡背；
 * 未公开的对手手牌两面都只画卡背——客户端根本没有卡号，无法作弊偷看。
 */
@OnlyIn(Dist.CLIENT)
public class HandCardRenderer extends EntityRenderer<HandCardEntity> {
    private static final float CARD_W = 0.55F;
    private static final float CARD_H = 0.8F;

    public HandCardRenderer(EntityRendererProvider.Context context) {
        super(context);
    }

    @Override
    public ResourceLocation getTextureLocation(HandCardEntity entity) {
        return CardTextureCache.CARD_BACK;
    }

    @Override
    public void render(HandCardEntity entity, float entityYaw, float partialTicks,
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
        float bob = Mth.sin((entity.tickCount + partialTicks) * 0.07F) * 0.035F;
        poseStack.translate(0.0, bob, 0.0);

        var viewer = Minecraft.getInstance().player;
        int code = viewer != null && entity.faceVisibleTo(viewer)
                ? ClientAnimBridge.privateCardCode(entity.getId(), entity.getCardCode()) : 0;
        ResourceLocation front = code > 0 ? CardTextureCache.get(code) : CardTextureCache.CARD_BACK;
        poseStack.translate(0.0, CARD_H * 0.5F, 0.0);
        ClientDuelAnimationRenderer.drawCard(poseStack, buffer, front, CARD_W, CARD_H, packedLight);
        poseStack.popPose();
        super.render(entity, entityYaw, partialTicks, poseStack, buffer, packedLight);
    }
}
