package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.AbstractWidget;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.EditBox;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

import java.util.ArrayList;
import java.util.List;

/**
 * 第二层：卡片池检索条件。
 * 面板高度按内容算，内容装不下时整块内容可以滚轮滚动，底部按钮固定在面板底边，不会互相压住。
 */
@OnlyIn(Dist.CLIENT)
public class CardSearchScreen extends Screen {
    private static final int COLOR_PANEL = 0xFFC6C6C6;
    private static final int COLOR_LIGHT = 0xFFFFFFFF;
    private static final int COLOR_DARK = 0xFF555555;
    private static final int COLOR_TITLE = 0x404040;
    private static final int COLOR_LABEL = 0xFF3F3F3F;
    private static final int COLOR_SLOT = 0xFF8B8B8B;
    private static final int COLOR_SLOT_DARK = 0xFF373737;

    private static final String[] ATTRIBUTE_NAMES = {"光", "暗", "地", "水", "炎", "风", "神"};
    private static final int[] ATTRIBUTE_BITS = {0x10, 0x20, 0x01, 0x02, 0x04, 0x08, 0x40};
    private static final String[] RACE_NAMES = {
            "战士", "魔法师", "天使", "恶魔", "不死", "机械", "水", "炎", "岩石",
            "鸟兽", "植物", "昆虫", "雷", "龙", "兽", "兽战士", "恐龙", "鱼",
            "海龙", "爬虫", "念动力", "幻神兽", "创造神", "幻龙", "电子界", "幻想魔"};
    private static final int[] RACE_BITS = {
            0x1, 0x2, 0x4, 0x8, 0x10, 0x20, 0x40, 0x80, 0x100,
            0x200, 0x400, 0x800, 0x1000, 0x2000, 0x4000, 0x8000, 0x10000, 0x20000,
            0x40000, 0x80000, 0x100000, 0x200000, 0x400000, 0x800000, 0x1000000, 0x2000000};
    private static final String[] SUB_NAMES = {
            "通常魔法", "永续魔法", "装备魔法", "速攻魔法", "仪式魔法", "场地魔法",
            "通常陷阱", "永续陷阱", "反击陷阱"};
    private static final int[] SUB_BITS = {
            CardFilter.SPELL_NORMAL, CardFilter.SUB_CONTINUOUS, CardFilter.SUB_EQUIP,
            CardFilter.SUB_QUICKPLAY, CardFilter.SUB_RITUAL, CardFilter.SUB_FIELD,
            CardFilter.TRAP_NORMAL, CardFilter.SUB_CONTINUOUS, CardFilter.SUB_COUNTER};
    private static final boolean[] SUB_IS_TRAP = {
            false, false, false, false, false, false, true, true, true};

    private final Screen parent;
    private final CardFilter filter;
    private final List<AbstractWidget> tracked = new ArrayList<>();
    private final List<Integer> trackedBaseY = new ArrayList<>();
    private final List<String> labelText = new ArrayList<>();
    private final List<Integer> labelX = new ArrayList<>();
    private final List<Integer> labelY = new ArrayList<>();

    private int leftPos;
    private int topPos;
    private int panelW;
    private int panelH;
    private int contentX;
    private int contentW;
    private int viewportTop;
    private int viewportBottom;
    private int contentHeight;
    private int scrollY;

    private Button kindMonster;
    private Button kindSpell;
    private Button kindTrap;
    private Button kindExtra;
    private Button kindLimited;
    private Button sortButton;
    private final List<Button> attributeButtons = new ArrayList<>();
    private final List<Button> raceButtons = new ArrayList<>();
    private final List<Button> subButtons = new ArrayList<>();
    private EditBox levelMinBox;
    private EditBox levelMaxBox;
    private EditBox atkMinBox;
    private EditBox atkMaxBox;
    private EditBox defMinBox;
    private EditBox defMaxBox;

    public CardSearchScreen(Screen parent, CardFilter filter) {
        super(Component.literal("卡片检索条件"));
        this.parent = parent;
        this.filter = filter;
    }

