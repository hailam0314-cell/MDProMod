package net.mdpromod;

import java.util.ArrayList;
import java.util.List;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

public final class AiDeckScreen extends Screen {
    private static final int MAX_COLUMNS = 9;
    private static final int ROWS = 3;
    private static final int CHROME = 120;
    private static final String[] ZONE_NAMES = new String[]{"主卡组", "额外卡组", "副卡组"};
    private final String[] keys;
    private final String[] labels;
    private final String[] descriptions;
    private final List<AiDeckScreen.Deck> decks = new ArrayList<>();
    private int selected;
    private int zone;
    private int page;
    private int panelLeft;
    private int panelTop;
    private int panelWidth;
    private int panelHeight;
    private int columns;
    private int rows;
    private int cardWidth;
    private int cardHeight;
    private int cellWidth;
    private int cellHeight;
    private int gridTop;
    private int hoveredCode = -1;

    public AiDeckScreen(ModNetwork.AiDeckOpen message, int selected) {
        super(Component.translatable("item.mdpromod.ai_deck"));
        this.keys = message.keys();
        this.labels = message.labels();
        this.descriptions = message.descriptions();
        int offset = 0;

        for (int i = 0; i < this.keys.length; i++) {
            int main = message.counts()[i * 3];
            int extra = message.counts()[i * 3 + 1];
            int side = message.counts()[i * 3 + 2];
            this.decks
                .add(
                    new AiDeckScreen.Deck(
                        slice(message.cards(), offset, main), slice(message.cards(), offset + main, extra), slice(message.cards(), offset + main + extra, side)
                    )
                );
            offset += main + extra + side;
        }

        this.selected = Math.max(0, Math.min(selected, Math.max(0, this.decks.size() - 1)));
    }

    public AiDeckScreen(ModNetwork.AiDeckOpen message) {
        this(message, indexOf(message));
    }

    private static int indexOf(ModNetwork.AiDeckOpen message) {
        String[] keys = message.keys();

        for (int i = 0; i < keys.length; i++) {
            if (keys[i].equalsIgnoreCase(message.selected())) {
                return i;
            }
        }

        return 0;
    }

    private static int[] slice(int[] source, int from, int length) {
        int[] out = new int[Math.max(0, length)];

        for (int i = 0; i < out.length && from + i < source.length; i++) {
            out[i] = source[from + i];
        }

        return out;
    }

    private AiDeckScreen.Deck deck() {
        return this.decks.isEmpty() ? new AiDeckScreen.Deck(new int[0], new int[0], new int[0]) : this.decks.get(this.selected);
    }

    private int[] zoneCodes() {
        return this.deck().zone(this.zone);
    }

    String label(int index) {
        if (index >= 0 && index < this.labels.length) {
            return this.labels[index] != null && !this.labels[index].isBlank() ? this.labels[index] : this.keys[index];
        } else {
            return "（没有人机卡组）";
        }
    }

    void applySelection(int index) {
        if (index >= 0 && index < this.keys.length) {
            this.selected = index;
            this.zone = 0;
            this.page = 0;
            ModNetwork.sendToServer(
                new ModNetwork.AiDeckSelect(this.keys[index], this.label(index), this.descriptions.length > index ? this.descriptions[index] : "")
            );
        }
    }

    private void step(int delta) {
        if (this.keys.length != 0) {
            this.applySelection((this.selected + delta + this.keys.length) % this.keys.length);
            this.rebuildWidgets();
        }
    }

    private int pageSize() {
        return Math.max(1, this.columns * this.rows);
    }

    private int pageCount() {
        return Math.max(1, (this.zoneCodes().length + this.pageSize() - 1) / this.pageSize());
    }

