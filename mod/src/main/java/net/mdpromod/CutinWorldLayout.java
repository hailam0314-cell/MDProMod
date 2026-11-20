package net.mdpromod;

final class CutinWorldLayout {
    private CutinWorldLayout() {
    }

    static CutinWorldLayout.View view(
        float x,
        float y,
        float width,
        float height,
        double fieldWidth,
        double fieldDepth,
        float fieldYawDegrees,
        double cameraX,
        double cameraY,
        double cameraZ
    ) {
        float boxWidth = width > 1.0F ? width : 1000.0F;
        float boxHeight = height > 1.0F ? height : 1000.0F;
        double fieldYaw = Math.toRadians(fieldYawDegrees);
        double horizontal = Math.hypot(cameraX, cameraZ);
        double yaw = horizontal > 1.0E-4 ? Math.atan2(cameraX, cameraZ) : -fieldYaw;
        double relativeYaw = yaw + fieldYaw;
        double span = Math.abs(Math.cos(relativeYaw)) * fieldWidth + Math.abs(Math.sin(relativeYaw)) * fieldDepth;
        float scale = (float)(span / boxWidth);
        double lift = boxHeight * scale / 2.0 + 0.2;
        double pitch = Math.atan2(cameraY - lift, horizontal);
        return new CutinWorldLayout.View(scale, x + boxWidth / 2.0F, y + boxHeight / 2.0F, lift, (float)Math.toDegrees(yaw), (float)Math.toDegrees(pitch));
    }

    record View(float scale, float centerX, float centerY, double lift, float yawDegrees, float pitchDegrees) {
    }
}
