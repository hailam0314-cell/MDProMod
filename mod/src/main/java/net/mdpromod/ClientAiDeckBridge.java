package net.mdpromod;

import net.minecraft.client.Minecraft;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public final class ClientAiDeckBridge {
    private ClientAiDeckBridge() {
    }

    public static void open(ModNetwork.AiDeckOpen message) {
        Minecraft.getInstance().setScreen(new AiDeckScreen(message));
    }
}
