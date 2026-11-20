package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.Tesselator;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.math.Axis;
import java.util.HashSet;
import java.util.Set;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.client.renderer.MultiBufferSource.BufferSource;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;
import org.joml.Matrix4f;

@OnlyIn(Dist.CLIENT)
public final class ClientCardSelect {
    private static int[] selectable = new int[0];
    private static Set<Integer> selected = new HashSet<>();

    private ClientCardSelect() {
    }

    public static void apply(int[] selectableIds, int[] selectedIds) {
        selectable = selectableIds == null ? new int[0] : selectableIds;
        Set<Integer> next = new HashSet<>();
        if (selectedIds != null) {
            for (int id : selectedIds) {
                next.add(id);
            }
        }

        selected = next;
    }

    public static boolean isSelectable(int entityId) {
        for (int id : selectable) {
            if (id == entityId) {
                return true;
            }
        }

        return false;
    }

    public static boolean isSelected(int entityId) {
        return selected.contains(entityId);
    }

    public static void tick(Minecraft minecraft) {
        if (selectable.length != 0 && minecraft.level != null) {
            for (int entityId : selectable) {
                Entity entity = minecraft.level.getEntity(entityId);
                if (entity != null) {
                    boolean chosen = selected.contains(entityId);
                    double spread = 0.28;
                    double x = entity.getX() + (minecraft.level.random.nextDouble() - 0.5) * spread;
                    double y = entity.getY() + minecraft.level.random.nextDouble() * entity.getBbHeight();
                    double z = entity.getZ() + (minecraft.level.random.nextDouble() - 0.5) * spread;
                    minecraft.level.addParticle(chosen ? ParticleTypes.END_ROD : ParticleTypes.HAPPY_VILLAGER, x, y, z, 0.0, 0.01, 0.0);
                }
            }
        }
    }

    public static void render(Minecraft minecraft, PoseStack poses) {
        if (selectable.length != 0 && minecraft.level != null) {
            Vec3 cam = minecraft.gameRenderer.getMainCamera().getPosition();
            BufferSource buffer = MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());
            VertexConsumer quads = buffer.getBuffer(RenderType.debugQuads());

            for (int entityId : selectable) {
                Entity entity = minecraft.level.getEntity(entityId);
                if (entity != null) {
                    boolean chosen = selected.contains(entityId);
                    poses.pushPose();
                    poses.translate(entity.getX() - cam.x, entity.getY() - cam.y, entity.getZ() - cam.z);
                    poses.mulPose(Axis.YP.rotationDegrees(-entity.getYRot()));
                    Matrix4f matrix = poses.last().pose();
                    float hw = 0.33F;
                    float hh = 0.46F;
                    float y = hh * 0.95F;
                    int r = chosen ? 255 : 120;
                    int g = chosen ? 220 : 255;
                    int b = chosen ? 90 : 140;
                    quad(quads, matrix, -hw, y - hh, hw, y + hh, r, g, b);
                    poses.popPose();
                }
            }

            buffer.endBatch();
        }
    }

    private static void quad(VertexConsumer quads, Matrix4f matrix, float x0, float y0, float x1, float y1, int r, int g, int b) {
        quads.vertex(matrix, x0, y0, -0.012F).color(r, g, b, 110).endVertex();
        quads.vertex(matrix, x0, y1, -0.012F).color(r, g, b, 110).endVertex();
        quads.vertex(matrix, x1, y1, -0.012F).color(r, g, b, 110).endVertex();
        quads.vertex(matrix, x1, y0, -0.012F).color(r, g, b, 110).endVertex();
    }
}
