package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.Tesselator;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.math.Axis;
import net.minecraft.client.Minecraft;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.client.renderer.LightTexture;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.client.renderer.texture.OverlayTexture;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;
import org.joml.Matrix4f;

import java.util.ArrayList;
import java.util.List;

/**
 * 召唤演出（六类正规出场）。两相：
 *
 * - phase 0「素材吸入」：场地中央先起一个小法阵，材料卡被吸过来（材料卡的飞行目标由服务端改到中央）。
 * - phase 1「正式召唤」：中央放该类型的大法阵（最宽约 12 格）+ 光柱 + 类型细节，怪兽随后从中央落到格子。
 *
 * 类型细节：同调按等级分档（星环层数 + 等级数字）、超量飘出叠放素材、连接按 Link 数放射箭头、
 * 融合按素材数量决定法阵层数。贴图取自 MD（`runtime\summon_fx`），配色照 MD 的材料粒子色。
 *
 * 开关：{@code -Dmdpromod.summonFx=false}；贴图目录：{@code -Dmdpromod.summonFxRoot}。
 */
@OnlyIn(Dist.CLIENT)
public final class ClientSummonEffects {
    private static final boolean ENABLED =
            Boolean.parseBoolean(System.getProperty("mdpromod.summonFx", "true"));

    /**
     * 每类三层贴图（从外到内、从大到小）。只能用"亮线稿"贴图——顶点色是**乘**在贴图上的，
     * 黑图乘任何颜色都还是黑的。
     */
    private static final String[][] LAYERS = {
            {"fusion_010", "fusion_011", "flash"},              // 0 融合
            {"synchro_noise", "synchro_line", "flash"},         // 1 同调
            {"fusion_011", "flash", "fusion_010"},              // 2 超量
            {"link_gate", "link_line", "flash"},                // 3 连接
            {"ritual_circle_03", "ritual_circle_01", "flash"},  // 4 仪式
            {"pendulum_mask", "flash", "synchro_noise"},        // 5 灵摆
    };
    private static final int[] COLORS = {
            0xC08CFF,   // 融合：紫
            0x66FFB4,   // 同调：绿
            0xFFD24D,   // 超量：金黄
            0xFF5A5A,   // 连接：红
            0x5AB4FF,   // 仪式：蓝
            0x8CFF9C,   // 灵摆：绿
    };
    private static final float[] LAYER_SCALE = {1.0F, 0.66F, 0.36F};
    private static final float[] LAYER_ALPHA = {0.75F, 0.95F, 0.85F};

    private record Active(int fieldEntityId, int kind, int phase, int code, int materialCount, Vec3 at,
                          long startMs, int durationMs) {
    }

    private static final List<Active> ACTIVE = new ArrayList<>();
    private static ClientLevel level;

    private ClientSummonEffects() {
    }

    public static void play(ModNetwork.SummonEffect message) {
        if (!ENABLED) return;
        resetForLevel(Minecraft.getInstance());
        ACTIVE.add(new Active(message.fieldEntityId(), message.kind(), message.phase(), message.code(), message.materialCount(),
                new Vec3(message.x(), message.y(), message.z()),
                System.currentTimeMillis(), Math.max(300, message.durationMs())));
        if (message.phase() == 1) {
            // 一行诊断：包到了 + 每层贴图有没有加载到（看不到特效时照这行二分）
            StringBuilder layers = new StringBuilder();
            for (String layer : LAYERS[Math.max(0, Math.min(LAYERS.length - 1, message.kind()))]) {
                if (layer == null) continue;
                if (layers.length() > 0) layers.append(',');
                layers.append(layer).append('=')
                        .append(SummonFxTextures.get(layer) != null ? "ok" : "MISSING");
            }
            org.slf4j.LoggerFactory.getLogger("mdpromod")
                    .info("Summon FX recv: kind={} code={} materials={} at=({}, {}) textures=[{}]",
                            message.kind(), message.code(), message.materialCount(),
                            message.x(), message.z(), layers);
        }
    }

    public static void stopField(int fieldId) {
        ACTIVE.removeIf(active -> active.fieldEntityId() == fieldId);
    }

    public static void tick(Minecraft minecraft) {
        resetForLevel(minecraft);
        if (ACTIVE.isEmpty()) return;
        long now = System.currentTimeMillis();
        ACTIVE.removeIf(active -> now - active.startMs() > active.durationMs());
    }

