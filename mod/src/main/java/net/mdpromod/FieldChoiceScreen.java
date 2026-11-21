package net.mdpromod;

import java.util.ArrayList;
import java.util.List;
import java.util.function.Consumer;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

final class FieldChoiceScreen extends Screen {
    private static final DuelFieldTheme[] THEMES = DuelFieldTheme.values();
    private final Screen parent;
    private final String selected;
    private final Consumer<String> onSelected;
    private final List<Button> tiles = new ArrayList<>();
    private int perPage, page;
    private Button previous, next;

    FieldChoiceScreen(Screen parent, String selected, Consumer<String> onSelected) {
        super(AppearanceScreen.label("choose_field"));
        this.parent = parent;
        this.selected = selected;
        this.onSelected = onSelected;
    }

    @Override
    protected void init() {
        tiles.clear();
        int columns = Math.max(1, Math.min(5, (width - 12) / 92));
        int rows = Math.max(1, Math.min(4, (height - 56) / 65));
        int cellW = Math.min(128, (width-12)/columns);
        int cellH = Math.min(86, (height-54)/rows);
        perPage = rows * columns;
        page = DuelFieldTheme.byId(selected).ordinal() / perPage;
        int x = (width - columns*cellW)/2, y = 27;
        for (int i=0; i<perPage; i++) {
            final int slot = i;
            tiles.add(addRenderableWidget(Button.builder(Component.empty(), b -> {
                onSelected.accept(THEMES[page*perPage+slot].id);
                onClose();
            }).createNarration(message -> AppearanceScreen.fieldName(THEMES[page*perPage+slot]).copy())
                .bounds(x+i%columns*cellW, y+i/columns*cellH, cellW-3, cellH-3).build()));
        }
        previous = addRenderableWidget(Button.builder(AppearanceScreen.label("previous"), b -> turn(-1))
            .bounds(width/2-80,height-22,46,17).build());
        next = addRenderableWidget(Button.builder(AppearanceScreen.label("next"), b -> turn(1))
            .bounds(width/2+34,height-22,46,17).build());
        addRenderableWidget(Button.builder(Component.translatable("gui.back"), b -> onClose()).bounds(6,height-22,42,17).build());
        turn(0);
    }

    private int pages() { return (THEMES.length + perPage - 1) / perPage; }

    private void turn(int step) {
        page = Math.max(0, Math.min(pages()-1, page+step));
        previous.active = page>0;
        next.active = page+1<pages();
        for(int i=0;i<tiles.size();i++) tiles.get(i).visible = page*perPage+i < THEMES.length;
    }

    @Override
    public void render(GuiGraphics g, int mx, int my, float tick) {
        renderBackground(g);
        super.render(g,mx,my,tick);
        g.drawCenteredString(font, title, width/2, 9, 0xffffff);
        g.drawCenteredString(font, (page+1)+" / "+pages(), width/2,height-18,0xffffff);
        Component tooltip = null;
        for(int i=0;i<tiles.size();i++) {
            var b=tiles.get(i);
            if(!b.visible) continue;
            var theme=THEMES[page*perPage+i];
            int x=b.getX(), y=b.getY(), w=b.getWidth(), h=b.getHeight();
            g.fill(x+1,y+1,x+w-1,y+h-1,0xff24303a);
            AppearanceScreen.fieldIcon(g,theme,x+2,y+2,w-4,h-17);
            var name=AppearanceScreen.fieldName(theme);
            g.drawCenteredString(font,font.plainSubstrByWidth(name.getString(),w-6),x+w/2,y+h-12,0xffffff);
            if(theme.id.equals(selected) || b.isHoveredOrFocused())
                g.renderOutline(x,y,w,h,theme.id.equals(selected)?0xff68dcbb:0xffffffff);
            if(b.isHovered() && font.width(name)>w-6) tooltip=name;
        }
        if(tooltip!=null) g.renderTooltip(font,tooltip,mx,my);
    }

    @Override public boolean mouseScrolled(double x,double y,double delta) { turn(delta>0?-1:1);return true; }
    @Override public void onClose() { minecraft.setScreen(parent); }
    @Override public boolean isPauseScreen() { return false; }
}
