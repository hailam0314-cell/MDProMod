package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

/**
 * 融合/仪式素材那种"一张一张选"的界面（MC 风格卡图网格）：
 * 上排是可选素材，下排是已选（点一下取消）；选够了按「完成」。
 */
public class UnselectCardScreen extends Screen {
    private static final int COLS = 6;
    private static final int GAP = 4;

    private final int[] selectCodes;
    private final int[] chosenCodes;
    private final int min;
    private final int max;
    private final boolean finishable;
    /** 两组卡各自的"在哪"（和 selectCodes / chosenCodes 一一对应；可能为空）。 */
    private final int[] selectAddrs;
    private final int[] chosenAddrs;
    private int panelLeft;
    private int panelTop;
    private int panelW;
    private int panelH;

    public UnselectCardScreen(int[] selectCodes, int[] chosenCodes, int min, int max, boolean finishable) {
        this(selectCodes, chosenCodes, min, max, finishable, new int[0], new int[0]);
    }

    public UnselectCardScreen(int[] selectCodes, int[] chosenCodes, int min, int max, boolean finishable,
                              int[] selectAddrs, int[] chosenAddrs) {
        super(Component.literal("选择素材"));
        this.selectCodes = selectCodes == null ? new int[0] : selectCodes;
        this.chosenCodes = chosenCodes == null ? new int[0] : chosenCodes;
        this.min = min;
        this.max = max;
        this.finishable = finishable;
        this.selectAddrs = selectAddrs == null ? new int[0] : selectAddrs;
        this.chosenAddrs = chosenAddrs == null ? new int[0] : chosenAddrs;
    }

    private static String name(int code) {
        return McPanel.displayName(code);
    }

    private int rows(int count) {
        return Math.max(1, (count + COLS - 1) / COLS);
    }

    @Override
    protected void init() {
        int selectRows = rows(selectCodes.length);
        int chosenRows = rows(chosenCodes.length);
        panelW = Math.min(Math.max(this.width - 40, 320), COLS * (McPanel.CARD_W + GAP) + 10);
        panelH = Math.min(Math.max(this.height - 40, 220),
                36 + (selectRows + chosenRows) * (McPanel.CARD_H + GAP) + 40);
        panelLeft = (this.width - panelW) / 2;
        panelTop = (this.height - panelH) / 2;
        addRenderableWidget(Button.builder(Component.literal("完成（已选 " + chosenCodes.length + "，需要 "
                + min + ".." + max + "）"), button -> {
            ModNetwork.sendToServer(new ModNetwork.UnselectPick(-1));
            onClose();
        }).bounds(panelLeft + panelW / 2 - 110, panelTop + panelH - 24, 220, 18).build());
    }

    private int gridLeft() {
        return panelLeft + (panelW - COLS * (McPanel.CARD_W + GAP)) / 2 + GAP / 2;
    }

    private int selectTop() {
        return panelTop + 28;
    }

    private int chosenTop() {
        return selectTop() + rows(selectCodes.length) * (McPanel.CARD_H + GAP) + 16;
    }

