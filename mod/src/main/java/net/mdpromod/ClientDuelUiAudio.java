package net.mdpromod;

import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.components.AbstractWidget;
import net.minecraft.client.gui.components.events.GuiEventListener;
import net.minecraft.client.gui.screens.Screen;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.client.event.ScreenEvent.Opening;
import net.minecraftforge.client.event.ScreenEvent.KeyPressed.Pre;
import net.minecraftforge.client.event.sound.PlaySoundEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod.EventBusSubscriber;

@EventBusSubscriber(modid = "mdpromod", value = Dist.CLIENT)
public final class ClientDuelUiAudio {
    private ClientDuelUiAudio() {
    }

    private static boolean duelPrompt(Screen screen) {
        return screen instanceof DecisionScreen
            || screen instanceof ChainChoiceScreen
            || screen instanceof SelectCardScreen
            || screen instanceof UnselectCardScreen
            || screen instanceof SortCardScreen
            || screen instanceof OptionScreen
            || screen instanceof PositionScreen
            || screen instanceof PileScreen;
    }

    private static boolean cardChoice(Screen screen) {
        return screen instanceof ChainChoiceScreen
            || screen instanceof SelectCardScreen
            || screen instanceof UnselectCardScreen
            || screen instanceof SortCardScreen
            || screen instanceof PileScreen;
    }

    @SubscribeEvent
    public static void screenChange(Opening event) {
        Screen previous = Minecraft.getInstance().screen;
        Screen next = event.getNewScreen();
        if (next != previous) {
            if (cardChoice(previous) && (next == null || next.getClass() != previous.getClass())) {
                ClientDuelAudio.playUi("SE_MENU_SLIDE_04");
            }

            if (cardChoice(next) && (previous == null || previous.getClass() != next.getClass())) {
                ClientDuelAudio.playUi("SE_MENU_SLIDE_03");
            }

            if (next instanceof DecisionScreen || next instanceof OptionScreen || next instanceof PositionScreen) {
                ClientDuelAudio.playUi("SE_SYS_VERIFY");
            }
        }
    }

    @SubscribeEvent
    public static void buttonSound(PlaySoundEvent event) {
        if (Boolean.parseBoolean(System.getProperty("mdpromod.duelAudio", "true"))
            && duelPrompt(Minecraft.getInstance().screen)
            && event.getOriginalSound().getLocation().toString().equals("minecraft:ui.button.click")) {
            Screen screen = Minecraft.getInstance().screen;
            AbstractWidget selected = null;

            for (GuiEventListener child : screen.children()) {
                if (child instanceof AbstractWidget widget && widget.isHovered()) {
                    selected = widget;
                    break;
                }
            }

            if (selected == null && screen.getFocused() instanceof AbstractWidget widget) {
                selected = widget;
            }

            String label = selected == null ? "" : selected.getMessage().getString();
            boolean cancel = label.equals("取消") || label.equals("否") || label.equals("关闭");
            ClientDuelAudio.playUi(cancel ? "SE_DUEL_CANCEL" : "SE_DUEL_DECIDE");
            event.setSound(null);
        }
    }

    @SubscribeEvent
    public static void escape(Pre event) {
        if (event.getKeyCode() == 256 && duelPrompt(event.getScreen()) && event.getScreen().shouldCloseOnEsc()) {
            ClientDuelAudio.playUi("SE_DUEL_CANCEL");
        }
    }
}
