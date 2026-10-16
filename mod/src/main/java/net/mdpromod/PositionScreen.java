package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

/** 召唤之后的"攻击表示 / 守备表示"选择（MC 风格小弹窗）。 */
public class PositionScreen extends Screen {
    private final int[] values;
    private final String[] labels;

    public PositionScreen(int[] values, String[] labels) {
        super(Component.literal("选择表示形式"));
        this.values = values == null ? new int[0] : values;
        this.labels = labels == null ? new String[0] : labels;
    }

    @Override
    protected void init() {
        int boxW = 240;
        int left = (width - boxW) / 2;
        int top = height / 2 - 30;
        int y = top + 30;
        for (int i = 0; i < values.length; i++) {
            final int value = values[i];
            addRenderableWidget(Button.builder(Component.literal(labels[i]), button -> {
                ModNetwork.sendToServer(new ModNetwork.PositionAnswer(value));
                onClose();
            }).bounds(left + 20, y, boxW - 40, 20).build());
            y += 24;
        }
    }

    @Override
    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        renderBackground(graphics);
        int boxW = 240;
        int boxH = 46 + values.length * 24;
        int left = (width - boxW) / 2;
        int top = height / 2 - 30;
        McPanel.panel(graphics, left, top, left + boxW, top + boxH);
        graphics.drawCenteredString(font, "选择表示形式", width / 2, top + 8, 0x404040);
        super.render(graphics, mouseX, mouseY, partialTick);
    }

    @Override
    public boolean isPauseScreen() {
        return false;
    }

    @Override
    public boolean shouldCloseOnEsc() {
        return false;
    }
}
