package net.mdpromod;

import net.minecraft.client.Minecraft;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

/** 客户端手牌快照与选中状态。 */
@OnlyIn(Dist.CLIENT)
public final class ClientHandBridge {
    private static int[] cards = new int[0];
    private static boolean active;
    private static int turnPlayer;
    private static int selected;

    private ClientHandBridge() {
    }

    public static void apply(ModNetwork.HandSync message) {
        cards = message.cards() == null ? new int[0] : message.cards();
        active = message.active();
        turnPlayer = message.turnPlayer();
        if (selected >= cards.length) selected = Math.max(0, cards.length - 1);
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
        return turnPlayer == 0;
    }

    public static int selected() {
        return selected;
    }

    public static void setSelected(int index) {
        selected = Math.max(0, Math.min(index, Math.max(0, cards.length - 1)));
    }

    /** 右键手牌实体：弹出大图详情。 */
    public static void openCard(int code) {
        if (code > 0) {
            ClientDuelAudio.playUi("SE_CARDEXPAND_DISPLAY");
            Minecraft.getInstance().setScreen(new HandCardScreen(code));
        }
    }
}
