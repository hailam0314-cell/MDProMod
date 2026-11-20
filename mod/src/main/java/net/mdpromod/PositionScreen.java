package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

public class PositionScreen extends Screen implements RequiredAnswer {
    private final int[] values;
    private final String[] labels;

    public PositionScreen(int[] values, String[] labels) {
        super(Component.literal("选择表示形式"));
        this.values = values == null ? new int[0] : values;
        this.labels = labels == null ? new String[0] : labels;
    }

    protected void init() {
        int boxW = 240;
        int left = (this.width - boxW) / 2;
        int top = this.height / 2 - 30;
        int y = top + 30;

        for (int i = 0; i < this.values.length; i++) {
            int value = this.values[i];
            this.addRenderableWidget(Button.builder(Component.literal(this.labels[i]), button -> {
                ModNetwork.sendToServer(new ModNetwork.PositionAnswer(value));
                this.onClose();
            }).bounds(left + 20, y, boxW - 40, 20).build());
            y += 24;
        }
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        int boxW = 240;
        int boxH = 46 + this.values.length * 24;
        int left = (this.width - boxW) / 2;
        int top = this.height / 2 - 30;
        McPanel.panel(graphics, left, top, left + boxW, top + boxH);
        graphics.drawCenteredString(this.font, "选择表示形式", this.width / 2, top + 8, 4210752);
        super.render(graphics, mouseX, mouseY, partialTick);
    }

    public boolean isPauseScreen() {
        return false;
    }

    public boolean shouldCloseOnEsc() {
        return false;
    }
}
