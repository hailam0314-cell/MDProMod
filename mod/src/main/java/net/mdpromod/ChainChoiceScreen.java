package net.mdpromod;

import java.util.ArrayList;
import java.util.List;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraft.util.FormattedCharSequence;

public class ChainChoiceScreen extends Screen implements RequiredAnswer {
    private static final int COLS = 5;
    private static final int GAP = 6;
    private final int[] codes;
    private final boolean forced;
    private final String timing;
    private final boolean opening;
    private final int[] addrs;
    private final int viewerSeat;
    private final String[] effectTexts;
    private int selected = -1;
    private int panelLeft;
    private int panelTop;
    private int panelW;
    private int panelH;
    private int gridLeft;
    private int gridTop;
    private int cols = 1;
    private int page;
    private int perPage = 1;
    private int detailTop;
    private int timingLines;
    private int rowGap = 6;
    private Button chainButton;
    private Button cancelButton;

    public ChainChoiceScreen(int[] codes, boolean forced) {
        this(codes, forced, "", false, new int[0]);
    }

    public ChainChoiceScreen(int[] codes, boolean forced, String timing) {
        this(codes, forced, timing, false, new int[0]);
    }

    public ChainChoiceScreen(int[] codes, boolean forced, String timing, boolean opening, int[] addrs) {
        this(codes, forced, timing, opening, addrs, 0);
    }

    public ChainChoiceScreen(int[] codes, boolean forced, String timing, boolean opening, int[] addrs, int viewerSeat) {
        this(codes, forced, timing, opening, addrs, viewerSeat, new String[0]);
    }

    public ChainChoiceScreen(int[] codes, boolean forced, String timing, boolean opening, int[] addrs, int viewerSeat, String[] effectTexts) {
        super(Component.literal(opening ? "发动效果" : "连锁"));
        this.codes = codes == null ? new int[0] : codes;
        this.forced = forced;
        this.timing = timing == null ? "" : timing;
        this.opening = opening;
        this.addrs = addrs == null ? new int[0] : addrs;
        this.viewerSeat = viewerSeat;
        this.effectTexts = effectTexts == null ? new String[0] : effectTexts;
        if (this.codes.length == 1) {
            this.selected = 0;
        }
    }

    private int rows() {
        return 1;
    }

    protected void init() {
        ClientDuelAudio.playUi("SE_DUEL_ACTIVE_POSSIBLE");
        this.cols = Math.min(Math.max(1, (this.width - 48) / 60), Math.min(5, Math.max(1, this.codes.length)));
        this.perPage = this.cols;
        this.page = Math.min(this.page, this.pages() - 1);
        int gridW = this.cols * 60 - 6;
        int textW = this.timing.isEmpty() ? 0 : this.font.width(this.timing) + 24;
        this.panelW = Math.min(Math.max(300, Math.max(gridW + 28, textW)), this.width - 20);
        this.timingLines = this.timing.isEmpty() ? 0 : this.font.split(Component.literal(this.timing), this.panelW - 20).size();
        boolean hasEffectTexts = false;

        for (String text : this.effectTexts) {
            if (text != null && !text.isEmpty()) {
                hasEffectTexts = true;
                break;
            }
        }

        this.rowGap = !hasEffectTexts && !this.opening ? 6 : 18;
        int gridH = this.rows() * (78 + this.rowGap) - this.rowGap;
        this.panelH = Math.min(gridH + 124 + this.timingLines * 10, this.height - 20);
        this.panelLeft = (this.width - this.panelW) / 2;
        this.panelTop = (this.height - this.panelH) / 2;
        this.gridLeft = this.panelLeft + Math.max(6, (this.panelW - gridW) / 2);
        this.gridTop = this.panelTop + 24 + this.timingLines * 10;
        this.detailTop = this.gridTop + 78 + this.rowGap;
        int bottom = this.panelTop + this.panelH - 26;
        this.chainButton = (Button)this.addRenderableWidget(
            Button.builder(Component.literal(this.opening ? "发动" : "连锁"), button -> this.submit())
                .bounds(this.panelLeft + this.panelW - 172, bottom, 80, 20)
                .build()
        );
        this.cancelButton = (Button)this.addRenderableWidget(
            Button.builder(Component.literal(this.opening ? "不发动" : "取消"), button -> this.giveUp())
                .bounds(this.panelLeft + this.panelW - 88, bottom, 80, 20)
                .build()
        );
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

        this.refreshButtons();
    }

