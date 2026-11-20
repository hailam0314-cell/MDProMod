package net.mdpromod;

import java.util.ArrayList;
import java.util.List;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.AbstractWidget;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.EditBox;
import net.minecraft.client.gui.components.events.GuiEventListener;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public class CardSearchScreen extends Screen {
    private static final int COLOR_PANEL = -3750202;
    private static final int COLOR_LIGHT = -1;
    private static final int COLOR_DARK = -11184811;
    private static final int COLOR_TITLE = 4210752;
    private static final int COLOR_LABEL = -12632257;
    private static final int COLOR_SLOT = -7631989;
    private static final int COLOR_SLOT_DARK = -13158601;
    private static final String[] ATTRIBUTE_NAMES = CardLabels.ATTRIBUTE_NAMES;
    private static final int[] ATTRIBUTE_BITS = CardLabels.ATTRIBUTE_BITS;
    private static final String[] RACE_NAMES = CardLabels.RACE_NAMES;
    private static final int[] RACE_BITS = CardLabels.RACE_BITS;
    private static final String[] SUB_NAMES = new String[]{"通常魔法", "永续魔法", "装备魔法", "速攻魔法", "仪式魔法", "场地魔法", "通常陷阱", "永续陷阱", "反击陷阱"};
    private static final int[] SUB_BITS = new int[]{1073741824, 131072, 262144, 65536, 128, 524288, 536870912, 131072, 1048576};
    private static final boolean[] SUB_IS_TRAP = new boolean[]{false, false, false, false, false, false, true, true, true};
    private static final String[] MONSTER_NAMES = new String[]{"通常", "效果", "仪式", "融合", "同调", "超量", "连接", "灵摆", "调整", "反转", "二重", "灵魂"};
    private static final int[] MONSTER_BITS = new int[]{16, 32, 128, 64, 8192, 8388608, 67108864, 16777216, 4096, 2097152, 2048, 512};
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
    private final List<Button> monsterButtons = new ArrayList<>();
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

    protected void init() {
        super.init();
        this.tracked.clear();
        this.trackedBaseY.clear();
        this.labelText.clear();
        this.labelX.clear();
        this.labelY.clear();
        this.attributeButtons.clear();
        this.monsterButtons.clear();
        this.raceButtons.clear();
        this.subButtons.clear();
        this.panelW = Math.min(Math.max(this.width - 40, 320), Math.min(620, Math.max(260, this.width - 8)));
        int rowW = this.panelW - 20;
        this.leftPos = (this.width - this.panelW) / 2;
        this.contentX = this.leftPos + 10;
        this.contentW = rowW;
        int raceCols = Math.max(4, Math.min(7, rowW / 62));
        int raceRows = (RACE_NAMES.length + raceCols - 1) / raceCols;
        int kindRowH = 16;
        int labelH = 10;
        int blockGap = 6;
        int numbersRowH = 20;
        int subRowsPlanned = (SUB_NAMES.length + 4) / 5;
        int monsterRowsPlanned = (MONSTER_NAMES.length + 4) / 5;
        this.contentHeight = (labelH + kindRowH + blockGap) * 2
            + labelH
            + monsterRowsPlanned * 18
            + blockGap
            + labelH
            + subRowsPlanned * 18
            + blockGap
            + labelH
            + raceRows * 18
            + blockGap
            + numbersRowH * 3
            + blockGap
            + kindRowH;
        int chromeTop = 22;
        int chromeBottom = 30;
        int wanted = this.contentHeight + chromeTop + chromeBottom;
        int maxPanelH = Math.min(470, this.height - 8);
        this.panelH = Math.max(160, Math.min(wanted, maxPanelH));
        this.topPos = Math.max(4, (this.height - this.panelH) / 2);
        this.viewportTop = this.topPos + chromeTop;
        this.viewportBottom = this.topPos + this.panelH - chromeBottom;
        if (this.viewportBottom - this.viewportTop < 40) {
            this.viewportBottom = this.viewportTop + 40;
        }

        int y = 0;
        this.label("种类", y);
        y += labelH;
        int kindW = Math.min(66, (this.contentW - 12) / 4);
        this.kindMonster = this.tracked(Button.builder(this.label("怪兽", this.filter.monster), b -> {
            this.filter.monster = !this.filter.monster;
            this.refreshLabels();
        }).bounds(this.contentX, 0, kindW, kindRowH).build(), y);
        this.kindSpell = this.tracked(Button.builder(this.label("魔法", this.filter.spell), b -> {
            this.filter.spell = !this.filter.spell;
            this.refreshLabels();
        }).bounds(this.contentX + kindW + 4, 0, kindW, kindRowH).build(), y);
        this.kindTrap = this.tracked(Button.builder(this.label("陷阱", this.filter.trap), b -> {
            this.filter.trap = !this.filter.trap;
            this.refreshLabels();
        }).bounds(this.contentX + (kindW + 4) * 2, 0, kindW, kindRowH).build(), y);
        this.kindExtra = this.tracked(Button.builder(this.label("只看额外", this.filter.extraOnly), b -> {
            this.filter.extraOnly = !this.filter.extraOnly;
            this.refreshLabels();
        }).bounds(this.contentX + (kindW + 4) * 3, 0, kindW, kindRowH).build(), y);
        y += kindRowH + blockGap;
        this.label("怪兽类型（可多选）", y);
        y += labelH;
        int monsterCols = 5;
        int monsterW = Math.max(40, Math.min(62, (this.contentW - (monsterCols - 1) * 4) / monsterCols));

        for (int i = 0; i < MONSTER_NAMES.length; i++) {
            int index = i;
            int col = i % monsterCols;
            int row = i / monsterCols;
            Button button = this.tracked(Button.builder(this.label(MONSTER_NAMES[i], (this.filter.monsterTypeMask & MONSTER_BITS[i]) != 0), b -> {
                this.filter.monsterTypeMask = this.filter.monsterTypeMask ^ MONSTER_BITS[index];
                this.refreshLabels();
            }).bounds(this.contentX + col * (monsterW + 4), 0, monsterW, kindRowH).build(), y + row * 18);
            this.monsterButtons.add(button);
        }

        y += monsterRowsPlanned * 18 + blockGap;
        this.label("属性（可多选）", y);
        y += labelH;
        int attrW = Math.min(50, (this.contentW - 24) / 7);

        for (int i = 0; i < ATTRIBUTE_NAMES.length; i++) {
            int index = i;
            Button button = this.tracked(Button.builder(this.label(ATTRIBUTE_NAMES[i], (this.filter.attributeMask & ATTRIBUTE_BITS[i]) != 0), b -> {
                this.filter.attributeMask = this.filter.attributeMask ^ ATTRIBUTE_BITS[index];
                this.refreshLabels();
            }).bounds(this.contentX + i * (attrW + 4), 0, attrW, kindRowH).build(), y);
            this.attributeButtons.add(button);
        }

        y += kindRowH + blockGap;
        this.label("魔陷种类", y);
        y += labelH;
        int subW = Math.max(40, Math.min(64, (this.contentW - (SUB_NAMES.length - 1) * 4) / SUB_NAMES.length));

        for (int i = 0; i < SUB_NAMES.length; i++) {
            int index = i;
            int col = i % 5;
            int row = i / 5;
            Button button = this.tracked(Button.builder(this.label(SUB_NAMES[i], this.subActive(i)), b -> {
                int bit = SUB_BITS[index];
                if (SUB_IS_TRAP[index]) {
                    this.filter.trapSubMask ^= bit;
                } else {
                    this.filter.spellSubMask ^= bit;
                }

                this.refreshLabels();
            }).bounds(this.contentX + col * (subW + 4), 0, subW, kindRowH).build(), y + row * 18);
            this.subButtons.add(button);
        }

        int subRows = (SUB_NAMES.length + 4) / 5;
        y += subRows * 18 + blockGap;
        this.label("种族（可多选）", y);
        y += labelH;
        int raceButtonW = Math.max(34, (this.contentW - (raceCols - 1) * 4) / raceCols);

        for (int i = 0; i < RACE_NAMES.length; i++) {
            int index = i;
            int col = i % raceCols;
            int row = i / raceCols;
            Button button = this.tracked(Button.builder(this.label(RACE_NAMES[i], (this.filter.raceMask & RACE_BITS[i]) != 0), b -> {
                this.filter.raceMask = this.filter.raceMask ^ RACE_BITS[index];
                this.refreshLabels();
            }).bounds(this.contentX + col * (raceButtonW + 4), 0, raceButtonW, 16).build(), y + row * 18);
            this.raceButtons.add(button);
        }

        y += raceRows * 18 + blockGap;
        this.label("等级", y + 4);
        this.label("攻击", y + 24);
        this.label("守备", y + 44);
        this.levelMinBox = this.tracked(this.numberBox(this.contentX + 40, 0, 44, this.filter.levelMin, this.levelMinBox), y);
        this.levelMaxBox = this.tracked(this.numberBox(this.contentX + 92, 0, 44, this.filter.levelMax, this.levelMaxBox), y);
        this.atkMinBox = this.tracked(this.numberBox(this.contentX + 40, 0, 44, this.filter.atkMin, this.atkMinBox), y + numbersRowH);
        this.atkMaxBox = this.tracked(this.numberBox(this.contentX + 92, 0, 44, this.filter.atkMax, this.atkMaxBox), y + numbersRowH);
        this.defMinBox = this.tracked(this.numberBox(this.contentX + 40, 0, 44, this.filter.defMin, this.defMinBox), y + numbersRowH * 2);
        this.defMaxBox = this.tracked(this.numberBox(this.contentX + 92, 0, 44, this.filter.defMax, this.defMaxBox), y + numbersRowH * 2);
        this.labelAt("~", this.contentX + 86, y + 4);
        this.labelAt("~", this.contentX + 86, y + 24);
        this.labelAt("~", this.contentX + 86, y + 44);
        this.labelAt("留空表示不限", this.contentX + 146, y + 4);
        y += numbersRowH * 3 + blockGap;
        this.kindLimited = this.tracked(Button.builder(this.label("仅限制卡", this.filter.limitedOnly), b -> {
            this.filter.limitedOnly = !this.filter.limitedOnly;
            this.refreshLabels();
        }).bounds(this.contentX, 0, 76, kindRowH).build(), y);
        this.sortButton = this.tracked(Button.builder(Component.literal("排序：" + CardFilter.sortName(this.filter.sortMode)), b -> {
            this.filter.sortMode = (this.filter.sortMode + 1) % 5;
            this.refreshLabels();
        }).bounds(this.contentX + 80, 0, 92, kindRowH).build(), y);
        y += kindRowH;
        this.contentHeight = y;
        int buttonY = this.topPos + this.panelH - 24;
        this.addRenderableWidget(Button.builder(Component.literal("重置条件"), b -> {
            this.filter.reset();

            for (EditBox box : new EditBox[]{this.levelMinBox, this.levelMaxBox, this.atkMinBox, this.atkMaxBox, this.defMinBox, this.defMaxBox}) {
                if (box != null) {
                    box.setValue("");
                }
            }

            this.refreshLabels();
        }).bounds(this.leftPos + 10, buttonY, 80, 18).build());
        this.addRenderableWidget(Button.builder(Component.literal("取消"), b -> this.back()).bounds(this.leftPos + this.panelW - 188, buttonY, 70, 18).build());
        this.addRenderableWidget(Button.builder(Component.literal("应用并返回"), b -> {
            this.applyNumbers();
            this.back();
        }).bounds(this.leftPos + this.panelW - 114, buttonY, 104, 18).build());
        this.applyScroll();
        this.refreshLabels();
    }

    private boolean subActive(int index) {
        int bit = SUB_BITS[index];
        return SUB_IS_TRAP[index] ? (this.filter.trapSubMask & bit) != 0 : (this.filter.spellSubMask & bit) != 0;
    }

    private Button tracked(Button button, int baseY) {
        this.tracked.add(button);
        this.trackedBaseY.add(baseY);
        this.addRenderableWidget(button);
        return button;
    }

    private EditBox tracked(EditBox box, int baseY) {
        this.tracked.add(box);
        this.trackedBaseY.add(baseY);
        this.addRenderableWidget(box);
        return box;
    }

    private EditBox numberBox(int x, int y, int width, int initial, EditBox previous) {
        EditBox box = new EditBox(this.font, x, y, width, 16, Component.literal("数值"));
        box.setMaxLength(5);
        box.setFilter(text -> text.matches("\\d{0,5}"));
        if (previous != null) {
            box.setValue(previous.getValue());
        } else if (initial > 0) {
            box.setValue(Integer.toString(initial));
        }

        return box;
    }

    private void label(String text, int baseY) {
        this.labelAt(text, this.contentX, baseY);
    }

    private void labelAt(String text, int x, int baseY) {
        this.labelText.add(text);
        this.labelX.add(x);
        this.labelY.add(baseY);
    }

    private Component label(String text, boolean active) {
        return Component.literal((active ? "✔" : "") + text);
    }

    private void applyScroll() {
        int max = Math.max(0, this.contentHeight - (this.viewportBottom - this.viewportTop));
        this.scrollY = Math.max(0, Math.min(this.scrollY, max));

        for (int i = 0; i < this.tracked.size(); i++) {
            AbstractWidget widget = this.tracked.get(i);
            int y = this.viewportTop + this.trackedBaseY.get(i) - this.scrollY;
            widget.setY(y);
            widget.visible = y + widget.getHeight() > this.viewportTop && y < this.viewportBottom;
        }
    }

    private void refreshLabels() {
        this.kindMonster.setMessage(this.label("怪兽", this.filter.monster));
        this.kindSpell.setMessage(this.label("魔法", this.filter.spell));
        this.kindTrap.setMessage(this.label("陷阱", this.filter.trap));
        this.kindExtra.setMessage(this.label("只看额外", this.filter.extraOnly));
        this.kindLimited.setMessage(this.label("仅限制卡", this.filter.limitedOnly));
        this.sortButton.setMessage(Component.literal("排序：" + CardFilter.sortName(this.filter.sortMode)));

        for (int i = 0; i < this.monsterButtons.size(); i++) {
            this.monsterButtons.get(i).setMessage(this.label(MONSTER_NAMES[i], (this.filter.monsterTypeMask & MONSTER_BITS[i]) != 0));
        }

        for (int i = 0; i < this.attributeButtons.size(); i++) {
            this.attributeButtons.get(i).setMessage(this.label(ATTRIBUTE_NAMES[i], (this.filter.attributeMask & ATTRIBUTE_BITS[i]) != 0));
        }

        for (int i = 0; i < this.subButtons.size(); i++) {
            this.subButtons.get(i).setMessage(this.label(SUB_NAMES[i], this.subActive(i)));
        }

        for (int i = 0; i < this.raceButtons.size(); i++) {
            this.raceButtons.get(i).setMessage(this.label(RACE_NAMES[i], (this.filter.raceMask & RACE_BITS[i]) != 0));
        }
    }

    private void applyNumbers() {
        this.filter.levelMin = this.parse(this.levelMinBox);
        this.filter.levelMax = this.parse(this.levelMaxBox);
        this.filter.atkMin = this.parse(this.atkMinBox);
        this.filter.atkMax = this.parse(this.atkMaxBox);
        this.filter.defMin = this.parse(this.defMinBox);
        this.filter.defMax = this.parse(this.defMaxBox);
    }

    private int parse(EditBox box) {
        if (box == null) {
            return 0;
        }

        String value = box.getValue().trim();
        if (value.isEmpty()) {
            return 0;
        }

        try {
            return Integer.parseInt(value);
        } catch (NumberFormatException ex) {
            return 0;
        }
    }

    private void back() {
        if (this.minecraft != null) {
            this.minecraft.setScreen(this.parent);
        }
    }

    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        if (mouseX >= this.leftPos && mouseX <= this.leftPos + this.panelW && mouseY >= this.viewportTop && mouseY <= this.viewportBottom) {
            this.scrollY = this.scrollY - (int)Math.signum(delta) * 18;
            this.applyScroll();
            return true;
        } else {
            return super.mouseScrolled(mouseX, mouseY, delta);
        }
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        graphics.fill(this.leftPos - 1, this.topPos - 1, this.leftPos + this.panelW + 1, this.topPos + this.panelH + 1, -16777216);
        graphics.fill(this.leftPos, this.topPos, this.leftPos + this.panelW, this.topPos + this.panelH, -3750202);
        graphics.fill(this.leftPos, this.topPos, this.leftPos + this.panelW, this.topPos + 1, -1);
        graphics.fill(this.leftPos, this.topPos, this.leftPos + 1, this.topPos + this.panelH, -1);
        graphics.fill(this.leftPos, this.topPos + this.panelH - 1, this.leftPos + this.panelW, this.topPos + this.panelH, -11184811);
        graphics.fill(this.leftPos + this.panelW - 1, this.topPos, this.leftPos + this.panelW, this.topPos + this.panelH, -11184811);
        graphics.drawString(this.font, Component.literal("卡片检索条件"), this.leftPos + 10, this.topPos + 6, 4210752, false);
        graphics.fill(this.leftPos + 4, this.viewportTop - 2, this.leftPos + this.panelW - 4, this.viewportBottom + 2, -7631989);
        graphics.fill(this.leftPos + 4, this.viewportTop - 2, this.leftPos + this.panelW - 4, this.viewportTop - 1, -13158601);
        graphics.fill(this.leftPos + 4, this.viewportTop - 2, this.leftPos + 5, this.viewportBottom + 2, -13158601);
        boolean scrollable = this.contentHeight > this.viewportBottom - this.viewportTop;
        int rightLimit = scrollable ? this.leftPos + this.panelW - 12 : this.leftPos + this.panelW - 5;
        graphics.enableScissor(this.leftPos + 5, this.viewportTop - 1, rightLimit, this.viewportBottom + 1);

        for (AbstractWidget widget : this.tracked) {
            widget.render(graphics, mouseX, mouseY, partialTick);
        }

        for (int i = 0; i < this.labelText.size(); i++) {
            int y = this.viewportTop + this.labelY.get(i) - this.scrollY;
            if (y + 8 >= this.viewportTop && y <= this.viewportBottom) {
                graphics.drawString(this.font, Component.literal(this.labelText.get(i)), this.labelX.get(i), y, -12632257, false);
            }
        }

        graphics.disableScissor();

        for (GuiEventListener child : this.children()) {
            if (child instanceof AbstractWidget widget && !this.tracked.contains(widget)) {
                widget.render(graphics, mouseX, mouseY, partialTick);
            }
        }

        if (scrollable) {
            int trackTop = this.viewportTop;
            int trackHeight = this.viewportBottom - this.viewportTop;
            graphics.fill(this.leftPos + this.panelW - 10, trackTop, this.leftPos + this.panelW - 6, trackTop + trackHeight, -9474193);
            int thumbHeight = Math.max(12, trackHeight * trackHeight / this.contentHeight);
            int max = this.contentHeight - trackHeight;
            int thumbY = trackTop + (max <= 0 ? 0 : (trackHeight - thumbHeight) * this.scrollY / max);
            graphics.fill(this.leftPos + this.panelW - 10, thumbY, this.leftPos + this.panelW - 6, thumbY + thumbHeight, -1);
        }

        String hint = scrollable ? "滚轮查看更多条件" : "";
        if (!hint.isEmpty()) {
            graphics.drawString(
                this.font, Component.literal(hint), this.leftPos + this.panelW - 16 - this.font.width(hint), this.topPos + 6, 4210752, false
            );
        }
    }

    public void onClose() {
        this.back();
    }

    public boolean isPauseScreen() {
        return false;
    }
}
