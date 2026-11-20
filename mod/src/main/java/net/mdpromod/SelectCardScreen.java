package net.mdpromod;

import java.util.ArrayList;
import java.util.List;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraft.util.FormattedCharSequence;

public class SelectCardScreen extends Screen implements RequiredAnswer {
    private static final int COLS = 6;
    private static final int GAP = 4;
    private final int[] codes;
    private final int[] addrs;
    private final int viewerSeat;
    private final String hint;
    private final boolean cancelable;
    private final boolean fieldFirst;
    private final int min;
    private final int max;
    private final List<Integer> chosen = new ArrayList<>();
    private int page;
    private int rows = 3;
    private int panelLeft;
    private int panelTop;
    private int panelW;
    private int panelH;
    private int gridLeft;
    private int gridTop;

    public SelectCardScreen(int[] codes, int min, int max) {
        this(codes, min, max, new int[0]);
    }

    public SelectCardScreen(int[] codes, int min, int max, int[] addrs) {
        this(codes, min, max, addrs, 0);
    }

    public SelectCardScreen(int[] codes, int min, int max, int[] addrs, int viewerSeat) {
        this(codes, min, max, addrs, viewerSeat, "");
    }

    public SelectCardScreen(int[] codes, int min, int max, int[] addrs, int viewerSeat, String hint) {
        this(codes, min, max, addrs, viewerSeat, hint, false, false);
    }

    public SelectCardScreen(int[] codes, int min, int max, int[] addrs, int viewerSeat, String hint, boolean cancelable, boolean fieldFirst) {
        super(Component.literal("选择卡片"));
        this.codes = codes == null ? new int[0] : codes;
        this.addrs = addrs == null ? new int[0] : addrs;
        this.viewerSeat = viewerSeat;
        this.hint = hint == null ? "" : hint;
        this.cancelable = cancelable;
        this.fieldFirst = cancelable && fieldFirst;
        this.min = Math.max(0, min);
        this.max = Math.max(this.min, max);
    }

    private int pages() {
        int perPage = 6 * Math.max(1, this.rows);
        return Math.max(1, (this.codes.length + perPage - 1) / perPage);
    }

    protected void init() {
        this.rows = Math.max(1, (this.height - (this.hint.isEmpty() ? 100 : 120)) / 82);
        this.panelW = Math.min(Math.max(this.width - 40, 300), 358);
        this.panelH = Math.min(Math.max(this.height - 40, 200), this.rows * 82 + (this.hint.isEmpty() ? 64 : 80));
        this.panelLeft = (this.width - this.panelW) / 2;
        this.panelTop = (this.height - this.panelH) / 2;
        this.gridLeft = this.panelLeft + (this.panelW - 348) / 2 + 2;
        this.gridTop = this.panelTop + (this.hint.isEmpty() ? 26 : 42);
        int bottom = this.panelTop + this.panelH - 24;
        this.addRenderableWidget(
            Button.builder(Component.literal("上一页"), button -> this.page = Math.max(0, this.page - 1))
                .bounds(this.panelLeft + 8, bottom, 48, 18)
                .build()
        );
        this.addRenderableWidget(
            Button.builder(Component.literal("下一页"), button -> this.page = Math.min(this.pages() - 1, this.page + 1))
                .bounds(this.panelLeft + 60, bottom, 48, 18)
                .build()
        );
        this.addRenderableWidget(
            Button.builder(Component.literal("确认"), button -> this.confirm()).bounds(this.panelLeft + this.panelW - 78, bottom, 70, 18).build()
        );
        if (this.cancelable) {
            this.addRenderableWidget(
                Button.builder(Component.literal(this.fieldFirst ? "选择其他区域" : "取消选择"), button -> this.cancelSelection())
                    .bounds(this.panelLeft + this.panelW - 186, bottom, 104, 18)
                    .build()
            );
        }
    }

