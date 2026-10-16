package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

import java.util.ArrayList;
import java.util.List;

/**
 * 卡堆界面（MC 弹窗风格）：**所有卡都在一页里**，滚轮上下翻看；
 * 现在能特殊召唤 / 能发动的卡排在**最前面**，并描一圈金框；下面固定两个按钮：
 * 「特殊召唤」（或「发动」）和「关闭」。
 *
 * <p>操作：左键点一张卡选中它（白框，选中能召唤的卡时下面那个按钮才可按），再按「特殊召唤」；
 * 右键点卡看大图（卡面 + 效果文本）。以前是分页网格 + 一行最多三个动作按钮，
 * 第 4 张之后根本点不到，所以改成现在这样。
 */
public class PileScreen extends Screen {
    private static final int GAP = 4;
    /** 标题条 / 提示行 / 底部工具条的高度。 */
    private static final int TITLE_H = 22;
    private static final int HINT_H = 14;
    private static final int FOOTER_H = 30;
    private static final int BUTTON_H = 20;

    private final int location;
    /** 能召唤/发动的卡：序号（= 在卡堆里的位置）与卡号。 */
    private final int[] actionSeqs;
    private final int[] actionCodes;
    /** 排序后的全部卡（能召唤的在前）：序号与卡号一一对应。 */
    private final int[] codes;
    private final int[] seqs;

    private int selectedSeq = -1;
    private int scroll;
    private int cols = 6;
    private int visibleRows = 3;
    private int panelLeft;
    private int panelTop;
    private int panelW;
    private int panelH;
    private int hintTop;
    private int listLeft;
    private int listTop;
    private int listW;
    private int listH;
    private Button actionButton;

    public PileScreen(int location, int[] actionSeqs, int[] actionCodes, int[] allCodes) {
        super(Component.literal(title(location)));
        this.location = location;
        this.actionSeqs = actionSeqs == null ? new int[0] : actionSeqs;
        this.actionCodes = actionCodes == null ? new int[0] : actionCodes;
        List<PileOrder.Entry> entries = PileOrder.sorted(this.actionSeqs, this.actionCodes, allCodes);
        this.seqs = new int[entries.size()];
        this.codes = new int[entries.size()];
        for (int i = 0; i < entries.size(); i++) {
            this.seqs[i] = entries.get(i).sequence();
            this.codes[i] = entries.get(i).code();
        }
    }

    private static String title(int location) {
        return switch (location) {
            case 64 -> "额外卡组";
            case 16 -> "墓地";
            default -> "除外区";
        };
    }

    /** 额外卡组是「特殊召唤」，墓地 / 除外是「发动」。 */
    private String verb() {
        return location == 64 ? "特殊召唤" : "发动";
    }

    private static String name(int code) {
        return McPanel.displayName(code);
    }

    /** 第 index 张（排序后的下标）现在能不能动。 */
    private boolean actionable(int index) {
        return actionSeq(index) >= 0;
    }

    /** 第 index 张对应的动作序号；不能动就返回 -1。 */
    private int actionSeq(int index) {
        int sequence = seqs[index];
        for (int i = 0; i < actionSeqs.length; i++) {
            if (actionSeqs[i] == sequence) return sequence;
        }
        return -1;
    }

    private int totalRows() {
        return Math.max(1, (codes.length + cols - 1) / cols);
    }

    private int maxScroll() {
        return Math.max(0, totalRows() - Math.max(1, visibleRows));
    }

    private int cardX(int index) {
        return listLeft + (index % cols) * (McPanel.CARD_W + GAP);
    }

    private int cardY(int index) {
        return listTop + (index / cols - scroll) * (McPanel.CARD_H + GAP);
    }

