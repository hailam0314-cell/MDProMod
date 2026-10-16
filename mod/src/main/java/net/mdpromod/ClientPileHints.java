package net.mdpromod;

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

import java.util.Optional;

/** 卡堆发光：额外卡组有可特殊召唤的卡、墓地/除外区有可发动的卡时，堆上泛光并冒粒子。 */
@OnlyIn(Dist.CLIENT)
public final class ClientPileHints {
    private static int fieldEntityId = -1;
    private static int[] zones = new int[0];

    private ClientPileHints() {
    }

    public static void apply(int fieldId, int[] packed) {
        int[] incoming = packed == null ? new int[0] : packed;
        if (incoming.length > 0 && (fieldEntityId != fieldId || !java.util.Arrays.equals(zones, incoming)))
            ClientDuelAudio.playUi("SE_DUEL_ACTIVE_POSSIBLE");
        fieldEntityId = fieldId;
        zones = incoming;
    }

    /** 每 tick 撒粒子（只有本人能看到）。 */
    public static void tick(Minecraft minecraft) {
        if (zones.length == 0 || minecraft.level == null) return;
        if (!(minecraft.level.getEntity(fieldEntityId) instanceof DuelFieldEntity field)) return;
        for (int location : zones) {
            String zoneId = DuelFieldLayout.zoneIdFor(0, location, 0);
            if (zoneId == null) continue;
            Optional<DuelFieldLayout.Zone> zone = DuelFieldLayout.byId(zoneId);
            if (zone.isEmpty()) continue;
            Vec3 at = field.zoneWorldPosition(zone.get());
            double x = at.x + (minecraft.level.random.nextDouble() - 0.5) * 1.2;
            double z = at.z + (minecraft.level.random.nextDouble() - 0.5) * 1.4;
            minecraft.level.addParticle(ParticleTypes.END_ROD, x, at.y + 0.35, z, 0.0, 0.02, 0.0);
        }
    }

    /** 3D 阶段：在堆上画一层半透明高亮。 */
    public static void render(Minecraft minecraft, PoseStack poses) {
        if (zones.length == 0 || minecraft.level == null) return;
        Entity entity = minecraft.level.getEntity(fieldEntityId);
        if (!(entity instanceof DuelFieldEntity field)) return;
        Vec3 cam = minecraft.gameRenderer.getMainCamera().getPosition();
        MultiBufferSource.BufferSource buffer = MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());
        VertexConsumer quads = buffer.getBuffer(RenderType.debugQuads());
        poses.pushPose();
        poses.translate(field.getX() - cam.x, field.getY() - cam.y, field.getZ() - cam.z);
        poses.mulPose(Axis.YP.rotationDegrees(-field.getYRot()));
        Matrix4f matrix = poses.last().pose();
        for (int location : zones) {
            String zoneId = DuelFieldLayout.zoneIdFor(0, location, 0);
            if (zoneId == null) continue;
            Optional<DuelFieldLayout.Zone> zone = DuelFieldLayout.byId(zoneId);
            if (zone.isEmpty()) continue;
            float x = (float) (zone.get().x() / 16.0);
            float z = (float) (zone.get().z() / 16.0);
            quad(quads, matrix, x - 0.75F, 0.09F, z - 0.875F, x + 0.75F, z + 0.875F);
        }
        poses.popPose();
        buffer.endBatch();
    }

    private static void quad(VertexConsumer quads, Matrix4f matrix,
                             float x0, float y, float z0, float x1, float z1) {
        quads.vertex(matrix, x0, y, z0).color(120, 220, 255, 80).endVertex();
        quads.vertex(matrix, x0, y, z1).color(120, 220, 255, 80).endVertex();
        quads.vertex(matrix, x1, y, z1).color(120, 220, 255, 80).endVertex();
        quads.vertex(matrix, x1, y, z0).color(120, 220, 255, 80).endVertex();
    }
}
