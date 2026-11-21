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
import net.minecraft.world.phys.AABB;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;
import org.joml.Matrix4f;

@OnlyIn(Dist.CLIENT)
public final class ClientFieldMenu {
    private static final float BUTTON_W = 0.72F;
    private static final float BUTTON_H = 0.2F;
    private static final float GAP = 0.09F;
    private static final float TEXT_SCALE = 0.018F;
    private static final float FORWARD = 0.9F;
    private static final float HEIGHT = 0.95F;
    private static final int ACTION_SURRENDER = 0;
    private static final int ACTION_TIMING = 1;
    private static int fieldEntityId = -1;
    private static String controlId = "";
    private static long openedAt;
    private static final long IDLE_CLOSE_MS = 8000L;

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

    public static void tick(Minecraft minecraft) {
        if (fieldEntityId >= 0) {
            if (field(minecraft) == null || System.currentTimeMillis() - openedAt > 8000L) {
                close();
            }
        }
    }

    private static DuelFieldEntity field(Minecraft minecraft) {
        if (minecraft.level != null && fieldEntityId >= 0) {
            return minecraft.level.getEntity(fieldEntityId) instanceof DuelFieldEntity value && !value.isRemoved() ? value : null;
        } else {
            return null;
        }
    }

    private static List<Integer> entries(DuelFieldEntity field) {
        List<Integer> out = new ArrayList<>(2);
        if (field.isGameStarted() && !field.hasResult()) {
            out.add(0);
        }

        out.add(1);
        return out;
    }

    private static String label(DuelFieldEntity field, int action) {
        return action == 0 ? "投降" : field.timingMode().buttonText();
    }

    private static Vec3 controlCenter(DuelFieldEntity field) {
        DuelFieldLayout.Control control = DuelFieldLayout.controlById(controlId).orElse(null);
        if (control == null) {
            return field.position().add(0.0, 0.95F, 0.0);
        }

        return field.controlWorldPosition(control);
    }

    private static List<Object[]> layout(Minecraft minecraft, DuelFieldEntity field) {
        List<Integer> actions = entries(field);
        List<Object[]> out = new ArrayList<>(actions.size());
        if (actions.isEmpty()) {
            return out;
        }

        Vec3 anchor = controlCenter(field);
        Vec3 viewer = minecraft.player == null ? null : minecraft.player.position();
        Vec3 front = viewer == null ? new Vec3(0.0, 0.0, 1.0) : new Vec3(viewer.x - anchor.x, 0.0, viewer.z - anchor.z);
        if (front.lengthSqr() < 1.0E-4) {
            front = new Vec3(0.0, 0.0, 1.0);
        }

        front = front.normalize();
        Vec3 right = new Vec3(-front.z, 0.0, front.x);
        float yaw = (float)Math.toDegrees(Math.atan2(front.x, front.z));
        float total = actions.size() * 0.72F + (actions.size() - 1) * 0.09F;
        float offset = -total / 2.0F + 0.36F;
        Vec3 base = anchor.add(front.scale(0.9F)).add(0.0, 0.95F, 0.0);

        for (int action : actions) {
            out.add(new Object[]{base.add(right.scale(offset)), action, yaw});
            offset += 0.81000006F;
        }

        return out;
    }

