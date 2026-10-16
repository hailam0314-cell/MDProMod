package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

/**
 * 「要不要接连锁」：MC 风格窗口。
 *
 * 候选卡摆在格子里（点一下选中，金框），下面只有两个按钮：
 * 「连锁」提交选中的那一张，「取消」放弃这次连锁（强制连锁时"取消"不可用）。
 */
public class ChainChoiceScreen extends Screen {
    private static final int COLS = 5;
    private static final int GAP = 6;

    private final int[] codes;
    private final boolean forced;
    /** 「〈卡〉的效果发动了，是否连锁？」这类时机说明，可能为空。 */
    private final String timing;
    /** 链上还什么都没有：这次问的是"要不要发动"，按钮和角标跟着变。 */
    private final boolean opening;
    private final int[] addrs;
    private int selected = -1;

    private int panelLeft;
    private int panelTop;
    private int panelW;
    private int panelH;
    private int gridLeft;
    private int gridTop;
    private int cols = 1;
    private Button chainButton;
    private Button cancelButton;

    public ChainChoiceScreen(int[] codes, boolean forced) {
        this(codes, forced, "", false, new int[0]);
    }

    public ChainChoiceScreen(int[] codes, boolean forced, String timing) {
        this(codes, forced, timing, false, new int[0]);
    }

    public ChainChoiceScreen(int[] codes, boolean forced, String timing, boolean opening, int[] addrs) {
        super(Component.literal(opening ? "发动效果" : "连锁"));
        this.codes = codes == null ? new int[0] : codes;
        this.forced = forced;
        this.timing = timing == null ? "" : timing;
        this.opening = opening;
        this.addrs = addrs == null ? new int[0] : addrs;
        // 只有一张候选时先替玩家选中，少点一次
        if (this.codes.length == 1) selected = 0;
    }

    private int rows() {
        return Math.max(1, (codes.length + cols - 1) / cols);
    }

    @Override
    protected void init() {
        cols = Math.min(COLS, Math.max(1, codes.length));
        int gridW = cols * (McPanel.CARD_W + GAP) - GAP;
        // 时机那句话可能比卡图排还宽，面板要能装下
        int textW = timing.isEmpty() ? 0 : this.font.width(timing) + 24;
        panelW = Math.min(Math.max(gridW + 28, textW), Math.max(160, this.width - 20));
        int gridH = rows() * (McPanel.CARD_H + GAP) - GAP;
        // 多留一行给时机说明
        panelH = Math.min(gridH + 96, Math.max(120, this.height - 20));
        panelLeft = (this.width - panelW) / 2;
        panelTop = (this.height - panelH) / 2;
        gridLeft = panelLeft + Math.max(6, (panelW - gridW) / 2);
        gridTop = panelTop + 36;
        int bottom = panelTop + panelH - 26;
        chainButton = addRenderableWidget(Button.builder(
                        Component.literal(opening ? "发动" : "连锁"), button -> submit())
                .bounds(panelLeft + panelW / 2 - 84, bottom, 80, 20).build());
        cancelButton = addRenderableWidget(Button.builder(
                        Component.literal(opening ? "不发动" : "取消"), button -> giveUp())
                .bounds(panelLeft + panelW / 2 + 4, bottom, 80, 20).build());
        refreshButtons();
    }

    private void refreshButtons() {
        if (chainButton != null) chainButton.active = selected >= 0 && selected < codes.length;
        if (cancelButton != null) cancelButton.active = !forced;
    }

    private void submit() {
        if (selected < 0 || selected >= codes.length) return;
        ModNetwork.sendToServer(new ModNetwork.ChainChoose(selected));
        onClose();
    }

    private void giveUp() {
        if (forced) return;                        // 强制连锁：核心不接受放弃
        ModNetwork.sendToServer(new ModNetwork.ChainChoose(-1));
        onClose();
    }

