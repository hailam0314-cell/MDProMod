package net.mdpromod;

import java.util.ArrayList;
import java.util.LinkedHashSet;
import java.util.List;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraft.util.FormattedCharSequence;

public final class SumChoiceScreen extends Screen implements RequiredAnswer {
    private static final int COLS = 5;
    private static final int GAP = 6;
    private final ModNetwork.SumChoice choice;
    private final LinkedHashSet<Integer> picked = new LinkedHashSet<>();
    private int page;
    private int perPage;
    private int cols = 5;
    private int panelLeft;
    private int panelTop;
    private int panelW;
    private int panelH;
    private int gridLeft;
    private int gridTop;
    private Button confirmButton;

    public SumChoiceScreen(ModNetwork.SumChoice choice) {
        super(Component.literal("选择素材"));
        this.choice = choice;
    }

    private int mustCount() {
        return this.choice.mustCodes() == null ? 0 : this.choice.mustCodes().length;
    }

    private int totalCards() {
        return this.mustCount() + (this.choice.codes() == null ? 0 : this.choice.codes().length);
    }

    private int codeAt(int index) {
        return index < this.mustCount() ? this.choice.mustCodes()[index] : this.choice.codes()[index - this.mustCount()];
    }

    private int addressAt(int index) {
        if (index < this.mustCount()) {
            return this.choice.mustAddrs() != null && index < this.choice.mustAddrs().length ? this.choice.mustAddrs()[index] : 0;
        }

        int at = index - this.mustCount();
        return this.choice.addrs() != null && at < this.choice.addrs().length ? this.choice.addrs()[at] : 0;
    }

    private int level1At(int index) {
        if (index < this.mustCount()) {
            return this.choice.mustLevel1() != null && index < this.choice.mustLevel1().length ? this.choice.mustLevel1()[index] : 0;
        }

        int at = index - this.mustCount();
        return this.choice.level1() != null && at < this.choice.level1().length ? this.choice.level1()[at] : 0;
    }

    private int level2At(int index) {
        if (index < this.mustCount()) {
            return this.choice.mustLevel2() != null && index < this.choice.mustLevel2().length ? this.choice.mustLevel2()[index] : this.level1At(index);
        }

        int at = index - this.mustCount();
        return this.choice.level2() != null && at < this.choice.level2().length ? this.choice.level2()[at] : this.level1At(index);
    }

    private int sumLevel(boolean second) {
        int sum = 0;

        for (int i = 0; i < this.mustCount(); i++) {
            sum += second ? this.level2At(i) : this.level1At(i);
        }

        for (int index : this.picked) {
            int global = this.mustCount() + index;
            sum += second ? this.level2At(global) : this.level1At(global);
        }

        return sum;
    }

    private boolean isValid() {
        int count = this.picked.size();
        if (count >= this.choice.min() && count <= this.choice.max()) {
            int first = this.sumLevel(false);
            int second = this.sumLevel(true);
            return this.choice.overFlow()
                ? first >= this.choice.target() || second >= this.choice.target()
                : first == this.choice.target() || second == this.choice.target();
        } else {
            return false;
        }
    }

    private int pages() {
        return Math.max(1, (this.totalCards() + Math.max(1, this.perPage) - 1) / Math.max(1, this.perPage));
    }

    protected void init() {
        this.cols = Math.min(5, Math.max(1, this.totalCards()));
        int maxRows = Math.max(1, (this.height - 210) / 100);
        int rows = Math.min(maxRows, Math.max(1, (this.totalCards() + this.cols - 1) / this.cols));
        this.perPage = Math.max(1, this.cols * rows);
        this.page = Math.max(0, Math.min(this.page, this.pages() - 1));
        int gridW = this.cols * 60 - 6;
        this.panelW = Math.min(this.width - 20, Math.max(340, gridW + 28));
        this.panelH = Math.min(this.height - 20, 150 + rows * 100);
        this.panelLeft = (this.width - this.panelW) / 2;
        this.panelTop = (this.height - this.panelH) / 2;
        this.gridLeft = this.panelLeft + (this.panelW - gridW) / 2;
        this.gridTop = this.panelTop + 62;
        int bottom = this.panelTop + this.panelH - 26;
        this.confirmButton = (Button)this.addRenderableWidget(
            Button.builder(Component.literal("确定"), b -> this.submit()).bounds(this.panelLeft + this.panelW - 150, bottom, 70, 20).build()
        );
        this.addRenderableWidget(Button.builder(Component.literal("重置"), b -> this.reset()).bounds(this.panelLeft + this.panelW - 74, bottom, 64, 20).build());
        if (this.pages() > 1) {
            ((Button)this.addRenderableWidget(
                    Button.builder(Component.literal("←"), b -> this.turnPage(-1)).bounds(this.panelLeft + 8, bottom, 24, 20).build()
                ))
                .active = this.page > 0;
            ((Button)this.addRenderableWidget(
                    Button.builder(Component.literal("→"), b -> this.turnPage(1)).bounds(this.panelLeft + 36, bottom, 24, 20).build()
                ))
                .active = this.page + 1 < this.pages();
        }

        this.refreshConfirm();
    }

    private void refreshConfirm() {
        if (this.confirmButton != null) {
            this.confirmButton.active = this.isValid();
        }
    }

