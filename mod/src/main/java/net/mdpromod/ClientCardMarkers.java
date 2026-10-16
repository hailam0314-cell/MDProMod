package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.Tesselator;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.math.Axis;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;
import org.joml.Matrix4f;

/**
 * 卡面标记（对双方可见）：
 * 连锁里发动的卡 = 橙红，被连锁中其他卡点名的卡 = 青蓝。
 *
 * <p>场地上的卡是平铺的，所以标记也贴着卡面平铺：一圈发光边框 + 一层很淡的光晕。
 * 两种标记同时存在时橙红在内（骑在卡沿上）、青蓝在外，最后再撒对应颜色的粒子
 * （粒子只有本地客户端看得见）。里侧卡一样只画边框，不带卡面/卡名，不会泄露身份。
 */
@OnlyIn(Dist.CLIENT)
public final class ClientCardMarkers {
    /** 连锁中：橙红。 */
    private static final int CHAIN_R = red(DuelMarks.CHAIN_RGB);
    private static final int CHAIN_G = green(DuelMarks.CHAIN_RGB);
    private static final int CHAIN_B = blue(DuelMarks.CHAIN_RGB);
    /** 被指定：青蓝。 */
    private static final int TARGET_R = red(DuelMarks.TARGET_RGB);
    private static final int TARGET_G = green(DuelMarks.TARGET_RGB);
    private static final int TARGET_B = blue(DuelMarks.TARGET_RGB);
    /** 和 DuelCardRenderer 里那张卡一样的尺寸。 */
    private static final float CARD_WIDTH = 13.75F / 16.0F;
    private static final float CARD_HEIGHT = 20.0F / 16.0F;

    private static int[] chain = new int[0];
    private static int[] target = new int[0];
    /** 同一批标记的"在哪"（打包地址）：选卡界面用它给清单里的候选卡描边。 */
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

    /** 退出世界时清干净。 */
    public static void clear() {
        chain = new int[0];
        target = new int[0];
        chainAddresses = new int[0];
        targetAddresses = new int[0];
    }

    /** 某个地址上的标记位（{@link DuelMarks}）；没有标记就是 0。 */
    public static int markAt(int packedAddress) {
        return DuelMarks.at(chainAddresses, targetAddresses, packedAddress);
    }

    /**
     * 选卡界面里给候选卡描边：橙红在内（贴着卡沿）、青蓝在外，和世界里的边框一个意思。
     * 悬停提示用 {@link DuelMarks#label(int)} 说明这两圈是什么。
     */
    public static void drawGuiMark(GuiGraphics graphics, int x, int y, int w, int h, int mark) {
        if ((mark & DuelMarks.CHAIN) != 0) ring(graphics, x, y, w, h, DuelMarks.CHAIN_RGB, 0);
        if ((mark & DuelMarks.TARGET) != 0) ring(graphics, x, y, w, h, DuelMarks.TARGET_RGB, 3);
    }

    private static void ring(GuiGraphics graphics, int x, int y, int w, int h, int rgb, int inset) {
        int x0 = x + inset, y0 = y + inset, x1 = x + w - inset, y1 = y + h - inset;
        int colour = 0xFF000000 | rgb;
        graphics.fill(x0, y0, x1, y0 + 2, colour);
        graphics.fill(x0, y1 - 2, x1, y1, colour);
        graphics.fill(x0, y0, x0 + 2, y1, colour);
        graphics.fill(x1 - 2, y0, x1, y1, colour);
    }

    private static int red(int rgb) { return (rgb >> 16) & 0xFF; }

    private static int green(int rgb) { return (rgb >> 8) & 0xFF; }

    private static int blue(int rgb) { return rgb & 0xFF; }

    /** 每客户端 tick 在带标记的卡上撒一点粒子（只有自己看得见）。 */
    public static void tick(Minecraft minecraft) {
        if (minecraft.level == null || chain.length == 0 && target.length == 0) return;
        spawnParticles(minecraft, chain, ParticleTypes.FLAME);
        spawnParticles(minecraft, target, ParticleTypes.SOUL_FIRE_FLAME);
    }

    private static void spawnParticles(Minecraft minecraft, int[] ids, net.minecraft.core.particles.SimpleParticleType particle) {
        var random = minecraft.level.random;
        for (int entityId : ids) {
            Entity entity = minecraft.level.getEntity(entityId);
            if (entity == null) continue;
            minecraft.level.addParticle(particle,
                    entity.getX() + (random.nextDouble() - 0.5) * 0.7,
                    entity.getY() + 0.08,
                    entity.getZ() + (random.nextDouble() - 0.5) * 0.7,
                    0.0, 0.01, 0.0);
        }
    }

