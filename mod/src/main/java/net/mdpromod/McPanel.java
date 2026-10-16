package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;

/** MC 风格的面板与卡图绘制：和手牌详情/检索界面用同一套配色与描边。 */
public final class McPanel {
    public static final int PANEL = 0xFFC6C6C6;
    public static final int LIGHT = 0xFFFFFFFF;
    public static final int DARK = 0xFF555555;
    public static final int SLOT = 0xFF8B8B8B;
    public static final int SLOT_DARK = 0xFF373737;
    public static final int GOLD = 0xFFFF55;

    /** 卡片缩略图尺寸（保持 176:256 的卡图比例）。 */
    public static final int CARD_W = 54;
    public static final int CARD_H = 78;

    private McPanel() {
    }

    /** MC 风格的面板：黑底 + 亮左上/暗右下描边。 */
    public static void panel(GuiGraphics graphics, int x1, int y1, int x2, int y2) {
        graphics.fill(x1 - 1, y1 - 1, x2 + 1, y2 + 1, 0xFF000000);
        graphics.fill(x1, y1, x2, y2, PANEL);
        graphics.fill(x1, y1, x2, y1 + 1, LIGHT);
        graphics.fill(x1, y1, x1 + 1, y2, LIGHT);
        graphics.fill(x1, y2 - 1, x2, y2, DARK);
        graphics.fill(x2 - 1, y1, x2, y2, DARK);
    }

    /** 凹槽：放卡图的格子。 */
    public static void slot(GuiGraphics graphics, int x, int y, int w, int h) {
        graphics.fill(x, y, x + w, y + h, SLOT);
        graphics.fill(x, y, x + w, y + 1, SLOT_DARK);
        graphics.fill(x, y, x + 1, y + h, SLOT_DARK);
        graphics.fill(x, y + h - 1, x + w, y + h, LIGHT);
        graphics.fill(x + w - 1, y, x + w, y + h, LIGHT);
    }

    /** 卡图（code <= 0 时画卡背）。 */
    public static void card(GuiGraphics graphics, int code, int x, int y, int w, int h) {
        graphics.blit(CardTextureCache.get(code), x, y, w, h, 0.0F, 0.0F, 176, 256, 176, 256);
    }

    /**
     * 列表里显示的卡名：code &lt;= 0 表示"这张卡的身份对自己不可见"
     * （对手的手牌 / 里侧盖卡等），不能猜也不能显示 #0。
     */
    public static String displayName(int code) {
        if (code <= 0) return "里侧卡片";
        return CardRegistry.get(code).map(CardInfo::name).orElse("#" + code);
    }

    public static void cardInSlot(GuiGraphics graphics, int code, int x, int y, boolean chosen) {
        slot(graphics, x - 2, y - 2, CARD_W + 4, CARD_H + 4);
        card(graphics, code, x, y, CARD_W, CARD_H);
        if (chosen) {
            graphics.fill(x - 2, y - 2, x + CARD_W + 2, y - 1, GOLD);
            graphics.fill(x - 2, y + CARD_H + 1, x + CARD_W + 2, y + CARD_H + 2, GOLD);
            graphics.fill(x - 2, y - 2, x - 1, y + CARD_H + 2, GOLD);
            graphics.fill(x + CARD_W + 1, y - 2, x + CARD_W + 2, y + CARD_H + 2, GOLD);
        }
    }

    /** 点是否落在某个格子里。 */
    public static boolean inside(double mouseX, double mouseY, int x, int y, int w, int h) {
        return mouseX >= x && mouseX < x + w && mouseY >= y && mouseY < y + h;
    }
}