    protected void init() {
        this.rows = 3;
        int available = Math.max(60, this.height - 120 - 12);
        this.cellHeight = Math.max(22, Math.min(82, available / this.rows));
        this.cardHeight = Math.max(16, this.cellHeight - 4);
        this.cardWidth = Math.max(11, this.cardHeight * 54 / 78);
        this.cellWidth = this.cardWidth + 4;
        this.columns = Math.max(1, Math.min(9, (this.width - 40) / this.cellWidth));
        this.panelWidth = this.columns * this.cellWidth + 24;
        this.panelHeight = 120 + this.rows * this.cellHeight;
        this.panelLeft = (this.width - this.panelWidth) / 2;
        this.panelTop = Math.max(6, (this.height - this.panelHeight) / 2);
        this.gridTop = this.panelTop + 80;
        this.page = Math.max(0, Math.min(this.page, this.pageCount() - 1));
        int selectorY = this.panelTop + 20;
        this.addRenderableWidget(Button.builder(Component.literal("◀"), button -> this.step(-1)).bounds(this.panelLeft + 12, selectorY, 22, 20).build());
        this.addRenderableWidget(
            Button.builder(Component.literal(this.label(this.selected) + "  ▾"), button -> this.openPicker())
                .bounds(this.panelLeft + 38, selectorY, this.panelWidth - 76, 20)
                .build()
        );
        this.addRenderableWidget(
            Button.builder(Component.literal("▶"), button -> this.step(1)).bounds(this.panelLeft + this.panelWidth - 34, selectorY, 22, 20).build()
        );
        int tabWidth = Math.min(96, (this.panelWidth - 24) / 3);

        for (int i = 0; i < 3; i++) {
            int index = i;
            String label = (i == this.zone ? "▶ " : "") + ZONE_NAMES[i] + " " + this.deck().zone(i).length;
            this.addRenderableWidget(Button.builder(Component.literal(label), button -> {
                this.zone = index;
                this.page = 0;
                this.rebuildWidgets();
            }).bounds(this.panelLeft + 12 + i * (tabWidth + 2), this.panelTop + 58, tabWidth, 18).build());
        }

        this.addRenderableWidget(
            Button.builder(Component.translatable("gui.done"), button -> this.onClose())
                .bounds(this.panelLeft + this.panelWidth / 2 - 40, this.gridTop + this.rows * this.cellHeight + 18, 80, 20)
                .build()
        );
    }

    private void openPicker() {
        if (this.minecraft != null) {
            this.minecraft.setScreen(new AiDeckPickerScreen(this, this.keys, this.labels, this.descriptions, this.selected));
        }
    }

    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        int next = this.page + (delta < 0.0 ? 1 : -1);
        this.page = Math.max(0, Math.min(this.pageCount() - 1, next));
        return true;
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        McPanel.panel(graphics, this.panelLeft, this.panelTop, this.panelLeft + this.panelWidth, this.panelTop + this.panelHeight);
        graphics.drawCenteredString(this.font, this.title, this.width / 2, this.panelTop + 6, 4210752);
        String description = this.descriptions.length > this.selected ? this.descriptions[this.selected] : "";
        if (!description.isBlank()) {
            graphics.drawCenteredString(this.font, description, this.width / 2, this.panelTop + 44, 5592405);
        } else {
            graphics.drawCenteredString(this.font, "第 " + (this.page + 1) + "/" + this.pageCount() + " 页（滚轮翻页）", this.width / 2, this.panelTop + 44, 5592405);
        }

        int from = this.page * this.pageSize();
        int[] codes = this.zoneCodes();
        this.hoveredCode = -1;

        for (int i = 0; i < this.pageSize(); i++) {
            int index = from + i;
            if (index >= codes.length) {
                break;
            }

            int x = this.panelLeft + 14 + i % this.columns * this.cellWidth;
            int y = this.gridTop + i / this.columns * this.cellHeight;
            int code = codes[index];
            boolean hover = McPanel.inside(mouseX, mouseY, x, y, this.cardWidth, this.cardHeight);
            McPanel.slot(graphics, x - 2, y - 2, this.cardWidth + 4, this.cardHeight + 4);
            McPanel.card(graphics, code, x, y, this.cardWidth, this.cardHeight);
            if (hover) {
                graphics.fill(x - 2, y - 2, x + this.cardWidth + 2, y - 1, 16777045);
                graphics.fill(x - 2, y + this.cardHeight + 1, x + this.cardWidth + 2, y + this.cardHeight + 2, 16777045);
                graphics.fill(x - 2, y - 2, x - 1, y + this.cardHeight + 2, 16777045);
                graphics.fill(x + this.cardWidth + 1, y - 2, x + this.cardWidth + 2, y + this.cardHeight + 2, 16777045);
            }

            if (hover) {
                this.hoveredCode = code;
            }
        }

        if (codes.length == 0) {
            graphics.drawCenteredString(this.font, "（这个区没有卡）", this.panelLeft + this.panelWidth / 2, this.gridTop + 20, 6316128);
        }

        super.render(graphics, mouseX, mouseY, partialTick);
        String footer = this.hoveredCode > 0
            ? McPanel.displayName(this.hoveredCode)
            : "第 " + (this.page + 1) + "/" + this.pageCount() + " 页 · 共 " + codes.length + " 张";
        graphics.drawCenteredString(this.font, footer, this.width / 2, this.gridTop + this.rows * this.cellHeight + 4, 3158064);
    }

    public boolean isPauseScreen() {
        return false;
    }

    record Deck(int[] main, int[] extra, int[] side) {
        int[] zone(int index) {
            return switch (index) {
                case 1 -> this.extra;
                case 2 -> this.side;
                default -> this.main;
            };
        }
    }
}
