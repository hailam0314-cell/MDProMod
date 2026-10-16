package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.Tesselator;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.math.Axis;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.LightTexture;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.client.renderer.texture.OverlayTexture;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;
import org.joml.Matrix4f;

import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

/**
 * 怪兽立绘：表侧怪兽站在怪兽区时，在卡片上方浮一张朝玩家的大图。
 *
 * 对齐 MDPro 的行为（见 docs/closeup-compress-2026-10-14.md）：
 * 只在**表侧 + 怪兽区 + 有立绘文件**时显示；出现时 0.3 秒从 0 缩放到目标（弹出）；
 * 之后 5 秒一个循环：上下浮动 + 朝观察者方向轻轻前倾 + 亮度呼吸；
 * **效果被无效**（核心 QUERY_STATUS bit0）时换成灰度版本（对应 MDPro 的 `Mono=1`）。
 * 不上属性色（这是本仓库的取舍：属性色留在卡框/图标上就够）。
 * 关闭：{@code -Dmdpromod.closeup=false}；贴图目录：{@code -Dmdpromod.closeupRoot}。
 */
@OnlyIn(Dist.CLIENT)
public final class ClientCloseups {
    private static final boolean ENABLED =
            Boolean.parseBoolean(System.getProperty("mdpromod.closeup", "true"));
    /** 立绘高度（方块）。场上卡片本身是 0.86 × 1.25，立绘比它明显大、但不至于把邻格盖住。 */
    private static final float HEIGHT = 1.9F;
    /** 底边离卡面多高。 */
    private static final float GAP = 0.30F;
    /** 弹出时长（毫秒）与待机周期。 */
    private static final long POP_MS = 300L;
    private static final long LOOP_MS = 5000L;
    /** 待机浮动幅度（方块）与朝观察者的前倾量。 */
    private static final float BOB = 0.07F;
    private static final float LEAN = 0.10F;
    /** entityId → 开始显示的时刻，用来算弹出与待机相位。 */
    private static final Map<Integer, Long> SHOWN_AT = new HashMap<>();

    private ClientCloseups() {
    }

    public static void render(Minecraft minecraft, PoseStack poses) {
        if (!ENABLED || minecraft.level == null || minecraft.player == null) return;
        Vec3 camera = minecraft.gameRenderer.getMainCamera().getPosition();
        List<DuelCardEntity> cards = minecraft.level.getEntitiesOfClass(DuelCardEntity.class,
                minecraft.player.getBoundingBox().inflate(48.0));
        if (cards.isEmpty()) {
            SHOWN_AT.clear();
            return;
        }
        long now = System.currentTimeMillis();
        Set<Integer> seen = new HashSet<>();
        MultiBufferSource.BufferSource buffer =
                MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());
        for (DuelCardEntity card : cards) {
            if (ClientAnimBridge.shouldHide(card)) continue;
            int code = card.getCardCode();
            if (code <= 0 || !card.isFaceUp() || !isMonsterZone(card.getZoneId())) continue;
            // 效果被无效 → 用灰度版本（等价于 MDPro 材质上的 Mono=1）
            CloseupTextureCache.Entry entry = CloseupTextureCache.get(code, card.isDisabled());
            if (entry == null) continue;
            seen.add(card.getId());
            render(minecraft, poses, buffer, camera, card, entry, now);
        }
        SHOWN_AT.keySet().retainAll(seen);
        buffer.endBatch();
    }

    private static void render(Minecraft minecraft, PoseStack poses, MultiBufferSource buffer,
                               Vec3 camera, DuelCardEntity card, CloseupTextureCache.Entry entry,
                               long now) {
        long shownAt = SHOWN_AT.computeIfAbsent(card.getId(), id -> now);
        long age = now - shownAt;
        float pop = Math.min(1.0F, age / (float) POP_MS);
        pop = 1.0F - (1.0F - pop) * (1.0F - pop) * (1.0F - pop);      // easeOutCubic
        float phase = (age % LOOP_MS) / (float) LOOP_MS;
        float wave = (float) Math.sin(phase * Math.PI * 2.0);

        float height = HEIGHT * pop;
        float width = height * entry.aspect();
        double x = card.getX();
        double y = card.getY() + 0.03 + GAP + height / 2.0 + wave * BOB;
        double z = card.getZ();
        // 朝向观察者（只在水平面转，立绘保持竖直）
        Vec3 toCamera = new Vec3(camera.x - x, 0.0, camera.z - z);
        if (toCamera.lengthSqr() < 1.0E-4) toCamera = new Vec3(0.0, 0.0, 1.0);
        else toCamera = toCamera.normalize();
        float yaw = (float) Math.toDegrees(Math.atan2(toCamera.x, toCamera.z));
        // 待机时朝观察者轻轻前倾（对应 MDPro 动画里的 +z 位移）
        double lean = wave * LEAN;
        x += toCamera.x * lean;
        z += toCamera.z * lean;

        float breathe = 0.90F + 0.10F * (0.5F + 0.5F * wave);          // 亮度呼吸
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

    private static void vertex(VertexConsumer consumer, Matrix4f matrix, float x, float y,
                               int red, int green, int blue, float u, float v) {
        consumer.vertex(matrix, x, y, 0.0F)
                .color(red, green, blue, 255)
                .uv(u, v)
                .overlayCoords(OverlayTexture.NO_OVERLAY)
                .uv2(LightTexture.FULL_BRIGHT)      // 立绘自带"发光感"，不受场地明暗影响
                .normal(0.0F, 0.0F, 1.0F)
                .endVertex();
    }

    /** 只在怪兽区显示：自己的 1~5 号怪兽区、共享额外怪兽区。 */
    private static boolean isMonsterZone(String zoneId) {
        if (zoneId == null) return false;
        return zoneId.startsWith("shared_emz_") || zoneId.contains("_mzone_");
    }

}
