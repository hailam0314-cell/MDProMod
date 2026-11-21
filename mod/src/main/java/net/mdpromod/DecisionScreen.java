package net.mdpromod;

import java.util.List;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraft.util.FormattedCharSequence;

public class DecisionScreen extends Screen implements RequiredAnswer {
    private final int function;
    private final String cardName;
    private final String question;
    private int panelW;
    private int panelH;
    private int panelTop;

    public DecisionScreen(int function, String cardName, String question) {
        super(Component.literal("决斗"));
        this.function = function;
        this.cardName = cardName == null ? "" : cardName;
        this.question = question == null ? "" : question;
    }

    protected void init() {
        ClientDuelAudio.playUi("SE_DUEL_ACTIVE_POSSIBLE");
        this.panelW = Math.min(this.width - 20, Math.max(260, this.font.width(this.question) + 30));
        int lines = this.font.split(Component.literal(this.question), this.panelW - 24).size();
        this.panelH = Math.min(this.height - 20, 76 + lines * 10);
        this.panelTop = (this.height - this.panelH) / 2;
        int y = this.panelTop + this.panelH - 46;
        int half = 70;
        this.addRenderableWidget(Button.builder(Component.literal("是"), button -> {
            ModNetwork.sendToServer(new ModNetwork.DecisionAnswer(1));
            this.onClose();
        }).bounds(this.width / 2 - half - 6, y + 18, half, 20).build());
        this.addRenderableWidget(Button.builder(Component.literal("否"), button -> {
            ModNetwork.sendToServer(new ModNetwork.DecisionAnswer(0));
            this.onClose();
        }).bounds(this.width / 2 + 6, y + 18, half, 20).build());
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        int boxW = this.panelW;
        int left = this.width / 2 - boxW / 2;
        int top = this.panelTop;
        McPanel.panel(graphics, left, top, left + boxW, top + this.panelH);
        graphics.drawCenteredString(this.font, this.function == 12 ? "发动确认" : "决斗确认", this.width / 2, top + 8, 4210752);
        List<FormattedCharSequence> lines = this.font.split(Component.literal(this.question), this.panelW - 24);

        for (int i = 0; i < lines.size(); i++) {
            graphics.drawString(this.font, lines.get(i), left + 12, top + 28 + i * 10, 3158064, false);
        }

        super.render(graphics, mouseX, mouseY, partialTick);
    }

    public boolean isPauseScreen() {
        return false;
    }

    public boolean shouldCloseOnEsc() {
        return false;
    }
}
