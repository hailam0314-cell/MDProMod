package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.Tesselator;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.math.Axis;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.client.renderer.MultiBufferSource.BufferSource;
import net.minecraft.client.renderer.texture.OverlayTexture;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;
import org.joml.Matrix4f;

@OnlyIn(Dist.CLIENT)
public final class ClientCloseups {
    private static final boolean ENABLED = Boolean.parseBoolean(System.getProperty("mdpromod.closeup", "true"));
    private static final float HEIGHT = 1.9F;
    private static final float GAP = 0.3F;
    private static final long POP_MS = 300L;
    private static final long LOOP_MS = 5000L;
    private static final float BOB = 0.07F;
    private static final float LEAN = 0.1F;
    private static final Map<Integer, Long> SHOWN_AT = new HashMap<>();

    private ClientCloseups() {
    }

    public static void render(Minecraft minecraft, PoseStack poses) {
        if (ENABLED && minecraft.level != null && minecraft.player != null) {
            Vec3 camera = minecraft.gameRenderer.getMainCamera().getPosition();
            List<DuelCardEntity> cards = minecraft.level.getEntitiesOfClass(DuelCardEntity.class, minecraft.player.getBoundingBox().inflate(48.0));
            if (cards.isEmpty()) {
                SHOWN_AT.clear();
            } else {
                long now = System.currentTimeMillis();
                Set<Integer> seen = new HashSet<>();
                BufferSource buffer = MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());

                for (DuelCardEntity card : cards) {
                    if (!ClientAnimBridge.shouldHide(card)) {
                        int code = card.getCardCode();
                        if (code > 0 && card.isFaceUp() && isMonsterZone(card.getZoneId())) {
                            CloseupTextureCache.Entry entry = CloseupTextureCache.get(code, card.isDisabled());
                            if (entry != null) {
                                seen.add(card.getId());
                                render(minecraft, poses, buffer, camera, card, entry, now);
                            }
                        }
                    }
                }

                SHOWN_AT.keySet().retainAll(seen);
                buffer.endBatch();
            }
        }
    }

    private static void render(
        Minecraft minecraft, PoseStack poses, MultiBufferSource buffer, Vec3 camera, DuelCardEntity card, CloseupTextureCache.Entry entry, long now
    ) {
        long shownAt = SHOWN_AT.computeIfAbsent(card.getId(), id -> now);
        long age = now - shownAt;
        float pop = Math.min(1.0F, (float)age / 300.0F);
        pop = 1.0F - (1.0F - pop) * (1.0F - pop) * (1.0F - pop);
        float phase = (float)(age % 5000L) / 5000.0F;
        float wave = (float)Math.sin(phase * Math.PI * 2.0);
        float height = 1.9F * pop;
        float width = height * entry.aspect();
        double x = card.getX();
        double y = card.getY() + 0.03 + 0.3F + height / 2.0 + wave * 0.07F;
        double z = card.getZ();
        Vec3 toCamera = new Vec3(camera.x - x, 0.0, camera.z - z);
        if (toCamera.lengthSqr() < 1.0E-4) {
            toCamera = new Vec3(0.0, 0.0, 1.0);
        } else {
            toCamera = toCamera.normalize();
        }

        float yaw = (float)Math.toDegrees(Math.atan2(toCamera.x, toCamera.z));
        double lean = wave * 0.1F;
        x += toCamera.x * lean;
        z += toCamera.z * lean;
        float breathe = 0.9F + 0.1F * (0.5F + 0.5F * wave);
        int red = Math.round(255.0F * breathe);
        int green = red;
        int blue = red;
        VertexConsumer consumer = buffer.getBuffer(RenderType.entityTranslucent(entry.location()));
        poses.pushPose();
        poses.translate(x - camera.x, y - camera.y, z - camera.z);
        poses.mulPose(Axis.YP.rotationDegrees(yaw));
        Matrix4f matrix = poses.last().pose();
        float hw = width / 2.0F;
        float hh = height / 2.0F;
        vertex(consumer, matrix, -hw, -hh, red, green, blue, 0.0F, 1.0F);
        vertex(consumer, matrix, hw, -hh, red, green, blue, 1.0F, 1.0F);
        vertex(consumer, matrix, hw, hh, red, green, blue, 1.0F, 0.0F);
        vertex(consumer, matrix, -hw, hh, red, green, blue, 0.0F, 0.0F);
        poses.popPose();
    }

    private static void vertex(VertexConsumer consumer, Matrix4f matrix, float x, float y, int red, int green, int blue, float u, float v) {
        consumer.vertex(matrix, x, y, 0.0F)
            .color(red, green, blue, 255)
            .uv(u, v)
            .overlayCoords(OverlayTexture.NO_OVERLAY)
            .uv2(15728880)
            .normal(0.0F, 0.0F, 1.0F)
            .endVertex();
    }

    private static boolean isMonsterZone(String zoneId) {
        return zoneId == null ? false : zoneId.startsWith("shared_emz_") || zoneId.contains("_mzone_");
    }
}
