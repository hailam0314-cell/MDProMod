package net.mdpromod;

/** Pure geometry for a field-sized, camera-facing entrance animation. */
final class CutinWorldLayout {
    record View(float scale, float centerX, float centerY, double lift,
                float yawDegrees, float pitchDegrees) {}

    private CutinWorldLayout() {}

    static View view(float x, float y, float width, float height, double fieldWidth,
                     double fieldDepth, float fieldYawDegrees,
                     double cameraX, double cameraY, double cameraZ) {
        float boxWidth = width > 1.0F ? width : 1000.0F;
        float boxHeight = height > 1.0F ? height : 1000.0F;
        double fieldYaw = Math.toRadians(fieldYawDegrees);
        double horizontal = Math.hypot(cameraX, cameraZ);
        // Directly overhead: retain the field's own orientation rather than a random yaw.
        double yaw = horizontal > 1.0E-4 ? Math.atan2(cameraX, cameraZ) : -fieldYaw;
        // Project the actual rectangular field onto the billboard's horizontal axis.
        double relativeYaw = yaw + fieldYaw;
        double span = Math.abs(Math.cos(relativeYaw)) * fieldWidth
                + Math.abs(Math.sin(relativeYaw)) * fieldDepth;
        float scale = (float) (span / boxWidth);
        double lift = boxHeight * scale / 2.0 + 0.20;
        // Also face spectators above the field. The bottom stays above the playing surface.
        double pitch = Math.atan2(cameraY - lift, horizontal);
        return new View(scale, x + boxWidth / 2.0F, y + boxHeight / 2.0F, lift,
                (float) Math.toDegrees(yaw), (float) Math.toDegrees(pitch));
    }
}
