package net.mdpromod;

import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.screens.ChatScreen;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public final class ClientPromptWatch {
    private static final long MIN_INTERVAL_MS = 1500L;
    private static long lastAskAt;

    private ClientPromptWatch() {
    }

    public static void tick(Minecraft minecraft) {
        if (minecraft != null && minecraft.player != null && minecraft.level != null) {
            if (!(minecraft.screen instanceof RequiredAnswer)) {
                if (!(minecraft.screen instanceof ChatScreen)) {
                    DuelFieldEntity field = waitingForMe(minecraft);
                    if (field != null) {
                        long now = System.currentTimeMillis();
                        if (now - lastAskAt >= 1500L) {
                            lastAskAt = now;
                            ModNetwork.sendToServer(new ModNetwork.PromptResend());
                        }
                    }
                }
            }
        }
    }

    private static DuelFieldEntity waitingForMe(Minecraft minecraft) {
        for (DuelFieldEntity field : minecraft.level.getEntitiesOfClass(DuelFieldEntity.class, minecraft.player.getBoundingBox().inflate(48.0))) {
            if (field.isGameStarted() && !field.hasResult()) {
                int asked = field.promptSide();
                if (asked >= 0 && field.isSeated(minecraft.player)) {
                    int mySeat = "p2".equals(field.sideOfPlayer(minecraft.player)) ? 1 : 0;
                    if (asked == mySeat) {
                        return field;
                    }
                }
            }
        }

        return null;
    }
}
