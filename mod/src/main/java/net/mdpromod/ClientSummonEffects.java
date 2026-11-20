package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.Tesselator;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.math.Axis;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.Font.DisplayMode;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.client.renderer.MultiBufferSource.BufferSource;
import net.minecraft.client.renderer.texture.OverlayTexture;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;
import org.joml.Matrix4f;
import org.joml.Quaternionf;
import org.slf4j.LoggerFactory;

@OnlyIn(Dist.CLIENT)
public final class ClientSummonEffects {
    private static final boolean ENABLED = Boolean.parseBoolean(System.getProperty("mdpromod.summonFx", "true"));
    private static final String[][] LAYERS = new String[][]{
        {"fusion_010", "fusion_011", "flash"},
        {"synchro_noise", "synchro_line", "flash"},
        {"fusion_011", "flash", "fusion_010"},
        {"link_gate", "link_line", "flash"},
        {"ritual_circle_03", "ritual_circle_01", "flash"},
        {"pendulum_mask", "flash", "synchro_noise"}
    };
    private static final int[] COLORS = new int[]{12619007, 6750132, 16765517, 5089023, 5944575, 9240476};
    private static final float[] LAYER_SCALE = new float[]{1.0F, 0.66F, 0.36F};
    private static final float[] LAYER_ALPHA = new float[]{0.75F, 0.95F, 0.85F};
    private static final Map<Integer, String> RING_OVERRIDE = Map.of(1, "techring_03");
    private static final List<ClientSummonEffects.Active> ACTIVE = new ArrayList<>();
    private static ClientLevel level;
    private static final String[] MESH_NAME = new String[]{
        "fxl_dithit_Ring_001", "SynchroCircle01", "XYZInMesh01", "SummonLinkTrail02", "SummonRitualCylinder02", "SummonPendulumChoker02"
    };
    private static final String[] MESH_TEXTURE = new String[]{"fusion_011", "synchro_line", "synchro_noise", "synchro_line", "ritual_fire_01", "pendulum_mask"};
    private static final float[] MESH_WIDTH = new float[]{9.0F, 9.5F, 6.0F, 7.5F, 7.0F, 7.5F};
    private static final int[] LINK_SLOT_ANGLES = new int[]{315, 225, 45, 135, 270, 0, 90, 180};

    private static String ringTextureName(int kind, int layer) {
        String override = RING_OVERRIDE.get(kind);
        return override != null ? override : LAYERS[kind][layer];
    }

    private ClientSummonEffects() {
    }

    public static void play(ModNetwork.SummonEffect message) {
        if (ENABLED) {
            resetForLevel(Minecraft.getInstance());
            ACTIVE.add(
                new ClientSummonEffects.Active(
                    message.fieldEntityId(),
                    message.kind(),
                    message.phase(),
                    message.code(),
                    message.materialCount(),
                    new Vec3(message.x(), message.y(), message.z()),
                    System.currentTimeMillis(),
                    Math.max(300, message.durationMs())
                )
            );
            if (message.phase() == 1) {
                StringBuilder layers = new StringBuilder();

                for (String layer : LAYERS[Math.max(0, Math.min(LAYERS.length - 1, message.kind()))]) {
                    if (layer != null) {
                        if (layers.length() > 0) {
                            layers.append(',');
                        }

                        layers.append(layer).append('=').append(SummonFxTextures.get(layer) != null ? "ok" : "MISSING");
                    }
                }

                LoggerFactory.getLogger("mdpromod")
                    .info(
                        "Summon FX recv: kind={} code={} materials={} at=({}, {}) textures=[{}]",
                        new Object[]{message.kind(), message.code(), message.materialCount(), message.x(), message.z(), layers}
                    );
            }
        }
    }

    public static void stopField(int fieldId) {
        ACTIVE.removeIf(active -> active.fieldEntityId() == fieldId);
    }

    public static void tick(Minecraft minecraft) {
        resetForLevel(minecraft);
        if (!ACTIVE.isEmpty()) {
            long now = System.currentTimeMillis();
            ACTIVE.removeIf(active -> now - active.startMs() > active.durationMs());
        }
    }

