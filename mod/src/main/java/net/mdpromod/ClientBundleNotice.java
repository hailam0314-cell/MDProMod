package net.mdpromod;

import net.minecraft.client.Minecraft;
import net.minecraft.network.chat.Component;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.event.TickEvent.ClientTickEvent;
import net.minecraftforge.event.TickEvent.Phase;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod.EventBusSubscriber;

@EventBusSubscriber(modid = "mdpromod", value = Dist.CLIENT)
public final class ClientBundleNotice {
    private static boolean doneReported;
    private static boolean failureReported;

    private ClientBundleNotice() {
    }

    @SubscribeEvent
    public static void onClientTick(ClientTickEvent event) {
        if (event.phase == Phase.END) {
            Minecraft minecraft = Minecraft.getInstance();
            if (minecraft.player != null) {
                ModBundle.State state = ModBundle.state();
                if (state == ModBundle.State.RUNNING) {
                    minecraft.player.displayClientMessage(Component.literal("MDProMod 正在检查资源：" + ModBundle.progressText()), true);
                } else if (state == ModBundle.State.DONE && !doneReported) {
                    doneReported = true;
                    if (ModBundle.bundled()) {
                        minecraft.player.displayClientMessage(Component.literal("MDProMod 资源准备完成（" + ModBundle.detail() + "）"), false);
                    }
                } else if (state == ModBundle.State.FAILED && !failureReported) {
                    failureReported = true;
                    minecraft.player.displayClientMessage(Component.literal("MDProMod 资源解包失败：" + ModBundle.detail()), false);
                }
            }
        }
    }
}