    @Override
    protected void init() {
        super.init();
        tracked.clear();
        trackedBaseY.clear();
        labelText.clear();
        labelX.clear();
        labelY.clear();
        attributeButtons.clear();
        raceButtons.clear();
        subButtons.clear();

        panelW = Math.min(Math.max(this.width - 40, 320), Math.min(620, Math.max(260, this.width - 8)));
        int rowW = panelW - 20;
        leftPos = (this.width - panelW) / 2;
        contentX = leftPos + 10;
        contentW = rowW;

        int raceCols = Math.max(4, Math.min(7, rowW / 62));
        int raceRows = (RACE_NAMES.length + raceCols - 1) / raceCols;
        int kindRowH = 16;
        int labelH = 10;
        int blockGap = 6;
        int numbersRowH = 20;

        int subRowsPlanned = (SUB_NAMES.length + 4) / 5;
        // 内容总高（用于决定面板高度和是否需要滚动）
        contentHeight = (labelH + kindRowH + blockGap) * 2
                + (labelH + subRowsPlanned * 18 + blockGap)
                + (labelH + raceRows * 18 + blockGap)
                + (numbersRowH * 3 + blockGap)
                + kindRowH;
        int chromeTop = 22;
        int chromeBottom = 30;
        int wanted = contentHeight + chromeTop + chromeBottom;
        int maxPanelH = Math.min(470, this.height - 8);
        panelH = Math.max(160, Math.min(wanted, maxPanelH));
        topPos = Math.max(4, (this.height - panelH) / 2);

        viewportTop = topPos + chromeTop;
        viewportBottom = topPos + panelH - chromeBottom;
        if (viewportBottom - viewportTop < 40) {
            viewportBottom = viewportTop + 40;
        }

        int y = 0;
        label("种类", y);
        y += labelH;
        int kindW = Math.min(66, (contentW - 12) / 4);
        kindMonster = tracked(Button.builder(label("怪兽", filter.monster), b -> {
            filter.monster = !filter.monster;
            refreshLabels();
        }).bounds(contentX, 0, kindW, kindRowH).build(), y);
        kindSpell = tracked(Button.builder(label("魔法", filter.spell), b -> {
            filter.spell = !filter.spell;
            refreshLabels();
        }).bounds(contentX + kindW + 4, 0, kindW, kindRowH).build(), y);
        kindTrap = tracked(Button.builder(label("陷阱", filter.trap), b -> {
            filter.trap = !filter.trap;
            refreshLabels();
        }).bounds(contentX + (kindW + 4) * 2, 0, kindW, kindRowH).build(), y);
        kindExtra = tracked(Button.builder(label("只看额外", filter.extraOnly), b -> {
            filter.extraOnly = !filter.extraOnly;
            refreshLabels();
        }).bounds(contentX + (kindW + 4) * 3, 0, kindW, kindRowH).build(), y);
        y += kindRowH + blockGap;

        label("属性（可多选）", y);
        y += labelH;
        int attrW = Math.min(50, (contentW - 24) / 7);
        for (int i = 0; i < ATTRIBUTE_NAMES.length; i++) {
            final int index = i;
            Button button = tracked(Button.builder(label(ATTRIBUTE_NAMES[i],
                            (filter.attributeMask & ATTRIBUTE_BITS[i]) != 0), b -> {
                        filter.attributeMask ^= ATTRIBUTE_BITS[index];
                        refreshLabels();
                    }).bounds(contentX + i * (attrW + 4), 0, attrW, kindRowH).build(), y);
            attributeButtons.add(button);
        }
        y += kindRowH + blockGap;

        label("魔陷种类", y);
        y += labelH;
        int subW = Math.max(40, Math.min(64, (contentW - (SUB_NAMES.length - 1) * 4) / SUB_NAMES.length));
        for (int i = 0; i < SUB_NAMES.length; i++) {
            final int index = i;
            int col = i % 5;
            int row = i / 5;
            Button button = tracked(Button.builder(label(SUB_NAMES[i], subActive(i)), b -> {
                int bit = SUB_BITS[index];
                if (SUB_IS_TRAP[index]) {
                    filter.trapSubMask ^= bit;
                } else {
                    filter.spellSubMask ^= bit;
                }
                refreshLabels();
            }).bounds(contentX + col * (subW + 4), 0, subW, kindRowH).build(), y + row * 18);
            subButtons.add(button);
        }
        int subRows = (SUB_NAMES.length + 4) / 5;
        y += subRows * 18 + blockGap;

        label("种族（可多选）", y);
        y += labelH;
        int raceButtonW = Math.max(34, (contentW - (raceCols - 1) * 4) / raceCols);
        for (int i = 0; i < RACE_NAMES.length; i++) {
            final int index = i;
            int col = i % raceCols;
            int row = i / raceCols;
            Button button = tracked(Button.builder(label(RACE_NAMES[i],
                            (filter.raceMask & RACE_BITS[i]) != 0), b -> {
                        filter.raceMask ^= RACE_BITS[index];
                        refreshLabels();
                    }).bounds(contentX + col * (raceButtonW + 4), 0, raceButtonW, 16).build(),
                    y + row * 18);
            raceButtons.add(button);
        }
        y += raceRows * 18 + blockGap;

        label("等级", y + 4);
        label("攻击", y + 24);
        label("守备", y + 44);
        levelMinBox = tracked(numberBox(contentX + 40, 0, 44), y);
        levelMaxBox = tracked(numberBox(contentX + 92, 0, 44), y);
        atkMinBox = tracked(numberBox(contentX + 40, 0, 44), y + numbersRowH);
        atkMaxBox = tracked(numberBox(contentX + 92, 0, 44), y + numbersRowH);
        defMinBox = tracked(numberBox(contentX + 40, 0, 44), y + numbersRowH * 2);
        defMaxBox = tracked(numberBox(contentX + 92, 0, 44), y + numbersRowH * 2);
        labelAt("~", contentX + 86, y + 4);
        labelAt("~", contentX + 86, y + 24);
        labelAt("~", contentX + 86, y + 44);
        labelAt("留空表示不限", contentX + 146, y + 4);
        y += numbersRowH * 3 + blockGap;

        kindLimited = tracked(Button.builder(label("仅限制卡", filter.limitedOnly), b -> {
            filter.limitedOnly = !filter.limitedOnly;
            refreshLabels();
        }).bounds(contentX, 0, 76, kindRowH).build(), y);
        sortButton = tracked(Button.builder(
                Component.literal("排序：" + CardFilter.sortName(filter.sortMode)), b -> {
                    filter.sortMode = (filter.sortMode + 1) % 5;
                    refreshLabels();
                }).bounds(contentX + 80, 0, 92, kindRowH).build(), y);
        y += kindRowH;
        contentHeight = y;

        int buttonY = topPos + panelH - 24;
        addRenderableWidget(Button.builder(Component.literal("重置条件"), b -> {
            filter.reset();
            for (EditBox box : new EditBox[]{levelMinBox, levelMaxBox, atkMinBox, atkMaxBox, defMinBox, defMaxBox}) {
                if (box != null) box.setValue("");
            }
            refreshLabels();
        }).bounds(leftPos + 10, buttonY, 80, 18).build());
        addRenderableWidget(Button.builder(Component.literal("取消"), b -> back())
                .bounds(leftPos + panelW - 188, buttonY, 70, 18).build());
        addRenderableWidget(Button.builder(Component.literal("应用并返回"), b -> {
            applyNumbers();
            back();
        }).bounds(leftPos + panelW - 114, buttonY, 104, 18).build());

        applyScroll();
        refreshLabels();
    }