    @Override
    public boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (button != 0) return super.mouseClicked(mouseX, mouseY, button);
        int left = gridLeft();
        for (int i = 0; i < selectCodes.length; i++) {
            int x = left + (i % COLS) * (McPanel.CARD_W + GAP);
            int y = selectTop() + (i / COLS) * (McPanel.CARD_H + GAP);
            if (McPanel.inside(mouseX, mouseY, x, y, McPanel.CARD_W, McPanel.CARD_H)) {
                ClientDuelAudio.playUi("SE_DUEL_SELECT");
                ModNetwork.sendToServer(new ModNetwork.UnselectPick(i));
                onClose();
                return true;
            }
        }
        for (int i = 0; i < chosenCodes.length; i++) {
            int x = left + (i % COLS) * (McPanel.CARD_W + GAP);
            int y = chosenTop() + (i / COLS) * (McPanel.CARD_H + GAP);
            if (McPanel.inside(mouseX, mouseY, x, y, McPanel.CARD_W, McPanel.CARD_H)) {
                // 已选的卡在合并列表里的下标 = 可选数 + 自己在这排里的位置
                ClientDuelAudio.playUi("SE_DUEL_SELECT");
                ModNetwork.sendToServer(new ModNetwork.UnselectPick(selectCodes.length + i));
                onClose();
                return true;
            }
        }
        return super.mouseClicked(mouseX, mouseY, button);
    }

    @Override
    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        renderBackground(graphics);
        McPanel.panel(graphics, panelLeft, panelTop, panelLeft + panelW, panelTop + panelH);
        graphics.drawCenteredString(font, "选择素材（已选 " + chosenCodes.length + " / " + min + ".." + max
                + "）", panelLeft + panelW / 2, panelTop + 8, 0x404040);
        int left = gridLeft();
        for (int i = 0; i < selectCodes.length; i++) {
            int x = left + (i % COLS) * (McPanel.CARD_W + GAP);
            int y = selectTop() + (i / COLS) * (McPanel.CARD_H + GAP);
            McPanel.cardInSlot(graphics, selectCodes[i], x, y, false);
            int mark = i < selectAddrs.length ? ClientCardMarkers.markAt(selectAddrs[i]) : 0;
            ClientCardMarkers.drawGuiMark(graphics, x, y, McPanel.CARD_W, McPanel.CARD_H, mark);
            if (McPanel.inside(mouseX, mouseY, x, y, McPanel.CARD_W, McPanel.CARD_H)) {
                renderCardTooltip(graphics, selectCodes[i], i, selectAddrs, "", mouseX, mouseY);
            }
        }
        int chosenY = chosenTop();
        if (chosenCodes.length == 0) {
            graphics.drawString(font, "（还没选素材）", left, chosenY + 6, 0x606060);
        }
        for (int i = 0; i < chosenCodes.length; i++) {
            int x = left + (i % COLS) * (McPanel.CARD_W + GAP);
            int y = chosenY + (i / COLS) * (McPanel.CARD_H + GAP);
            McPanel.cardInSlot(graphics, chosenCodes[i], x, y, true);
            int mark = i < chosenAddrs.length ? ClientCardMarkers.markAt(chosenAddrs[i]) : 0;
            ClientCardMarkers.drawGuiMark(graphics, x, y, McPanel.CARD_W, McPanel.CARD_H, mark);
            if (McPanel.inside(mouseX, mouseY, x, y, McPanel.CARD_W, McPanel.CARD_H)) {
                renderCardTooltip(graphics, chosenCodes[i], i, chosenAddrs, "（点一下取消）", mouseX, mouseY);
            }
        }
        super.render(graphics, mouseX, mouseY, partialTick);
    }

    /** 悬停：卡名 + 它在哪 + 卡面标记（能拿到就说清楚）。 */
    private void renderCardTooltip(GuiGraphics graphics, int code, int index, int[] addresses,
                                   String suffix, int mouseX, int mouseY) {
        java.util.List<net.minecraft.util.FormattedCharSequence> lines = new java.util.ArrayList<>();
        lines.add(Component.literal(name(code) + suffix).getVisualOrderText());
        if (index < addresses.length) {
            lines.add(Component.literal(DuelLocation.tooltipLine(addresses[index])).getVisualOrderText());
            String mark = DuelMarks.label(ClientCardMarkers.markAt(addresses[index]));
            if (!mark.isEmpty()) lines.add(Component.literal("状态：" + mark).getVisualOrderText());
        }
        graphics.renderTooltip(font, lines, mouseX, mouseY);
    }

    @Override
    public boolean isPauseScreen() {
        return false;
    }

    @Override
    public boolean shouldCloseOnEsc() {
        return finishable;
    }
}
