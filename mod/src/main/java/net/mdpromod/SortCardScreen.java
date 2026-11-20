package net.mdpromod;

import java.util.ArrayList;
import java.util.List;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

public class SortCardScreen extends Screen implements RequiredAnswer {
    private static final int GAP = 8;
    private final int[] codes;
    private final List<Integer> order = new ArrayList<>();
    private int panelLeft;
    private int panelTop;
    private int panelW;
    private int panelH;

    public SortCardScreen(int[] codes) {
        super(Component.literal("排序"));
        this.codes = codes == null ? new int[0] : codes;
    }

    private static String name(int code) {
        return McPanel.displayName(code);
    }

    protected void init() {
        this.panelW = Math.min(Math.max(this.width - 40, 320), this.codes.length * 62 + 24);
        this.panelH = Math.min(Math.max(this.height - 40, 180), 148);
        this.panelLeft = (this.width - this.panelW) / 2;
        this.panelTop = (this.height - this.panelH) / 2;
    }

    private int cardLeft() {
        int total = this.codes.length * 62 - 8;
        return this.panelLeft + (this.panelW - total) / 2;
    }

    private int cardTop() {
        return this.panelTop + 30;
    }

    private void pick(int index) {
        ClientDuelAudio.playUi("SE_DUEL_SELECT");
        if (!this.order.remove(Integer.valueOf(index))) {
            this.order.add(index);
        }

        if (this.order.size() == this.codes.length) {
            ClientDuelAudio.playUi("SE_DUEL_DECIDE");
            int[] result = this.order.stream().mapToInt(Integer::intValue).toArray();
            ModNetwork.sendToServer(new ModNetwork.SortOrder(result));
            this.onClose();
        }
    }

    public boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (button == 0) {
            for (int i = 0; i < this.codes.length; i++) {
                int x = this.cardLeft() + i * 62;
                int y = this.cardTop();
                if (McPanel.inside(mouseX, mouseY, x, y, 54, 78)) {
                    this.pick(i);
                    return true;
                }
            }
        }

        return super.mouseClicked(mouseX, mouseY, button);
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        McPanel.panel(graphics, this.panelLeft, this.panelTop, this.panelLeft + this.panelW, this.panelTop + this.panelH);
        graphics.drawCenteredString(this.font, "按想要的顺序依次点卡（再点一次取消）", this.panelLeft + this.panelW / 2, this.panelTop + 8, 4210752);

        for (int i = 0; i < this.codes.length; i++) {
            int x = this.cardLeft() + i * 62;
            int y = this.cardTop();
            boolean picked = this.order.contains(i);
            McPanel.cardInSlot(graphics, this.codes[i], x, y, picked);
            if (picked) {
                graphics.drawString(this.font, Integer.toString(this.order.indexOf(i) + 1), x + 2, y + 2, 16776960);
            }

            if (McPanel.inside(mouseX, mouseY, x, y, 54, 78)) {
                graphics.renderTooltip(this.font, Component.literal(name(this.codes[i])), mouseX, mouseY);
            }
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
