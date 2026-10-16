package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

/**
 * 「人机卡组」的第二层界面：从列表里挑一个人机。
 * 点一条就选好并退回上一层（那边会立刻显示这个人的卡组）。
 */
public final class AiDeckPickerScreen extends Screen {

    private static final int ROW_H = 22;
    private static final int LIST_TOP = 40;
    private static final int NAV_H = 20;
    private static final int MAX_ROWS = 9;

    private final AiDeckScreen parent;
    private final String[] keys;
    private final String[] labels;
    private final String[] descriptions;
    private final int current;

    private int panelLeft;
    private int panelTop;
    private int panelWidth;
    private int panelHeight;
    private int columns;
    private int rows;
    private int page;
    private int hovered = -1;

    public AiDeckPickerScreen(AiDeckScreen parent, String[] keys, String[] labels,
                              String[] descriptions, int current) {
        super(Component.literal("选择人机"));
        this.parent = parent;
        this.keys = keys;
        this.labels = labels;
        this.descriptions = descriptions;
        this.current = current;
        this.page = Math.max(0, current / 12);
    }

    private String label(int index) {
        if (index < 0 || index >= labels.length) return "";
        return labels[index] == null || labels[index].isBlank() ? keys[index] : labels[index];
    }

    private int pageSize() {
        return Math.max(1, columns * rows);
    }

    private int pageCount() {
        return Math.max(1, (keys.length + pageSize() - 1) / pageSize());
    }

    @Override
    protected void init() {
        columns = width >= 420 ? 2 : 1;
        panelWidth = Math.min(width - 20, columns * 200 + 24);
        rows = Math.max(3, Math.min(MAX_ROWS, (height - LIST_TOP - NAV_H - 24) / ROW_H));
        // 列表 + 底部说明 + 导航行，都排在面板里
        panelHeight = LIST_TOP + rows * ROW_H + 4 + 14 + NAV_H + 6;
        panelLeft = (width - panelWidth) / 2;
        panelTop = Math.max(6, (height - panelHeight) / 2);
        page = Math.max(0, Math.min(page, pageCount() - 1));

        int columnWidth = (panelWidth - 24 - (columns - 1) * 4) / columns;
        int start = page * pageSize();
        for (int i = 0; i < pageSize(); i++) {
            int index = start + i;
            if (index >= keys.length) break;
            final int chosen = index;
            String text = (index == current ? "✓ " : "") + label(index);
            addRenderableWidget(Button.builder(Component.literal(text), button -> pick(chosen))
                    .bounds(panelLeft + 12 + (i % columns) * (columnWidth + 4),
                            panelTop + LIST_TOP + (i / columns) * ROW_H,
                            columnWidth, ROW_H - 2).build());
        }
        int navY = panelTop + panelHeight - NAV_H - 6;
        addRenderableWidget(Button.builder(Component.literal("◀"), button -> turn(-1))
                .bounds(panelLeft + 12, navY, 26, 20).build());
        addRenderableWidget(Button.builder(Component.literal("▶"), button -> turn(1))
                .bounds(panelLeft + 42, navY, 26, 20).build());
        addRenderableWidget(Button.builder(Component.translatable("gui.back"), button -> back())
                .bounds(panelLeft + panelWidth - 86, navY, 74, 20).build());
    }

    private void pick(int index) {
        parent.applySelection(index);
        back();
    }

    private void back() {
        if (minecraft != null) minecraft.setScreen(parent);
    }

    private void turn(int delta) {
        int next = Math.max(0, Math.min(pageCount() - 1, page + delta));
        if (next == page) return;
        page = next;
        rebuildWidgets();
    }

    @Override
    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        turn(delta < 0 ? 1 : -1);
        return true;
    }

    @Override
    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        renderBackground(graphics);
        McPanel.panel(graphics, panelLeft, panelTop, panelLeft + panelWidth, panelTop + panelHeight);
        graphics.drawCenteredString(font, title, width / 2, panelTop + 8, 0x404040);
        graphics.drawCenteredString(font, "共 " + keys.length + " 个人机 · 第 "
                + (page + 1) + "/" + pageCount() + " 页（滚轮翻页）",
                width / 2, panelTop + 22, 0x555555);

        // 鼠标悬停在哪个按钮上，就在底部显示谁的描述
        hovered = -1;
        int columnWidth = (panelWidth - 24 - (columns - 1) * 4) / columns;
        int start = page * pageSize();
        for (int i = 0; i < pageSize(); i++) {
            int index = start + i;
            if (index >= keys.length) break;
            int x = panelLeft + 12 + (i % columns) * (columnWidth + 4);
            int y = panelTop + LIST_TOP + (i / columns) * ROW_H;
            if (McPanel.inside(mouseX, mouseY, x, y, columnWidth, ROW_H - 2)) hovered = index;
        }
        String footer = hovered >= 0 && hovered < descriptions.length && !descriptions[hovered].isBlank()
                ? descriptions[hovered]
                : "选一个人机，回到上一页可以看到他的卡组";
        graphics.drawCenteredString(font, footer, width / 2,
                panelTop + LIST_TOP + rows * ROW_H + 4, 0x303030);

        super.render(graphics, mouseX, mouseY, partialTick);
    }

    @Override
    public void onClose() {
        back();
    }

    @Override
    public boolean isPauseScreen() {
        return false;
    }
}