    private int pages() {
        return Math.max(1, (this.codes.length + this.perPage - 1) / this.perPage);
    }

    private void turnPage(int delta) {
        this.page = Math.max(0, Math.min(this.pages() - 1, this.page + delta));
        this.rebuildWidgets();
    }

    public boolean mouseScrolled(double x, double y, double delta) {
        this.turnPage(-((int)Math.signum(delta)));
        return true;
    }

    private void refreshButtons() {
        if (this.chainButton != null) {
            this.chainButton.active = this.selected >= 0 && this.selected < this.codes.length;
        }

        if (this.cancelButton != null) {
            this.cancelButton.active = !this.forced;
        }
    }

    private void submit() {
        if (this.selected >= 0 && this.selected < this.codes.length) {
            ModNetwork.sendToServer(new ModNetwork.ChainChoose(this.selected));
            this.onClose();
        }
    }

    private void giveUp() {
        if (!this.forced) {
            ModNetwork.sendToServer(new ModNetwork.ChainChoose(-1));
            this.onClose();
        }
    }

    private int indexAt(double mouseX, double mouseY) {
        for (int i = this.page * this.perPage; i < Math.min(this.codes.length, (this.page + 1) * this.perPage); i++) {
            int x = this.gridLeft + i % this.cols * 60;
            int y = this.gridTop;
            if (McPanel.inside(mouseX, mouseY, x, y, 54, 78)) {
                return i;
            }
        }

        return -1;
    }

    public boolean keyPressed(int keyCode, int scanCode, int modifiers) {
        if (this.codes.length > 1 && (keyCode == 263 || keyCode == 262)) {
            int step = keyCode == 263 ? -1 : 1;
            this.selected = this.selected < 0 ? (step > 0 ? 0 : this.codes.length - 1) : Math.floorMod(this.selected + step, this.codes.length);
            if (this.page != this.selected / this.perPage) {
                this.page = this.selected / this.perPage;
                this.rebuildWidgets();
            }

            ClientDuelAudio.playUi("SE_DUEL_SELECT");
            this.refreshButtons();
            return true;
        } else {
            return super.keyPressed(keyCode, scanCode, modifiers);
        }
    }

    public boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (button == 0) {
            int index = this.indexAt(mouseX, mouseY);
            if (index >= 0) {
                ClientDuelAudio.playUi("SE_DUEL_SELECT");
                this.selected = index;
                this.refreshButtons();
                return true;
            }
        }

        return super.mouseClicked(mouseX, mouseY, button);
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        McPanel.panel(graphics, this.panelLeft, this.panelTop, this.panelLeft + this.panelW, this.panelTop + this.panelH);
        graphics.drawCenteredString(this.font, this.opening ? "发动效果" : "连锁", this.panelLeft + this.panelW / 2, this.panelTop + 8, 4210752);
        if (!this.timing.isEmpty()) {
            List<FormattedCharSequence> lines = this.font.split(Component.literal(this.timing), this.panelW - 20);

            for (int i = 0; i < lines.size(); i++) {
                graphics.drawString(this.font, lines.get(i), this.panelLeft + 10, this.panelTop + 21 + i * 10, 3166330, false);
            }
        }

        if (this.forced) {
            graphics.drawString(this.font, this.opening ? "必须发动" : "必须接", this.panelLeft + 6, this.panelTop + 8, 10502144);
        }

