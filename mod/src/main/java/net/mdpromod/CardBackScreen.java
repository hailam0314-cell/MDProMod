package net.mdpromod;

import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.EditBox;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

final class CardBackScreen extends Screen {
    private final Screen parent;
    private final java.util.function.IntConsumer onSelected;
    private final int selected;
    private final List<Button> tiles = new ArrayList<>();
    private List<CardBacks.Entry> matches = CardBacks.ALL;
    private EditBox search;
    private Button previous, next;
    private String query = "";
    private int page, perPage, thumbW, thumbH;
    private boolean firstOpen = true;

    CardBackScreen(CardCaseScreen parent, int selected) {
        this(parent, selected, id -> ModNetwork.sendToServer(new ModNetwork.CardCaseAction(5, 0, id, "")));
    }

    CardBackScreen(Screen parent, int selected, java.util.function.IntConsumer onSelected) {
        super(Component.literal("选择卡背"));
        this.parent = parent;
        this.selected = selected;
        this.onSelected = onSelected;
    }

    private boolean chinese() {
        return minecraft.getLanguageManager().getSelected().startsWith("zh");
    }

    @Override
    protected void init() {
        tiles.clear();
        int columns = Math.max(1, Math.min(10, (width - 16) / 49));
        int rows = Math.max(1, Math.min(5, (height - 58) / 68));
        perPage = rows * columns;
        thumbH = Math.max(16, Math.min(88, (height - 58) / rows - 6));
        thumbW = thumbH * 11 / 16;
        int cellW = Math.min(thumbW + 8, (width - 12) / columns);
        thumbW = Math.min(thumbW, cellW - 5);
        thumbH = thumbW * 16 / 11;
        int gridX = (width - columns * cellW) / 2;
        int searchW = Math.min(180, Math.max(72, width - 154));
        search = addRenderableWidget(new EditBox(font, width - searchW - 9, 7, searchW, 15, Component.literal("搜索")));
        search.setMaxLength(64);
        search.setHint(Component.literal("名称或编号"));
        search.setValue(query);
        for (int i = 0; i < perPage; i++) {
            final int tile = i;
            tiles.add(addRenderableWidget(Button.builder(Component.empty(), b -> choose(tile))
                .createNarration(defaultMessage -> Component.literal(matches.get(page * perPage + tile).name(chinese())))
                .bounds(gridX + i % columns * cellW, 31 + i / columns * (thumbH + 6), cellW - 2, thumbH + 4).build()));
        }
        previous = addRenderableWidget(Button.builder(Component.literal("上一页"), b -> turnPage(-1))
            .bounds(width / 2 - 80, height - 22, 45, 17).build());
        next = addRenderableWidget(Button.builder(Component.literal("下一页"), b -> turnPage(1))
            .bounds(width / 2 + 35, height - 22, 45, 17).build());
        addRenderableWidget(Button.builder(Component.literal("返回"), b -> onClose())
            .bounds(6, height - 22, 35, 17).build());
        if (firstOpen) {
            for (int i = 0; i < matches.size(); i++) {
                if (matches.get(i).id() == selected) page = i / perPage;
            }
            firstOpen = false;
        }
        search.setResponder(text -> {
            query = text;
            String term = text.trim().toLowerCase(Locale.ROOT);
            matches = CardBacks.ALL.stream().filter(e -> Integer.toString(e.id()).contains(term)
                || e.chinese().toLowerCase(Locale.ROOT).contains(term) || e.english().toLowerCase(Locale.ROOT).contains(term)).toList();
            page = 0;
            refresh();
        });
        refresh();
    }

    private int lastPage() {
        return Math.max(0, (matches.size() - 1) / perPage);
    }

    private void turnPage(int delta) {
        page = Math.max(0, Math.min(lastPage(), page + delta));
        refresh();
    }

    private void refresh() {
        page = Math.min(lastPage(), page);
        previous.active = page > 0;
        next.active = page < lastPage();
        for (int i = 0; i < tiles.size(); i++) {
            int index = page * perPage + i;
            Button tile = tiles.get(i);
            tile.visible = index < matches.size();
        }
    }

    private void choose(int tile) {
        int id = matches.get(page * perPage + tile).id();
        onSelected.accept(id);
        onClose();
    }

    @Override
    public void render(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
        renderBackground(g);
        g.drawString(font, title, 8, 10, 0xffffff);
        super.render(g, mouseX, mouseY, partialTick);
        CardBacks.Entry hovered = null;
        for (int i = 0; i < tiles.size(); i++) {
            Button tile = tiles.get(i);
            if (!tile.visible) continue;
            CardBacks.Entry entry = matches.get(page * perPage + i);
            int x = tile.getX() + (tile.getWidth() - thumbW) / 2;
            int y = tile.getY() + 2;
            g.fill(tile.getX()+1, tile.getY()+1, tile.getX()+tile.getWidth()-1, tile.getY()+tile.getHeight()-1, 0xff30343a);
            g.blit(entry.texture(), x, y, thumbW, thumbH, 0, 0, 176, 256, 176, 256);
            if (entry.id() == selected || tile.isHoveredOrFocused()) {
                g.renderOutline(x-1, y-1, thumbW+2, thumbH+2, entry.id() == selected ? 0xff68dcbb : 0xffffffff);
            }
            if (tile.isHovered()) hovered = entry;
        }
        if (matches.isEmpty()) g.drawCenteredString(font, "未找到卡背", width/2, height/2, 0xffffff);
        g.drawCenteredString(font, (page+1) + " / " + (lastPage()+1), width/2, height-18, 0xffffff);
        if (hovered != null) g.renderTooltip(font, Component.literal(hovered.name(chinese()) + (hovered.id()==0 ? "" : " · " + hovered.id())), mouseX, mouseY);
    }

    @Override
    public boolean mouseScrolled(double x, double y, double delta) {
        turnPage(delta > 0 ? -1 : 1);
        return true;
    }

    @Override
    public void onClose() {
        minecraft.setScreen(parent);
    }

    @Override
    public boolean isPauseScreen() {
        return false;
    }
}
