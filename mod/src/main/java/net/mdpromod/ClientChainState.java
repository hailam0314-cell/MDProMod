package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.Tesselator;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.math.Axis;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.LightTexture;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.network.chat.Component;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;
import org.joml.Matrix4f;

/**
 * 连锁演出：在决斗场中央把参与连锁的卡片排成一排（对双方可见），
 * 每张上面挂一个醒目的链号；新增的链节会弹一下，解决的链节逐个消失。
 */
@OnlyIn(Dist.CLIENT)
public final class ClientChainState {
    private static final float CARD_W = 0.55F;
    private static final float CARD_H = 0.8F;
    private static final float SPACING = 0.62F;

    private static int fieldEntityId = -1;
    private static int[] codes = new int[0];
    private static int resolving;
    private static int lastCount = -1;
    private static long changedAt;

    private ClientChainState() {
    }

    public static void apply(int fieldId, int[] newCodes, int newResolving) {
        if (newCodes != null && (codes.length != newCodes.length)) {
            changedAt = System.currentTimeMillis();
        }
        fieldEntityId = fieldId;
        codes = newCodes == null ? new int[0] : newCodes;
        resolving = newResolving;
    }

    public static void render(Minecraft minecraft, PoseStack poses) {
        if (codes.length == 0 || minecraft.level == null) return;
        Entity entity = minecraft.level.getEntity(fieldEntityId);
        if (!(entity instanceof DuelFieldEntity field)) return;
        Vec3 cam = minecraft.gameRenderer.getMainCamera().getPosition();
        MultiBufferSource.BufferSource buffer = MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());
        float total = codes.length * SPACING;
        for (int i = 0; i < codes.length; i++) {
            float offset = -total / 2.0F + SPACING / 2.0F + i * SPACING;
            boolean popping = i == codes.length - 1 && System.currentTimeMillis() - changedAt < 400;
            float pop = popping ? 1.25F : 1.0F;
            boolean active = resolving == i + 1;
            poses.pushPose();
            poses.translate(field.getX() - cam.x, field.getY() - cam.y + 1.5, field.getZ() - cam.z);
            poses.mulPose(Axis.YP.rotationDegrees(-field.getYRot()));
            poses.translate(offset, 0.0F, 0.0F);
            poses.mulPose(Axis.XP.rotationDegrees(15.0F));
            poses.scale(pop, pop, pop);
            ClientDuelAnimationRenderer.drawCard(poses, buffer,
                    CardTextureCache.get(codes[i]), CARD_W, CARD_H, LightTexture.FULL_BRIGHT);
            poses.popPose();
            // 链号：画在卡片上方，正在解决的那张换成金色
            poses.pushPose();
            poses.translate(field.getX() - cam.x, field.getY() - cam.y + 1.5, field.getZ() - cam.z);
            poses.mulPose(Axis.YP.rotationDegrees(-field.getYRot()));
            poses.translate(offset, CARD_H * 0.62F, 0.0F);
            poses.scale(-0.02F, -0.02F, 0.02F);
            String label = Integer.toString(i + 1);
            int width = minecraft.font.width(label);
            minecraft.font.drawInBatch(Component.literal(label), -width / 2.0F, -4.0F,
                    active ? 0xFFFF55 : 0xFF8080, true, poses.last().pose(), buffer,
                    net.minecraft.client.gui.Font.DisplayMode.SEE_THROUGH, 0, LightTexture.FULL_BRIGHT);
            poses.popPose();
        }
        buffer.endBatch();
    }
}
