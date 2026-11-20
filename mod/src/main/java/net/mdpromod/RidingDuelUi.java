package net.mdpromod;

import net.minecraft.client.Minecraft;

public final class RidingDuelUi {
    private RidingDuelUi() {
    }

    public static boolean active(Minecraft minecraft) {
        return minecraft != null
            && minecraft.player != null
            && minecraft.player.getVehicle() != null
            && "net.mdpromod.mdwheel.DWheelEntity".equals(minecraft.player.getVehicle().getClass().getName());
    }
}
