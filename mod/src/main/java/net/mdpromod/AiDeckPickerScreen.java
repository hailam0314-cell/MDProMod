package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

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

    public AiDeckPickerScreen(AiDeckScreen parent, String[] keys, String[] labels, String[] descriptions, int current) {
        super(Component.literal("选择人机"));
        this.parent = parent;
        this.keys = keys;
        this.labels = labels;
        this.descriptions = descriptions;
        this.current = current;
        this.page = Math.max(0, current / 12);
    }

    private String label(int index) {
        if (index >= 0 && index < this.labels.length) {
            return this.labels[index] != null && !this.labels[index].isBlank() ? this.labels[index] : this.keys[index];
        } else {
            return "";
        }
    }

    private int pageSize() {
        return Math.max(1, this.columns * this.rows);
    }

    private int pageCount() {
        return Math.max(1, (this.keys.length + this.pageSize() - 1) / this.pageSize());
    }

    protected void init() {
        this.columns = this.width >= 420 ? 2 : 1;
        this.panelWidth = Math.min(this.width - 20, this.columns * 200 + 24);
        this.rows = Math.max(3, Math.min(9, (this.height - 40 - 20 - 24) / 22));
        this.panelHeight = 40 + this.rows * 22 + 4 + 14 + 20 + 6;
        this.panelLeft = (this.width - this.panelWidth) / 2;
        this.panelTop = Math.max(6, (this.height - this.panelHeight) / 2);
        this.page = Math.max(0, Math.min(this.page, this.pageCount() - 1));
        int columnWidth = (this.panelWidth - 24 - (this.columns - 1) * 4) / this.columns;
        int start = this.page * this.pageSize();

        for (int i = 0; i < this.pageSize(); i++) {
            int index = start + i;
            if (index >= this.keys.length) {
                break;
            }

            int chosen = index;
            String text = (index == this.current ? "✓ " : "") + this.label(index);
            this.addRenderableWidget(
                Button.builder(Component.literal(text), button -> this.pick(chosen))
                    .bounds(this.panelLeft + 12 + i % this.columns * (columnWidth + 4), this.panelTop + 40 + i / this.columns * 22, columnWidth, 20)
                    .build()
            );
        }

        int navY = this.panelTop + this.panelHeight - 20 - 6;
        this.addRenderableWidget(Button.builder(Component.literal("◀"), button -> this.turn(-1)).bounds(this.panelLeft + 12, navY, 26, 20).build());
        this.addRenderableWidget(Button.builder(Component.literal("▶"), button -> this.turn(1)).bounds(this.panelLeft + 42, navY, 26, 20).build());
        this.addRenderableWidget(
            Button.builder(Component.translatable("gui.back"), button -> this.back()).bounds(this.panelLeft + this.panelWidth - 86, navY, 74, 20).build()
        );
    }

    private void pick(int index) {
        this.parent.applySelection(index);
        this.back();
    }

    private void back() {
        if (this.minecraft != null) {
            this.minecraft.setScreen(this.parent);
        }
    }

    private void turn(int delta) {
        int next = Math.max(0, Math.min(this.pageCount() - 1, this.page + delta));
        if (next != this.page) {
            this.page = next;
            this.rebuildWidgets();
        }
    }

    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        this.turn(delta < 0.0 ? 1 : -1);
        return true;
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        McPanel.panel(graphics, this.panelLeft, this.panelTop, this.panelLeft + this.panelWidth, this.panelTop + this.panelHeight);
        graphics.drawCenteredString(this.font, this.title, this.width / 2, this.panelTop + 8, 4210752);
        graphics.drawCenteredString(
            this.font,
            "共 " + this.keys.length + " 个人机 · 第 " + (this.page + 1) + "/" + this.pageCount() + " 页（滚轮翻页）",
            this.width / 2,
            this.panelTop + 22,
            5592405
        );
        this.hovered = -1;
        int columnWidth = (this.panelWidth - 24 - (this.columns - 1) * 4) / this.columns;
        int start = this.page * this.pageSize();

        for (int i = 0; i < this.pageSize(); i++) {
            int index = start + i;
            if (index >= this.keys.length) {
                break;
            }

            int x = this.panelLeft + 12 + i % this.columns * (columnWidth + 4);
            int y = this.panelTop + 40 + i / this.columns * 22;
            if (McPanel.inside(mouseX, mouseY, x, y, columnWidth, 20)) {
                this.hovered = index;
            }
        }

        String footer = this.hovered >= 0 && this.hovered < this.descriptions.length && !this.descriptions[this.hovered].isBlank()
            ? this.descriptions[this.hovered]
            : "选一个人机，回到上一页可以看到他的卡组";
        graphics.drawCenteredString(this.font, footer, this.width / 2, this.panelTop + 40 + this.rows * 22 + 4, 3158064);
        super.render(graphics, mouseX, mouseY, partialTick);
    }

    public void onClose() {
        this.back();
    }

    public boolean isPauseScreen() {
        return false;
    }
}
