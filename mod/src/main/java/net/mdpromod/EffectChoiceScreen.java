package net.mdpromod;

import java.util.ArrayList;
import java.util.List;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraft.util.FormattedCharSequence;

public final class EffectChoiceScreen extends Screen implements RequiredAnswer {
    private final ModNetwork.EffectChoices choices;
    private int page;
    private int perPage;
    private int left;
    private int top;
    private int panelW;
    private int panelH;

    public EffectChoiceScreen(ModNetwork.EffectChoices choices) {
        super(Component.literal("选择发动的效果"));
        this.choices = choices;
    }

    protected void init() {
        this.panelW = Math.min(560, this.width - 20);
        this.perPage = Math.max(1, (this.height - 120) / 66);
        this.page = Math.min(this.page, this.pages() - 1);
        this.panelH = Math.min(this.height - 20, 90 + Math.min(this.perPage, this.choices.values().length) * 66);
        this.left = (this.width - this.panelW) / 2;
        this.top = (this.height - this.panelH) / 2;
        int start = this.page * this.perPage;

        for (int i = start; i < Math.min(start + this.perPage, this.choices.values().length); i++) {
            int index = i;
            int y = this.top + 40 + (i - start) * 66;
            this.addRenderableWidget(
                Button.builder(Component.literal("发动效果 " + (i + 1)), button -> this.pick(this.choices.values()[index]))
                    .bounds(this.left + 10, y, 90, 20)
                    .build()
            );
        }

        int bottom = this.top + this.panelH - 26;
        ((Button)this.addRenderableWidget(Button.builder(Component.literal("上一页"), b -> this.turn(-1)).bounds(this.left + 10, bottom, 60, 20).build())).active = this.page
            > 0;
        ((Button)this.addRenderableWidget(Button.builder(Component.literal("下一页"), b -> this.turn(1)).bounds(this.left + 76, bottom, 60, 20).build())).active = this.page
                + 1
            < this.pages();
        this.addRenderableWidget(Button.builder(Component.literal("返回"), b -> this.pick(-1)).bounds(this.left + this.panelW - 70, bottom, 60, 20).build());
    }

    private int pages() {
        return Math.max(1, (this.choices.values().length + Math.max(1, this.perPage) - 1) / Math.max(1, this.perPage));
    }

    private String labelAt(int index) {
        return this.hasOwnLabel(index) ? this.choices.labels()[index] : "效果 " + (index + 1) + "（无单独说明）";
    }

    private boolean hasOwnLabel(int index) {
        return index >= 0 && index < this.choices.labels().length && this.choices.labels()[index] != null && !this.choices.labels()[index].isBlank();
    }

    private String cardText() {
        return CardRegistry.get(this.choices.code()).map(CardInfo::desc).orElse("").strip();
    }

    private void turn(int delta) {
        this.page = Math.max(0, Math.min(this.pages() - 1, this.page + delta));
        this.rebuildWidgets();
    }

    private void pick(int value) {
        ModNetwork.sendToServer(new ModNetwork.EffectPick(this.choices.seq(), value));
        this.onClose();
    }

    public boolean mouseScrolled(double x, double y, double delta) {
        this.turn(-((int)Math.signum(delta)));
        return true;
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        McPanel.panel(graphics, this.left, this.top, this.left + this.panelW, this.top + this.panelH);
        graphics.drawCenteredString(this.font, "选择「" + McPanel.displayName(this.choices.code()) + "」的效果", this.width / 2, this.top + 8, 4210752);
        graphics.drawCenteredString(this.font, "第 " + (this.page + 1) + "/" + this.pages() + " 页 · 悬停说明可查看全文", this.width / 2, this.top + 23, 6316128);

        for (int i = this.page * this.perPage; i < Math.min((this.page + 1) * this.perPage, this.choices.values().length); i++) {
            int y = this.top + 40 + i % this.perPage * 66;
            String text = this.labelAt(i);
            List<FormattedCharSequence> lines = this.font.split(Component.literal(text), Math.max(40, this.panelW - 122));

            for (int row = 0; row < Math.min(5, lines.size()); row++) {
                graphics.drawString(this.font, lines.get(row), this.left + 108, y + row * 10, 3158064, false);
            }

            if (McPanel.inside(mouseX, mouseY, this.left + 102, y, this.panelW - 112, 56)) {
                List<FormattedCharSequence> tooltip = new ArrayList<>(lines);
                if (!this.hasOwnLabel(i)) {
                    String cardText = this.cardText();
                    if (!cardText.isEmpty()) {
                        tooltip.addAll(this.font.split(Component.literal("卡面效果：" + cardText), Math.min(360, this.width - 40)));
                    }
                }

                graphics.renderTooltip(this.font, tooltip, mouseX, mouseY);
            }
        }

        super.render(graphics, mouseX, mouseY, partialTick);
    }

    public boolean shouldCloseOnEsc() {
        return false;
    }

    public boolean isPauseScreen() {
        return false;
    }
}