    private void turnPage(int delta) {
        this.page = Math.max(0, Math.min(this.pages() - 1, this.page + delta));
        this.rebuildWidgets();
    }

    private void reset() {
        this.picked.clear();
        this.refreshConfirm();
    }

    private void submit() {
        if (this.isValid()) {
            int[] chosen = this.picked.stream().mapToInt(Integer::intValue).toArray();
            ModNetwork.sendToServer(new ModNetwork.SumAnswer(this.choice.seq(), chosen));
            this.onClose();
        }
    }

    private int indexAt(double mouseX, double mouseY) {
        int start = this.page * this.perPage;

        for (int i = start; i < Math.min(this.totalCards(), start + this.perPage); i++) {
            int slot = i - start;
            int x = this.gridLeft + slot % this.cols * 60;
            int y = this.gridTop + slot / this.cols * 100;
            if (McPanel.inside(mouseX, mouseY, x, y, 54, 78)) {
                return i;
            }
        }

        return -1;
    }

    public boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (button == 0) {
            int index = this.indexAt(mouseX, mouseY);
            if (index >= this.mustCount()) {
                int pickedIndex = index - this.mustCount();
                if (!this.picked.remove(pickedIndex)) {
                    this.picked.add(pickedIndex);
                }

                ClientDuelAudio.playUi("SE_DUEL_SELECT");
                this.refreshConfirm();
                return true;
            }
        }

        return super.mouseClicked(mouseX, mouseY, button);
    }

    public boolean mouseScrolled(double x, double y, double delta) {
        if (this.pages() > 1) {
            this.turnPage(-((int)Math.signum(delta)));
            return true;
        } else {
            return super.mouseScrolled(x, y, delta);
        }
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        McPanel.panel(graphics, this.panelLeft, this.panelTop, this.panelLeft + this.panelW, this.panelTop + this.panelH);
        graphics.drawCenteredString(this.font, "选择素材", this.width / 2, this.panelTop + 8, 4210752);
        String target = (this.choice.overFlow() ? "至少 " : "合计 ") + this.choice.target();
        graphics.drawCenteredString(this.font, "目标：" + target, this.width / 2, this.panelTop + 24, 3166330);
        String pickedText = "已选合计 " + this.sumLevel(false) + " / " + this.choice.target();
        if (this.sumLevel(true) != this.sumLevel(false)) {
            pickedText = pickedText + "（另一形态 " + this.sumLevel(true) + "）";
        }

        graphics.drawCenteredString(this.font, pickedText, this.width / 2, this.panelTop + 36, 6316128);
        String limit = this.choice.min() == this.choice.max() ? "再选 " + this.choice.min() + " 张" : "再选 " + this.choice.min() + "~" + this.choice.max() + " 张";
        graphics.drawCenteredString(this.font, limit + " · 已选 " + this.picked.size() + " 张", this.width / 2, this.panelTop + 48, 6316128);
        int start = this.page * this.perPage;

        for (int i = start; i < Math.min(this.totalCards(), start + this.perPage); i++) {
            int slot = i - start;
            int x = this.gridLeft + slot % this.cols * 60;
            int y = this.gridTop + slot / this.cols * 100;
            int pickedIndex = i - this.mustCount();
            boolean selected = i < this.mustCount() || this.picked.contains(pickedIndex);
            McPanel.cardInSlot(graphics, this.codeAt(i), x, y, selected);
            int level1 = this.level1At(i);
            int level2 = this.level2At(i);
            String level = level2 != level1 ? "Lv " + level1 + "/" + level2 : "Lv " + level1;
            graphics.drawString(this.font, level, x + 2, y + 2, 16777215, true);
            if (i < this.mustCount()) {
                graphics.drawString(this.font, "固定", x + 2, y + 78 - 10, 16777088, true);
            }
        }

        if (this.pages() > 1) {
            graphics.drawCenteredString(
                this.font,
                "第 " + (this.page + 1) + "/" + this.pages() + " 页（滚轮翻页）",
                this.panelLeft + this.panelW / 2,
                this.panelTop + this.panelH - 40,
                6316128
            );
        }

        super.render(graphics, mouseX, mouseY, partialTick);
        int hovered = this.indexAt(mouseX, mouseY);
        if (hovered >= 0) {
            this.renderCardTooltip(graphics, hovered, mouseX, mouseY);
        }
    }

    private void renderCardTooltip(GuiGraphics graphics, int index, double mouseX, double mouseY) {
        List<FormattedCharSequence> lines = new ArrayList<>();
        lines.add(Component.literal(McPanel.displayName(this.codeAt(index))).getVisualOrderText());
        if (index < this.mustCount()) {
            lines.add(Component.literal("固定素材").getVisualOrderText());
        }

        int address = this.addressAt(index);
        if (address > 0) {
            lines.add(Component.literal(DuelLocation.tooltipLine(address, this.choice.viewerSeat())).getVisualOrderText());
        }

        int level1 = this.level1At(index);
        int level2 = this.level2At(index);
        lines.add(Component.literal(level2 != level1 ? "等级/阶级：" + level1 + " 或 " + level2 : "等级/阶级：" + level1).getVisualOrderText());
        graphics.renderTooltip(this.font, lines, (int)mouseX, (int)mouseY);
    }

    public boolean isPauseScreen() {
        return false;
    }

    public boolean shouldCloseOnEsc() {
        return false;
    }
}
