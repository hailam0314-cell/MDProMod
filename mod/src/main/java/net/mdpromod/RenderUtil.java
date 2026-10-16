package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.VertexConsumer;
import net.minecraft.client.renderer.texture.OverlayTexture;
import org.joml.Matrix4f;

public final class RenderUtil {
    private RenderUtil() {
    }

    public static void flatQuad(PoseStack poseStack, VertexConsumer consumer,
                                float width, float depth, float y,
                                float u0, float v0, float u1, float v1,
                                int packedLight) {
        Matrix4f matrix = poseStack.last().pose();
        float x0 = -width / 2.0F;
        float x1 = width / 2.0F;
        float z0 = -depth / 2.0F;
        float z1 = depth / 2.0F;
        vertex(consumer, matrix, x0, y, z0, u0, v0, packedLight);
        vertex(consumer, matrix, x1, y, z0, u1, v0, packedLight);
        vertex(consumer, matrix, x1, y, z1, u1, v1, packedLight);
        vertex(consumer, matrix, x0, y, z1, u0, v1, packedLight);
    }

    /** Flat top quad with a solid tint, used by the in-world duel controls. */
    public static void flatQuadColor(PoseStack poseStack, VertexConsumer consumer,
                                     float width, float depth, float y,
                                     int red, int green, int blue, int alpha,
                                     int packedLight) {
        Matrix4f matrix = poseStack.last().pose();
        float x0 = -width / 2.0F, x1 = width / 2.0F;
        float z0 = -depth / 2.0F, z1 = depth / 2.0F;
        colorVertex(consumer, matrix, x0, y, z0, red, green, blue, alpha, packedLight);
        colorVertex(consumer, matrix, x1, y, z0, red, green, blue, alpha, packedLight);
        colorVertex(consumer, matrix, x1, y, z1, red, green, blue, alpha, packedLight);
        colorVertex(consumer, matrix, x0, y, z1, red, green, blue, alpha, packedLight);
    }

    private static void colorVertex(VertexConsumer consumer, Matrix4f matrix,
                                    float x, float y, float z,
                                    int red, int green, int blue, int alpha, int packedLight) {
        consumer.vertex(matrix, x, y, z)
                .color(red, green, blue, alpha)
                .uv(0.0F, 0.0F)
                .overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(packedLight)
                .normal(0, 1, 0)
                .endVertex();
    }

    public static void box(PoseStack poseStack, VertexConsumer consumer,
                           float x0, float y0, float z0,
                           float x1, float y1, float z1,
                           float[][] uv, int packedLight) {
        Matrix4f matrix = poseStack.last().pose();
        // north (-Z)
        face(consumer, matrix, x0, y1, z0, x1, y1, z0, x1, y0, z0, x0, y0, z0,
                uv[0], packedLight, 0, 0, -1);
        // south (+Z)
        face(consumer, matrix, x1, y1, z1, x0, y1, z1, x0, y0, z1, x1, y0, z1,
                uv[1], packedLight, 0, 0, 1);
        // east (+X)
        face(consumer, matrix, x1, y1, z0, x1, y1, z1, x1, y0, z1, x1, y0, z0,
                uv[2], packedLight, 1, 0, 0);
        // west (-X)
        face(consumer, matrix, x0, y1, z1, x0, y1, z0, x0, y0, z0, x0, y0, z1,
                uv[3], packedLight, -1, 0, 0);
        // up (+Y)
        face(consumer, matrix, x0, y1, z1, x1, y1, z1, x1, y1, z0, x0, y1, z0,
                uv[4], packedLight, 0, 1, 0);
        // down (-Y)
        face(consumer, matrix, x0, y0, z0, x1, y0, z0, x1, y0, z1, x0, y0, z1,
                uv[5], packedLight, 0, -1, 0);
    }

    private static void vertex(VertexConsumer consumer, Matrix4f matrix,
                               float x, float y, float z,
                               float u, float v, int packedLight) {
        consumer.vertex(matrix, x, y, z)
                .color(255, 255, 255, 255)
                .uv(u, v)
                .overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(packedLight)
                .normal(0, 1, 0)
                .endVertex();
    }

    /** 竖直立起来的卡面：front=true 朝 +Z，false 朝 -Z（背面）。 */
    public static void uprightQuad(PoseStack poseStack, VertexConsumer consumer,
                                   float width, float height, float z,
                                   float u0, float v0, float u1, float v1,
                                   int packedLight, boolean front) {
        Matrix4f matrix = poseStack.last().pose();
        float x0 = -width / 2.0F;
        float x1 = width / 2.0F;
        float y0 = 0.0F;
        float y1 = height;
        float nx = front ? 0.0F : 0.0F;
        float nz = front ? 1.0F : -1.0F;
        if (front) {
            quadVertex(consumer, matrix, x0, y1, z, u0, v0, packedLight, nx, 0, nz);
            quadVertex(consumer, matrix, x1, y1, z, u1, v0, packedLight, nx, 0, nz);
            quadVertex(consumer, matrix, x1, y0, z, u1, v1, packedLight, nx, 0, nz);
            quadVertex(consumer, matrix, x0, y0, z, u0, v1, packedLight, nx, 0, nz);
        } else {
            quadVertex(consumer, matrix, x1, y1, z, u0, v0, packedLight, nx, 0, nz);
            quadVertex(consumer, matrix, x0, y1, z, u1, v0, packedLight, nx, 0, nz);
            quadVertex(consumer, matrix, x0, y0, z, u1, v1, packedLight, nx, 0, nz);
            quadVertex(consumer, matrix, x1, y0, z, u0, v1, packedLight, nx, 0, nz);
        }
    }

    private static void face(VertexConsumer consumer, Matrix4f matrix,
                             float ax, float ay, float az,
                             float bx, float by, float bz,
                             float cx, float cy, float cz,
                             float dx, float dy, float dz,
                             float[] uv, int packedLight,
                             float nx, float ny, float nz) {
        quadVertex(consumer, matrix, ax, ay, az, uv[0], uv[1], packedLight, nx, ny, nz);
        quadVertex(consumer, matrix, bx, by, bz, uv[2], uv[1], packedLight, nx, ny, nz);
        quadVertex(consumer, matrix, cx, cy, cz, uv[2], uv[3], packedLight, nx, ny, nz);
        quadVertex(consumer, matrix, dx, dy, dz, uv[0], uv[3], packedLight, nx, ny, nz);
    }

    private static void quadVertex(VertexConsumer consumer, Matrix4f matrix,
                                   float x, float y, float z,
                                   float u, float v, int packedLight,
                                   float nx, float ny, float nz) {
        consumer.vertex(matrix, x, y, z)
                .color(255, 255, 255, 255)
                .uv(u, v)
                .overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(packedLight)
                .normal(nx, ny, nz)
                .endVertex();
    }
}