    /** 3D 阶段：贴着卡面画发光边框。 */
    public static void render(Minecraft minecraft, PoseStack poses, float partialTick) {
        if (minecraft.level == null || chain.length == 0 && target.length == 0) return;
        Vec3 cam = minecraft.gameRenderer.getMainCamera().getPosition();
        MultiBufferSource.BufferSource buffer = MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());
        VertexConsumer quads = buffer.getBuffer(RenderType.debugQuads());
        // 里圈的青蓝先画、外圈的橙红后画，两者都在时不会互相盖住
        drawLayer(minecraft, poses, quads, cam, partialTick, target, TARGET_R, TARGET_G, TARGET_B, 0.09F, 0.05F, 0.042F);
        drawLayer(minecraft, poses, quads, cam, partialTick, chain, CHAIN_R, CHAIN_G, CHAIN_B, 0.0F, 0.06F, 0.05F);
        buffer.endBatch();
    }

    private static void drawLayer(Minecraft minecraft, PoseStack poses, VertexConsumer quads, Vec3 cam,
                                  float partialTick, int[] ids, int red, int green, int blue,
                                  float outward, float thickness, float height) {
        for (int entityId : ids) {
            Entity entity = minecraft.level.getEntity(entityId);
            if (!(entity instanceof DuelCardEntity card)) continue;
            // 飞行演出期间实体是藏起来的（那一段由动画层自己画），标记跟着一起收起来
            if (ClientAnimBridge.shouldHide(entity)) continue;
            ClientAnimBridge.Transform anim = ClientAnimBridge.renderOffset(entity, partialTick);
            poses.pushPose();
            if (anim != null) poses.translate(anim.offset().x, anim.offset().y, anim.offset().z);
            poses.translate(entity.getX() - cam.x, entity.getY() - cam.y, entity.getZ() - cam.z);
            poses.mulPose(Axis.YP.rotationDegrees(-entity.getYRot()));
            if (anim != null && anim.roll() != 0.0F) poses.mulPose(Axis.ZP.rotationDegrees(anim.roll()));
            if (card.isDefense()) poses.mulPose(Axis.YP.rotationDegrees(90.0F));
            poses.mulPose(Axis.XP.rotationDegrees(-90.0F));
            Matrix4f matrix = poses.last().pose();
            float hw = CARD_WIDTH * 0.5F, hh = CARD_HEIGHT * 0.5F;
            // 先铺一层淡光晕（略往卡面里收一点），再压一条亮边
            frame(quads, matrix, hw, hh, outward - 0.02F, thickness + 0.10F, height - 0.006F,
                    red, green, blue, 36);
            frame(quads, matrix, hw, hh, outward, thickness, height, red, green, blue, 205);
            poses.popPose();
        }
    }

    /**
     * 绕着卡片一圈的发光边框，画在卡片所在的平面里（{@code z} 抬高一点，压在卡面之上）。
     * {@code outward} 是边框中心线离卡沿的距离（0 = 骑在卡沿上），{@code thickness} 是边框宽度。
     */
    private static void frame(VertexConsumer quads, Matrix4f matrix, float hw, float hh,
                              float outward, float thickness, float z,
                              int red, int green, int blue, int alpha) {
        float x = hw + outward, y = hh + outward, t = thickness * 0.5F;
        // 上下两条（顺便盖住四个角）
        quad(quads, matrix, -(x + t), y - t, x + t, y + t, z, red, green, blue, alpha);
        quad(quads, matrix, -(x + t), -y - t, x + t, -y + t, z, red, green, blue, alpha);
        // 左右两条只铺卡身高，避免和上下两条重叠处加深
        quad(quads, matrix, -x - t, -hh, -x + t, hh, z, red, green, blue, alpha);
        quad(quads, matrix, x - t, -hh, x + t, hh, z, red, green, blue, alpha);
    }

    private static void quad(VertexConsumer quads, Matrix4f matrix,
                             float x0, float y0, float x1, float y1, float z,
                             int red, int green, int blue, int alpha) {
        quads.vertex(matrix, x0, y0, z).color(red, green, blue, alpha).endVertex();
        quads.vertex(matrix, x0, y1, z).color(red, green, blue, alpha).endVertex();
        quads.vertex(matrix, x1, y1, z).color(red, green, blue, alpha).endVertex();
        quads.vertex(matrix, x1, y0, z).color(red, green, blue, alpha).endVertex();
    }
}
