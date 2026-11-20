package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

public class RpsScreen extends Screen implements RequiredAnswer {
    public RpsScreen() {
        super(Component.literal("猜拳"));
    }

    protected void init() {
        int w = 100;
        int h = 20;
        int gap = 8;
        int total = 3 * w + 2 * gap;
        int left = (this.width - total) / 2;
        int y = this.height / 2 + 2;
        this.addRenderableWidget(Button.builder(Component.literal("石头"), b -> this.pick(1)).bounds(left, y, w, h).build());
        this.addRenderableWidget(Button.builder(Component.literal("剪刀"), b -> this.pick(2)).bounds(left + w + gap, y, w, h).build());
        this.addRenderableWidget(Button.builder(Component.literal("布"), b -> this.pick(3)).bounds(left + (w + gap) * 2, y, w, h).build());
    }

    private void pick(int hand) {
        ModNetwork.sendToServer(new ModNetwork.RpsPick(hand));
        ClientDuelAudio.playUi("SE_DUEL_DECIDE");
        this.onClose();
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        int boxW = Math.min(380, this.width - 20);
        int boxH = 92;
        int left = (this.width - boxW) / 2;
        int top = this.height / 2 - 46;
        McPanel.panel(graphics, left, top, left + boxW, top + boxH);
        graphics.drawCenteredString(this.font, "猜拳 · 赢的人选先后手", this.width / 2, top + 10, 4210752);
        graphics.drawCenteredString(this.font, "两边同时出，对方看不到你出的是什么", this.width / 2, top + 24, 6316128);
        super.render(graphics, mouseX, mouseY, partialTick);
    }

    public boolean isPauseScreen() {
        return false;
    }

    public boolean shouldCloseOnEsc() {
        return false;
    }
}
