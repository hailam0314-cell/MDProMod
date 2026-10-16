package net.mdpromod;

import com.badlogic.gdx.graphics.Color;
import com.badlogic.gdx.utils.Array;
import com.esotericsoftware.spine.AnimationState;
import com.esotericsoftware.spine.AnimationStateData;
import com.esotericsoftware.spine.BlendMode;
import com.esotericsoftware.spine.Skeleton;
import com.esotericsoftware.spine.SkeletonData;
import com.esotericsoftware.spine.Slot;
import com.esotericsoftware.spine.attachments.ClippingAttachment;
import com.esotericsoftware.spine.attachments.MeshAttachment;
import com.esotericsoftware.spine.attachments.RegionAttachment;
import com.esotericsoftware.spine.utils.SkeletonClipping;
import com.mojang.blaze3d.platform.GlStateManager;
import com.mojang.blaze3d.systems.RenderSystem;
import com.mojang.blaze3d.vertex.BufferBuilder;
import com.mojang.blaze3d.vertex.BufferUploader;
import com.mojang.blaze3d.vertex.DefaultVertexFormat;
import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.VertexFormat;
import com.mojang.math.Axis;
import net.minecraft.client.Minecraft;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.client.renderer.GameRenderer;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;
import net.mdpromod.spine.CutinAssets;
import net.mdpromod.spine.CutinRoster;
import org.joml.Matrix4f;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;

/**
 * MD 的 Spine 登场演出。位置固定在决斗场中央，宽度取场地的实际方块尺寸。
 * 每个客户端将这张世界中的演出面朝向自己的相机，因此对面的玩家也看到正面。
 * 同一场地顺序播放；不同场地的演出互不占用时序或播放锁。
 */
@OnlyIn(Dist.CLIENT)
public final class ClientCutinEffects {
    private static final boolean ENABLED =
            Boolean.parseBoolean(System.getProperty("mdpromod.cutin", "true"));
    private static final short[] QUAD_TRIANGLES = {0, 1, 2, 0, 2, 3};
    private static final BufferBuilder BUFFER = new BufferBuilder(65536);

    private static final class Active {
        final int fieldEntityId;
        final int code;
        final long startMs;
        final long durationMs;
        final CutinAssets.Loaded loaded;
        final Skeleton skeleton;
        final AnimationState state;
        final SkeletonClipping clipping = new SkeletonClipping();
        long advancedMs;

        Active(int fieldEntityId, int code, long startMs, long durationMs, CutinAssets.Loaded loaded) {
            this.fieldEntityId = fieldEntityId;
            this.code = code;
            this.startMs = startMs;
            this.durationMs = durationMs;
            this.loaded = loaded;
            this.skeleton = new Skeleton(loaded.data());
            this.state = new AnimationState(new AnimationStateData(loaded.data()));
            this.state.setAnimation(0, loaded.recipe().animation(), false);
            this.state.apply(skeleton);
            this.skeleton.updateWorldTransform(Skeleton.Physics.update);
        }
    }

    private record Scheduled(int fieldEntityId, int code, long startAtMs, int durationMs) {}
    private record Pass(ResourceLocation texture, BlendMode blend, boolean pma) {}

    private static final List<Active> ACTIVE = new ArrayList<>();
    private static final List<Scheduled> PENDING = new ArrayList<>();
    private static ClientLevel level;

    private ClientCutinEffects() {}

    /** 使用包里的场地 ID；普通召唤的包坐标可能仍是卡片起点，不能作为演出中心。 */
    public static void play(ModNetwork.SummonEffect message) {
        if (!ENABLED || !CutinRoster.has(message.code())) return;
        Minecraft minecraft = Minecraft.getInstance();
        resetForLevel(minecraft);
        if (minecraft.level == null) return;
        int duration = message.durationMs() > 0 ? message.durationMs()
                : CutinRoster.durationMs(message.code());
        long now = System.currentTimeMillis();
        long startAt = now;
        // 只看这块决斗场的法阵。通常召唤不应等待附近另一场对局的法阵。
        if (message.kind() >= 0) {
            long circleStart = ClientSummonEffects.circleStartMs(message.fieldEntityId());
            int circleDuration = ClientSummonEffects.circleDurationMs(message.fieldEntityId());
            if (circleStart > 0L && circleDuration > 0) {
                startAt = Math.max(startAt, circleStart + circleDuration - SummonTiming.LAND_MS);
            }
        }
        for (Active active : ACTIVE) {
            if (active.fieldEntityId == message.fieldEntityId()) {
                startAt = Math.max(startAt, active.startMs + active.durationMs);
            }
        }
        for (Scheduled scheduled : PENDING) {
            if (scheduled.fieldEntityId() == message.fieldEntityId()) {
                startAt = Math.max(startAt, scheduled.startAtMs() + scheduled.durationMs());
            }
        }
        if (startAt <= now) {
            start(message.fieldEntityId(), message.code(), duration, now);
        } else {
            PENDING.add(new Scheduled(message.fieldEntityId(), message.code(), startAt, duration));
        }
    }

