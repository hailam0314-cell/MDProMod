package net.mdpromod;

import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.screens.Screen;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public final class ClientHandBridge {
    private static ModNetwork.HandSync snapshot;
    private static int[] cards = new int[0];
    private static boolean active;
    private static boolean yourTurn;
    private static int selected;

    private ClientHandBridge() {
    }

    public static void apply(ModNetwork.HandSync message) {
        if (snapshot != null && snapshot.active() && !message.active() && snapshot.duel() != null
            && !snapshot.duel().equals(message.duel())) return;
        if (snapshot == null || !java.util.Objects.equals(snapshot.duel(), message.duel())) selected = 0;
        snapshot = message;
        cards = message.cards() == null ? new int[0] : message.cards();
        active = message.active();
        yourTurn = message.yourTurn();
        if (selected >= cards.length) {
            selected = Math.max(0, cards.length - 1);
        }
    }

    public static void clear() {
        snapshot = null;
        cards = new int[0];
        active = false;
        yourTurn = false;
        selected = 0;
    }

    static ModNetwork.HandSync snapshot() {
        return snapshot;
    }

    static DuelFieldEntity field() {
        Minecraft mc = Minecraft.getInstance();
        return active && snapshot != null && snapshot.duel() != null && mc.level != null
            && mc.level.getEntity(snapshot.field()) instanceof DuelFieldEntity f && !f.hasResult() ? f : null;
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
