package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

import java.util.ArrayList;
import java.util.List;

/**
 * 「人机卡组」界面（MC 风格，和编辑卡组同一套面板/凹槽/卡图）：
 *
 * <ul>
 *   <li>顶部一排：{@code ◀ [人机名 ▾] ▶}，点中间那个按钮打开**第二层**选择界面；</li>
 *   <li>下面三个分区标签（主卡组 / 额外卡组 / 副卡组），一次看一个区的卡图网格，滚轮翻页；</li>
 *   <li>只读：不能加卡减卡，只能换人机。换完立刻写回物品。</li>
 * </ul>
 */
public final class AiDeckScreen extends Screen {

    private static final int MAX_COLUMNS = 9;
    private static final int ROWS = 3;
    /** 标题 + 选择行 + 描述 + 分区标签 + 页脚 + 按钮 占掉的固定高度。 */
    private static final int CHROME = 120;
    private static final String[] ZONE_NAMES = {"主卡组", "额外卡组", "副卡组"};

    /** 一份人机卡组（服务端发过来的三个数组）。 */
    record Deck(int[] main, int[] extra, int[] side) {
        int[] zone(int index) {
            return switch (index) {
                case 1 -> extra;
                case 2 -> side;
                default -> main;
            };
        }
    }

    private final String[] keys;
    private final String[] labels;
    private final String[] descriptions;
    private final List<Deck> decks = new ArrayList<>();
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
        for (int i = 0; i < keys.length; i++) {
            int main = message.counts()[i * 3];
            int extra = message.counts()[i * 3 + 1];
            int side = message.counts()[i * 3 + 2];
            decks.add(new Deck(slice(message.cards(), offset, main),
                    slice(message.cards(), offset + main, extra),
                    slice(message.cards(), offset + main + extra, side)));
            offset += main + extra + side;
        }
        this.selected = Math.max(0, Math.min(selected, Math.max(0, decks.size() - 1)));
    }

    /** 便捷入口：从包里的 selected 键推断下标。 */
    public AiDeckScreen(ModNetwork.AiDeckOpen message) {
        this(message, indexOf(message));
    }

    private static int indexOf(ModNetwork.AiDeckOpen message) {
        String[] keys = message.keys();
        for (int i = 0; i < keys.length; i++) {
            if (keys[i].equalsIgnoreCase(message.selected())) return i;
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

    private Deck deck() {
        return decks.isEmpty() ? new Deck(new int[0], new int[0], new int[0]) : decks.get(selected);
    }

    /** 当前人机、当前分区的卡（跟着选择走，不是固定字段）。 */
    private int[] zoneCodes() {
        return deck().zone(zone);
    }

    String label(int index) {
        if (index < 0 || index >= labels.length) return "（没有人机卡组）";
        return labels[index] == null || labels[index].isBlank() ? keys[index] : labels[index];
    }

    /** 换一个人机：写回物品，并刷新这一页。由第二层选择界面调用。 */
    void applySelection(int index) {
        if (index < 0 || index >= keys.length) return;
        selected = index;
        zone = 0;
        page = 0;
        ModNetwork.sendToServer(new ModNetwork.AiDeckSelect(keys[index], label(index),
                descriptions.length > index ? descriptions[index] : ""));
    }

    private void step(int delta) {
        if (keys.length == 0) return;
        applySelection((selected + delta + keys.length) % keys.length);
        rebuildWidgets();
    }

    private int pageSize() {
        return Math.max(1, columns * rows);
    }

    private int pageCount() {
        return Math.max(1, (zoneCodes().length + pageSize() - 1) / pageSize());
    }

    @Override
    protected void init() {
        // 固定 3 行，靠卡图大小适配窗口高度：小窗口（GUI scale 3）也不会溢出
        rows = ROWS;
        int available = Math.max(60, height - CHROME - 12);
        cellHeight = Math.max(22, Math.min(McPanel.CARD_H + 4, available / rows));
        cardHeight = Math.max(16, cellHeight - 4);
        cardWidth = Math.max(11, cardHeight * McPanel.CARD_W / McPanel.CARD_H);
        cellWidth = cardWidth + 4;
        columns = Math.max(1, Math.min(MAX_COLUMNS, (width - 40) / cellWidth));
        panelWidth = columns * cellWidth + 24;
        panelHeight = CHROME + rows * cellHeight;
        panelLeft = (width - panelWidth) / 2;
        panelTop = Math.max(6, (height - panelHeight) / 2);
        gridTop = panelTop + 80;
        page = Math.max(0, Math.min(page, pageCount() - 1));

        int selectorY = panelTop + 20;
        addRenderableWidget(Button.builder(Component.literal("◀"), button -> step(-1))
                .bounds(panelLeft + 12, selectorY, 22, 20).build());
        addRenderableWidget(Button.builder(
                        Component.literal(label(selected) + "  ▾"),
                        button -> openPicker())
                .bounds(panelLeft + 38, selectorY, panelWidth - 76, 20).build());
        addRenderableWidget(Button.builder(Component.literal("▶"), button -> step(1))
                .bounds(panelLeft + panelWidth - 34, selectorY, 22, 20).build());

        int tabWidth = Math.min(96, (panelWidth - 24) / 3);
        for (int i = 0; i < 3; i++) {
            final int index = i;
            String label = (i == zone ? "▶ " : "") + ZONE_NAMES[i]
                    + " " + deck().zone(i).length;
            addRenderableWidget(Button.builder(Component.literal(label), button -> {
                zone = index;
                page = 0;
                rebuildWidgets();
            }).bounds(panelLeft + 12 + i * (tabWidth + 2), panelTop + 58, tabWidth, 18).build());
        }

        addRenderableWidget(Button.builder(Component.translatable("gui.done"), button -> onClose())
                .bounds(panelLeft + panelWidth / 2 - 40,
                        gridTop + rows * cellHeight + 18, 80, 20).build());
    }

    private void openPicker() {
        if (minecraft != null) {
            minecraft.setScreen(new AiDeckPickerScreen(this, keys, labels, descriptions, selected));
        }
    }

    @Override
    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        int next = page + (delta < 0 ? 1 : -1);
        page = Math.max(0, Math.min(pageCount() - 1, next));
        return true;
    }

    @Override
    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        renderBackground(graphics);
        McPanel.panel(graphics, panelLeft, panelTop, panelLeft + panelWidth, panelTop + panelHeight);
        graphics.drawCenteredString(font, title, width / 2, panelTop + 6, 0x404040);
        String description = descriptions.length > selected ? descriptions[selected] : "";
        if (!description.isBlank()) {
            graphics.drawCenteredString(font, description, width / 2, panelTop + 44, 0x555555);
        } else {
            graphics.drawCenteredString(font, "第 " + (page + 1) + "/" + pageCount() + " 页（滚轮翻页）",
                    width / 2, panelTop + 44, 0x555555);
        }

        // 凹槽网格（只读展示）
        int from = page * pageSize();
        int[] codes = zoneCodes();
        hoveredCode = -1;
        for (int i = 0; i < pageSize(); i++) {
            int index = from + i;
            if (index >= codes.length) break;
            int x = panelLeft + 14 + (i % columns) * cellWidth;
            int y = gridTop + (i / columns) * cellHeight;
            int code = codes[index];
            boolean hover = McPanel.inside(mouseX, mouseY, x, y, cardWidth, cardHeight);
            McPanel.slot(graphics, x - 2, y - 2, cardWidth + 4, cardHeight + 4);
            McPanel.card(graphics, code, x, y, cardWidth, cardHeight);
            if (hover) {
                graphics.fill(x - 2, y - 2, x + cardWidth + 2, y - 1, McPanel.GOLD);
                graphics.fill(x - 2, y + cardHeight + 1, x + cardWidth + 2, y + cardHeight + 2, McPanel.GOLD);
                graphics.fill(x - 2, y - 2, x - 1, y + cardHeight + 2, McPanel.GOLD);
                graphics.fill(x + cardWidth + 1, y - 2, x + cardWidth + 2, y + cardHeight + 2, McPanel.GOLD);
            }
            if (hover) hoveredCode = code;
        }
        if (codes.length == 0) {
            graphics.drawCenteredString(font, "（这个区没有卡）",
                    panelLeft + panelWidth / 2, gridTop + 20, 0x606060);
        }

        super.render(graphics, mouseX, mouseY, partialTick);

        String footer = hoveredCode > 0
                ? McPanel.displayName(hoveredCode)
                : "第 " + (page + 1) + "/" + pageCount() + " 页 · 共 " + codes.length + " 张";
        graphics.drawCenteredString(font, footer, width / 2,
                gridTop + rows * cellHeight + 4, 0x303030);
    }

    @Override
    public boolean isPauseScreen() {
        return false;
    }
}