    /** 鼠标下面是第几张（没落在卡上返回 -1）。 */
    private int cardAt(double mouseX, double mouseY) {
        for (int i = 0; i < codes.length; i++) {
            int y = cardY(i);
            if (y < listTop || y + McPanel.CARD_H > listTop + listH) continue;
            if (McPanel.inside(mouseX, mouseY, cardX(i), y, McPanel.CARD_W, McPanel.CARD_H)) return i;
        }
        return -1;
    }

    /** 选中的那张能不能召唤：不能就禁用下面那个按钮。 */
    private void refreshActionButton() {
        if (actionButton == null) return;
        actionButton.active = selectedSeq >= 0 && actionAvailable(selectedSeq);
    }

    private boolean actionAvailable(int sequence) {
        for (int value : actionSeqs) {
            if (value == sequence) return true;
        }
        return false;
    }

    private void summon() {
        if (selectedSeq < 0 || !actionAvailable(selectedSeq)) return;
        ClientDuelAudio.playUi("SE_DUEL_DECIDE");
        ModNetwork.sendToServer(new ModNetwork.PileAct(location, selectedSeq));
        onClose();
    }

    @Override
    protected void init() {
        panelW = Math.min(Math.max(this.width - 24, 340), 640);
        panelH = Math.min(Math.max(this.height - 24, 220), 430);
        panelLeft = (this.width - panelW) / 2;
        panelTop = (this.height - panelH) / 2;
        hintTop = panelTop + TITLE_H;
        listLeft = panelLeft + 10;
        listW = panelW - 20 - 8;                 // 右边留一条滚动条
        listTop = hintTop + HINT_H + 4;
        listH = panelTop + panelH - FOOTER_H - listTop;
        cols = Math.max(3, (listW + GAP) / (McPanel.CARD_W + GAP));
        visibleRows = Math.max(1, (listH + GAP) / (McPanel.CARD_H + GAP));
        scroll = Math.min(scroll, maxScroll());

        // 工具条在底部右侧：主按钮在左、关闭在最右
        int bottom = panelTop + panelH - BUTTON_H - 6;
        int right = panelLeft + panelW - 10;
        addRenderableWidget(Button.builder(Component.literal("关闭"), button -> onClose())
                .bounds(right - 70, bottom, 70, BUTTON_H).build());
        actionButton = addRenderableWidget(Button.builder(
                        Component.literal(verb()), button -> summon())
                .bounds(right - 70 - 8 - 110, bottom, 110, BUTTON_H).build());
        refreshActionButton();
    }

    @Override
    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        renderBackground(graphics);
        McPanel.panel(graphics, panelLeft, panelTop, panelLeft + panelW, panelTop + panelH);
        int ready = 0;
        for (int i = 0; i < codes.length; i++) if (actionable(i)) ready++;
        drawTitleBar(graphics, ready);
        String hint = "滚轮翻看 · 左键选中 · 右键看大图";
        graphics.drawString(font, hint, listLeft, hintTop + 2, 0x707070, false);
        if (selectedSeq >= 0) {
            // "已选"写在提示行右边（那一行本来就空着），底部工具条留给张数和按钮
            int x = listLeft + font.width(hint) + 14;
            int room = panelLeft + panelW - 10 - x;
            if (room > 60) {
                graphics.drawString(font, trim("已选：" + name(codeOf(selectedSeq)), room),
                        x, hintTop + 2, 0x1F5F1F, false);
            }
        }

