package net.mdpromod;

import com.mojang.blaze3d.vertex.BufferBuilder;
import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.math.Axis;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.LevelRenderer;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.client.renderer.MultiBufferSource.BufferSource;
import net.minecraft.client.renderer.texture.OverlayTexture;
import net.minecraft.core.BlockPos;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.util.RandomSource;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.client.event.RenderLevelStageEvent;
import net.minecraftforge.client.event.ClientPlayerNetworkEvent.LoggingOut;
import net.minecraftforge.client.event.RenderLevelStageEvent.Stage;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod.EventBusSubscriber;
import org.joml.Matrix3f;
import org.joml.Matrix4f;

@EventBusSubscriber(modid = "mdpromod", value = Dist.CLIENT)
public final class ClientDuelAnimationRenderer {
    private static final BufferSource BUFFER = MultiBufferSource.immediate(new BufferBuilder(32768));
    private static final int[] ABSORB_COLORS = new int[]{12619007, 6750132, 16765517, 16734810, 5944575, 9240476};

    private ClientDuelAnimationRenderer() {
    }

    private static void spawnAbsorbParticles(Minecraft minecraft, ClientAnimBridge.Frame frame) {
        if (minecraft.level != null) {
            int kind;
            try {
                kind = Integer.parseInt(frame.kind().substring("absorb".length()));
            } catch (RuntimeException ex) {
                return;
            }

            if (kind >= 0 && kind < ABSORB_COLORS.length) {
                int color = ABSORB_COLORS[kind];
                float red = (color >> 16 & 0xFF) / 255.0F;
                float green = (color >> 8 & 0xFF) / 255.0F;
                float blue = (color & 0xFF) / 255.0F;
                Vec3 at = frame.center();
                RandomSource random = minecraft.level.random;

                for (int i = 0; i < 4; i++) {
                    minecraft.level
                        .addParticle(
                            ParticleTypes.ENTITY_EFFECT,
                            at.x + (random.nextDouble() - 0.5) * 0.6,
                            at.y + (random.nextDouble() - 0.3) * 0.6,
                            at.z + (random.nextDouble() - 0.5) * 0.6,
                            red,
                            green,
                            blue
                        );
                }
            }
        }
    }

    @SubscribeEvent
    public static void render(RenderLevelStageEvent event) {
        if (event.getStage() == Stage.AFTER_TRANSLUCENT_BLOCKS) {
            Minecraft minecraft = Minecraft.getInstance();
            if (minecraft.level != null) {
                Vec3 camera = event.getCamera().getPosition();
                PoseStack poses = event.getPoseStack();

                for (ClientAnimBridge.Frame frame : ClientAnimBridge.frames()) {
                    if (!(frame.scale() <= 0.002F) && !(frame.center().distanceToSqr(camera) > 16384.0)) {
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
                            case "effect", "effect_reveal", "reveal", "search_reveal", "material" -> 15728880;
                            default -> LevelRenderer.getLightColor(minecraft.level, BlockPos.containing(frame.center()));
                        };
                        ResourceLocation back = CardBacks.texture(frame.cardBack());
                        ResourceLocation front = frame.faceUp() && frame.cardCode() > 0 ? CardTextureCache.get(frame.cardCode()) : back;
                        drawCard(poses, BUFFER, front, back, frame.width(), frame.height(), light);
                        poses.popPose();
                    }
                }

                BUFFER.endBatch();
            }
        }
    }

    @SubscribeEvent
    public static void loggedOut(LoggingOut event) {
        ClientAnimBridge.clear();
        ClientCardMarkers.clear();
        ClientHandBridge.clear();
    }

    public static void drawCard(PoseStack poses, MultiBufferSource buffers, ResourceLocation frontTexture, float width, float height, int light) {
        drawCard(poses, buffers, frontTexture, CardTextureCache.CARD_BACK, width, height, light);
    }

    public static void drawCard(PoseStack poses, MultiBufferSource buffers, ResourceLocation frontTexture, ResourceLocation backTexture,
                                float width, float height, int light) {
        VertexConsumer front = buffers.getBuffer(RenderType.entityCutout(frontTexture));
        side(poses, front, width, height, 0.002F, light, true);
        VertexConsumer back = buffers.getBuffer(RenderType.entityCutout(backTexture));
        side(poses, back, width, height, -0.002F, light, false);
    }

    private static void side(PoseStack poses, VertexConsumer vertices, float width, float height, float z, int light, boolean front) {
        float left = -width * 0.5F;
        float right = width * 0.5F;
        float bottom = -height * 0.5F;
        float top = height * 0.5F;
        if (front) {
            vertex(poses, vertices, left, top, z, 0.0F, 0.0F, light, 1.0F);
            vertex(poses, vertices, left, bottom, z, 0.0F, 1.0F, light, 1.0F);
            vertex(poses, vertices, right, bottom, z, 1.0F, 1.0F, light, 1.0F);
            vertex(poses, vertices, right, top, z, 1.0F, 0.0F, light, 1.0F);
        } else {
            vertex(poses, vertices, right, top, z, 0.0F, 0.0F, light, -1.0F);
            vertex(poses, vertices, right, bottom, z, 0.0F, 1.0F, light, -1.0F);
            vertex(poses, vertices, left, bottom, z, 1.0F, 1.0F, light, -1.0F);
            vertex(poses, vertices, left, top, z, 1.0F, 0.0F, light, -1.0F);
        }
    }

    private static void vertex(PoseStack poses, VertexConsumer vertices, float x, float y, float z, float u, float v, int light, float normalZ) {
        Matrix4f pose = poses.last().pose();
        Matrix3f normal = poses.last().normal();
        vertices.vertex(pose, x, y, z)
            .color(255, 255, 255, 255)
            .uv(u, v)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(light)
            .normal(normal, 0.0F, 0.0F, normalZ)
            .endVertex();
    }
}