    private void cancelSelection() {
        if (this.cancelable) {
            ModNetwork.sendToServer(new ModNetwork.SelectChoose(new int[]{-1}));
            this.onClose();
        }
    }

    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        this.page = Math.max(0, Math.min(this.pages() - 1, this.page - (int)Math.signum(delta)));
        return true;
    }

    private void toggle(int index) {
        ClientDuelAudio.playUi("SE_DUEL_SELECT");
        if (!this.chosen.remove(Integer.valueOf(index))) {
            if (this.chosen.size() >= this.max) {
                return;
            }

            this.chosen.add(index);
        }

        if (this.chosen.size() >= this.max) {
            this.confirm();
        }
    }

    private void confirm() {
        if (this.chosen.size() >= this.min) {
            ClientDuelAudio.playUi("SE_DUEL_DECIDE");
            int[] indices = this.chosen.stream().mapToInt(Integer::intValue).toArray();
            ModNetwork.sendToServer(new ModNetwork.SelectChoose(indices));
            this.onClose();
        }
    }

    public boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (button == 0) {
            int perPage = 6 * Math.max(1, this.rows);
            int start = this.page * perPage;

            for (int i = 0; i < perPage && start + i < this.codes.length; i++) {
                int x = this.gridLeft + i % 6 * 58;
                int y = this.gridTop + i / 6 * 82;
                if (McPanel.inside(mouseX, mouseY, x, y, 54, 78)) {
                    this.toggle(start + i);
                    return true;
                }
            }
        }

        return super.mouseClicked(mouseX, mouseY, button);
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        McPanel.panel(graphics, this.panelLeft, this.panelTop, this.panelLeft + this.panelW, this.panelTop + this.panelH);
        graphics.drawCenteredString(
            this.font,
            "选择卡片　已选 " + this.chosen.size() + " / " + this.min + ".." + this.max + "　（第 " + (this.page + 1) + "/" + this.pages() + " 页）",
            this.panelLeft + this.panelW / 2,
            this.panelTop + 8,
            4210752
        );
        if (!this.hint.isEmpty()) {
            graphics.drawCenteredString(this.font, this.hint, this.panelLeft + this.panelW / 2, this.panelTop + 22, 3166330);
        }

        int perPage = 6 * Math.max(1, this.rows);
        int start = this.page * perPage;

        for (int i = 0; i < perPage && start + i < this.codes.length; i++) {
            int x = this.gridLeft + i % 6 * 58;
            int y = this.gridTop + i / 6 * 82;
            boolean picked = this.chosen.contains(start + i);
            McPanel.cardInSlot(graphics, this.codes[start + i], x, y, picked);
            if (start + i < this.addrs.length) {
                ClientCardMarkers.drawGuiMark(graphics, x, y, 54, 78, ClientCardMarkers.markAt(this.addrs[start + i]));
            }

            if (picked) {
                graphics.drawString(this.font, "✔", x + 54 - 9, y + 2, 16776960);
            }
        }

        for (int i = 0; i < perPage && start + i < this.codes.length; i++) {
            int x = this.gridLeft + i % 6 * 58;
            int y = this.gridTop + i / 6 * 82;
            if (McPanel.inside(mouseX, mouseY, x, y, 54, 78)) {
                int index = start + i;
                int code = this.codes[index];
                String name = McPanel.displayName(code);
                if (index < this.addrs.length) {
                    List<FormattedCharSequence> lines = new ArrayList<>(
                        List.of(
                            Component.literal(name).getVisualOrderText(), Component.literal(DuelLocation.tooltipLine(this.addrs[index], this.viewerSeat)).getVisualOrderText()
                        )
                    );
                    String mark = DuelMarks.label(ClientCardMarkers.markAt(this.addrs[index]));
                    if (!mark.isEmpty()) {
                        lines.add(Component.literal("状态：" + mark).getVisualOrderText());
                    }

                    graphics.renderTooltip(this.font, lines, mouseX, mouseY);
                } else {
                    graphics.renderTooltip(this.font, Component.literal(name), mouseX, mouseY);
                }
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
