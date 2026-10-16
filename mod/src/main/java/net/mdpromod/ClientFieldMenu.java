package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.Tesselator;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.math.Axis;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.LightTexture;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.network.chat.Component;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.phys.AABB;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;
import org.joml.Matrix4f;

import java.util.ArrayList;
import java.util.List;

/**
 * 决斗盘右下角那个确认键的菜单：左键点它之后，在键前面浮出一排按钮——
 * 「投降」（只在决斗进行中给）和「时点」（点一下在 全时点 / 默认 / 关闭 之间循环）。
 *
 * <p>和卡片动作按钮一个用法：按钮固定在世界坐标里，用准星对着它左键就能点；
 * 左键打空就收起菜单（这次点击放行给世界）。
 */
@OnlyIn(Dist.CLIENT)
public final class ClientFieldMenu {
    private static final float BUTTON_W = 0.72F;
    private static final float BUTTON_H = 0.20F;
    private static final float GAP = 0.09F;
    private static final float TEXT_SCALE = 0.018F;
    /** 菜单浮在键的前上方，好瞄也好认。 */
    private static final float FORWARD = 0.90F;
    private static final float HEIGHT = 0.95F;

    private static final int ACTION_SURRENDER = 0;
    private static final int ACTION_TIMING = 1;

    private static int fieldEntityId = -1;
    private static String controlId = "";
    /** 菜单是几点开的：久久没动作就自己收起来，免得一直挂着挡事。 */
    private static long openedAt;
    private static final long IDLE_CLOSE_MS = 8000;

    private ClientFieldMenu() {
    }

    public static void open(int fieldId, String control) {
        fieldEntityId = fieldId;
        controlId = control == null ? "" : control;
        openedAt = System.currentTimeMillis();
        ClientDuelAudio.playUi("SE_DUEL_SELECT");
    }

    public static void close() {
        fieldEntityId = -1;
        controlId = "";
    }

    public static boolean isOpen() {
        return fieldEntityId >= 0;
    }

    /** 每 tick 检查一下：决斗盘没了就把菜单收掉，免得按钮留在空气里。 */
    public static void tick(Minecraft minecraft) {
        if (fieldEntityId < 0) return;
        if (field(minecraft) == null || System.currentTimeMillis() - openedAt > IDLE_CLOSE_MS) close();
    }

    private static DuelFieldEntity field(Minecraft minecraft) {
        if (minecraft.level == null || fieldEntityId < 0) return null;
        Entity entity = minecraft.level.getEntity(fieldEntityId);
        return entity instanceof DuelFieldEntity value && !value.isRemoved() ? value : null;
    }

    /** 菜单里该有哪几个按钮（投降只在决斗进行中出现）。 */
    private static List<Integer> entries(DuelFieldEntity field) {
        List<Integer> out = new ArrayList<>(2);
        if (field.isGameStarted() && !field.hasResult()) out.add(ACTION_SURRENDER);
        out.add(ACTION_TIMING);
        return out;
    }

    private static String label(DuelFieldEntity field, int action) {
        return action == ACTION_SURRENDER ? "投降" : field.timingMode().buttonText();
    }

    /** 确认键的世界坐标（场地局部坐标 → 世界坐标，和命中判定用同一套算法）。 */
    private static Vec3 controlCenter(DuelFieldEntity field) {
        DuelFieldLayout.Control control = DuelFieldLayout.controlById(controlId).orElse(null);
        if (control == null) return field.position().add(0.0, HEIGHT, 0.0);
        Vec3 local = new Vec3(control.x() / 16.0, control.y() / 16.0, control.z() / 16.0)
                .yRot(-(float) Math.toRadians(field.getYRot()));
        return field.position().add(local);
    }

