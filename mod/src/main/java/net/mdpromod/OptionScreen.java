package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

/** 选项弹窗（MC 风格）：核心给了若干个处理方式，选一个。 */
public class OptionScreen extends Screen {
    private static final int MIN_BOX_W = 320;
    private final int[] values;
    private final String[] labels;

    public OptionScreen(int[] values, String[] labels) {
        super(Component.literal("选择选项"));
        this.values = values == null ? new int[0] : values;
        this.labels = labels == null ? new String[0] : labels;
    }

    @Override
    protected void init() {
        int boxWidth = boxWidth();
        int left = (width - boxWidth) / 2;
        int top = height / 2 - 20 - values.length * 12;
        int y = top + 30;
        for (int i = 0; i < values.length; i++) {
            final int value = values[i];
            String text = label(i);
            addRenderableWidget(Button.builder(Component.literal(text), button -> {
                ModNetwork.sendToServer(new ModNetwork.OptionAnswer(value));
                onClose();
            }).bounds(left + 20, y, boxWidth - 40, 20).build());
            y += 24;
        }
    }

    /** 标签为空时给个可点的占位，别让按钮变成空白。 */
    private String label(int index) {
        String text = index < labels.length ? labels[index] : "";
        return text == null || text.isBlank() ? "选项 " + (index + 1) : text;
    }

    /** 选项文字可能很长（卡脚本原文），窗口按最长的一条自适应，避免文字溢出按钮。 */
    private int boxWidth() {
        int widest = 0;
        for (int i = 0; i < values.length; i++) {
            widest = Math.max(widest, font.width(label(i)));
        }
        return Math.max(MIN_BOX_W, Math.min(width - 20, widest + 60));
    }

    @Override
    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        renderBackground(graphics);
        int boxH = 46 + values.length * 24;
        int boxWidth = boxWidth();
        int left = (width - boxWidth) / 2;
        int top = height / 2 - 20 - values.length * 12;
        McPanel.panel(graphics, left, top, left + boxWidth, top + boxH);
        graphics.drawCenteredString(font, "选择选项", width / 2, top + 8, 0x404040);
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
