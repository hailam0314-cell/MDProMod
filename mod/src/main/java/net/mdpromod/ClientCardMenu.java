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
 * 点开一张卡之后，在"卡片前方"固定出几个按钮：按钮固定在世界坐标里，
 * 用准星对着它左键就能点。能出现哪些按钮由服务端按核心候选列表给出。
 */
@OnlyIn(Dist.CLIENT)
public final class ClientCardMenu {
    private static final float BUTTON_W = 0.52F;
    private static final float BUTTON_H = 0.17F;
    private static final float FRONT_GAP = 0.62F;
    private static final float GAP = 0.07F;
    private static final float TEXT_SCALE = 0.018F;
    private static final String[] LABELS = {"召唤", "盖放", "发动", "攻击", "表示", "特殊召唤"};

    private static int entityId = -1;
    private static int mask;

    private ClientCardMenu() {
    }

    public static void open(int id, int actionMask) {
        if (actionMask == 0) {
            close();
            return;
        }
        entityId = id;
        mask = actionMask;
        ClientDuelAudio.playUi("SE_DUEL_SELECT");
    }

    public static void close() {
        entityId = -1;
        mask = 0;
    }

    public static boolean isOpen() {
        return entityId >= 0;
    }

    private static Entity card(Minecraft minecraft) {
        if (minecraft.level == null || entityId < 0) return null;
        Entity entity = minecraft.level.getEntity(entityId);
        return entity == null || entity.isRemoved() ? null : entity;
    }

    /** 卡片前方那一排按钮的位置（世界坐标）与种类。 */
    private static List<Object[]> layout(Entity card) {
        List<Object[]> out = new ArrayList<>();
        List<Integer> kinds = new ArrayList<>();
        for (int kind = 0; kind < LABELS.length; kind++) {
            if ((mask & (1 << kind)) != 0) kinds.add(kind);
        }
        if (kinds.isEmpty()) return out;
        // "卡片前方"以玩家为准：按钮永远放在卡片与玩家之间，并转身面向玩家
        Entity viewer = Minecraft.getInstance().player;
        Vec3 front = viewer == null
                ? new Vec3(0, 0, 1)
                : new Vec3(viewer.getX() - card.getX(), 0, viewer.getZ() - card.getZ());
        if (front.lengthSqr() < 1.0E-4) front = new Vec3(0, 0, 1);
        front = front.normalize();
        Vec3 right = new Vec3(-front.z, 0, front.x);
        float yaw = (float) Math.toDegrees(Math.atan2(front.x, front.z));
        float total = kinds.size() * BUTTON_W + (kinds.size() - 1) * GAP;
        float offset = -total / 2 + BUTTON_W / 2;
        Vec3 base = card.position().add(front.scale(FRONT_GAP)).add(0.0, 0.10, 0.0);
        for (int kind : kinds) {
            out.add(new Object[]{base.add(right.scale(offset)), kind, yaw});
            offset += BUTTON_W + GAP;
        }
        return out;
    }

    public static void render(Minecraft minecraft, PoseStack poses) {
        if (entityId < 0) return;
        Entity card = card(minecraft);
        if (card == null) {
            close();
            return;
        }
        List<Object[]> buttons = layout(card);
        if (buttons.isEmpty()) return;
        Vec3 cam = minecraft.gameRenderer.getMainCamera().getPosition();
        MultiBufferSource.BufferSource plates = MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());
        VertexConsumer quads = plates.getBuffer(RenderType.debugQuads());
        for (Object[] entry : buttons) {
            Vec3 at = (Vec3) entry[0];
            float yaw = (Float) entry[2];
            poses.pushPose();
            poses.translate(at.x - cam.x, at.y - cam.y, at.z - cam.z);
            poses.mulPose(Axis.YP.rotationDegrees(yaw));
            Matrix4f m = poses.last().pose();
            float hw = BUTTON_W / 2 + 0.02F;
            float hh = BUTTON_H / 2 + 0.02F;
            quad(quads, m, -hw, -hh, hw, hh);
            poses.popPose();
        }
        plates.endBatch();
        MultiBufferSource.BufferSource texts = MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());
        for (Object[] entry : buttons) {
            Vec3 at = (Vec3) entry[0];
            String label = LABELS[(Integer) entry[1]];
            float yaw = (Float) entry[2];
            float width = minecraft.font.width(label);
            poses.pushPose();
            poses.translate(at.x - cam.x, at.y - cam.y, at.z - cam.z);
            poses.mulPose(Axis.YP.rotationDegrees(yaw));
            poses.scale(TEXT_SCALE, -TEXT_SCALE, TEXT_SCALE);
            minecraft.font.drawInBatch(Component.literal(label), -width / 2.0F, -4.0F,
                    0xFFFFFFFF, true, poses.last().pose(), texts,
                    net.minecraft.client.gui.Font.DisplayMode.SEE_THROUGH, 0, LightTexture.FULL_BRIGHT);
            poses.popPose();
        }
        texts.endBatch();
    }

    private static void quad(VertexConsumer quads, Matrix4f matrix,
                             float x0, float y0, float x1, float y1) {
        quads.vertex(matrix, x0, y0, 0.0F).color(20, 30, 20, 210).endVertex();
        quads.vertex(matrix, x0, y1, 0.0F).color(20, 30, 20, 210).endVertex();
        quads.vertex(matrix, x1, y1, 0.0F).color(20, 30, 20, 210).endVertex();
        quads.vertex(matrix, x1, y0, 0.0F).color(20, 30, 20, 210).endVertex();
    }

    /** 左键：准星打到哪个按钮就执行哪个；打空就收起菜单（这次点击放行给世界）。 */
    public static boolean mouseClicked(Minecraft minecraft) {
        if (entityId < 0) return false;
        Entity card = card(minecraft);
        if (card == null || minecraft.player == null) {
            close();
            return false;
        }
        Vec3 eye = minecraft.player.getEyePosition();
        Vec3 end = eye.add(minecraft.player.getViewVector(1.0F).scale(4.0));
        int hit = -1;
        double best = Double.MAX_VALUE;
        for (Object[] entry : layout(card)) {
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
        if (hit >= 0) {
            ClientDuelAudio.playUi("SE_DUEL_DECIDE");
            ModNetwork.sendToServer(new ModNetwork.CardAct(entityId, hit));
            close();
            return true;
        }
        close();
        return false;
    }
}