    private int indexAt(double mouseX, double mouseY) {
        for (int i = 0; i < codes.length; i++) {
            int x = gridLeft + (i % cols) * (McPanel.CARD_W + GAP);
            int y = gridTop + (i / cols) * (McPanel.CARD_H + GAP);
            if (McPanel.inside(mouseX, mouseY, x, y, McPanel.CARD_W, McPanel.CARD_H)) return i;
        }
        return -1;
    }

    /** 方向键 / Tab 也能换选哪一张。 */
    @Override
    public boolean keyPressed(int keyCode, int scanCode, int modifiers) {
        if (codes.length > 1 && (keyCode == 263 || keyCode == 262)) {   // ← / →
            int step = keyCode == 263 ? -1 : 1;
            selected = Math.floorMod(selected < 0 ? (step > 0 ? -1 : 0) : selected + step, codes.length);
            ClientDuelAudio.playUi("SE_DUEL_SELECT");
            refreshButtons();
            return true;
        }
        return super.keyPressed(keyCode, scanCode, modifiers);
    }

    @Override
    public boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (button == 0) {
            int index = indexAt(mouseX, mouseY);
            if (index >= 0) {
                ClientDuelAudio.playUi("SE_DUEL_SELECT");
                selected = index;
                refreshButtons();
                return true;
            }
        }
        return super.mouseClicked(mouseX, mouseY, button);
    }

    @Override
    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        renderBackground(graphics);
        McPanel.panel(graphics, panelLeft, panelTop, panelLeft + panelW, panelTop + panelH);
        graphics.drawCenteredString(font, opening ? "发动效果" : "连锁",
                panelLeft + panelW / 2, panelTop + 8, 0x404040);
        if (!timing.isEmpty()) {
            graphics.drawCenteredString(font, timing, panelLeft + panelW / 2, panelTop + 21, 0x30507A);
        }
        if (forced) {
            graphics.drawString(font, opening ? "必须发动" : "必须接", panelLeft + 6, panelTop + 8, 0xA04000);
        }
        for (int i = 0; i < codes.length; i++) {
            int x = gridLeft + (i % cols) * (McPanel.CARD_W + GAP);
            int y = gridTop + (i / cols) * (McPanel.CARD_H + GAP);
            McPanel.cardInSlot(graphics, codes[i], x, y, i == selected);
            // 卡面标记：连锁中/被指定（和世界里的边框同一套颜色）
            if (i < addrs.length) {
                ClientCardMarkers.drawGuiMark(graphics, x, y, McPanel.CARD_W, McPanel.CARD_H,
                        ClientCardMarkers.markAt(addrs[i]));
            }
            // 这一张是第几环：角上一个数字，不占地方（"要不要发动"时没有环号）
            if (!opening) {
                graphics.drawString(font, Integer.toString(i + 1), x + 3, y + 3,
                        i == selected ? 0xFFFF00 : 0xFFAA00);
            }
        }
        int hovered = indexAt(mouseX, mouseY);
        if (hovered >= 0) {
            renderCardTooltip(graphics, hovered, mouseX, mouseY);
        }
        super.render(graphics, mouseX, mouseY, partialTick);
    }

    /** 悬停：卡名（+ 它在哪 + 卡面标记，能拿到就说清楚）。 */
    private void renderCardTooltip(GuiGraphics graphics, int index, int mouseX, int mouseY) {
        java.util.List<net.minecraft.util.FormattedCharSequence> lines = new java.util.ArrayList<>();
        lines.add(Component.literal(McPanel.displayName(codes[index])).getVisualOrderText());
        if (index < addrs.length) {
            lines.add(Component.literal(DuelLocation.tooltipLine(addrs[index])).getVisualOrderText());
            String mark = DuelMarks.label(ClientCardMarkers.markAt(addrs[index]));
            if (!mark.isEmpty()) lines.add(Component.literal("状态：" + mark).getVisualOrderText());
        }
        graphics.renderTooltip(font, lines, mouseX, mouseY);
    }

    @Override
    public boolean isPauseScreen() {
        return false;
    }

    /** 结束时要给核心一个答案，所以不允许 ESC 直接关掉。 */
    @Override
    public boolean shouldCloseOnEsc() {
        return false;
    }
}
