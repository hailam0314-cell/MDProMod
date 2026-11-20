package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

public class TpScreen extends Screen implements RequiredAnswer {
    public TpScreen() {
        super(Component.literal("选择先后手"));
    }

    protected void init() {
        int y = this.height / 2 + 2;
        this.addRenderableWidget(Button.builder(Component.literal("先攻"), b -> this.pick(true)).bounds(this.width / 2 - 90, y, 84, 20).build());
        this.addRenderableWidget(Button.builder(Component.literal("后攻"), b -> this.pick(false)).bounds(this.width / 2 + 6, y, 84, 20).build());
    }

    private void pick(boolean first) {
        ModNetwork.sendToServer(new ModNetwork.TpPick(first));
        ClientDuelAudio.playUi("SE_DUEL_DECIDE");
        this.onClose();
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        int boxW = Math.min(320, this.width - 20);
        int boxH = 84;
        int left = (this.width - boxW) / 2;
        int top = this.height / 2 - 42;
        McPanel.panel(graphics, left, top, left + boxW, top + boxH);
        graphics.drawCenteredString(this.font, "你赢了猜拳", this.width / 2, top + 10, 4210752);
        graphics.drawCenteredString(this.font, "要先攻还是后攻？", this.width / 2, top + 24, 6316128);
        super.render(graphics, mouseX, mouseY, partialTick);
    }

    public boolean isPauseScreen() {
        return false;
    }

    public boolean shouldCloseOnEsc() {
        return false;
    }
}