    private static void resetForLevel(Minecraft minecraft) {
        if (level != minecraft.level) {
            ACTIVE.clear();
            level = minecraft.level;
        }
    }

    /**
     * 正在播的"正式召唤"法阵（phase 1）的开始时刻，没有返回 0。
     * 王牌登场演出要接在它之后、和落地并行，所以由客户端按这个时刻自己排期。
     */
    public static long circleStartMs(int fieldEntityId) {
        for (int i = ACTIVE.size() - 1; i >= 0; i--) {
            if (ACTIVE.get(i).fieldEntityId() == fieldEntityId && ACTIVE.get(i).phase() == 1)
                return ACTIVE.get(i).startMs();
        }
        return 0L;
    }

    /** 正在播的法阵演出总长（毫秒），没有返回 0。 */
    public static int circleDurationMs(int fieldEntityId) {
        for (int i = ACTIVE.size() - 1; i >= 0; i--) {
            if (ACTIVE.get(i).fieldEntityId() == fieldEntityId && ACTIVE.get(i).phase() == 1)
                return ACTIVE.get(i).durationMs();
        }
        return 0;
    }

    public static void render(Minecraft minecraft, PoseStack poses) {
        resetForLevel(minecraft);
        if (ACTIVE.isEmpty() || minecraft.level == null) return;
        Vec3 camera = minecraft.gameRenderer.getMainCamera().getPosition();
        long now = System.currentTimeMillis();
        MultiBufferSource.BufferSource buffer =
                MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());
        for (Active active : ACTIVE) {
            long ageMs = now - active.startMs();
            float t = Math.min(1.0F, ageMs / (float) active.durationMs());
            float ease = 1.0F - (1.0F - t) * (1.0F - t) * (1.0F - t);
            int kind = Math.max(0, Math.min(LAYERS.length - 1, active.kind()));
            int color = COLORS[kind];
            int red = (color >> 16) & 0xFF;
            int green = (color >> 8) & 0xFF;
            int blue = color & 0xFF;

            if (active.phase() == 0) {
                // 素材吸入：中央起一个小法阵，越转越大（自转同样按毫秒算，别在 1.33 s 里猛转一圈）
                float alpha = Math.min(1.0F, t * 3.0F) * (1.0F - Math.max(0.0F, t - 0.7F) * 3.3F) * 0.8F;
                ring(buffer, poses, camera, active.at(), LAYERS[kind][0],
                        0.9F + 3.1F * ease, (ageMs / 1000.0F) * 110.0F, red, green, blue, alpha, 0.04F);
                continue;
            }

            // 出场节奏（比例）：0→0.35 法阵立起来（淡入 + 放大，融合 3.31 s 的话正好 1.16 s，
            // 和 MD 材料段那些 MagicCircle 动画轨道 1.167 s 对齐），0.35→0.78 保持、缓慢匀速自转，
            // 0.78→1 外扩淡出——正好和"怪兽最后一刻从中央落下"接上。
            float appear = Math.min(1.0F, t / 0.35F);
            float leave = t < 0.78F ? 1.0F : Math.max(0.0F, 1.0F - (t - 0.78F) / 0.22F);
            float open = 1.0F - (1.0F - appear) * (1.0F - appear);      // ease-out
            float alpha = open * leave * 0.95F;
            int layerCount = Math.max(1, Math.min(layerCount(active), LAYERS[kind].length));
            float size = 3.5F + 8.5F * open + 1.5F * (1.0F - leave);     // 最大约 12 格
            // MD 的法阵是**匀速**转的：角度按毫秒算，演出变长了也不会越转越猛
            float spin = (kind % 2 == 0 ? 1.0F : -1.0F) * (ageMs / 1000.0F) * 62.0F;
            for (int layer = 0; layer < layerCount; layer++) {
                ring(buffer, poses, camera, active.at(), LAYERS[kind][layer],
                        size * LAYER_SCALE[Math.min(layer, LAYER_SCALE.length - 1)],
                        spin * (layer % 2 == 0 ? 1.0F : -1.35F),
                        red, green, blue,
                        alpha * LAYER_ALPHA[Math.min(layer, LAYER_ALPHA.length - 1)],
                        0.06F + layer * 0.03F + open * 0.25F);
            }
            column(buffer, poses, camera, active.at(), t, alpha, red, green, blue);
            // 真几何：MD 导出的网格（同调星环 / 超量螺旋锥 / 仪式光柱 / 连接飘带 / 灵摆传送门 / 融合冲击环）
            int level = active.code() > 0
                    ? CardRegistry.get(active.code()).map(CardInfo::level).orElse(0) : 0;
            String meshName = kind == 1
                    ? (level >= 9 ? "SynchroCircle03" : level >= 5 ? "SynchroCircle01" : "SynchroCircle02")
                    : MESH_NAME[kind];
            float meshSpin = spin * (kind == 2 ? 1.8F : kind == 4 ? 0.5F : 1.0F);
            float meshLift = switch (kind) {
                case 2 -> 0.2F + open * 2.2F;      // 螺旋锥从下往上升
                case 4 -> 0.0F;                    // 光柱从地面起
                default -> 0.08F + open * 0.3F;
            };
            float meshScaleY = switch (kind) {
                case 2 -> 0.7F + open * 0.9F;
                case 4 -> 0.5F + open * 1.6F;
                default -> 1.0F;
            };
            mesh(buffer, poses, camera, active.at(), meshName, MESH_TEXTURE[kind],
                    MESH_WIDTH[kind], meshSpin, meshScaleY, meshLift, red, green, blue, alpha * 0.9F);
            details(minecraft, buffer, poses, camera, active, kind, size, open, alpha);
        }
        buffer.endBatch();
    }

    /**
     * 每类用哪块 MD 网格 + 贴哪张抽出来的贴图（网格是 MD 的真几何，纹理先用我们抽到的那批）。
     * 目标宽度是这块网格在场上铺开的方块数——网格会按自身半径自动缩放到这个宽度。
     */
    private static final String[] MESH_NAME = {
            "fxl_dithit_Ring_001",       // 0 融合：冲击环
            "SynchroCircle01",           // 1 同调：箭形标记环
            "XYZInMesh01",               // 2 超量：螺旋锥
            "SummonLinkTrail02",         // 3 连接：螺旋飘带
            "SummonRitualCylinder02",    // 4 仪式：漏斗光柱
            "SummonPendulumChoker02",    // 5 灵摆：八边形传送门
    };
    private static final String[] MESH_TEXTURE = {
            "fusion_011", "synchro_line", "synchro_noise",
            "synchro_line", "ritual_fire_01", "pendulum_mask",
    };
    private static final float[] MESH_WIDTH = {9.0F, 9.5F, 6.0F, 7.5F, 7.0F, 7.5F};

    /** 一层法阵：平铺在地面上的四边形，绕 Y 轴自转。 */
    private static void ring(MultiBufferSource buffer, PoseStack poses, Vec3 camera, Vec3 at,
                             String textureName, float size, float spin,
                             int red, int green, int blue, float alpha, float lift) {
        ResourceLocation texture = SummonFxTextures.get(textureName);
        if (texture == null) return;
        VertexConsumer consumer = buffer.getBuffer(RenderType.entityTranslucentEmissive(texture));
        poses.pushPose();
        poses.translate(at.x - camera.x, (float) at.y + lift - camera.y, at.z - camera.z);
        poses.mulPose(Axis.YP.rotationDegrees(spin));
        flatQuad(consumer, poses.last().pose(), size, red, green, blue,
                Math.max(0, Math.min(255, Math.round(alpha * 255.0F))));
        poses.popPose();
    }

    /**
     * 柔和光柱：十字交叉的两片，**底宽顶窄**，纵向靠顶点色渐隐、横向靠贴图自带的 alpha 渐隐
     * （{@code black_circle} 是径向软光，不是那种黑底不透明的条），随时间"长出来 / 收回去"。
     * 原来那根是用 {@code pendulum_spot} 画的：那张图完全不透明，黑底在 MC 的普通 alpha 混合里
     * 会被当成"半透明黑"糊上去，所以看起来就是一根硬邦邦的深色矩形——这就是"突兀"的来源。
     */
    private static void column(MultiBufferSource buffer, PoseStack poses, Vec3 camera, Vec3 at,
                               float t, float alpha, int red, int green, int blue) {
        ResourceLocation texture = SummonFxTextures.get("black_circle");
        if (texture == null || alpha <= 0.02F) return;
        VertexConsumer consumer = buffer.getBuffer(RenderType.entityTranslucentEmissive(texture));
        float grow = Math.max(0.0F, Math.min(1.0F, (t - 0.06F) / 0.30F));   // 法阵立起来之后才长
        float fade = t < 0.80F ? 1.0F : Math.max(0.0F, 1.0F - (t - 0.80F) / 0.20F);
        float height = 5.6F * grow * fade;
        if (height <= 0.05F || fade <= 0.01F) return;
        float halfBottom = 1.85F;
        float halfTop = 0.80F;
        int bottom = Math.max(0, Math.min(255, Math.round(alpha * 88.0F * fade)));
        for (float yaw : new float[]{0.0F, 90.0F}) {
            poses.pushPose();
            poses.translate(at.x - camera.x, (float) at.y - camera.y, at.z - camera.z);
            poses.mulPose(Axis.YP.rotationDegrees(yaw));
            Matrix4f matrix = poses.last().pose();
            columnQuad(consumer, matrix, halfBottom, halfTop, height, red, green, blue, bottom);
            poses.popPose();
        }
    }

    /**
     * 一片光柱：底边宽、顶边窄，顶部 alpha = 0（渐隐），正反各一遍防剔除。
     * UV 只取径向软光贴图中间那条窄带——这样左右两边也是软的，不会切出直边。
     */
    private static void columnQuad(VertexConsumer consumer, Matrix4f matrix,
                                   float halfBottom, float halfTop, float height,
                                   int red, int green, int blue, int bottomAlpha) {
        consumer.vertex(matrix, -halfBottom, 0.0F, 0.0F).color(red, green, blue, bottomAlpha)
                .uv(0.0F, 0.58F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(0.0F, 1.0F, 0.0F).endVertex();
        consumer.vertex(matrix, halfBottom, 0.0F, 0.0F).color(red, green, blue, bottomAlpha)
                .uv(1.0F, 0.58F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(0.0F, 1.0F, 0.0F).endVertex();
        consumer.vertex(matrix, halfTop, height, 0.0F).color(red, green, blue, 0)
                .uv(1.0F, 0.42F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(0.0F, 1.0F, 0.0F).endVertex();
        consumer.vertex(matrix, -halfTop, height, 0.0F).color(red, green, blue, 0)
                .uv(0.0F, 0.42F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(0.0F, 1.0F, 0.0F).endVertex();
        // 反面
        consumer.vertex(matrix, halfTop, height, 0.0F).color(red, green, blue, 0)
                .uv(1.0F, 0.42F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(0.0F, -1.0F, 0.0F).endVertex();
        consumer.vertex(matrix, halfBottom, 0.0F, 0.0F).color(red, green, blue, bottomAlpha)
                .uv(1.0F, 0.58F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(0.0F, -1.0F, 0.0F).endVertex();
        consumer.vertex(matrix, -halfBottom, 0.0F, 0.0F).color(red, green, blue, bottomAlpha)
                .uv(0.0F, 0.58F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(0.0F, -1.0F, 0.0F).endVertex();
        consumer.vertex(matrix, -halfTop, height, 0.0F).color(red, green, blue, 0)
                .uv(0.0F, 0.42F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(0.0F, -1.0F, 0.0F).endVertex();
    }

    /** 画一块 MD 网格：按自身半径缩放到目标宽度、绕 Y 自转、可垂直拉伸。 */
    private static void mesh(MultiBufferSource buffer, PoseStack poses, Vec3 camera, Vec3 at,
                             String name, String textureName, float targetWidth, float spinDeg,
                             float scaleY, float lift, int red, int green, int blue, float alpha) {
        SummonMeshes.Mesh mesh = SummonMeshes.get(name);
        ResourceLocation texture = SummonFxTextures.get(textureName);
        if (mesh == null || texture == null) return;
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
            // 正反各画一遍：不管渲染层有没有剔除背面，都一定看得见
            triangle(consumer, matrix, positions, uvs, indices[i], uvIndices[i],
                    indices[i + 1], uvIndices[i + 1], indices[i + 2], uvIndices[i + 2],
                    red, green, blue, value);
            triangle(consumer, matrix, positions, uvs, indices[i + 2], uvIndices[i + 2],
                    indices[i + 1], uvIndices[i + 1], indices[i], uvIndices[i],
                    red, green, blue, value);
        }
        poses.popPose();
    }

    private static void triangle(VertexConsumer consumer, Matrix4f matrix,
                                 float[] positions, float[] uvs,
                                 int v0, int t0, int v1, int t1, int v2, int t2,
                                 int red, int green, int blue, int alpha) {
        vertex(consumer, matrix, positions, uvs, v0, t0, red, green, blue, alpha);
        vertex(consumer, matrix, positions, uvs, v1, t1, red, green, blue, alpha);
        vertex(consumer, matrix, positions, uvs, v2, t2, red, green, blue, alpha);
    }

    private static void vertex(VertexConsumer consumer, Matrix4f matrix, float[] positions, float[] uvs,
                               int vertexIndex, int uvIndex, int red, int green, int blue, int alpha) {
        int vi = vertexIndex * 3;
        if (vi + 2 >= positions.length) return;
        int ti = uvIndex * 2;
        float u = ti + 1 < uvs.length ? uvs[ti] : 0.0F;
        float v = ti + 1 < uvs.length ? 1.0F - uvs[ti + 1] : 0.0F;   // Unity 的 V 朝上，MC 朝下
        consumer.vertex(matrix, positions[vi], positions[vi + 1], positions[vi + 2])
                .color(red, green, blue, alpha)
                .uv(u, v)
                .overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT)
                .normal(0.0F, 1.0F, 0.0F).endVertex();
    }

    /** 类型细节：同调等级数字、超量叠放素材、连接箭头。 */
    private static void details(Minecraft minecraft, MultiBufferSource buffer, PoseStack poses,
                                Vec3 camera, Active active, int kind, float size, float ease,
                                float alpha) {
        int code = active.code();
        if (code <= 0) return;
        int level = CardRegistry.get(code).map(CardInfo::level).orElse(0);
        if (kind == 1 && level > 0) {
            // 同调：把等级数字浮在法阵上方
            text(minecraft, buffer, poses, camera, Integer.toString(level), active.at(),
                    1.2F + ease * 0.8F, 3.4F, alpha);
        } else if (kind == 2) {
            // 超量：飘出叠放素材（卡背小方块）
            int count = Math.max(1, Math.min(3, active.materialCount()));
            VertexConsumer consumer = buffer.getBuffer(
                    RenderType.entityTranslucent(CardTextureCache.CARD_BACK));
            int value = Math.max(0, Math.min(255, Math.round(alpha * 210.0F)));
            for (int i = 0; i < count; i++) {
                double angle = Math.toRadians(90.0 + i * (360.0 / count));
                float radius = size * 0.30F;
                poses.pushPose();
                poses.translate(active.at().x + Math.cos(angle) * radius - camera.x,
                        1.0F + 0.35F * ease + i * 0.16F, active.at().z + Math.sin(angle) * radius - camera.z);
                poses.mulPose(Axis.YP.rotationDegrees((float) Math.toDegrees(angle) + 90.0F));
                flatQuad(consumer, poses.last().pose(), 0.85F, 255, 255, 255, value);
                poses.popPose();
            }
        } else if (kind == 3 && level > 0) {
            // 连接：按 Link 数向外放射箭头（用 link_line 那条细杠当箭头尾迹）
            ResourceLocation texture = SummonFxTextures.get("link_line");
            if (texture == null) return;
            VertexConsumer consumer = buffer.getBuffer(RenderType.entityTranslucentEmissive(texture));
            int value = Math.max(0, Math.min(255, Math.round(alpha * 220.0F)));
            for (int i = 0; i < Math.min(8, level); i++) {
                double angle = Math.toRadians(i * (360.0 / Math.min(8, level)) + ease * 40.0);
                float radius = size * 0.34F;
                poses.pushPose();
                poses.translate(active.at().x + Math.cos(angle) * radius - camera.x,
                        active.at().y + 0.5F - camera.y,
                        active.at().z + Math.sin(angle) * radius - camera.z);
                verticalQuad(consumer, poses.last().pose(), 0.9F, 0.34F, 255, 90, 90, value,
                        (float) Math.toDegrees(angle));
                poses.popPose();
            }
        }
    }

    /** 面向相机的浮空文字（同调等级数字）。 */
    private static void text(Minecraft minecraft, MultiBufferSource buffer, PoseStack poses,
                             Vec3 camera, String value, Vec3 at, float height, float scale,
                             float alpha) {
        Vec3 toCamera = new Vec3(camera.x - at.x, 0.0, camera.z - at.z);
        if (toCamera.lengthSqr() < 1.0E-4) toCamera = new Vec3(0.0, 0.0, 1.0);
        float yaw = (float) Math.toDegrees(Math.atan2(toCamera.x, toCamera.z));
        int color = 0xFFFFFF | (Math.max(0, Math.min(255, Math.round(alpha * 255.0F))) << 24);
        poses.pushPose();
        poses.translate(at.x - camera.x, at.y + height - camera.y, at.z - camera.z);
        poses.mulPose(Axis.YP.rotationDegrees(yaw));
        poses.scale(-scale, -scale, scale);
        float width = minecraft.font.width(value);
        minecraft.font.drawInBatch(Component.literal(value), -width / 2.0F, -4.0F, color, true,
                poses.last().pose(), buffer, net.minecraft.client.gui.Font.DisplayMode.SEE_THROUGH,
                0, LightTexture.FULL_BRIGHT);
        poses.popPose();
    }

    /** 融合按素材数量、同调按等级分档决定法阵层数，其余固定 3 层。 */
    private static int layerCount(Active active) {
        int level = active.code() > 0
                ? CardRegistry.get(active.code()).map(CardInfo::level).orElse(0) : 0;
        return switch (active.kind()) {
            case 0 -> Math.max(1, Math.min(3, active.materialCount()));       // 融合
            case 1 -> level < 5 ? 1 : level < 9 ? 2 : 3;                      // 同调
            default -> 3;
        };
    }

    /** 平铺在地面上的四边形（+Y 朝上）。 */
    private static void flatQuad(VertexConsumer consumer, Matrix4f matrix, float size,
                                 int red, int green, int blue, int alpha) {
        float half = size / 2.0F;
        consumer.vertex(matrix, -half, 0.0F, -half).color(red, green, blue, alpha)
                .uv(0.0F, 0.0F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(0.0F, 1.0F, 0.0F).endVertex();
        consumer.vertex(matrix, -half, 0.0F, half).color(red, green, blue, alpha)
                .uv(0.0F, 1.0F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(0.0F, 1.0F, 0.0F).endVertex();
        consumer.vertex(matrix, half, 0.0F, half).color(red, green, blue, alpha)
                .uv(1.0F, 1.0F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(0.0F, 1.0F, 0.0F).endVertex();
        consumer.vertex(matrix, half, 0.0F, -half).color(red, green, blue, alpha)
                .uv(1.0F, 0.0F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(0.0F, 1.0F, 0.0F).endVertex();
        // 反面再来一遍
        consumer.vertex(matrix, half, 0.0F, -half).color(red, green, blue, alpha)
                .uv(1.0F, 0.0F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(0.0F, -1.0F, 0.0F).endVertex();
        consumer.vertex(matrix, half, 0.0F, half).color(red, green, blue, alpha)
                .uv(1.0F, 1.0F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(0.0F, -1.0F, 0.0F).endVertex();
        consumer.vertex(matrix, -half, 0.0F, half).color(red, green, blue, alpha)
                .uv(0.0F, 1.0F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(0.0F, -1.0F, 0.0F).endVertex();
        consumer.vertex(matrix, -half, 0.0F, -half).color(red, green, blue, alpha)
                .uv(0.0F, 0.0F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(0.0F, -1.0F, 0.0F).endVertex();
    }

    /** 竖直四边形，绕 Y 轴转 yaw 度（光柱 / 连接箭头用，十字交叉从任何角度都看得见）。 */
    private static void verticalQuad(VertexConsumer consumer, Matrix4f matrix, float width, float height,
                                     int red, int green, int blue, int alpha, float yaw) {
        float half = width / 2.0F;
        double radians = Math.toRadians(yaw);
        float dx = (float) Math.sin(radians) * half;
        float dz = (float) Math.cos(radians) * half;
        float halfHeight = height / 2.0F;
        consumer.vertex(matrix, -dx, -halfHeight, -dz).color(red, green, blue, alpha)
                .uv(0.0F, 1.0F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(-dz, 0.0F, dx).endVertex();
        consumer.vertex(matrix, dx, -halfHeight, dz).color(red, green, blue, alpha)
                .uv(1.0F, 1.0F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(-dz, 0.0F, dx).endVertex();
        consumer.vertex(matrix, dx, halfHeight, dz).color(red, green, blue, alpha)
                .uv(1.0F, 0.0F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(-dz, 0.0F, dx).endVertex();
        consumer.vertex(matrix, -dx, halfHeight, -dz).color(red, green, blue, alpha)
                .uv(0.0F, 0.0F).overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT).normal(-dz, 0.0F, dx).endVertex();
    }
}
