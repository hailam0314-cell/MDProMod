package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

public class UnselectCardScreen extends Screen implements RequiredAnswer {
    private static final int COLS = 6;
    private static final int GAP = 4;
    private final int[] selectCodes;
    private final int[] chosenCodes;
    private final int[] selectAddrs;
    private final int[] chosenAddrs;
    private final int min;
    private final int max;
    private final int viewerSeat;
    private final boolean finishable;
    private final String hint;
    private int panelLeft;
    private int panelTop;
    private int panelW;
    private int panelH;

    public UnselectCardScreen(int[] selectCodes, int[] chosenCodes, int min, int max, boolean finishable) {
        this(selectCodes, chosenCodes, min, max, finishable, new int[0], new int[0], 0, "");
    }

    public UnselectCardScreen(int[] selectCodes, int[] chosenCodes, int min, int max, boolean finishable, int[] selectAddrs, int[] chosenAddrs) {
        this(selectCodes, chosenCodes, min, max, finishable, selectAddrs, chosenAddrs, 0, "");
    }

    public UnselectCardScreen(int[] selectCodes, int[] chosenCodes, int min, int max, boolean finishable, int[] selectAddrs, int[] chosenAddrs, int viewerSeat) {
        this(selectCodes, chosenCodes, min, max, finishable, selectAddrs, chosenAddrs, viewerSeat, "");
    }

    public UnselectCardScreen(
        int[] selectCodes, int[] chosenCodes, int min, int max, boolean finishable, int[] selectAddrs, int[] chosenAddrs, int viewerSeat, String hint
    ) {
        super(Component.literal("选择素材"));
        this.selectCodes = selectCodes == null ? new int[0] : selectCodes;
        this.chosenCodes = chosenCodes == null ? new int[0] : chosenCodes;
        this.selectAddrs = selectAddrs == null ? new int[0] : selectAddrs;
        this.chosenAddrs = chosenAddrs == null ? new int[0] : chosenAddrs;
        this.min = min;
        this.max = max;
        this.finishable = finishable;
        this.viewerSeat = viewerSeat;
        this.hint = hint == null ? "" : hint;
    }

    private int rows(int count) {
        return Math.max(1, (count + 6 - 1) / 6);
    }

    private int gridLeft() {
        return this.panelLeft + (this.panelW - 348) / 2 + 2;
    }

    private int selectTop() {
        return this.panelTop + (this.hint.isEmpty() ? 28 : 44);
    }

    private int chosenTop() {
        return this.selectTop() + this.rows(this.selectCodes.length) * 82 + 16;
    }

    protected void init() {
        this.panelW = Math.min(Math.max(this.width - 40, 320), 358);
        this.panelH = Math.min(Math.max(this.height - 40, 220), 36 + (this.rows(this.selectCodes.length) + this.rows(this.chosenCodes.length)) * 82 + 40);
        this.panelLeft = (this.width - this.panelW) / 2;
        this.panelTop = (this.height - this.panelH) / 2;
        this.addRenderableWidget(Button.builder(Component.literal("完成"), b -> {
            ModNetwork.sendToServer(new ModNetwork.UnselectPick(-1));
            this.onClose();
        }).bounds(this.panelLeft + this.panelW / 2 - 110, this.panelTop + this.panelH - 24, 220, 18).build());
    }

    public boolean mouseClicked(double mx, double my, int button) {
        if (button != 0) {
            return super.mouseClicked(mx, my, button);
        }

        int left = this.gridLeft();

        for (int i = 0; i < this.selectCodes.length; i++) {
            int x = left + i % 6 * 58;
            int y = this.selectTop() + i / 6 * 82;
            if (McPanel.inside(mx, my, x, y, 54, 78)) {
                ModNetwork.sendToServer(new ModNetwork.UnselectPick(i));
                this.onClose();
                return true;
            }
        }

        for (int i = 0; i < this.chosenCodes.length; i++) {
            int x = left + i % 6 * 58;
            int y = this.chosenTop() + i / 6 * 82;
            if (McPanel.inside(mx, my, x, y, 54, 78)) {
                ModNetwork.sendToServer(new ModNetwork.UnselectPick(this.selectCodes.length + i));
                this.onClose();
                return true;
            }
        }

        return super.mouseClicked(mx, my, button);
    }

    public void render(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(g);
        McPanel.panel(g, this.panelLeft, this.panelTop, this.panelLeft + this.panelW, this.panelTop + this.panelH);
        g.drawCenteredString(
            this.font, "选择素材 " + this.chosenCodes.length + " / " + this.min + ".." + this.max, this.panelLeft + this.panelW / 2, this.panelTop + 8, 4210752
        );
        if (!this.hint.isEmpty()) {
            g.drawCenteredString(this.font, this.hint, this.panelLeft + this.panelW / 2, this.panelTop + 22, 3166330);
        }

        int left = this.gridLeft();

        for (int i = 0; i < this.selectCodes.length; i++) {
            this.drawCard(g, this.selectCodes[i], this.selectAddrs, i, left + i % 6 * 58, this.selectTop() + i / 6 * 82, false);
        }

        for (int i = 0; i < this.chosenCodes.length; i++) {
            this.drawCard(g, this.chosenCodes[i], this.chosenAddrs, i, left + i % 6 * 58, this.chosenTop() + i / 6 * 82, true);
        }

        super.render(g, mouseX, mouseY, partialTick);
    }

    private void drawCard(GuiGraphics g, int code, int[] addrs, int i, int x, int y, boolean picked) {
        McPanel.cardInSlot(g, code, x, y, picked);
        if (i < addrs.length) {
            ClientCardMarkers.drawGuiMark(g, x, y, 54, 78, ClientCardMarkers.markAt(addrs[i]));
        }
    }

    public boolean isPauseScreen() {
        return false;
    }

    public boolean shouldCloseOnEsc() {
        return this.finishable;
    }
}
