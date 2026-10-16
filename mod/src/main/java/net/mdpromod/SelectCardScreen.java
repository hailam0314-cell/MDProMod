package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

import java.util.ArrayList;
import java.util.List;

/**
 * 选卡清单（MC 弹窗风格）：候选卡以卡图网格排列，左键点选，金色边框表示已选。
 * 卡组/额外卡组里的卡没有世界实体，靠这个界面选。
 */
public class SelectCardScreen extends Screen {
    private static final int COLS = 6;
    private static final int GAP = 4;

    private final int[] codes;
    /** 每张候选卡在哪（和 codes 一一对应；可能为空数组）。 */
    private final int[] addrs;
    private final int min;
    private final int max;
    private final List<Integer> chosen = new ArrayList<>();
    private int page;
    private int rows = 3;
    private int panelLeft;
    private int panelTop;
    private int panelW;
    private int panelH;
    private int gridLeft;
    private int gridTop;

    public SelectCardScreen(int[] codes, int min, int max) {
        this(codes, min, max, new int[0]);
    }

    public SelectCardScreen(int[] codes, int min, int max, int[] addrs) {
        super(Component.literal("选择卡片"));
        this.codes = codes == null ? new int[0] : codes;
        this.addrs = addrs == null ? new int[0] : addrs;
        this.min = Math.max(1, min);
        this.max = Math.max(this.min, max);
    }

    private int pages() {
        int perPage = COLS * Math.max(1, rows);
        return Math.max(1, (codes.length + perPage - 1) / perPage);
    }

    @Override
    protected void init() {
        // 每页行数按可用高度算，卡再多也不会顶出面板
        rows = Math.max(1, (this.height - 120) / (McPanel.CARD_H + GAP));
        panelW = Math.min(Math.max(this.width - 40, 300), COLS * (McPanel.CARD_W + GAP) + 10);
        panelH = Math.min(Math.max(this.height - 40, 200),
                rows * (McPanel.CARD_H + GAP) + 76);
        panelLeft = (this.width - panelW) / 2;
        panelTop = (this.height - panelH) / 2;
        gridLeft = panelLeft + (panelW - COLS * (McPanel.CARD_W + GAP)) / 2 + GAP / 2;
        gridTop = panelTop + 26;
        int bottom = panelTop + panelH - 24;
        addRenderableWidget(Button.builder(Component.literal("上一页"), button -> {
            page = Math.max(0, page - 1);
        }).bounds(panelLeft + 8, bottom, 60, 18).build());
        addRenderableWidget(Button.builder(Component.literal("下一页"), button -> {
            page = Math.min(pages() - 1, page + 1);
        }).bounds(panelLeft + 72, bottom, 60, 18).build());
        addRenderableWidget(Button.builder(Component.literal("确认"), button -> confirm())
                .bounds(panelLeft + panelW - 78, bottom, 70, 18).build());
    }

    /** 滚轮翻页（卡图多时主要靠它）。 */
    @Override
    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        page = Math.max(0, Math.min(pages() - 1, page - (int) Math.signum(delta)));
        return true;
    }

    private void toggle(int index) {
        ClientDuelAudio.playUi("SE_DUEL_SELECT");
        if (!chosen.remove(Integer.valueOf(index))) {
            if (chosen.size() >= max) return;
            chosen.add(index);
        }
        if (chosen.size() >= max) {
            confirm();
        }
    }

    private void confirm() {
        if (chosen.size() < min) return;
        ClientDuelAudio.playUi("SE_DUEL_DECIDE");
        int[] indices = chosen.stream().mapToInt(Integer::intValue).toArray();
        ModNetwork.sendToServer(new ModNetwork.SelectChoose(indices));
        onClose();
    }

    @Override
    public boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (button == 0) {
            int perPage = COLS * Math.max(1, rows);
            int start = page * perPage;
            for (int i = 0; i < perPage && start + i < codes.length; i++) {
                int x = gridLeft + (i % COLS) * (McPanel.CARD_W + GAP);
                int y = gridTop + (i / COLS) * (McPanel.CARD_H + GAP);
                if (McPanel.inside(mouseX, mouseY, x, y, McPanel.CARD_W, McPanel.CARD_H)) {
                    toggle(start + i);
                    return true;
                }
            }
        }
        return super.mouseClicked(mouseX, mouseY, button);
    }

    @Override
    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        renderBackground(graphics);
        McPanel.panel(graphics, panelLeft, panelTop, panelLeft + panelW, panelTop + panelH);
        graphics.drawCenteredString(font, "选择卡片　已选 " + chosen.size() + " / " + min + ".." + max
                + "　（第 " + (page + 1) + "/" + pages() + " 页）",
                panelLeft + panelW / 2, panelTop + 8, 0x404040);
        int perPage = COLS * Math.max(1, rows);
        int start = page * perPage;
        for (int i = 0; i < perPage && start + i < codes.length; i++) {
            int x = gridLeft + (i % COLS) * (McPanel.CARD_W + GAP);
            int y = gridTop + (i / COLS) * (McPanel.CARD_H + GAP);
            boolean picked = chosen.contains(start + i);
            McPanel.cardInSlot(graphics, codes[start + i], x, y, picked);
            // 卡面标记：连锁中/被指定（和世界里的边框同一套颜色）
            if (start + i < addrs.length) {
                ClientCardMarkers.drawGuiMark(graphics, x, y, McPanel.CARD_W, McPanel.CARD_H,
                        ClientCardMarkers.markAt(addrs[start + i]));
            }
            if (picked) {
                graphics.drawString(font, "✔", x + McPanel.CARD_W - 9, y + 2, 0xFFFF00);
            }
        }
        // 悬停时显示卡名 + 它在哪，方便辨认（尤其是同名卡/墓地里的卡）
        for (int i = 0; i < perPage && start + i < codes.length; i++) {
            int x = gridLeft + (i % COLS) * (McPanel.CARD_W + GAP);
            int y = gridTop + (i / COLS) * (McPanel.CARD_H + GAP);
            if (McPanel.inside(mouseX, mouseY, x, y, McPanel.CARD_W, McPanel.CARD_H)) {
                int index = start + i;
                int code = codes[index];
                String name = McPanel.displayName(code);
                if (index < addrs.length) {
                    List<net.minecraft.util.FormattedCharSequence> lines = new ArrayList<>(List.of(
                            Component.literal(name).getVisualOrderText(),
                            Component.literal(DuelLocation.tooltipLine(addrs[index]))
                                    .getVisualOrderText()));
                    String mark = DuelMarks.label(ClientCardMarkers.markAt(addrs[index]));
                    if (!mark.isEmpty()) {
                        lines.add(Component.literal("状态：" + mark).getVisualOrderText());
                    }
                    graphics.renderTooltip(font, lines, mouseX, mouseY);
                } else {
                    graphics.renderTooltip(font, Component.literal(name), mouseX, mouseY);
                }
            }
        }
        super.render(graphics, mouseX, mouseY, partialTick);
    }

    @Override
    public boolean isPauseScreen() {
        return false;
    }

    @Override
    public boolean shouldCloseOnEsc() {
        return false;
    }
}
