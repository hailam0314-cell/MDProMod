package net.mdpromod;

import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.screens.Screen;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public final class ClientHandBridge {
    private static int[] cards = new int[0];
    private static boolean active;
    private static boolean yourTurn;
    private static int selected;

    private ClientHandBridge() {
    }

    public static void apply(ModNetwork.HandSync message) {
        cards = message.cards() == null ? new int[0] : message.cards();
        active = message.active();
        yourTurn = message.yourTurn();
        if (selected >= cards.length) {
            selected = Math.max(0, cards.length - 1);
        }
    }

    public static void clear() {
        cards = new int[0];
        active = false;
        selected = 0;
    }

    public static int[] cards() {
        return cards;
    }

    public static boolean active() {
        return active;
    }

    public static boolean yourTurn() {
        return yourTurn;
    }

    public static int selected() {
        return selected;
    }

    public static void setSelected(int index) {
        selected = Math.max(0, Math.min(index, Math.max(0, cards.length - 1)));
    }

    public static void openCard(int code) {
        openCard(code, null);
    }

    public static void openCard(int code, Screen returnScreen) {
        if (code > 0) {
            ClientDuelAudio.playUi("SE_CARDEXPAND_DISPLAY");
            Minecraft.getInstance().setScreen(new HandCardScreen(code, returnScreen));
        }
    }
}
