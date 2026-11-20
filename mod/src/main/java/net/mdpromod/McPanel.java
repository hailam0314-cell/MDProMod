package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;

public final class McPanel {
    public static final int PANEL = -3750202;
    public static final int LIGHT = -1;
    public static final int DARK = -11184811;
    public static final int SLOT = -7631989;
    public static final int SLOT_DARK = -13158601;
    public static final int GOLD = 16777045;
    public static final int CARD_W = 54;
    public static final int CARD_H = 78;

    private McPanel() {
    }

    public static void panel(GuiGraphics graphics, int x1, int y1, int x2, int y2) {
        graphics.fill(x1 - 1, y1 - 1, x2 + 1, y2 + 1, -16777216);
        graphics.fill(x1, y1, x2, y2, -3750202);
        graphics.fill(x1, y1, x2, y1 + 1, -1);
        graphics.fill(x1, y1, x1 + 1, y2, -1);
        graphics.fill(x1, y2 - 1, x2, y2, -11184811);
        graphics.fill(x2 - 1, y1, x2, y2, -11184811);
    }

    public static void slot(GuiGraphics graphics, int x, int y, int w, int h) {
        graphics.fill(x, y, x + w, y + h, -7631989);
        graphics.fill(x, y, x + w, y + 1, -13158601);
        graphics.fill(x, y, x + 1, y + h, -13158601);
        graphics.fill(x, y + h - 1, x + w, y + h, -1);
        graphics.fill(x + w - 1, y, x + w, y + h, -1);
    }

    public static void card(GuiGraphics graphics, int code, int x, int y, int w, int h) {
        graphics.blit(CardTextureCache.get(code), x, y, w, h, 0.0F, 0.0F, 176, 256, 176, 256);
    }

    public static String displayName(int code) {
        return code <= 0 ? "里侧卡片" : CardRegistry.get(code).map(CardInfo::name).orElse("#" + code);
    }

    public static void cardInSlot(GuiGraphics graphics, int code, int x, int y, boolean chosen) {
        slot(graphics, x - 2, y - 2, 58, 82);
        card(graphics, code, x, y, 54, 78);
        if (chosen) {
            graphics.fill(x - 2, y - 2, x + 54 + 2, y - 1, 16777045);
            graphics.fill(x - 2, y + 78 + 1, x + 54 + 2, y + 78 + 2, 16777045);
            graphics.fill(x - 2, y - 2, x - 1, y + 78 + 2, 16777045);
            graphics.fill(x + 54 + 1, y - 2, x + 54 + 2, y + 78 + 2, 16777045);
        }
    }

    public static boolean inside(double mouseX, double mouseY, int x, int y, int w, int h) {
        return mouseX >= x && mouseX < x + w && mouseY >= y && mouseY < y + h;
    }
}