    private static void resetForLevel(Minecraft minecraft) {
        if (level != minecraft.level) {
            ACTIVE.clear();
            level = minecraft.level;
        }
    }

    public static long circleStartMs(int fieldEntityId) {
        for (int i = ACTIVE.size() - 1; i >= 0; i--) {
            if (ACTIVE.get(i).fieldEntityId() == fieldEntityId && ACTIVE.get(i).phase() == 1) {
                return ACTIVE.get(i).startMs();
            }
        }

        return 0L;
    }

    public static int circleDurationMs(int fieldEntityId) {
        for (int i = ACTIVE.size() - 1; i >= 0; i--) {
            if (ACTIVE.get(i).fieldEntityId() == fieldEntityId && ACTIVE.get(i).phase() == 1) {
                return ACTIVE.get(i).durationMs();
            }
        }

        return 0;
    }

    public static void render(Minecraft minecraft, PoseStack poses) {
        resetForLevel(minecraft);
        if (!ACTIVE.isEmpty() && minecraft.level != null) {
            Vec3 camera = minecraft.gameRenderer.getMainCamera().getPosition();
            long now = System.currentTimeMillis();
            BufferSource buffer = MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());

            for (ClientSummonEffects.Active active : ACTIVE) {
                long ageMs = now - active.startMs();
                float t = Math.min(1.0F, (float)ageMs / active.durationMs());
                float ease = 1.0F - (1.0F - t) * (1.0F - t) * (1.0F - t);
                int kind = Math.max(0, Math.min(LAYERS.length - 1, active.kind()));
                if (kind != 5) {
                    int color = COLORS[kind];
                    int red = color >> 16 & 0xFF;
                    int green = color >> 8 & 0xFF;
                    int blue = color & 0xFF;
                    if (active.phase() == 0) {
                        float alpha = Math.min(1.0F, t * 3.0F) * (1.0F - Math.max(0.0F, t - 0.7F) * 3.3F) * 0.8F;
                        if (kind == 2) {
                            vortex(buffer, poses, camera, active, ageMs);
                        } else {
                            ring(
                                buffer,
                                poses,
                                camera,
                                active.at(),
                                ringTextureName(kind, 0),
                                0.9F + 3.1F * ease,
                                kind == 3 ? 0.0F : (float)ageMs / 1000.0F * 110.0F,
                                red,
                                green,
                                blue,
                                alpha,
                                0.04F
                            );
                        }
                    } else {
                        float appear = Math.min(1.0F, t / 0.35F);
                        float leave = t < 0.78F ? 1.0F : Math.max(0.0F, 1.0F - (t - 0.78F) / 0.22F);
                        float open = 1.0F - (1.0F - appear) * (1.0F - appear);
                        float alpha = open * leave * 0.95F;
                        int layerCount = Math.max(1, Math.min(layerCount(active), LAYERS[kind].length));
                        float size = 3.5F + 8.5F * open + 1.5F * (1.0F - leave);
                        float spin = (kind % 2 == 0 ? 1.0F : -1.0F) * ((float)ageMs / 1000.0F) * 62.0F;
                        if (kind == 3) {
                            spin = 0.0F;
                        }

                        if (kind == 2) {
                            vortex(buffer, poses, camera, active, ageMs);
                        } else {
                            for (int layer = 0; layer < layerCount; layer++) {
                                ring(
                                    buffer,
                                    poses,
                                    camera,
                                    active.at(),
                                    ringTextureName(kind, layer),
                                    size * LAYER_SCALE[Math.min(layer, LAYER_SCALE.length - 1)],
                                    spin * (layer % 2 == 0 ? 1.0F : -1.35F),
                                    red,
                                    green,
                                    blue,
                                    alpha * LAYER_ALPHA[Math.min(layer, LAYER_ALPHA.length - 1)],
                                    0.06F + layer * 0.03F + open * 0.25F
                                );
                            }
                        }

                        column(buffer, poses, camera, active.at(), t, alpha, red, green, blue);
                        int level = active.code() > 0 ? CardRegistry.get(active.code()).map(CardInfo::level).orElse(0) : 0;
                        String meshName = kind == 1 ? (level >= 9 ? "SynchroCircle03" : (level >= 5 ? "SynchroCircle01" : "SynchroCircle02")) : MESH_NAME[kind];
                        float meshSpin = spin * (kind == 2 ? 1.8F : (kind == 4 ? 0.5F : 1.0F));

                        float meshLift = switch (kind) {
                            case 2 -> 0.2F + open * 2.2F;
                            case 4 -> 0.0F;
                            default -> 0.08F + open * 0.3F;
                        };

                        float meshScaleY = switch (kind) {
                            case 2 -> 0.7F + open * 0.9F;
                            case 4 -> 0.5F + open * 1.6F;
                            default -> 1.0F;
                        };
                        mesh(
                            buffer,
                            poses,
                            camera,
                            active.at(),
                            meshName,
                            MESH_TEXTURE[kind],
                            MESH_WIDTH[kind],
                            meshSpin,
                            meshScaleY,
                            meshLift,
                            red,
                            green,
                            blue,
                            alpha * 0.9F
                        );
                        if (kind == 1) {
                            synchroStars(minecraft, buffer, poses, camera, active, level, alpha);
                        }

                        if (kind == 3) {
                            int linkCount = level > 0 ? level : active.materialCount();
                            linkArrowOverlays(minecraft, buffer, poses, camera, active, size, open, linkCount);
                        }

                        details(minecraft, buffer, poses, camera, active, kind, size, open, alpha);
                    }
                }
            }

            buffer.endBatch();
        }
    }

    private static void ring(
        MultiBufferSource buffer,
        PoseStack poses,
        Vec3 camera,
        Vec3 at,
        String textureName,
        float size,
        float spin,
        int red,
        int green,
        int blue,
        float alpha,
        float lift
    ) {
        ResourceLocation texture = SummonFxTextures.get(textureName);
        if (texture != null) {
            VertexConsumer consumer = buffer.getBuffer(RenderType.entityTranslucentEmissive(texture));
            poses.pushPose();
            poses.translate(at.x - camera.x, (float)at.y + lift - camera.y, at.z - camera.z);
            poses.mulPose(Axis.YP.rotationDegrees(spin));
            flatQuad(consumer, poses.last().pose(), size, red, green, blue, Math.max(0, Math.min(255, Math.round(alpha * 255.0F))));
            poses.popPose();
        }
    }

    private static void column(MultiBufferSource buffer, PoseStack poses, Vec3 camera, Vec3 at, float t, float alpha, int red, int green, int blue) {
        ResourceLocation texture = SummonFxTextures.get("black_circle");
        if (texture != null && !(alpha <= 0.02F)) {
            VertexConsumer consumer = buffer.getBuffer(RenderType.entityTranslucentEmissive(texture));
            float grow = Math.max(0.0F, Math.min(1.0F, (t - 0.06F) / 0.3F));
            float fade = t < 0.8F ? 1.0F : Math.max(0.0F, 1.0F - (t - 0.8F) / 0.2F);
            float height = 5.6F * grow * fade;
            if (!(height <= 0.05F) && !(fade <= 0.01F)) {
                float halfBottom = 1.85F;
                float halfTop = 0.8F;
                int bottom = Math.max(0, Math.min(255, Math.round(alpha * 88.0F * fade)));

                for (float yaw : new float[]{0.0F, 90.0F}) {
                    poses.pushPose();
                    poses.translate(at.x - camera.x, (float)at.y - camera.y, at.z - camera.z);
                    poses.mulPose(Axis.YP.rotationDegrees(yaw));
                    Matrix4f matrix = poses.last().pose();
                    columnQuad(consumer, matrix, halfBottom, halfTop, height, red, green, blue, bottom);
                    poses.popPose();
                }
            }
        }
    }

    private static void columnQuad(
        VertexConsumer consumer, Matrix4f matrix, float halfBottom, float halfTop, float height, int red, int green, int blue, int bottomAlpha
    ) {
        consumer.vertex(matrix, -halfBottom, 0.0F, 0.0F)
            .color(red, green, blue, bottomAlpha)
            .uv(0.0F, 0.58F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, 1.0F, 0.0F)
            .endVertex();
        consumer.vertex(matrix, halfBottom, 0.0F, 0.0F)
            .color(red, green, blue, bottomAlpha)
            .uv(1.0F, 0.58F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, 1.0F, 0.0F)
            .endVertex();
        consumer.vertex(matrix, halfTop, height, 0.0F)
            .color(red, green, blue, 0)
            .uv(1.0F, 0.42F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, 1.0F, 0.0F)
            .endVertex();
        consumer.vertex(matrix, -halfTop, height, 0.0F)
            .color(red, green, blue, 0)
            .uv(0.0F, 0.42F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, 1.0F, 0.0F)
            .endVertex();
        consumer.vertex(matrix, halfTop, height, 0.0F)
            .color(red, green, blue, 0)
            .uv(1.0F, 0.42F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, -1.0F, 0.0F)
            .endVertex();
        consumer.vertex(matrix, halfBottom, 0.0F, 0.0F)
            .color(red, green, blue, bottomAlpha)
            .uv(1.0F, 0.58F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, -1.0F, 0.0F)
            .endVertex();
        consumer.vertex(matrix, -halfBottom, 0.0F, 0.0F)
            .color(red, green, blue, bottomAlpha)
            .uv(0.0F, 0.58F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, -1.0F, 0.0F)
            .endVertex();
        consumer.vertex(matrix, -halfTop, height, 0.0F)
            .color(red, green, blue, 0)
            .uv(0.0F, 0.42F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, -1.0F, 0.0F)
            .endVertex();
    }

    private static void mesh(
        MultiBufferSource buffer,
        PoseStack poses,
        Vec3 camera,
        Vec3 at,
        String name,
        String textureName,
        float targetWidth,
        float spinDeg,
        float scaleY,
        float lift,
        int red,
        int green,
        int blue,
        float alpha
    ) {
        SummonMeshes.Mesh mesh = SummonMeshes.get(name);
        ResourceLocation texture = SummonFxTextures.get(textureName);
        if (mesh != null && texture != null) {
            VertexConsumer consumer = buffer.getBuffer(RenderType.entityTranslucentEmissive(texture));
            float scale = targetWidth / Math.max(0.001F, mesh.radius() * 2.0F);
            int value = Math.max(0, Math.min(255, Math.round(alpha * 255.0F)));
            float[] positions = mesh.positions();
            float[] uvs = mesh.uvs();
            int[] indices = mesh.indices();
            int[] uvIndices = mesh.uvIndices();
            poses.pushPose();
            poses.translate(at.x - camera.x, at.y + lift - camera.y, at.z - camera.z);
            poses.mulPose(Axis.YP.rotationDegrees(spinDeg));
            poses.scale(scale, scale * scaleY, scale);
            Matrix4f matrix = poses.last().pose();

            for (int i = 0; i + 2 < indices.length; i += 3) {
                triangle(
                    consumer,
                    matrix,
                    positions,
                    uvs,
                    indices[i],
                    uvIndices[i],
                    indices[i + 1],
                    uvIndices[i + 1],
                    indices[i + 2],
                    uvIndices[i + 2],
                    red,
                    green,
                    blue,
                    value
                );
                triangle(
                    consumer,
                    matrix,
                    positions,
                    uvs,
                    indices[i + 2],
                    uvIndices[i + 2],
                    indices[i + 1],
                    uvIndices[i + 1],
                    indices[i],
                    uvIndices[i],
                    red,
                    green,
                    blue,
                    value
                );
            }

            poses.popPose();
        }
    }

    private static float smoother(float t) {
        t = Math.max(0.0F, Math.min(1.0F, t));
        return t * t * t * (t * (t * 6.0F - 15.0F) + 10.0F);
    }

    private static void billboardQuad(
        VertexConsumer consumer,
        PoseStack poses,
        Quaternionf cameraRotation,
        Vec3 camera,
        double x,
        double y,
        double z,
        float size,
        int red,
        int green,
        int blue,
        int alpha
    ) {
        poses.pushPose();
        poses.translate(x - camera.x, y - camera.y, z - camera.z);
        poses.mulPose(cameraRotation);
        poses.mulPose(Axis.YP.rotationDegrees(180.0F));
        float half = size / 2.0F;
        Matrix4f matrix = poses.last().pose();
        consumer.vertex(matrix, -half, -half, 0.0F)
            .color(red, green, blue, alpha)
            .uv(0.0F, 1.0F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, 0.0F, 1.0F)
            .endVertex();
        consumer.vertex(matrix, -half, half, 0.0F)
            .color(red, green, blue, alpha)
            .uv(0.0F, 0.0F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, 0.0F, 1.0F)
            .endVertex();
        consumer.vertex(matrix, half, half, 0.0F)
            .color(red, green, blue, alpha)
            .uv(1.0F, 0.0F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, 0.0F, 1.0F)
            .endVertex();
        consumer.vertex(matrix, half, -half, 0.0F)
            .color(red, green, blue, alpha)
            .uv(1.0F, 1.0F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, 0.0F, 1.0F)
            .endVertex();
        poses.popPose();
    }

    private static void vortex(MultiBufferSource buffer, PoseStack poses, Vec3 camera, ClientSummonEffects.Active active, long ageMs) {
        float gms = active.phase() == 0 ? (float)ageMs : 1390.0F + (float)ageMs;
        float t = Math.max(0.0F, Math.min(1.0F, (float)ageMs / active.durationMs()));
        float alpha = active.phase() == 0
            ? Math.min(1.0F, Math.max(0.0F, (float)ageMs / 420.0F))
            : (t < 0.78F ? 1.0F : Math.max(0.0F, 1.0F - (t - 0.78F) / 0.22F));
        ring(buffer, poses, camera, active.at(), "xyz_galaxy_v1", 12.4F, -gms / 1000.0F * 22.0F, 255, 255, 255, alpha, 0.07F);
    }

    private static void synchroStars(
        Minecraft minecraft, MultiBufferSource buffer, PoseStack poses, Vec3 camera, ClientSummonEffects.Active active, int level, float alphaMul
    ) {
        ResourceLocation texture = SummonFxTextures.get("star");
        if (texture != null && level > 0) {
            float t = Math.min(1.0F, (float)(System.currentTimeMillis() - active.startMs()) / active.durationMs());
            if (!(t < 0.2F) && !(t >= 0.72F)) {
                VertexConsumer consumer = buffer.getBuffer(RenderType.entityTranslucentEmissive(texture));
                Quaternionf rotation = minecraft.gameRenderer.getMainCamera().rotation();
                int total = Math.max(1, Math.min(12, level));

                for (int s = 0; s < total; s++) {
                    double baseAngle = s * Math.PI * 2.0 / total;
                    float pop = Math.min(1.0F, (t - 0.2F) / 0.05F);
                    float fall = Math.max(0.0F, Math.min(1.0F, (t - 0.4F) / 0.32F));
                    float se = smoother(fall);
                    double angle = baseAngle + (Math.PI / 2) * se;
                    float radius = 0.9F * (1.0F - se);
                    float height = 1.1F + -0.91F * se;
                    float vanish = 1.0F - Math.max(0.0F, (fall - 0.85F) / 0.15F);
                    float size = 0.6F * (0.35F + 0.65F * pop) * Math.max(0.0F, vanish);
                    if (!(size <= 0.01F)) {
                        billboardQuad(
                            consumer,
                            poses,
                            rotation,
                            camera,
                            Math.cos(angle) * radius,
                            height,
                            Math.sin(angle) * radius,
                            size,
                            255,
                            247,
                            209,
                            Math.round(0.95F * Math.max(0.0F, vanish) * alphaMul * 255.0F)
                        );
                    }
                }
            }
        }
    }

    private static void linkArrowOverlays(
        Minecraft minecraft, MultiBufferSource buffer, PoseStack poses, Vec3 camera, ClientSummonEffects.Active active, float size, float open, int linkCount
    ) {
        ResourceLocation texture = SummonFxTextures.get("star");
        if (texture != null) {
            float f = Math.min(1.0F, (float)(System.currentTimeMillis() - active.startMs()) / active.durationMs());
            float flightFrac = 2396.0F / Math.max(1.0F, active.durationMs());
            int count = Math.max(1, Math.min(6, active.materialCount()));
            int arrows = Math.max(1, Math.min(8, linkCount));
            VertexConsumer consumer = buffer.getBuffer(RenderType.entityTranslucentEmissive(texture));
            float lift = 0.06F + open * 0.25F + 0.02F;

            for (int k = 0; k < count && k < arrows; k++) {
                float arrive = 0.02F + (k + 1) * flightFrac;
                if (!(f < arrive)) {
                    int ang = LINK_SLOT_ANGLES[k % LINK_SLOT_ANGLES.length];
                    boolean corner = ang % 90 != 0;
                    float rBase = (corner ? 0.502F : 0.408F) * size;
                    float rTip = (corner ? 0.6F : 0.5F) * size;
                    double halfAngle = Math.toRadians(corner ? 10.2 : 11.5);
                    double theta = Math.toRadians(ang);
                    poses.pushPose();
                    poses.translate(
                        active.at().x - camera.x, active.at().y + lift - camera.y, active.at().z - camera.z
                    );
                    Matrix4f matrix = poses.last().pose();
                    int alpha = 235;
                    tri(consumer, matrix, Math.cos(theta - halfAngle) * rBase, Math.sin(theta - halfAngle) * rBase, alpha);
                    tri(consumer, matrix, Math.cos(theta + halfAngle) * rBase, Math.sin(theta + halfAngle) * rBase, alpha);
                    tri(consumer, matrix, Math.cos(theta) * rTip, Math.sin(theta) * rTip, alpha);
                    poses.popPose();
                }
            }
        }
    }

    private static void tri(VertexConsumer consumer, Matrix4f matrix, double x, double z, int alpha) {
        consumer.vertex(matrix, (float)x, 0.0F, (float)z)
            .color(255, 64, 64, alpha)
            .uv(0.5F, 0.5F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, 1.0F, 0.0F)
            .endVertex();
    }

    private static void triangle(
        VertexConsumer consumer,
        Matrix4f matrix,
        float[] positions,
        float[] uvs,
        int v0,
        int t0,
        int v1,
        int t1,
        int v2,
        int t2,
        int red,
        int green,
        int blue,
        int alpha
    ) {
        vertex(consumer, matrix, positions, uvs, v0, t0, red, green, blue, alpha);
        vertex(consumer, matrix, positions, uvs, v1, t1, red, green, blue, alpha);
        vertex(consumer, matrix, positions, uvs, v2, t2, red, green, blue, alpha);
    }

    private static void vertex(
        VertexConsumer consumer, Matrix4f matrix, float[] positions, float[] uvs, int vertexIndex, int uvIndex, int red, int green, int blue, int alpha
    ) {
        int vi = vertexIndex * 3;
        if (vi + 2 < positions.length) {
            int ti = uvIndex * 2;
            float u = ti + 1 < uvs.length ? uvs[ti] : 0.0F;
            float v = ti + 1 < uvs.length ? 1.0F - uvs[ti + 1] : 0.0F;
            consumer.vertex(matrix, positions[vi], positions[vi + 1], positions[vi + 2])
                .color(red, green, blue, alpha)
                .uv(u, v)
                .overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(15728880)
                .normal(0.0F, 1.0F, 0.0F)
                .endVertex();
        }
    }

    private static void details(
        Minecraft minecraft,
        MultiBufferSource buffer,
        PoseStack poses,
        Vec3 camera,
        ClientSummonEffects.Active active,
        int kind,
        float size,
        float ease,
        float alpha
    ) {
        int code = active.code();
        if (code > 0) {
            int level = CardRegistry.get(code).map(CardInfo::level).orElse(0);
            if (kind == 1 && level > 0) {
                text(minecraft, buffer, poses, camera, Integer.toString(level), active.at(), 1.2F + ease * 0.8F, 3.4F, alpha);
            }
        }
    }

    private static void text(
        Minecraft minecraft, MultiBufferSource buffer, PoseStack poses, Vec3 camera, String value, Vec3 at, float height, float scale, float alpha
    ) {
        Vec3 toCamera = new Vec3(camera.x - at.x, 0.0, camera.z - at.z);
        if (toCamera.lengthSqr() < 1.0E-4) {
            toCamera = new Vec3(0.0, 0.0, 1.0);
        }

        float yaw = (float)Math.toDegrees(Math.atan2(toCamera.x, toCamera.z));
        int color = 16777215 | Math.max(0, Math.min(255, Math.round(alpha * 255.0F))) << 24;
        poses.pushPose();
        poses.translate(at.x - camera.x, at.y + height - camera.y, at.z - camera.z);
        poses.mulPose(Axis.YP.rotationDegrees(yaw));
        poses.scale(-scale, -scale, scale);
        float width = minecraft.font.width(value);
        minecraft.font
            .drawInBatch(
                Component.literal(value), -width / 2.0F, -4.0F, color, true, poses.last().pose(), buffer, DisplayMode.SEE_THROUGH, 0, 15728880
            );
        poses.popPose();
    }

    private static int layerCount(ClientSummonEffects.Active active) {
        int level = active.code() > 0 ? CardRegistry.get(active.code()).map(CardInfo::level).orElse(0) : 0;

        return switch (active.kind()) {
            case 0 -> Math.max(1, Math.min(3, active.materialCount()));
            case 1 -> level < 5 ? 1 : (level < 9 ? 2 : 3);
            default -> 3;
        };
    }

    private static void flatQuad(VertexConsumer consumer, Matrix4f matrix, float size, int red, int green, int blue, int alpha) {
        float half = size / 2.0F;
        consumer.vertex(matrix, -half, 0.0F, -half)
            .color(red, green, blue, alpha)
            .uv(0.0F, 0.0F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, 1.0F, 0.0F)
            .endVertex();
        consumer.vertex(matrix, -half, 0.0F, half)
            .color(red, green, blue, alpha)
            .uv(0.0F, 1.0F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, 1.0F, 0.0F)
            .endVertex();
        consumer.vertex(matrix, half, 0.0F, half)
            .color(red, green, blue, alpha)
            .uv(1.0F, 1.0F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, 1.0F, 0.0F)
            .endVertex();
        consumer.vertex(matrix, half, 0.0F, -half)
            .color(red, green, blue, alpha)
            .uv(1.0F, 0.0F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, 1.0F, 0.0F)
            .endVertex();
        consumer.vertex(matrix, half, 0.0F, -half)
            .color(red, green, blue, alpha)
            .uv(1.0F, 0.0F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, -1.0F, 0.0F)
            .endVertex();
        consumer.vertex(matrix, half, 0.0F, half)
            .color(red, green, blue, alpha)
            .uv(1.0F, 1.0F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, -1.0F, 0.0F)
            .endVertex();
        consumer.vertex(matrix, -half, 0.0F, half)
            .color(red, green, blue, alpha)
            .uv(0.0F, 1.0F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, -1.0F, 0.0F)
            .endVertex();
        consumer.vertex(matrix, -half, 0.0F, -half)
            .color(red, green, blue, alpha)
            .uv(0.0F, 0.0F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, -1.0F, 0.0F)
            .endVertex();
    }

    private static void verticalQuad(VertexConsumer consumer, Matrix4f matrix, float width, float height, int red, int green, int blue, int alpha, float yaw) {
        float half = width / 2.0F;
        double radians = Math.toRadians(yaw);
        float dx = (float)Math.sin(radians) * half;
        float dz = (float)Math.cos(radians) * half;
        float halfHeight = height / 2.0F;
        consumer.vertex(matrix, -dx, -halfHeight, -dz)
            .color(red, green, blue, alpha)
            .uv(0.0F, 1.0F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(-dz, 0.0F, dx)
            .endVertex();
        consumer.vertex(matrix, dx, -halfHeight, dz)
            .color(red, green, blue, alpha)
            .uv(1.0F, 1.0F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(-dz, 0.0F, dx)
            .endVertex();
        consumer.vertex(matrix, dx, halfHeight, dz)
            .color(red, green, blue, alpha)
            .uv(1.0F, 0.0F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(-dz, 0.0F, dx)
            .endVertex();
        consumer.vertex(matrix, -dx, halfHeight, -dz)
            .color(red, green, blue, alpha)
            .uv(0.0F, 0.0F)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(-dz, 0.0F, dx)
            .endVertex();
    }

    private record Active(int fieldEntityId, int kind, int phase, int code, int materialCount, Vec3 at, long startMs, int durationMs) {
    }
}