        // 卡图网格（视口裁剪）
        graphics.enableScissor(listLeft - 2, listTop - 2, listLeft + listW + 8, listTop + listH + 2);
        int hovered = cardAt(mouseX, mouseY);
        for (int i = 0; i < codes.length; i++) {
            int y = cardY(i);
            if (y + McPanel.CARD_H < listTop || y > listTop + listH) continue;
            int x = cardX(i);
            McPanel.cardInSlot(graphics, codes[i], x, y, false);
            if (actionable(i)) {
                frame(graphics, x, y, 3, 2, McPanel.GOLD, 0xFF7A5A00);
            }
            if (seqs[i] == selectedSeq) {
                // 选中的：白框描在金框外面 + 右上角一个勾
                int out = actionable(i) ? 5 : 2;
                frame(graphics, x, y, out, 2, McPanel.LIGHT, 0xFF404040);
                graphics.drawString(font, "✔", x + McPanel.CARD_W - 10, y + 2, 0xFFFFFF, true);
            } else if (i == hovered) {
                // 悬停：轻轻盖一层白纱，鼠标在哪张一眼看得出来
                graphics.fill(x - 2, y - 2, x + McPanel.CARD_W + 2, y + McPanel.CARD_H + 2, 0x40FFFFFF);
            }
        }
        graphics.disableScissor();

