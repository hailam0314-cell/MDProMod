package net.mdpromod;

import net.minecraft.client.Minecraft;
import net.minecraft.network.chat.Component;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.event.TickEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;

/**
 * 首次启动解包资源时给玩家一句人话：action bar 报进度，成功/失败在聊天栏各报一次。
 *
 * <p>解包在后台线程跑（{@link ModBundle}），所以这里只负责"看得见"。
 */
@Mod.EventBusSubscriber(modid = MDProMod.MODID, value = Dist.CLIENT)
public final class ClientBundleNotice {
    private static boolean doneReported;
    private static boolean failureReported;

    private ClientBundleNotice() {
    }

    @SubscribeEvent
    public static void onClientTick(TickEvent.ClientTickEvent event) {
        if (event.phase != TickEvent.Phase.END) return;
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.player == null) return;
        ModBundle.State state = ModBundle.state();
        if (state == ModBundle.State.RUNNING) {
            minecraft.player.displayClientMessage(
                    Component.literal("MDProMod 正在解包资源：" + ModBundle.progressText()), true);
        } else if (state == ModBundle.State.DONE && !doneReported) {
            doneReported = true;
            if (ModBundle.bundled()) {
                minecraft.player.displayClientMessage(
                        Component.literal("MDProMod 资源准备完成（" + ModBundle.detail() + "）"), false);
            }
        } else if (state == ModBundle.State.FAILED && !failureReported) {
            failureReported = true;
            minecraft.player.displayClientMessage(
                    Component.literal("MDProMod 资源解包失败：" + ModBundle.detail()), false);
        }
    }
}
