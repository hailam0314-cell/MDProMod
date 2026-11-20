package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.Tesselator;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.math.Axis;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.Font.DisplayMode;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.client.renderer.MultiBufferSource.BufferSource;
import net.minecraft.network.chat.Component;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.phys.AABB;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;
import org.joml.Matrix4f;

@OnlyIn(Dist.CLIENT)
public final class ClientCardMenu {
    private static final float BUTTON_W = 0.52F;
    private static final float BUTTON_H = 0.17F;
    private static final float FRONT_GAP = 0.62F;
    private static final float GAP = 0.07F;
    private static final float TEXT_SCALE = 0.018F;
    private static final String[] LABELS = new String[]{"召唤", "盖放", "发动", "攻击", "表示", "特殊召唤"};
    private static int entityId = -1;
    private static int mask;

    private ClientCardMenu() {
    }

    public static void open(int id, int actionMask) {
        if (actionMask == 0) {
            close();
        } else {
            entityId = id;
            mask = actionMask;
            ClientDuelAudio.playUi("SE_DUEL_SELECT");
        }
    }

    public static void close() {
        entityId = -1;
        mask = 0;
    }

    public static boolean isOpen() {
        return entityId >= 0;
    }

    private static Entity card(Minecraft minecraft) {
        if (minecraft.level != null && entityId >= 0) {
            Entity entity = minecraft.level.getEntity(entityId);
            return entity != null && !entity.isRemoved() ? entity : null;
        } else {
            return null;
        }
    }

    private static List<Object[]> layout(Entity card) {
        List<Object[]> out = new ArrayList<>();
        List<Integer> kinds = new ArrayList<>();

        for (int kind = 0; kind < LABELS.length; kind++) {
            if ((mask & 1 << kind) != 0) {
                kinds.add(kind);
            }
        }

        if (kinds.isEmpty()) {
            return out;
        }

        Entity viewer = Minecraft.getInstance().player;
        Vec3 front = viewer == null ? new Vec3(0.0, 0.0, 1.0) : new Vec3(viewer.getX() - card.getX(), 0.0, viewer.getZ() - card.getZ());
        if (front.lengthSqr() < 1.0E-4) {
            front = new Vec3(0.0, 0.0, 1.0);
        }

        front = front.normalize();
        Vec3 right = new Vec3(-front.z, 0.0, front.x);
        float yaw = (float)Math.toDegrees(Math.atan2(front.x, front.z));
        float total = kinds.size() * 0.52F + (kinds.size() - 1) * 0.07F;
        float offset = -total / 2.0F + 0.26F;
        Vec3 base = card.position().add(front.scale(0.62F)).add(0.0, 0.1, 0.0);

        for (int kind : kinds) {
            out.add(new Object[]{base.add(right.scale(offset)), kind, yaw});
            offset += 0.59F;
        }

        return out;
    }

    public static void render(Minecraft minecraft, PoseStack poses) {
        if (entityId >= 0) {
            Entity card = card(minecraft);
            if (card == null) {
                close();
            } else {
                List<Object[]> buttons = layout(card);
                if (!buttons.isEmpty()) {
                    Vec3 cam = minecraft.gameRenderer.getMainCamera().getPosition();
                    BufferSource plates = MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());
                    VertexConsumer quads = plates.getBuffer(RenderType.debugQuads());

                    for (Object[] entry : buttons) {
                        Vec3 at = (Vec3)entry[0];
                        float yaw = (Float)entry[2];
                        poses.pushPose();
                        poses.translate(at.x - cam.x, at.y - cam.y, at.z - cam.z);
                        poses.mulPose(Axis.YP.rotationDegrees(yaw));
                        Matrix4f m = poses.last().pose();
                        float hw = 0.28F;
                        float hh = 0.105000004F;
                        quad(quads, m, -hw, -hh, hw, hh);
                        poses.popPose();
                    }

                    plates.endBatch();
                    BufferSource texts = MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());

                    for (Object[] entry : buttons) {
                        Vec3 at = (Vec3)entry[0];
                        String label = LABELS[(Integer)entry[1]];
                        float yaw = (Float)entry[2];
                        float width = minecraft.font.width(label);
                        poses.pushPose();
                        poses.translate(at.x - cam.x, at.y - cam.y, at.z - cam.z);
                        poses.mulPose(Axis.YP.rotationDegrees(yaw));
                        poses.scale(0.018F, -0.018F, 0.018F);
                        minecraft.font
                            .drawInBatch(
                                Component.literal(label),
                                -width / 2.0F,
                                -4.0F,
                                -1,
                                true,
                                poses.last().pose(),
                                texts,
                                DisplayMode.SEE_THROUGH,
                                0,
                                15728880
                            );
                        poses.popPose();
                    }

                    texts.endBatch();
                }
            }
        }
    }

    private static void quad(VertexConsumer quads, Matrix4f matrix, float x0, float y0, float x1, float y1) {
        quads.vertex(matrix, x0, y0, 0.0F).color(20, 30, 20, 210).endVertex();
        quads.vertex(matrix, x0, y1, 0.0F).color(20, 30, 20, 210).endVertex();
        quads.vertex(matrix, x1, y1, 0.0F).color(20, 30, 20, 210).endVertex();
        quads.vertex(matrix, x1, y0, 0.0F).color(20, 30, 20, 210).endVertex();
    }

    public static boolean mouseClicked(Minecraft minecraft) {
        if (entityId < 0) {
            return false;
        }

        Entity card = card(minecraft);
        if (card != null && minecraft.player != null) {
            Vec3 eye = minecraft.player.getEyePosition();
            Vec3 end = eye.add(minecraft.player.getViewVector(1.0F).scale(4.0));
            int hit = -1;
            double best = Double.MAX_VALUE;

            for (Object[] entry : layout(card)) {
                Vec3 at = (Vec3)entry[0];
                AABB box = new AABB(
                    at.x - 0.26F, at.y - 0.085F, at.z - 0.26F, at.x + 0.26F, at.y + 0.085F, at.z + 0.26F
                );
                Optional<Vec3> clip = box.clip(eye, end);
                if (!clip.isEmpty()) {
                    double distance = clip.get().distanceToSqr(eye);
                    if (!(distance >= best)) {
                        best = distance;
                        hit = (Integer)entry[1];
                    }
                }
            }

            if (hit >= 0) {
                ClientDuelAudio.playUi("SE_DUEL_DECIDE");
                ModNetwork.sendToServer(new ModNetwork.CardAct(entityId, hit));
                close();
                return true;
            } else {
                close();
                return false;
            }
        } else {
            close();
            return false;
        }
    }
}
