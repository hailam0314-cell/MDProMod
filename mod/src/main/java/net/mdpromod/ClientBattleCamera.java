package net.mdpromod;

import net.minecraft.client.CameraType;
import net.minecraft.client.Minecraft;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.EntityType;
import net.minecraft.world.entity.decoration.ArmorStand;
import net.minecraft.world.phys.Vec3;

/** A local camera for an attack watched from the duel panel. The player stays at their seat. */
final class ClientBattleCamera {
    private record Shot(DuelControlScreen panel, int field, Vec3 target, double start, double end) {}
    private static Shot shot;
    private static ArmorStand camera;
    private static Entity previous;
    private static CameraType perspective;

    private ClientBattleCamera() {}

    static void onAnimation(ModNetwork.DuelAnimation event, double start, double end) {
        Minecraft mc = Minecraft.getInstance();
        if (!(mc.screen instanceof DuelControlScreen panel) || !panel.isCurrent()) return;
        var state = ClientHandBridge.snapshot();
        if (state.field() != event.fieldId() || !state.yourTurn()) return;
        if ("attack".equals(event.kind())) {
            shot = new Shot(panel, state.field(), event.from().lerp(event.to(), 0.5), start, end + 420);
        } else if ("destroy".equals(event.kind()) && shot != null && shot.panel == panel && start <= shot.end + 200) {
            shot = new Shot(panel, state.field(), shot.target, shot.start, Math.max(shot.end, end));
        }
    }

    static boolean showing(DuelControlScreen panel) {
        return camera != null && shot != null && shot.panel == panel;
    }

    static void tick(Minecraft mc) {
        if (shot == null) return;
        double now = ClientAnimBridge.now();
        if (mc.screen != shot.panel || !shot.panel.isCurrent() || now >= shot.end
            || camera != null && mc.getCameraEntity() != camera) {
            restore(mc);
            return;
        }
        if (now < shot.start || camera != null) return;
        DuelFieldEntity field = ClientHandBridge.field();
        Vec3 target = shot.target;
        double side = ClientHandBridge.snapshot().seat() == 0 ? 1 : -1;
        double distance = Math.max(1, 70.0 / mc.options.fov().get());
        Vec3 offset = new Vec3(0, 11 * distance, 7 * side * distance).yRot((float)-Math.toRadians(field.getYRot()));
        Vec3 eye = target.add(offset);
        camera = new ArmorStand(EntityType.ARMOR_STAND, mc.level);
        camera.moveTo(eye.x, eye.y - camera.getEyeHeight(), eye.z,
            (float)Math.toDegrees(Math.atan2(offset.x, -offset.z)),
            (float)Math.toDegrees(Math.atan2(offset.y, offset.horizontalDistance())));
        camera.setOldPosAndRot();
        previous = mc.getCameraEntity();
        perspective = mc.options.getCameraType();
        mc.options.setCameraType(CameraType.FIRST_PERSON);
        mc.setCameraEntity(camera);
    }

    static void restore(Minecraft mc) {
        shot = null;
        if (camera == null) return;
        if (mc.getCameraEntity() == camera) {
            mc.setCameraEntity(previous != null && previous.level() == mc.level && !previous.isRemoved() ? previous : mc.player);
        }
        if (mc.options.getCameraType() == CameraType.FIRST_PERSON) mc.options.setCameraType(perspective);
        camera = null;
        previous = null;
    }
}