    /** 每个按钮的世界坐标与朝向（面向本地玩家）。 */
    private static List<Object[]> layout(Minecraft minecraft, DuelFieldEntity field) {
        List<Integer> actions = entries(field);
        List<Object[]> out = new ArrayList<>(actions.size());
        if (actions.isEmpty()) return out;
        Vec3 anchor = controlCenter(field);
        Vec3 viewer = minecraft.player == null ? null : minecraft.player.position();
        Vec3 front = viewer == null ? new Vec3(0, 0, 1)
                : new Vec3(viewer.x - anchor.x, 0, viewer.z - anchor.z);
        if (front.lengthSqr() < 1.0E-4) front = new Vec3(0, 0, 1);
        front = front.normalize();
        Vec3 right = new Vec3(-front.z, 0, front.x);
        // 和卡片动作按钮同一套：+Z 轴朝玩家，字才是正的
        float yaw = (float) Math.toDegrees(Math.atan2(front.x, front.z));
        float total = actions.size() * BUTTON_W + (actions.size() - 1) * GAP;
        float offset = -total / 2 + BUTTON_W / 2;
        Vec3 base = anchor.add(front.scale(FORWARD)).add(0.0, HEIGHT, 0.0);
        for (int action : actions) {
            out.add(new Object[]{base.add(right.scale(offset)), action, yaw});
            offset += BUTTON_W + GAP;
        }
        return out;
    }

