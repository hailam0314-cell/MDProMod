package net.mdpromod;

import net.minecraft.client.Minecraft;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.client.event.RenderLevelStageEvent;
import net.minecraftforge.client.event.InputEvent.MouseButton.Pre;
import net.minecraftforge.client.event.RenderGuiEvent.Post;
import net.minecraftforge.client.event.RenderLevelStageEvent.Stage;
import net.minecraftforge.event.TickEvent.ClientTickEvent;
import net.minecraftforge.event.TickEvent.Phase;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod.EventBusSubscriber;

@EventBusSubscriber(modid = "mdpromod", value = Dist.CLIENT)
public final class ClientForgeEvents {
    private static final boolean HAND_HUD = Boolean.parseBoolean(System.getProperty("mdpromod.handHud", "false"));

    private ClientForgeEvents() {
    }

    @SubscribeEvent
    public static void onRenderGui(Post event) {
        if (ClientHandBridge.field() != null) {
            DuelControlScreen.renderHud(event.getGuiGraphics());
        } else if (HAND_HUD) {
            HandHudOverlay.render(event.getGuiGraphics());
        }
    }

    @SubscribeEvent
    public static void onClientTick(ClientTickEvent event) {
        if (event.phase == Phase.END) {
            Minecraft mc = Minecraft.getInstance();
            ClientBattleCamera.tick(mc);
            while (ClientModEvents.DUEL_PANEL.consumeClick()) {
                if (mc.screen == null && ClientHandBridge.field() != null) {
                    mc.setScreen(new DuelControlScreen(ClientHandBridge.snapshot().duel()));
                }
            }
            ClientCardSelect.tick(Minecraft.getInstance());
            ClientPileHints.tick(Minecraft.getInstance());
            ClientCardMarkers.tick(Minecraft.getInstance());
            ClientFieldMenu.tick(Minecraft.getInstance());
            ClientSummonEffects.tick(Minecraft.getInstance());
            ClientCutinEffects.tick(Minecraft.getInstance());
            ClientDuelAudio.tick(Minecraft.getInstance());
            ClientPromptWatch.tick(Minecraft.getInstance());
        }
    }

    @SubscribeEvent
    public static void onRenderLevel(RenderLevelStageEvent event) {
        if (event.getStage() == Stage.AFTER_TRANSLUCENT_BLOCKS) {
            Minecraft minecraft = Minecraft.getInstance();
            if (minecraft.level != null) {
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
        }
    }

    @SubscribeEvent
    public static void onMouseButton(Pre event) {
        if (event.getAction() == 1) {
            Minecraft minecraft = Minecraft.getInstance();
            if (minecraft.screen == null) {
                if (minecraft.player != null) {
                    if (event.getButton() == 1 && minecraft.player.getMainHandItem().is(ModItems.APPEARANCE_SETTINGS_ITEM.get())) return;
                    if (ClientFieldMenu.handleMouse(minecraft, event.getButton())) {
                        event.setCanceled(true);
                    } else if (event.getButton() == 0) {
                        if (ClientCardMenu.isOpen() && ClientCardMenu.mouseClicked(minecraft)) {
                            event.setCanceled(true);
                        } else if (HAND_HUD && ClientHandBridge.field() == null) {
                            double[] mouse = HandHudOverlay.mousePosition(minecraft);
                            if (HandHudOverlay.mouseClicked(mouse[0], mouse[1], event.getButton())) {
                                event.setCanceled(true);
                            }
                        }
                    }
                }
            }
        }
    }
}
