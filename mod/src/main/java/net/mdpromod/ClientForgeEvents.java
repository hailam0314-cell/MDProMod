package net.mdpromod;

import net.minecraft.client.Minecraft;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.client.event.RenderGuiEvent;
import net.minecraftforge.client.event.RenderLevelStageEvent;
import net.minecraftforge.client.event.InputEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;

/** 客户端 Forge 事件：手牌 HUD 的绘制与鼠标交互。 */
@Mod.EventBusSubscriber(modid = MDProMod.MODID, value = Dist.CLIENT)
public final class ClientForgeEvents {
    /** 快捷栏上方那排手牌 HUD 默认关闭：手牌以世界里的实体为准。 */
    private static final boolean HAND_HUD =
            Boolean.parseBoolean(System.getProperty("mdpromod.handHud", "false"));

    private ClientForgeEvents() {
    }

    @SubscribeEvent
    public static void onRenderGui(RenderGuiEvent.Post event) {
        if (HAND_HUD) HandHudOverlay.render(event.getGuiGraphics());
    }

    /** 客户端 tick：选卡提示的粒子（只有本人能看到）。 */
    @SubscribeEvent
    public static void onClientTick(net.minecraftforge.event.TickEvent.ClientTickEvent event) {
        if (event.phase != net.minecraftforge.event.TickEvent.Phase.END) return;
        ClientCardSelect.tick(Minecraft.getInstance());
        ClientPileHints.tick(Minecraft.getInstance());
        ClientCardMarkers.tick(Minecraft.getInstance());
        ClientFieldMenu.tick(Minecraft.getInstance());
        ClientSummonEffects.tick(Minecraft.getInstance());
        ClientCutinEffects.tick(Minecraft.getInstance());
        ClientDuelAudio.tick(Minecraft.getInstance());
    }

    /** 3D 阶段：动作按钮固定在卡片前方，落点格子画绿色高亮。 */
    @SubscribeEvent
    public static void onRenderLevel(RenderLevelStageEvent event) {
        if (event.getStage() != RenderLevelStageEvent.Stage.AFTER_TRANSLUCENT_BLOCKS) return;
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.level == null) return;
        ClientCardMenu.render(minecraft, event.getPoseStack());
        ClientFieldMenu.render(minecraft, event.getPoseStack());
        ClientPlaceHints.render(minecraft, event.getPoseStack());
        ClientPileHints.render(minecraft, event.getPoseStack());
        ClientCardMarkers.render(minecraft, event.getPoseStack(), event.getPartialTick());
        ClientCardSelect.render(minecraft, event.getPoseStack());
        ClientChainState.render(minecraft, event.getPoseStack());
        ClientCloseups.render(minecraft, event.getPoseStack());
        ClientSummonEffects.render(minecraft, event.getPoseStack());
        ClientCutinEffects.render(minecraft, event.getPoseStack());
    }

    @SubscribeEvent
    public static void onMouseButton(InputEvent.MouseButton.Pre event) {
        if (event.getAction() != 1) return;
        Minecraft minecraft = Minecraft.getInstance();
        // 有界面开着（连锁窗口、选卡、卡堆……）就一律让路：这一版事件连 GUI 里的点击也会触发，
        // 抢了它就会变成"弹窗里的按钮点不动"（真发生过）
        if (minecraft.screen != null) return;
        if (minecraft.player == null) return;
        // 决斗盘的按键（阶段键 / 确认键）和它弹出来的菜单：左右键都先问它们
        if (ClientFieldMenu.handleMouse(minecraft, event.getButton())) {
            event.setCanceled(true);
            return;
        }
        if (event.getButton() != 0) return;
        if (ClientCardMenu.isOpen() && ClientCardMenu.mouseClicked(minecraft)) {
            event.setCanceled(true);
            return;
        }
        if (!HAND_HUD) return;
        double[] mouse = HandHudOverlay.mousePosition(minecraft);
        if (HandHudOverlay.mouseClicked(mouse[0], mouse[1], event.getButton())) {
            event.setCanceled(true);
        }
    }
}