    public static void render(Minecraft minecraft, PoseStack poses) {
        if (fieldEntityId < 0) return;
        DuelFieldEntity field = field(minecraft);
        if (field == null) {
            close();
            return;
        }
        List<Object[]> buttons = layout(minecraft, field);
        if (buttons.isEmpty()) return;
        Vec3 cam = minecraft.gameRenderer.getMainCamera().getPosition();
        MultiBufferSource.BufferSource plates = MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());
        VertexConsumer quads = plates.getBuffer(RenderType.debugQuads());
        for (Object[] entry : buttons) {
            Vec3 at = (Vec3) entry[0];
            poses.pushPose();
            poses.translate(at.x - cam.x, at.y - cam.y, at.z - cam.z);
            poses.mulPose(Axis.YP.rotationDegrees((Float) entry[2]));
            Matrix4f m = poses.last().pose();
            float hw = BUTTON_W / 2 + 0.02F;
            float hh = BUTTON_H / 2 + 0.02F;
            quad(quads, m, -hw, -hh, hw, hh, (Integer) entry[1] == ACTION_SURRENDER);
            poses.popPose();
        }
        plates.endBatch();
        MultiBufferSource.BufferSource texts = MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());
        for (Object[] entry : buttons) {
            Vec3 at = (Vec3) entry[0];
            String text = label(field, (Integer) entry[1]);
            float width = minecraft.font.width(text);
            poses.pushPose();
            poses.translate(at.x - cam.x, at.y - cam.y, at.z - cam.z);
            poses.mulPose(Axis.YP.rotationDegrees((Float) entry[2]));
            poses.scale(TEXT_SCALE, -TEXT_SCALE, TEXT_SCALE);
            minecraft.font.drawInBatch(Component.literal(text), -width / 2.0F, -4.0F,
                    0xFFFFFFFF, true, poses.last().pose(), texts,
                    net.minecraft.client.gui.Font.DisplayMode.SEE_THROUGH, 0, LightTexture.FULL_BRIGHT);
            poses.popPose();
        }
        texts.endBatch();
    }

    /** 投降按钮画成偏红，时点偏蓝黑：一眼分得清。 */
    private static void quad(VertexConsumer quads, Matrix4f matrix,
                             float x0, float y0, float x1, float y1, boolean danger) {
        int r = danger ? 70 : 20;
        int g = danger ? 20 : 30;
        int b = danger ? 20 : 40;
        quads.vertex(matrix, x0, y0, 0.0F).color(r, g, b, 215).endVertex();
        quads.vertex(matrix, x0, y1, 0.0F).color(r, g, b, 215).endVertex();
        quads.vertex(matrix, x1, y1, 0.0F).color(r, g, b, 215).endVertex();
        quads.vertex(matrix, x1, y0, 0.0F).color(r, g, b, 215).endVertex();
    }

    /**
     * 鼠标总入口（左右键都走这里）：
     * <ol>
     *   <li>菜单开着、准星打到按钮 → 执行（投降后收起；时点留在原地，方便连点换档）；</li>
     *   <li>菜单开着、没打到按钮 → 收起菜单，**这一下点击放行**（点阶段键/卡片照常生效，
     *       不然第一下总被菜单吃掉，"按钮很难点"就是这么来的）；</li>
     *   <li>菜单没开、准星对着自己那侧的**确认键**：左键开菜单；</li>
     *   <li>菜单没开、准星对着自己那侧的**确认键 / 阶段键**：左右键都算"按一下"，
     *       发 {@link ModNetwork.FieldClick} 交给服务端校验执行——判定用客户端此刻的视线，
     *       比服务端上一 tick 的视角准，按键也就好点了。</li>
     * </ol>
     *
     * @return true = 这次点击已经被处理掉，别再交给世界
     */
    public static boolean handleMouse(Minecraft minecraft, int button) {
        // 有界面开着就别插手：鼠标归那个界面（哪怕只是"界面打开前的最后一帧"也不抢）
        if (minecraft.screen != null) return false;
        if (minecraft.player == null || minecraft.level == null) return false;
        if (fieldEntityId < 0) return pressControl(minecraft, button);
        DuelFieldEntity field = field(minecraft);
        if (field == null) {
            close();
            return false;
        }
        int hit = buttonAt(minecraft, field);
        if (hit < 0) {
            close();
            // 关键：这一下要放行——玩家多半是想点阶段键或者卡片
            return false;
        }
        openedAt = System.currentTimeMillis();     // 还在点，别被空闲超时收走
        ClientDuelAudio.playUi(hit == ACTION_SURRENDER ? "SE_DUEL_DECIDE" : "SE_DUEL_SELECT");
        if (hit == ACTION_SURRENDER) {
            ModNetwork.sendToServer(new ModNetwork.FieldAction(ACTION_SURRENDER, 0));
            close();
        } else {
            // 时点：点一下就换下一档；界面上的字跟着决斗盘的同步状态立刻变，
            // 所以这里不关菜单，方便连着点几下挑到想要的那档
            ModNetwork.sendToServer(new ModNetwork.FieldAction(ACTION_TIMING,
                    field.timingMode().next().ordinal()));
        }
        return true;
    }

    /** 准星打在菜单按钮上了吗（返回按钮的动作号，没有返回 -1）。 */
    private static int buttonAt(Minecraft minecraft, DuelFieldEntity field) {
        Vec3 eye = minecraft.player.getEyePosition();
        Vec3 end = eye.add(minecraft.player.getViewVector(1.0F).scale(4.0));
        int hit = -1;
        double best = Double.MAX_VALUE;
        for (Object[] entry : layout(minecraft, field)) {
            Vec3 at = (Vec3) entry[0];
            AABB box = new AABB(at.x - BUTTON_W / 2, at.y - BUTTON_H / 2, at.z - BUTTON_W / 2,
                    at.x + BUTTON_W / 2, at.y + BUTTON_H / 2, at.z + BUTTON_W / 2);
            var clip = box.clip(eye, end);
            if (clip.isEmpty()) continue;
            double distance = clip.get().distanceToSqr(eye);
            if (distance >= best) continue;
            best = distance;
            hit = (Integer) entry[1];
        }
        return hit;
    }

    /**
     * 菜单没开时按下鼠标：准星对着自己那侧的确认键就开菜单（左键），
     * 对着确认键 / 阶段键就发点击给服务端（左右键都算按一下）。
     */
    private static boolean pressControl(Minecraft minecraft, int button) {
        for (DuelFieldEntity field : minecraft.level.getEntitiesOfClass(DuelFieldEntity.class,
                minecraft.player.getBoundingBox().inflate(24.0))) {
            DuelFieldLayout.Control control = field.controlUnderCrosshair(minecraft.player);
            if (control == null) continue;
            if (!field.playerSide(minecraft.player).equals(control.owner())) continue;
            if ("confirm".equals(control.kind()) && button == 0) {
                open(field.getId(), control.id());
                return true;
            }
            if ("confirm".equals(control.kind()) || "phase".equals(control.kind())) {
                ModNetwork.sendToServer(new ModNetwork.FieldClick(control.id()));
                ClientDuelAudio.playUi("SE_DUEL_SELECT");
                return true;
            }
            return false;
        }
        return false;
    }
}
