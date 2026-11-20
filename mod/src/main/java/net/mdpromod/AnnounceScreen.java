package net.mdpromod;

import java.util.ArrayList;
import java.util.List;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.EditBox;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

public class AnnounceScreen extends Screen implements RequiredAnswer {
    private static final int PANEL_W = 320;
    private static final int ROWS = 8;
    private final String kind;
    private final int pick;
    private final int[] values;
    private final String hint;
    private final List<Integer> chosen = new ArrayList<>();
    private final List<Button> valueButtons = new ArrayList<>();
    private Button confirmButton;
    private Button previousButton;
    private Button nextButton;
    private int choicePage;
    private final List<CardInfo> matches = new ArrayList<>();
    private EditBox searchBox;
    private String searchText = "";
    private final List<Button> resultRows = new ArrayList<>();
    private int scroll;

    public AnnounceScreen(String kind, int pick, int[] values, String hint) {
        super(Component.literal(titleOf(kind)));
        this.kind = kind == null ? "" : kind;
        this.pick = Math.max(1, pick);
        this.values = values == null ? new int[0] : values;
        this.hint = hint == null ? "" : hint;
    }

    private static String titleOf(String kind) {
        return switch (kind == null ? "" : kind) {
            case "race" -> "宣言种族";
            case "attrib" -> "宣言属性";
            case "card" -> "宣言卡名";
            default -> "宣言数字";
        };
    }

    private String valueLabel(int value) {
        return switch (this.kind) {
            case "race" -> CardLabels.raceName(value);
            case "attrib" -> CardLabels.attributeName(value);
            default -> Integer.toString(value);
        };
    }

    private int panelLeft() {
        return (this.width - Math.min(320, this.width - 20)) / 2;
    }

    private int panelWidth() {
        return Math.min(320, this.width - 20);
    }

    private int panelTop() {
        return Math.max(10, (this.height - this.panelHeight()) / 2);
    }

    private boolean isCard() {
        return !this.kind.equals("race") && !this.kind.equals("attrib") && !this.kind.equals("number");
    }

    private int columns() {
        return this.kind.equals("number") ? 4 : 3;
    }

    private int visibleRows() {
        if (this.isCard()) {
            return Math.max(1, Math.min(8, (this.height - 100) / 22));
        }

        int total = Math.max(1, (this.values.length + this.columns() - 1) / this.columns());
        return Math.max(1, Math.min(total, (this.height - 104) / 24));
    }

    private int pageCapacity() {
        return this.visibleRows() * this.columns();
    }

    private int maxChoicePage() {
        return Math.max(0, (this.values.length - 1) / this.pageCapacity());
    }

    private int panelHeight() {
        return this.isCard() ? 80 + this.visibleRows() * 22 : 84 + this.visibleRows() * 24;
    }

    protected void init() {
        if (this.searchBox != null) {
            this.searchText = this.searchBox.getValue();
        }

        this.valueButtons.clear();
        this.resultRows.clear();
        this.confirmButton = null;
        this.previousButton = null;
        this.nextButton = null;
        int left = this.panelLeft();
        int boxWidth = this.panelWidth();
        int top = this.panelTop() + 34;
        switch (this.kind) {
            case "race":
            case "attrib":
            case "number":
                this.choicePage = Math.min(this.choicePage, this.maxChoicePage());
                int columns = this.columns();
                int cellW = (boxWidth - 24) / columns;

                for (int i = 0; i < Math.min(this.values.length, this.pageCapacity()); i++) {
                    int slot = i;
                    int x = left + 12 + i % columns * cellW;
                    int y = top + i / columns * 24;
                    Button button = Button.builder(Component.empty(), b -> {
                        int index = this.choicePage * this.pageCapacity() + slot;
                        if (index < this.values.length) {
                            if (this.kind.equals("number")) {
                                this.answer(index);
                            } else {
                                this.toggle(this.values[index], b);
                            }
                        }
                    }).bounds(x, y, cellW - 6, 20).build();
                    this.valueButtons.add(button);
                    this.addRenderableWidget(button);
                }

                int footer = top + this.visibleRows() * 24 + 4;
                if (this.maxChoicePage() > 0) {
                    this.previousButton = (Button)this.addRenderableWidget(
                        Button.builder(Component.literal("上一页"), b -> this.changePage(-1)).bounds(left + 12, footer, 60, 20).build()
                    );
                    this.nextButton = (Button)this.addRenderableWidget(
                        Button.builder(Component.literal("下一页"), b -> this.changePage(1)).bounds(left + boxWidth - 72, footer, 60, 20).build()
                    );
                }

                if (!this.kind.equals("number") && this.pick > 1) {
                    this.confirmButton = (Button)this.addRenderableWidget(
                        Button.builder(Component.literal("确定"), b -> this.confirm()).bounds(left + boxWidth / 2 - 40, footer, 80, 20).build()
                    );
                }

                this.updateChoices();
                break;
            default:
                this.searchBox = new EditBox(this.font, left + 12, top, boxWidth - 24, 18, Component.literal("卡名、拼音或卡号"));
                this.searchBox.setMaxLength(32);
                this.searchBox.setValue(this.searchText);
                this.searchBox.setResponder(text -> this.refreshMatches());
                this.addRenderableWidget(this.searchBox);
                this.setInitialFocus(this.searchBox);

                for (int row = 0; row < this.visibleRows(); row++) {
                    int slot = row;
                    Button button = Button.builder(Component.literal("-"), b -> {
                        int index = this.scroll + slot;
                        if (index < this.matches.size()) {
                            this.answer(this.matches.get(index).code());
                        }
                    }).bounds(left + 12, top + 26 + row * 22, boxWidth - 24, 20).build();
                    button.active = false;
                    this.resultRows.add(button);
                    this.addRenderableWidget(button);
                }

                int previousScroll = this.scroll;
                this.refreshMatches();
                this.scroll = Math.min(previousScroll, Math.max(0, this.matches.size() - this.resultRows.size()));
                this.updateRows();
        }
    }