    private boolean subActive(int index) {
        int bit = SUB_BITS[index];
        return SUB_IS_TRAP[index] ? (filter.trapSubMask & bit) != 0 : (filter.spellSubMask & bit) != 0;
    }

    private Button tracked(Button button, int baseY) {
        tracked.add(button);
        trackedBaseY.add(baseY);
        addRenderableWidget(button);
        return button;
    }

    private EditBox tracked(EditBox box, int baseY) {
        tracked.add(box);
        trackedBaseY.add(baseY);
        addRenderableWidget(box);
        return box;
    }

    private EditBox numberBox(int x, int y, int width) {
        EditBox box = new EditBox(this.font, x, y, width, 16, Component.literal("数值"));
        box.setMaxLength(5);
        box.setFilter(text -> text.matches("\\d{0,5}"));
        return box;
    }

    private void label(String text, int baseY) {
        labelAt(text, contentX, baseY);
    }

    private void labelAt(String text, int x, int baseY) {
        labelText.add(text);
        labelX.add(x);
        labelY.add(baseY);
    }

    private Component label(String text, boolean active) {
        return Component.literal((active ? "✔" : "") + text);
    }

    private void applyScroll() {
        int max = Math.max(0, contentHeight - (viewportBottom - viewportTop));
        scrollY = Math.max(0, Math.min(scrollY, max));
        for (int i = 0; i < tracked.size(); i++) {
            AbstractWidget widget = tracked.get(i);
            int y = viewportTop + trackedBaseY.get(i) - scrollY;
            widget.setY(y);
            widget.visible = y + widget.getHeight() > viewportTop && y < viewportBottom;
        }
    }

    private void refreshLabels() {
        kindMonster.setMessage(label("怪兽", filter.monster));
        kindSpell.setMessage(label("魔法", filter.spell));
        kindTrap.setMessage(label("陷阱", filter.trap));
        kindExtra.setMessage(label("只看额外", filter.extraOnly));
        kindLimited.setMessage(label("仅限制卡", filter.limitedOnly));
        sortButton.setMessage(Component.literal("排序：" + CardFilter.sortName(filter.sortMode)));
        for (int i = 0; i < attributeButtons.size(); i++) {
            attributeButtons.get(i).setMessage(label(ATTRIBUTE_NAMES[i],
                    (filter.attributeMask & ATTRIBUTE_BITS[i]) != 0));
        }
        for (int i = 0; i < subButtons.size(); i++) {
            subButtons.get(i).setMessage(label(SUB_NAMES[i], subActive(i)));
        }
        for (int i = 0; i < raceButtons.size(); i++) {
            raceButtons.get(i).setMessage(label(RACE_NAMES[i], (filter.raceMask & RACE_BITS[i]) != 0));
        }
    }

