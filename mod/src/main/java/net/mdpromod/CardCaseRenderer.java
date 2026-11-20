package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.math.Axis;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.client.renderer.entity.EntityRenderer;
import net.minecraft.client.renderer.entity.EntityRendererProvider.Context;
import net.minecraft.resources.ResourceLocation;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public class CardCaseRenderer extends EntityRenderer<CardCaseEntity> {
    private static final ResourceLocation ATLAS = ResourceLocation.fromNamespaceAndPath("mdpromod", "textures/entity/card_case_atlas.png");

    public CardCaseRenderer(Context context) {
        super(context);
    }

    public ResourceLocation getTextureLocation(CardCaseEntity entity) {
        return ATLAS;
    }

    public void render(CardCaseEntity entity, float entityYaw, float partialTicks, PoseStack poseStack, MultiBufferSource buffer, int packedLight) {
        poseStack.pushPose();
        poseStack.mulPose(Axis.YP.rotationDegrees(-entityYaw));
        VertexConsumer consumer = buffer.getBuffer(RenderType.entityCutoutNoCull(ATLAS));
        float[][] bodyUv = new float[][]{
            uv(256, 0, 384, 192), uv(0, 0, 128, 192), uv(0, 192, 64, 384), uv(0, 192, 64, 384), uv(128, 192, 256, 384), uv(448, 448, 512, 512)
        };
        RenderUtil.box(poseStack, consumer, -0.2625F, 0.0F, -0.175F, 0.2625F, 0.5F, 0.175F, bodyUv, packedLight);
        float[][] lidUv = new float[][]{
            uv(192, 384, 384, 462), uv(0, 384, 192, 462), uv(64, 192, 128, 320), uv(64, 192, 128, 320), uv(384, 384, 512, 448), uv(256, 192, 384, 320)
        };
        RenderUtil.box(poseStack, consumer, -0.26875F, 0.5F, -0.18125F, 0.26875F, 0.71875F, 0.18125F, lidUv, packedLight);
        poseStack.popPose();
        super.render(entity, entityYaw, partialTicks, poseStack, buffer, packedLight);
    }

    private static float[] uv(int u0, int v0, int u1, int v1) {
        return new float[]{u0 / 512.0F, v0 / 512.0F, u1 / 512.0F, v1 / 512.0F};
    }
}