    private static void start(int fieldEntityId, int code, int durationMs, long startMs) {
        CutinAssets.Loaded loaded = CutinAssets.load(Integer.toString(code));
        if (loaded == null) {
            org.slf4j.LoggerFactory.getLogger("mdpromod")
                    .warn("Cut-in 素材缺失：field={} code={}", fieldEntityId, code);
            return;
        }
        ACTIVE.add(new Active(fieldEntityId, code, startMs, durationMs, loaded));
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.level != null && minecraft.level.getEntity(fieldEntityId) instanceof DuelFieldEntity field) {
            int attribute = CardRegistry.get(code).map(CardInfo::attribute).orElse(0);
            String soundAttribute = (attribute & 0x20) != 0 ? "DARK"
                    : (attribute & 0x10) != 0 ? "LIGHT"
                    : (attribute & 0x01) != 0 ? "EARTH"
                    : (attribute & 0x02) != 0 ? "WATER"
                    : (attribute & 0x04) != 0 ? "FIRE"
                    : (attribute & 0x08) != 0 ? "WIND" : "DIVINE";
            ClientDuelAudio.playTimeline("cutin", soundAttribute, null, fieldEntityId, field.position());
        }
        org.slf4j.LoggerFactory.getLogger("mdpromod")
                .info("Cut-in 开始：field={} code={} 动画={} 时长={}ms", fieldEntityId, code,
                        loaded.recipe().animation(), durationMs);
    }

    public static void tick(Minecraft minecraft) {
        resetForLevel(minecraft);
        if (!ENABLED || minecraft.level == null) return;
        long now = System.currentTimeMillis();
        ACTIVE.removeIf(active -> now - active.startMs >= active.durationMs
                || !(minecraft.level.getEntity(active.fieldEntityId) instanceof DuelFieldEntity));
        PENDING.removeIf(scheduled -> {
            if (!(minecraft.level.getEntity(scheduled.fieldEntityId()) instanceof DuelFieldEntity)) return true;
            if (scheduled.startAtMs() > now) return false;
            start(scheduled.fieldEntityId(), scheduled.code(), scheduled.durationMs(), scheduled.startAtMs());
            return true;
        });
    }

    private static void resetForLevel(Minecraft minecraft) {
        if (level != minecraft.level) {
            stop();
            level = minecraft.level;
        }
    }

    /** 退出或切换世界时只清理播放实例；可复用的骨架和纹理仍由素材缓存管理。 */
    public static void stop() {
        ACTIVE.clear();
        PENDING.clear();
    }

    public static void stopField(int fieldId) {
        ACTIVE.removeIf(active -> active.fieldEntityId == fieldId);
        PENDING.removeIf(scheduled -> scheduled.fieldEntityId() == fieldId);
    }

    /** 是否正有王牌登场演出在播——BGM 据此切到「王牌登场」那一栏的曲子。 */
    public static boolean isPlaying() {
        return !ACTIVE.isEmpty();
    }

    public static void render(Minecraft minecraft, PoseStack poses) {
        resetForLevel(minecraft);
        if (!ENABLED || ACTIVE.isEmpty() || minecraft.level == null) return;
        Vec3 camera = minecraft.gameRenderer.getMainCamera().getPosition();
        List<Active> visible = new ArrayList<>(ACTIVE);
        // 透明演出不写深度；多个场地从远到近绘制，避免近场被远场覆盖。
        visible.sort(Comparator.comparingDouble((Active active) -> {
            var field = minecraft.level.getEntity(active.fieldEntityId);
            return field == null ? 0.0 : field.position().distanceToSqr(camera);
        }).reversed());

        float[] previousShaderColor = RenderSystem.getShaderColor().clone();
        RenderSystem.enableDepthTest();
        RenderSystem.depthMask(false);
        RenderSystem.disableCull();
        RenderSystem.enableBlend();
        RenderSystem.setShader(GameRenderer::getPositionColorTexShader);
        RenderSystem.setShaderColor(1.0F, 1.0F, 1.0F, 1.0F);
        try {
            for (Active active : visible) {
                if (!(minecraft.level.getEntity(active.fieldEntityId) instanceof DuelFieldEntity field)
                        || field.position().distanceToSqr(camera) > 128.0 * 128.0) continue;
                if (System.currentTimeMillis() - active.startMs >= active.durationMs) continue;
                advance(active);
                SkeletonData data = active.loaded.data();
                CutinWorldLayout.View view = CutinWorldLayout.view(data.getX(), data.getY(),
                        data.getWidth(), data.getHeight(), DuelFieldLayout.widthBlocks(),
                        DuelFieldLayout.depthBlocks(), field.getYRot(),
                        camera.x - field.getX(), camera.y - field.getY(), camera.z - field.getZ());
                poses.pushPose();
                try {
                    poses.translate(field.getX() - camera.x, field.getY() + view.lift() - camera.y,
                            field.getZ() - camera.z);
                    poses.mulPose(Axis.YP.rotationDegrees(view.yawDegrees()));
                    poses.mulPose(Axis.XP.rotationDegrees(-view.pitchDegrees()));
                    draw(active, poses.last().pose(), view);
                } finally {
                    poses.popPose();
                }
            }
        } finally {
            RenderSystem.defaultBlendFunc();
            RenderSystem.disableBlend();
            RenderSystem.enableCull();
            RenderSystem.depthMask(true);
            RenderSystem.enableDepthTest();
            RenderSystem.setShaderColor(previousShaderColor[0], previousShaderColor[1],
                    previousShaderColor[2], previousShaderColor[3]);
        }
    }

    /** Spine 时间按渲染帧推进；世界坐标保留 y 向上。 */
    private static void advance(Active active) {
        long elapsed = Math.max(0L, System.currentTimeMillis() - active.startMs);
        float dt = Math.max(0.0F, (elapsed - active.advancedMs) / 1000.0F);
        active.advancedMs = elapsed;
        if (dt > 0.0F) {
            active.state.update(dt);
            active.state.apply(active.skeleton);
            active.skeleton.updateWorldTransform(Skeleton.Physics.update);
        }
    }

    private static void applyBlend(Pass pass) {
        GlStateManager.SourceFactor source = pass.pma()
                ? GlStateManager.SourceFactor.ONE : GlStateManager.SourceFactor.SRC_ALPHA;
        GlStateManager.SourceFactor alpha = GlStateManager.SourceFactor.ONE;
        GlStateManager.DestFactor destination = GlStateManager.DestFactor.ONE_MINUS_SRC_ALPHA;
        if (pass.blend() == BlendMode.additive) {
            destination = GlStateManager.DestFactor.ONE;
        } else if (pass.blend() == BlendMode.multiply) {
            source = GlStateManager.SourceFactor.DST_COLOR;
            alpha = GlStateManager.SourceFactor.ONE_MINUS_SRC_ALPHA;
        } else if (pass.blend() == BlendMode.screen) {
            source = GlStateManager.SourceFactor.ONE;
            alpha = GlStateManager.SourceFactor.ONE_MINUS_SRC_COLOR;
            destination = GlStateManager.DestFactor.ONE_MINUS_SRC_COLOR;
        }
        RenderSystem.blendFuncSeparate(source, destination, alpha, destination);
    }

    private static Pass passOf(Slot slot) {
        Object attachment = slot.getAttachment();
        com.badlogic.gdx.graphics.Texture page;
        if (attachment instanceof RegionAttachment region) {
            page = region.getRegion() == null ? null : region.getRegion().getTexture();
        } else if (attachment instanceof MeshAttachment mesh) {
            page = mesh.getRegion() == null ? null : mesh.getRegion().getTexture();
        } else {
            return null;
        }
        if (page == null) return null;
        ResourceLocation texture = ClientCutinTextures.of(page);
        return texture == null ? null : new Pass(texture, slot.getData().getBlendMode(), page.pma);
    }

    /** 仅合并相邻的相同绘制状态，始终保留 Spine 的层次顺序和裁剪范围。 */
    private static void draw(Active active, Matrix4f matrix, CutinWorldLayout.View view) {
        Color skeletonColor = active.skeleton.getColor();
        Array<Slot> order = active.skeleton.getDrawOrder();
        Pass currentPass = null;
        boolean drawing = false;
        try {
            for (int i = 0; i < order.size; i++) {
                Slot slot = order.get(i);
                try {
                    if (!slot.getBone().isActive()) continue;
                    Object attachment = slot.getAttachment();
                    if (attachment instanceof ClippingAttachment clipping) {
                        active.clipping.clipStart(slot, clipping);
                        continue;
                    }
                    Pass pass = passOf(slot);
                    if (pass == null) continue;
                    Color slotColor = slot.getColor();
                    float[] vertices;
                    float[] uvs;
                    short[] triangles;
                    Color tint;
                    if (attachment instanceof RegionAttachment region) {
                        vertices = new float[8];
                        region.computeWorldVertices(slot, vertices, 0, 2);
                        uvs = region.getUVs();
                        triangles = QUAD_TRIANGLES;
                        tint = region.getColor();
                    } else {
                        MeshAttachment mesh = (MeshAttachment) attachment;
                        vertices = new float[mesh.getWorldVerticesLength()];
                        mesh.computeWorldVertices(slot, 0, vertices.length, vertices, 0, 2);
                        uvs = mesh.getUVs();
                        triangles = mesh.getTriangles();
                        tint = mesh.getColor();
                    }
                    float alpha = skeletonColor.a * slotColor.a * tint.a;
                    if (uvs == null || alpha <= 0.004F || vertices.length < 6) continue;
                    int vertexCount = vertices.length;
                    int triangleCount = triangles.length;
                    if (active.clipping.isClipping()) {
                        active.clipping.clipTrianglesUnpacked(vertices, 0,
                                triangles, triangleCount, uvs);
                        vertices = active.clipping.getClippedVertices().items;
                        uvs = active.clipping.getClippedUvs().items;
                        triangles = active.clipping.getClippedTriangles().items;
                        vertexCount = active.clipping.getClippedVertices().size;
                        triangleCount = active.clipping.getClippedTriangles().size;
                    }
                    if (triangleCount < 3) continue;
                    if (!pass.equals(currentPass)) {
                        if (drawing) {
                            drawing = false;
                            BufferUploader.drawWithShader(BUFFER.end());
                        }
                        applyBlend(pass);
                        RenderSystem.setShaderTexture(0, pass.texture());
                        BUFFER.begin(VertexFormat.Mode.TRIANGLES, DefaultVertexFormat.POSITION_COLOR_TEX);
                        drawing = true;
                        currentPass = pass;
                    }
                    // PMA 纹理已乘贴图 alpha；顶点淡入淡出还必须同步乘进 RGB。
                    float colorAlpha = pass.pma() ? alpha : 1.0F;
                    int packed = pack(skeletonColor.r * slotColor.r * tint.r * colorAlpha,
                            skeletonColor.g * slotColor.g * tint.g * colorAlpha,
                            skeletonColor.b * slotColor.b * tint.b * colorAlpha, alpha);
                    for (int t = 0; t + 2 < triangleCount; t += 3) {
                        boolean valid = true;
                        for (int corner = 0; corner < 3; corner++) {
                            int v = (triangles[t + corner] & 0xFFFF) * 2;
                            if (v + 1 >= vertexCount || v + 1 >= uvs.length) valid = false;
                        }
                        if (!valid) continue;
                        for (int corner = 0; corner < 3; corner++) {
                            int v = (triangles[t + corner] & 0xFFFF) * 2;
                            BUFFER.vertex(matrix, (vertices[v] - view.centerX()) * view.scale(),
                                            (vertices[v + 1] - view.centerY()) * view.scale(), 0.0F)
                                    .color(packed).uv(uvs[v], uvs[v + 1]).endVertex();
                        }
                    }
                } finally {
                    active.clipping.clipEnd(slot);
                }
            }
        } finally {
            try {
                if (drawing) BufferUploader.drawWithShader(BUFFER.end());
            } finally {
                active.clipping.clipEnd();
            }
        }
    }

    private static int pack(float red, float green, float blue, float alpha) {
        return (clamp(alpha) << 24) | (clamp(red) << 16) | (clamp(green) << 8) | clamp(blue);
    }

    private static int clamp(float value) {
        return Math.max(0, Math.min(255, Math.round(value * 255.0F)));
    }
}