        for (int i = this.page * this.perPage; i < Math.min(this.codes.length, (this.page + 1) * this.perPage); i++) {
            int x = this.gridLeft + i % this.cols * 60;
            int y = this.gridTop;
            McPanel.cardInSlot(graphics, this.codes[i], x, y, i == this.selected);
            if (i < this.addrs.length) {
                ClientCardMarkers.drawGuiMark(graphics, x, y, 54, 78, ClientCardMarkers.markAt(this.addrs[i]));
            }

            graphics.drawString(this.font, Integer.toString(i + 1), x + 3, y + 3, i == this.selected ? 16776960 : 16755200);
            String effectText = this.effectTextAt(i);
            if (!effectText.isEmpty()) {
                String text = effectText;
                int maxWidth = 62;

                while (this.font.width(text) > maxWidth && text.length() > 1) {
                    text = text.substring(0, text.length() - 1);
                }

                if (!text.equals(effectText)) {
                    text = text + "…";
                }

                graphics.drawString(this.font, text, x, y + 78 + 2, 3158064, false);
            } else if (this.opening) {
                graphics.drawString(this.font, "效果 " + (i + 1), x, y + 78 + 2, 7368816, false);
            }
        }

        String detail = this.selected >= 0 && this.selected < this.codes.length ? this.detailText(this.selected) : "选择候选卡，查看对应效果说明";
        List<FormattedCharSequence> detailLines = this.font.split(Component.literal(detail), this.panelW - 20);
        int available = Math.max(0, (this.panelTop + this.panelH - 32 - this.detailTop) / 10);

        for (int i = 0; i < Math.min(available, detailLines.size()); i++) {
            graphics.drawString(this.font, detailLines.get(i), this.panelLeft + 10, this.detailTop + i * 10, 3158064, false);
        }

        if (McPanel.inside(mouseX, mouseY, this.panelLeft + 8, this.detailTop, this.panelW - 16, available * 10)) {
            graphics.renderTooltip(this.font, detailLines, mouseX, mouseY);
        }

        int hovered = this.indexAt(mouseX, mouseY);
        if (hovered >= 0) {
            this.renderCardTooltip(graphics, hovered, mouseX, mouseY);
        }

        super.render(graphics, mouseX, mouseY, partialTick);
    }

    private String effectTextAt(int index) {
        if (index >= 0 && index < this.effectTexts.length && this.effectTexts[index] != null) {
            return this.effectTexts[index].isBlank() ? "" : this.effectTexts[index];
        } else {
            return "";
        }
    }

    private String cardDescription(int index) {
        return index >= 0 && index < this.codes.length && this.codes[index] > 0
            ? CardRegistry.get(this.codes[index]).map(CardInfo::desc).orElse("").strip()
            : "";
    }

    private String detailText(int index) {
        String effect = this.effectTextAt(index);
        if (!effect.isEmpty()) {
            return "效果 " + (index + 1) + "：" + effect;
        }

        String cardText = this.cardDescription(index);
        return !cardText.isEmpty() ? "效果 " + (index + 1) + "：这张卡没有单独的效果说明。卡面效果：" + cardText : "效果 " + (index + 1) + "：这张卡没有单独的效果说明";
    }

    private void renderCardTooltip(GuiGraphics graphics, int index, int mouseX, int mouseY) {
        List<FormattedCharSequence> lines = new ArrayList<>();
        lines.add(Component.literal(McPanel.displayName(this.codes[index])).getVisualOrderText());
        if (index < this.addrs.length) {
            lines.add(Component.literal(DuelLocation.tooltipLine(this.addrs[index], this.viewerSeat)).getVisualOrderText());
            String mark = DuelMarks.label(ClientCardMarkers.markAt(this.addrs[index]));
            if (!mark.isEmpty()) {
                lines.add(Component.literal("状态：" + mark).getVisualOrderText());
            }
        }

        String effect = this.effectTextAt(index);
        if (!effect.isEmpty()) {
            lines.addAll(this.font.split(Component.literal("效果：" + effect), Math.min(360, this.width - 40)));
        } else {
            String cardText = this.cardDescription(index);
            if (!cardText.isEmpty()) {
                lines.addAll(this.font.split(Component.literal("卡面效果：" + cardText), Math.min(360, this.width - 40)));
            }
        }

        graphics.renderTooltip(this.font, lines, mouseX, mouseY);
    }

    public boolean isPauseScreen() {
        return false;
    }

    public boolean shouldCloseOnEsc() {
        return false;
    }
}
