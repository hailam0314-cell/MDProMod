package net.mdpromod;

import com.mojang.blaze3d.vertex.BufferBuilder;
import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.math.Axis;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.LevelRenderer;
import net.minecraft.client.renderer.LightTexture;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.client.renderer.texture.OverlayTexture;
import net.minecraft.core.BlockPos;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.client.event.ClientPlayerNetworkEvent;
import net.minecraftforge.client.event.RenderLevelStageEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;
import org.joml.Matrix3f;
import org.joml.Matrix4f;

/** Renders moving cards separately from their authoritative world entities. */
@Mod.EventBusSubscriber(modid = MDProMod.MODID, value = Dist.CLIENT)
public final class ClientDuelAnimationRenderer {
    private static final MultiBufferSource.BufferSource BUFFER =
            MultiBufferSource.immediate(new BufferBuilder(32768));

    private ClientDuelAnimationRenderer() { }

    /** 素材吸入时的粒子配色，和 ClientSummonEffects.COLORS 一致。 */
    private static final int[] ABSORB_COLORS = {
            0xC08CFF, 0x66FFB4, 0xFFD24D, 0xFF5A5A, 0x5AB4FF, 0x8CFF9C,
    };

    /** 沿素材卡的飞行路径撒该类型配色的粒子（kind 形如 absorbN，N 是召唤类型）。 */
    private static void spawnAbsorbParticles(Minecraft minecraft, ClientAnimBridge.Frame frame) {
        if (minecraft.level == null) return;
        int kind;
        try {
            kind = Integer.parseInt(frame.kind().substring("absorb".length()));
        } catch (RuntimeException ex) {
            return;
        }
        if (kind < 0 || kind >= ABSORB_COLORS.length) return;
        int color = ABSORB_COLORS[kind];
        float red = ((color >> 16) & 0xFF) / 255.0F;
        float green = ((color >> 8) & 0xFF) / 255.0F;
        float blue = (color & 0xFF) / 255.0F;
        Vec3 at = frame.center();
        var random = minecraft.level.random;
        for (int i = 0; i < 4; i++) {
            minecraft.level.addParticle(net.minecraft.core.particles.ParticleTypes.ENTITY_EFFECT,
                    at.x + (random.nextDouble() - 0.5) * 0.6,
                    at.y + (random.nextDouble() - 0.3) * 0.6,
                    at.z + (random.nextDouble() - 0.5) * 0.6,
                    red, green, blue);
        }
    }

    @SubscribeEvent
    public static void render(RenderLevelStageEvent event) {
        if (event.getStage() != RenderLevelStageEvent.Stage.AFTER_TRANSLUCENT_BLOCKS) return;
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.level == null) return;
        Vec3 camera = event.getCamera().getPosition();
        PoseStack poses = event.getPoseStack();
        for (ClientAnimBridge.Frame frame : ClientAnimBridge.frames()) {
            if (frame.scale() <= 0.002F || frame.center().distanceToSqr(camera) > 128.0 * 128.0) continue;
            // 素材被吸向场地中央时，沿路径撒该类型配色的粒子
            if (frame.kind().startsWith("absorb")) {
                spawnAbsorbParticles(minecraft, frame);
            }
            poses.pushPose();
            Vec3 at = frame.center().subtract(camera);
            poses.translate(at.x, at.y, at.z);
            poses.mulPose(Axis.YP.rotationDegrees(-frame.yaw()));
            poses.mulPose(Axis.XP.rotationDegrees(-frame.pitch()));
            poses.mulPose(Axis.ZP.rotationDegrees(frame.roll()));
            poses.scale(frame.scale(), frame.scale(), frame.scale());
            int light = switch (frame.kind()) {
                case "effect", "effect_reveal", "reveal", "search_reveal", "material" -> LightTexture.FULL_BRIGHT;
                default -> LevelRenderer.getLightColor(minecraft.level, BlockPos.containing(frame.center()));
            };
            ResourceLocation front = frame.faceUp() && frame.cardCode() > 0
                    ? CardTextureCache.get(frame.cardCode()) : CardTextureCache.CARD_BACK;
            drawCard(poses, BUFFER, front, frame.width(), frame.height(), light);
            poses.popPose();
        }
        BUFFER.endBatch();
    }

    @SubscribeEvent
    public static void loggedOut(ClientPlayerNetworkEvent.LoggingOut event) {
        ClientAnimBridge.clear();
        ClientCardMarkers.clear();
    }

    /**
     * Both sides are culled independently. A card face can never show through its
     * reverse side; a card with no approved code uses the back texture on both sides.
     * Caller places the card center at the local origin.
     */
    public static void drawCard(PoseStack poses, MultiBufferSource buffers,
                                ResourceLocation frontTexture, float width, float height,
                                int light) {
        VertexConsumer front = buffers.getBuffer(RenderType.entityCutout(frontTexture));
        side(poses, front, width, height, 0.002F, light, true);
        VertexConsumer back = buffers.getBuffer(RenderType.entityCutout(CardTextureCache.CARD_BACK));
        side(poses, back, width, height, -0.002F, light, false);
    }

    private static void side(PoseStack poses, VertexConsumer vertices, float width,
                              float height, float z, int light, boolean front) {
        float left = -width * 0.5F, right = width * 0.5F;
        float bottom = -height * 0.5F, top = height * 0.5F;
        if (front) {
            vertex(poses, vertices, left, top, z, 0, 0, light, 1);
            vertex(poses, vertices, left, bottom, z, 0, 1, light, 1);
            vertex(poses, vertices, right, bottom, z, 1, 1, light, 1);
            vertex(poses, vertices, right, top, z, 1, 0, light, 1);
        } else {
            vertex(poses, vertices, right, top, z, 0, 0, light, -1);
            vertex(poses, vertices, right, bottom, z, 0, 1, light, -1);
            vertex(poses, vertices, left, bottom, z, 1, 1, light, -1);
            vertex(poses, vertices, left, top, z, 1, 0, light, -1);
        }
    }

    private static void vertex(PoseStack poses, VertexConsumer vertices, float x, float y,
                                float z, float u, float v, int light, float normalZ) {
        Matrix4f pose = poses.last().pose();
        Matrix3f normal = poses.last().normal();
        vertices.vertex(pose, x, y, z).color(255, 255, 255, 255).uv(u, v)
                .overlayCoords(OverlayTexture.NO_OVERLAY).uv2(light)
                .normal(normal, 0.0F, 0.0F, normalZ).endVertex();
    }
}