        drawScrollbar(graphics);
        drawFooter(graphics);
        if (hovered >= 0) {
            List<net.minecraft.util.FormattedCharSequence> lines = new ArrayList<>();
            lines.add(Component.literal(name(codes[hovered])).getVisualOrderText());
            if (actionable(hovered)) {
                lines.add(Component.literal("现在可以" + verb()).getVisualOrderText());
            }
            if (seqs[hovered] == selectedSeq) {
                lines.add(Component.literal("已选中").getVisualOrderText());
            }
            lines.add(Component.literal("右键看大图").getVisualOrderText());
            graphics.renderTooltip(font, lines, mouseX, mouseY);
        }
        super.render(graphics, mouseX, mouseY, partialTick);
    }

    /** 标题条：凹进去一条，左边「额外卡组 + 张数」，右边一个状态标签。 */
    private void drawTitleBar(GuiGraphics graphics, int ready) {
        int x1 = panelLeft + 1;
        int x2 = panelLeft + panelW - 1;
        int y2 = panelTop + TITLE_H;
        graphics.fill(x1, panelTop + 1, x2, y2, McPanel.SLOT);
        graphics.fill(x1, panelTop + 1, x2, panelTop + 2, McPanel.SLOT_DARK);
        graphics.fill(x1, y2 - 1, x2, y2, McPanel.DARK);
        String title = title(location);
        graphics.drawString(font, title, x1 + 7, panelTop + 7, 0xFFFFFF, true);
        graphics.drawString(font, codes.length + " 张", x1 + 7 + font.width(title) + 8, panelTop + 7,
                0xD0D0D0, true);
        if (codes.length == 0) {
            tag(graphics, "这堆现在是空的", x2 - 7, panelTop + 6, McPanel.DARK, 0xD0D0D0);
        } else if (ready > 0) {
            tag(graphics, "可以" + verb() + " " + ready + " 张", x2 - 7, panelTop + 6, McPanel.GOLD, 0xFFE9E9E9);
        } else {
            tag(graphics, "现在没有可以" + verb() + "的卡", x2 - 7, panelTop + 6, McPanel.DARK, 0xB0B0B0);
        }
    }

    /** 深色小标签 + 一圈彩色描边（标题条右边那个）。 */
    private void tag(GuiGraphics graphics, String text, int rightX, int y, int border, int textColor) {
        int width = font.width(text) + 10;
        int x = rightX - width;
        graphics.fill(x, y, x + width, y + 12, 0xFF3C3C3C);
        graphics.fill(x, y, x + width, y + 1, border);
        graphics.fill(x, y + 11, x + width, y + 12, border);
        graphics.fill(x, y, x + 1, y + 12, border);
        graphics.fill(x + width - 1, y, x + width, y + 12, border);
        graphics.drawString(font, text, x + 5, y + 2, textColor, true);
    }

    /** 一张卡外面套一圈框（外扩 out、线宽 thick）；outer 不为 0 时再包一圈细边，立体一点。 */
    private void frame(GuiGraphics graphics, int x, int y, int out, int thick, int color, int outer) {
        if (outer != 0) {
            int bx0 = x - out - thick, by0 = y - out - thick;
            int bx1 = x + McPanel.CARD_W + out + thick, by1 = y + McPanel.CARD_H + out + thick;
            graphics.fill(bx0, by0, bx1, by0 + 1, outer);
            graphics.fill(bx0, by1 - 1, bx1, by1, outer);
            graphics.fill(bx0, by0, bx0 + 1, by1, outer);
            graphics.fill(bx1 - 1, by0, bx1, by1, outer);
        }
        int x0 = x - out, y0 = y - out;
        int x1 = x + McPanel.CARD_W + out, y1 = y + McPanel.CARD_H + out;
        graphics.fill(x0, y0, x1, y0 + thick, color);
        graphics.fill(x0, y1 - thick, x1, y1, color);
        graphics.fill(x0, y0, x0 + thick, y1, color);
        graphics.fill(x1 - thick, y0, x1, y1, color);
    }

    /** MC 风格的滚动条：深槽 + 亮滑块（只在真的需要滚动时出现）。 */
    private void drawScrollbar(GuiGraphics graphics) {
        if (maxScroll() <= 0) return;
        int barX = listLeft + listW + 3;
        int width = 6;
        graphics.fill(barX, listTop, barX + width, listTop + listH, 0xFF2E2E2E);
        graphics.fill(barX, listTop, barX + width, listTop + 1, McPanel.SLOT_DARK);
        int barH = Math.max(14, listH * Math.max(1, visibleRows) / totalRows());
        int barY = listTop + (listH - barH) * scroll / Math.max(1, maxScroll());
        graphics.fill(barX + 1, barY, barX + width - 1, barY + barH, 0xFFC0C0C0);
        graphics.fill(barX + 1, barY, barX + width - 1, barY + 1, McPanel.LIGHT);
        graphics.fill(barX + 1, barY + barH - 1, barX + width - 1, barY + barH, McPanel.DARK);
    }

    /** 底部工具条：左边写"看到第几张"，右边两个按钮（按钮是原版控件，在 init 里建）。 */
    private void drawFooter(GuiGraphics graphics) {
        int footerTop = panelTop + panelH - FOOTER_H;
        graphics.fill(panelLeft + 1, footerTop, panelLeft + panelW - 1, footerTop + 1, McPanel.DARK);
        String range;
        if (codes.length == 0) {
            range = "（空的）";
        } else {
            int first = Math.min(codes.length, scroll * cols + 1);
            int last = Math.min(codes.length, (scroll + Math.max(1, visibleRows)) * cols);
            range = "看到第 " + first + "–" + last + " 张 / 共 " + codes.length + " 张";
        }
        graphics.drawString(font, range, panelLeft + 10, footerTop + 12, 0x505050, false);
    }

    private int codeOf(int sequence) {
        for (int i = 0; i < seqs.length; i++) {
            if (seqs[i] == sequence) return codes[i];
        }
        return 0;
    }

    private String trim(String text, int width) {
        return font.width(text) <= width ? text : font.plainSubstrByWidth(text, Math.max(8, width - 6)) + "…";
    }

    @Override
    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        scroll = Math.max(0, Math.min(maxScroll(), scroll - (int) Math.signum(delta)));
        return true;
    }

    /** 左键选中 / 取消选中；右键看大图。 */
    @Override
    public boolean mouseClicked(double mouseX, double mouseY, int button) {
        int index = cardAt(mouseX, mouseY);
        if (index >= 0) {
            if (button == 0) {
                ClientDuelAudio.playUi("SE_DUEL_SELECT");
                selectedSeq = seqs[index] == selectedSeq ? -1 : seqs[index];
                refreshActionButton();
                return true;
            }
            if (button == 1) {
                ClientHandBridge.openCard(codes[index]);
                return true;
            }
        }
        return super.mouseClicked(mouseX, mouseY, button);
    }

    @Override
    public boolean isPauseScreen() {
        return false;
    }
}