    private void changePage(int delta) {
        this.choicePage = Math.max(0, Math.min(this.maxChoicePage(), this.choicePage + delta));
        this.updateChoices();
    }

    private void updateChoices() {
        for (int slot = 0; slot < this.valueButtons.size(); slot++) {
            int index = this.choicePage * this.pageCapacity() + slot;
            Button button = this.valueButtons.get(slot);
            button.active = index < this.values.length;
            button.visible = button.active;
            if (button.active) {
                int value = this.values[index];
                button.setMessage(Component.literal((this.chosen.contains(value) ? "▶ " : "") + this.valueLabel(value)));
            }
        }

        if (this.previousButton != null) {
            this.previousButton.active = this.choicePage > 0;
        }

        if (this.nextButton != null) {
            this.nextButton.active = this.choicePage < this.maxChoicePage();
        }

        if (this.confirmButton != null) {
            this.confirmButton.active = this.chosen.size() == this.pick;
        }
    }

    private void toggle(int value, Button button) {
        if (this.pick == 1) {
            this.answer(value);
        } else {
            boolean removed = this.chosen.remove(Integer.valueOf(value));
            if (!removed && this.chosen.size() < this.pick) {
                this.chosen.add(value);
            }

            button.setMessage(Component.literal((this.chosen.contains(value) ? "▶ " : "") + this.valueLabel(value)));
            if (this.confirmButton != null) {
                this.confirmButton.active = this.chosen.size() == this.pick;
            }
        }
    }

    private void confirm() {
        int sum = 0;

        for (int value : this.chosen) {
            sum |= value;
        }

        this.answer(sum);
    }

    protected void answer(int value) {
        ModNetwork.sendToServer(new ModNetwork.AnnounceAnswer(value));
        ClientDuelAudio.playUi("SE_DUEL_DECIDE");
        this.onClose();
    }

    private void refreshMatches() {
        this.matches.clear();
        this.scroll = 0;
        String needle = this.searchBox == null ? "" : this.searchBox.getValue().trim();

        for (CardInfo info : CardRegistry.all()) {
            if (needle.isEmpty() || CardNameSearch.matches(info, needle)) {
                this.matches.add(info);
                if (this.matches.size() >= 400) {
                    break;
                }
            }
        }

        this.updateRows();
    }

    private void updateRows() {
        for (int row = 0; row < this.resultRows.size(); row++) {
            int index = this.scroll + row;
            Button button = this.resultRows.get(row);
            if (index < this.matches.size()) {
                CardInfo info = this.matches.get(index);
                button.setMessage(Component.literal(info.name() + "  #" + info.code()));
                button.active = true;
            } else {
                button.setMessage(Component.literal("-"));
                button.active = false;
            }
        }
    }

    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        if (!this.resultRows.isEmpty() && !this.matches.isEmpty()) {
            int max = Math.max(0, this.matches.size() - this.resultRows.size());
            this.scroll = Math.max(0, Math.min(max, this.scroll - (int)Math.signum(delta) * this.resultRows.size()));
            this.updateRows();
            return true;
        } else if (!this.isCard() && this.maxChoicePage() > 0) {
            this.changePage(-((int)Math.signum(delta)));
            return true;
        } else {
            return super.mouseScrolled(mouseX, mouseY, delta);
        }
    }

    public boolean keyPressed(int keyCode, int scanCode, int modifiers) {
        if ((keyCode == 257 || keyCode == 335) && this.searchBox != null && !this.matches.isEmpty()) {
            this.answer(this.matches.get(this.scroll).code());
            return true;
        } else {
            return super.keyPressed(keyCode, scanCode, modifiers);
        }
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        int left = this.panelLeft();
        int boxWidth = this.panelWidth();
        int top = this.panelTop();
        int heightOfPanel = this.panelHeight();
        McPanel.panel(graphics, left, top, left + boxWidth, top + heightOfPanel);
        graphics.drawCenteredString(this.font, titleOf(this.kind), this.width / 2, top + 8, 4210752);
        if (!this.hint.isEmpty()) {
            graphics.drawCenteredString(this.font, this.font.plainSubstrByWidth(this.hint, boxWidth - 20), this.width / 2, top + 20, 6316128);
        }

        boolean riding = RidingDuelUi.active(this.minecraft);
        String status;
        if (this.isCard()) {
            status = riding ? "结果 " + this.matches.size() + " 条" : "输入卡名、拼音或卡号，滚轮翻页（共 " + this.matches.size() + " 条）";
        } else {
            status = this.pick > 1 ? "已选 " + this.chosen.size() + " / " + this.pick : (riding ? "请选择" : "点击候选进行宣言");
            if (this.maxChoicePage() > 0) {
                status = status + "  第 " + (this.choicePage + 1) + "/" + (this.maxChoicePage() + 1) + " 页";
            }
        }

        graphics.drawString(this.font, this.font.plainSubstrByWidth(status, boxWidth - 24), left + 12, top + heightOfPanel - 14, 5263440, false);
        super.render(graphics, mouseX, mouseY, partialTick);
    }

    public boolean isPauseScreen() {
        return false;
    }

    public boolean shouldCloseOnEsc() {
        return false;
    }
}