    private void applyNumbers() {
        filter.levelMin = parse(levelMinBox);
        filter.levelMax = parse(levelMaxBox);
        filter.atkMin = parse(atkMinBox);
        filter.atkMax = parse(atkMaxBox);
        filter.defMin = parse(defMinBox);
        filter.defMax = parse(defMaxBox);
    }

    private int parse(EditBox box) {
        if (box == null) return 0;
        String value = box.getValue().trim();
        if (value.isEmpty()) return 0;
        try {
            return Integer.parseInt(value);
        } catch (NumberFormatException ex) {
            return 0;
        }
    }

    private void back() {
        if (this.minecraft != null) {
            this.minecraft.setScreen(parent);
        }
    }

    @Override
    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        if (mouseX >= leftPos && mouseX <= leftPos + panelW
                && mouseY >= viewportTop && mouseY <= viewportBottom) {
            scrollY -= (int) Math.signum(delta) * 18;
            applyScroll();
            return true;
        }
        return super.mouseScrolled(mouseX, mouseY, delta);
    }

    @Override
    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        renderBackground(graphics);
        graphics.fill(leftPos - 1, topPos - 1, leftPos + panelW + 1, topPos + panelH + 1, 0xFF000000);
        graphics.fill(leftPos, topPos, leftPos + panelW, topPos + panelH, COLOR_PANEL);
        graphics.fill(leftPos, topPos, leftPos + panelW, topPos + 1, COLOR_LIGHT);
        graphics.fill(leftPos, topPos, leftPos + 1, topPos + panelH, COLOR_LIGHT);
        graphics.fill(leftPos, topPos + panelH - 1, leftPos + panelW, topPos + panelH, COLOR_DARK);
        graphics.fill(leftPos + panelW - 1, topPos, leftPos + panelW, topPos + panelH, COLOR_DARK);
        graphics.drawString(this.font, Component.literal("卡片检索条件"), leftPos + 10, topPos + 6, COLOR_TITLE, false);

        // 内容区背景 + 裁剪
        graphics.fill(leftPos + 4, viewportTop - 2, leftPos + panelW - 4, viewportBottom + 2, COLOR_SLOT);
        graphics.fill(leftPos + 4, viewportTop - 2, leftPos + panelW - 4, viewportTop - 1, COLOR_SLOT_DARK);
        graphics.fill(leftPos + 4, viewportTop - 2, leftPos + 5, viewportBottom + 2, COLOR_SLOT_DARK);

        boolean scrollable = contentHeight > viewportBottom - viewportTop;
        int rightLimit = scrollable ? leftPos + panelW - 12 : leftPos + panelW - 5;
        graphics.enableScissor(leftPos + 5, viewportTop - 1, rightLimit, viewportBottom + 1);
        super.render(graphics, mouseX, mouseY, partialTick);
        for (int i = 0; i < labelText.size(); i++) {
            int y = viewportTop + labelY.get(i) - scrollY;
            if (y + 8 < viewportTop || y > viewportBottom) continue;
            graphics.drawString(this.font, Component.literal(labelText.get(i)), labelX.get(i), y,
                    COLOR_LABEL, false);
        }
        graphics.disableScissor();

        if (scrollable) {
            int trackTop = viewportTop;
            int trackHeight = viewportBottom - viewportTop;
            graphics.fill(leftPos + panelW - 10, trackTop, leftPos + panelW - 6, trackTop + trackHeight, 0xFF6F6F6F);
            int thumbHeight = Math.max(12, trackHeight * trackHeight / contentHeight);
            int max = contentHeight - trackHeight;
            int thumbY = trackTop + (max <= 0 ? 0 : (trackHeight - thumbHeight) * scrollY / max);
            graphics.fill(leftPos + panelW - 10, thumbY, leftPos + panelW - 6, thumbY + thumbHeight, COLOR_LIGHT);
        }

        String hint = scrollable ? "滚轮查看更多条件" : "";
        if (!hint.isEmpty()) {
            graphics.drawString(this.font, Component.literal(hint),
                    leftPos + panelW - 16 - this.font.width(hint), topPos + 6, COLOR_TITLE, false);
        }
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
