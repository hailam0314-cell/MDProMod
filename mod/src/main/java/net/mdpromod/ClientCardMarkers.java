package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.Tesselator;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.math.Axis;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.client.renderer.MultiBufferSource.BufferSource;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.core.particles.SimpleParticleType;
import net.minecraft.util.RandomSource;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;
import org.joml.Matrix4f;

@OnlyIn(Dist.CLIENT)
public final class ClientCardMarkers {
    private static final int CHAIN_R = red(16739895);
    private static final int CHAIN_G = green(16739895);
    private static final int CHAIN_B = blue(16739895);
    private static final int TARGET_R = red(4633855);
    private static final int TARGET_G = green(4633855);
    private static final int TARGET_B = blue(4633855);
    private static final float CARD_WIDTH = 0.859375F;
    private static final float CARD_HEIGHT = 1.25F;
    private static int[] chain = new int[0];
    private static int[] target = new int[0];
    private static int[] chainAddresses = new int[0];
    private static int[] targetAddresses = new int[0];

    private ClientCardMarkers() {
    }

    public static void apply(int[] chainIds, int[] targetIds, int[] chainAddrs, int[] targetAddrs) {
        chain = chainIds == null ? new int[0] : chainIds;
        target = targetIds == null ? new int[0] : targetIds;
        chainAddresses = chainAddrs == null ? new int[0] : chainAddrs;
        targetAddresses = targetAddrs == null ? new int[0] : targetAddrs;
    }

    public static void clear() {
        chain = new int[0];
        target = new int[0];
        chainAddresses = new int[0];
        targetAddresses = new int[0];
    }

    public static int markAt(int packedAddress) {
        return DuelMarks.at(chainAddresses, targetAddresses, packedAddress);
    }

    public static void drawGuiMark(GuiGraphics graphics, int x, int y, int w, int h, int mark) {
        if ((mark & 1) != 0) {
            ring(graphics, x, y, w, h, 16739895, 0);
        }

        if ((mark & 2) != 0) {
            ring(graphics, x, y, w, h, 4633855, 3);
        }
    }

    private static void ring(GuiGraphics graphics, int x, int y, int w, int h, int rgb, int inset) {
        int x0 = x + inset;
        int y0 = y + inset;
        int x1 = x + w - inset;
        int y1 = y + h - inset;
        int colour = 0xFF000000 | rgb;
        graphics.fill(x0, y0, x1, y0 + 2, colour);
        graphics.fill(x0, y1 - 2, x1, y1, colour);
        graphics.fill(x0, y0, x0 + 2, y1, colour);
        graphics.fill(x1 - 2, y0, x1, y1, colour);
    }

    private static int red(int rgb) {
        return rgb >> 16 & 0xFF;
    }

    private static int green(int rgb) {
        return rgb >> 8 & 0xFF;
    }

    private static int blue(int rgb) {
        return rgb & 0xFF;
    }

    public static void tick(Minecraft minecraft) {
        if (minecraft.level != null && (chain.length != 0 || target.length != 0)) {
            spawnParticles(minecraft, chain, ParticleTypes.FLAME);
            spawnParticles(minecraft, target, ParticleTypes.SOUL_FIRE_FLAME);
        }
    }

    private static void spawnParticles(Minecraft minecraft, int[] ids, SimpleParticleType particle) {
        RandomSource random = minecraft.level.random;

        for (int entityId : ids) {
            Entity entity = minecraft.level.getEntity(entityId);
            if (entity != null) {
                minecraft.level
                    .addParticle(
                        particle,
                        entity.getX() + (random.nextDouble() - 0.5) * 0.7,
                        entity.getY() + 0.08,
                        entity.getZ() + (random.nextDouble() - 0.5) * 0.7,
                        0.0,
                        0.01,
                        0.0
                    );
            }
        }
    }

    public static void render(Minecraft minecraft, PoseStack poses, float partialTick) {
        if (minecraft.level != null && (chain.length != 0 || target.length != 0)) {
            Vec3 cam = minecraft.gameRenderer.getMainCamera().getPosition();
            BufferSource buffer = MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());
            VertexConsumer quads = buffer.getBuffer(RenderType.debugQuads());
            drawLayer(minecraft, poses, quads, cam, partialTick, target, TARGET_R, TARGET_G, TARGET_B, 0.09F, 0.05F, 0.042F);
            drawLayer(minecraft, poses, quads, cam, partialTick, chain, CHAIN_R, CHAIN_G, CHAIN_B, 0.0F, 0.06F, 0.05F);
            buffer.endBatch();
        }
    }

    private static void drawLayer(
        Minecraft minecraft,
        PoseStack poses,
        VertexConsumer quads,
        Vec3 cam,
        float partialTick,
        int[] ids,
        int red,
        int green,
        int blue,
        float outward,
        float thickness,
        float height
    ) {
        for (int entityId : ids) {
            Entity entity = minecraft.level.getEntity(entityId);
            if (entity instanceof DuelCardEntity card && !ClientAnimBridge.shouldHide(entity)) {
                ClientAnimBridge.Transform anim = ClientAnimBridge.renderOffset(entity, partialTick);
                poses.pushPose();
                if (anim != null) {
                    poses.translate(anim.offset().x, anim.offset().y, anim.offset().z);
                }

                poses.translate(entity.getX() - cam.x, entity.getY() - cam.y, entity.getZ() - cam.z);
                poses.mulPose(Axis.YP.rotationDegrees(-entity.getYRot()));
                if (anim != null && anim.roll() != 0.0F) {
                    poses.mulPose(Axis.ZP.rotationDegrees(anim.roll()));
                }

                if (card.isDefense()) {
                    poses.mulPose(Axis.YP.rotationDegrees(90.0F));
                }

                poses.mulPose(Axis.XP.rotationDegrees(-90.0F));
                Matrix4f matrix = poses.last().pose();
                float hw = 0.4296875F;
                float hh = 0.625F;
                frame(quads, matrix, hw, hh, outward - 0.02F, thickness + 0.1F, height - 0.006F, red, green, blue, 36);
                frame(quads, matrix, hw, hh, outward, thickness, height, red, green, blue, 205);
                poses.popPose();
            }
        }
    }

    private static void frame(
        VertexConsumer quads, Matrix4f matrix, float hw, float hh, float outward, float thickness, float z, int red, int green, int blue, int alpha
    ) {
        float x = hw + outward;
        float y = hh + outward;
        float t = thickness * 0.5F;
        quad(quads, matrix, -(x + t), y - t, x + t, y + t, z, red, green, blue, alpha);
        quad(quads, matrix, -(x + t), -y - t, x + t, -y + t, z, red, green, blue, alpha);
        quad(quads, matrix, -x - t, -hh, -x + t, hh, z, red, green, blue, alpha);
        quad(quads, matrix, x - t, -hh, x + t, hh, z, red, green, blue, alpha);
    }

    private static void quad(VertexConsumer quads, Matrix4f matrix, float x0, float y0, float x1, float y1, float z, int red, int green, int blue, int alpha) {
        quads.vertex(matrix, x0, y0, z).color(red, green, blue, alpha).endVertex();
        quads.vertex(matrix, x0, y1, z).color(red, green, blue, alpha).endVertex();
        quads.vertex(matrix, x1, y1, z).color(red, green, blue, alpha).endVertex();
        quads.vertex(matrix, x1, y0, z).color(red, green, blue, alpha).endVertex();
    }
}
