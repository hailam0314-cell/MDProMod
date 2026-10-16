package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

/** "是否发动 xxx 的效果？"这类是和否的决定。 */
public class DecisionScreen extends Screen {
    private final int function;
    private final String cardName;
    private final String question;

    public DecisionScreen(int function, String cardName, String question) {
        super(Component.literal("决斗"));
        this.function = function;
        this.cardName = cardName == null ? "" : cardName;
        this.question = question == null ? "" : question;
    }

    @Override
    protected void init() {
        int y = height / 2 - 10;
        int half = 70;
        addRenderableWidget(Button.builder(Component.literal("是"), button -> {
            ModNetwork.sendToServer(new ModNetwork.DecisionAnswer(1));
            onClose();
        }).bounds(width / 2 - half - 6, y + 18, half, 20).build());
        addRenderableWidget(Button.builder(Component.literal("否"), button -> {
            ModNetwork.sendToServer(new ModNetwork.DecisionAnswer(0));
            onClose();
        }).bounds(width / 2 + 6, y + 18, half, 20).build());
    }

    @Override
    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        renderBackground(graphics);
        int boxW = Math.max(220, font.width(question) + 40);
        int left = width / 2 - boxW / 2;
        int top = height / 2 - 40;
        McPanel.panel(graphics, left, top, left + boxW, top + 74);
        graphics.drawCenteredString(font, "发动确认", width / 2, top + 8, 0x404040);
        graphics.drawCenteredString(font, question, width / 2, top + 30, 0x000000);
        if (!cardName.isEmpty()) {
            graphics.drawCenteredString(font, "「" + cardName + "」", width / 2, top + 46, 0x0000A0);
        }
        super.render(graphics, mouseX, mouseY, partialTick);
    }

    /** 这种弹窗不该暂停游戏：对手和引擎都在等。 */
    @Override
    public boolean isPauseScreen() {
        return false;
    }

    @Override
    public boolean shouldCloseOnEsc() {
        return false;
    }
}
