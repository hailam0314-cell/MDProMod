package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.Tesselator;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.math.Axis;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;
import org.joml.Matrix4f;

import java.util.Optional;

/** 核心正在等落点时，把可以选的格子高亮出来（绿色半透明）。 */
@OnlyIn(Dist.CLIENT)
public final class ClientPlaceHints {
    private static int fieldEntityId = -1;
    private static int[] places = new int[0];

    private ClientPlaceHints() {
    }

    public static void apply(int fieldId, int[] packed) {
        fieldEntityId = fieldId;
        places = packed == null ? new int[0] : packed;
    }

    public static void render(Minecraft minecraft, PoseStack poses) {
        if (places.length == 0 || minecraft.level == null) return;
        Entity entity = minecraft.level.getEntity(fieldEntityId);
        if (!(entity instanceof DuelFieldEntity field)) return;
        Vec3 cam = minecraft.gameRenderer.getMainCamera().getPosition();
        MultiBufferSource.BufferSource buffer = MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());
        VertexConsumer quads = buffer.getBuffer(RenderType.debugQuads());
        poses.pushPose();
        poses.translate(field.getX() - cam.x, field.getY() - cam.y, field.getZ() - cam.z);
        poses.mulPose(Axis.YP.rotationDegrees(-field.getYRot()));
        Matrix4f matrix = poses.last().pose();
        for (int packed : places) {
            int controller = (packed >> 16) & 1;
            int location = (packed >> 8) & 0xFF;
            int sequence = packed & 0xFF;
            String zoneId = DuelFieldLayout.zoneIdFor(controller, location, sequence);
            Optional<DuelFieldLayout.Zone> zone = zoneId == null
                    ? Optional.empty() : DuelFieldLayout.byId(zoneId);
            if (zone.isEmpty()) continue;
            float x = (float) (zone.get().x() / 16.0);
            float z = (float) (zone.get().z() / 16.0);
            quad(quads, matrix, x - 0.75F, 0.06F, z - 0.875F, x + 0.75F, z + 0.875F);
        }
        poses.popPose();
        buffer.endBatch();
    }

    private static void quad(VertexConsumer quads, Matrix4f matrix,
                             float x0, float y, float z0, float x1, float z1) {
        quads.vertex(matrix, x0, y, z0).color(90, 255, 120, 90).endVertex();
        quads.vertex(matrix, x0, y, z1).color(90, 255, 120, 90).endVertex();
        quads.vertex(matrix, x1, y, z1).color(90, 255, 120, 90).endVertex();
        quads.vertex(matrix, x1, y, z0).color(90, 255, 120, 90).endVertex();
    }
}
