package net.mdpromod;

import java.util.ArrayList;
import java.util.List;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraft.util.FormattedCharSequence;

public class OptionScreen extends Screen implements RequiredAnswer {
    private static final int MIN_BOX_W = 320;
    private final int[] values;
    private final String[] labels;
    private final List<Button> optionButtons = new ArrayList<>();

    public OptionScreen(int[] values, String[] labels) {
        super(Component.literal("选择选项"));
        this.values = values == null ? new int[0] : values;
        this.labels = labels == null ? new String[0] : labels;
    }

    protected void init() {
        this.optionButtons.clear();
        int boxWidth = this.boxWidth();
        int left = (this.width - boxWidth) / 2;
        int top = this.height / 2 - 20 - this.values.length * 12;
        int y = top + 30;

        for (int i = 0; i < this.values.length; i++) {
            int value = this.values[i];
            Button button = Button.builder(Component.literal(this.fittedLabel(i)), b -> {
                ModNetwork.sendToServer(new ModNetwork.OptionAnswer(value));
                this.onClose();
            }).bounds(left + 20, y, boxWidth - 40, 20).build();
            this.optionButtons.add(button);
            this.addRenderableWidget(button);
            y += 24;
        }
    }

    private String fittedLabel(int index) {
        String text = this.label(index);
        int max = Math.max(40, this.boxWidth() - 56);
        if (this.font.width(text) <= max) {
            return text;
        }

        String cut = text;

        while (!cut.isEmpty() && this.font.width(cut + "…") > max) {
            cut = cut.substring(0, cut.length() - 1);
        }

        return cut + "…";
    }

    private String label(int index) {
        String text = index < this.labels.length ? this.labels[index] : "";
        return text != null && !text.isBlank() ? text : "选项 " + (index + 1);
    }

    private int boxWidth() {
        int widest = 0;

        for (int i = 0; i < this.values.length; i++) {
            widest = Math.max(widest, this.font.width(this.label(i)));
        }

        return Math.max(320, Math.min(this.width - 20, widest + 60));
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        int boxH = 46 + this.values.length * 24;
        int boxWidth = this.boxWidth();
        int left = (this.width - boxWidth) / 2;
        int top = this.height / 2 - 20 - this.values.length * 12;
        McPanel.panel(graphics, left, top, left + boxWidth, top + boxH);
        graphics.drawCenteredString(this.font, "选择选项", this.width / 2, top + 8, 4210752);
        super.render(graphics, mouseX, mouseY, partialTick);

        for (int i = 0; i < this.optionButtons.size(); i++) {
            if (this.optionButtons.get(i).isHovered()) {
                List<FormattedCharSequence> lines = this.font.split(Component.literal(this.label(i)), Math.min(400, this.width - 40));
                graphics.renderTooltip(this.font, lines, mouseX, mouseY);
                break;
            }
        }
    }

    public boolean isPauseScreen() {
        return false;
    }

    public boolean shouldCloseOnEsc() {
        return false;
    }
}