    public static void render(Minecraft minecraft, PoseStack poses) {
        if (fieldEntityId >= 0) {
            DuelFieldEntity field = field(minecraft);
            if (field == null) {
                close();
            } else {
                List<Object[]> buttons = layout(minecraft, field);
                if (!buttons.isEmpty()) {
                    Vec3 cam = minecraft.gameRenderer.getMainCamera().getPosition();
                    BufferSource plates = MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());
                    VertexConsumer quads = plates.getBuffer(RenderType.debugQuads());

                    for (Object[] entry : buttons) {
                        Vec3 at = (Vec3)entry[0];
                        poses.pushPose();
                        poses.translate(at.x - cam.x, at.y - cam.y, at.z - cam.z);
                        poses.mulPose(Axis.YP.rotationDegrees((Float)entry[2]));
                        Matrix4f m = poses.last().pose();
                        float hw = 0.38000003F;
                        float hh = 0.120000005F;
                        quad(quads, m, -hw, -hh, hw, hh, (Integer)entry[1] == 0);
                        poses.popPose();
                    }

                    plates.endBatch();
                    BufferSource texts = MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());

                    for (Object[] entry : buttons) {
                        Vec3 at = (Vec3)entry[0];
                        String text = label(field, (Integer)entry[1]);
                        float width = minecraft.font.width(text);
                        poses.pushPose();
                        poses.translate(at.x - cam.x, at.y - cam.y, at.z - cam.z);
                        poses.mulPose(Axis.YP.rotationDegrees((Float)entry[2]));
                        poses.scale(0.018F, -0.018F, 0.018F);
                        minecraft.font
                            .drawInBatch(
                                Component.literal(text),
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

    private static void quad(VertexConsumer quads, Matrix4f matrix, float x0, float y0, float x1, float y1, boolean danger) {
        int r = danger ? 70 : 20;
        int g = danger ? 20 : 30;
        int b = danger ? 20 : 40;
        quads.vertex(matrix, x0, y0, 0.0F).color(r, g, b, 215).endVertex();
        quads.vertex(matrix, x0, y1, 0.0F).color(r, g, b, 215).endVertex();
        quads.vertex(matrix, x1, y1, 0.0F).color(r, g, b, 215).endVertex();
        quads.vertex(matrix, x1, y0, 0.0F).color(r, g, b, 215).endVertex();
    }

    public static boolean handleMouse(Minecraft minecraft, int button) {
        if (minecraft.screen != null) {
            return false;
        }

        if (minecraft.player == null || minecraft.level == null) {
            return false;
        }

        if (fieldEntityId < 0) {
            return pressControl(minecraft, button);
        }

        DuelFieldEntity field = field(minecraft);
        if (field == null) {
            close();
            return false;
        }

        int hit = buttonAt(minecraft, field);
        if (hit < 0) {
            close();
            return false;
        }

        openedAt = System.currentTimeMillis();
        ClientDuelAudio.playUi(hit == 0 ? "SE_DUEL_DECIDE" : "SE_DUEL_SELECT");
        if (hit == 0) {
            ModNetwork.sendToServer(new ModNetwork.FieldAction(0, 0));
            close();
        } else {
            ModNetwork.sendToServer(new ModNetwork.FieldAction(1, field.timingMode().next().ordinal()));
        }

        return true;
    }

    private static int buttonAt(Minecraft minecraft, DuelFieldEntity field) {
        Vec3 eye = minecraft.player.getEyePosition();
        Vec3 end = eye.add(minecraft.player.getViewVector(1.0F).scale(4.0));
        int hit = -1;
        double best = Double.MAX_VALUE;

        for (Object[] entry : layout(minecraft, field)) {
            Vec3 at = (Vec3)entry[0];
            AABB box = new AABB(at.x - 0.36F, at.y - 0.1F, at.z - 0.36F, at.x + 0.36F, at.y + 0.1F, at.z + 0.36F);
            Optional<Vec3> clip = box.clip(eye, end);
            if (!clip.isEmpty()) {
                double distance = clip.get().distanceToSqr(eye);
                if (!(distance >= best)) {
                    best = distance;
                    hit = (Integer)entry[1];
                }
            }
        }

        return hit;
    }

    private static boolean pressControl(Minecraft minecraft, int button) {
        for (DuelFieldEntity field : minecraft.level.getEntitiesOfClass(DuelFieldEntity.class, minecraft.player.getBoundingBox().inflate(24.0))) {
            DuelFieldLayout.Control control = field.controlUnderCrosshair(minecraft.player);
            if (control != null && field.sideOfPlayer(minecraft.player).equals(control.owner())) {
                if ("confirm".equals(control.kind()) && button == 0) {
                    open(field.getId(), control.id());
                    return true;
                }

                if (!"confirm".equals(control.kind()) && !"phase".equals(control.kind())) {
                    return false;
                }

                ModNetwork.sendToServer(new ModNetwork.FieldClick(control.id()));
                ClientDuelAudio.playUi("SE_DUEL_SELECT");
                return true;
            }
        }

        return false;
    }
}
