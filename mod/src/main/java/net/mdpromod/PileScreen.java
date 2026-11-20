package net.mdpromod;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.UUID;
import java.util.stream.IntStream;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraft.util.FormattedCharSequence;

public class PileScreen extends Screen {
    private static final int GAP = 18;
    private static final int CARD_MARGIN = 8;
    private static final int ACTIVATE_FRAME = -10662;
    private static final int SUMMON_FRAME = -7019521;
    private static final int FRAME_SHADOW = -14405315;
    private static final int TITLE_H = 22;
    private static final int HINT_H = 28;
    private static final int FOOTER_H = 30;
    private static final int BUTTON_H = 20;
    private final int location;
    private final int[] activateSeqs;
    private final int[] summonSeqs;
    private final int[] codes;
    private final int[] seqs;
    private final int hidden;
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
    private Button activateButton;
    private Button summonButton;
    private int actionsLeft;
    private final UUID sourceField;

    public PileScreen(int location, int[] activateSeqs, int[] activateCodes, int[] summonSeqs, int[] summonCodes, int[] allCodes, int hidden) {
        this(location, activateSeqs, activateCodes, summonSeqs, summonCodes, allCodes, hidden, null);
    }

    public PileScreen(int location, int[] activateSeqs, int[] activateCodes, int[] summonSeqs, int[] summonCodes, int[] allCodes, int hidden, UUID fieldId) {
        super(Component.literal(title(location)));
        this.sourceField = fieldId;
        this.location = location;
        this.activateSeqs = activateSeqs == null ? new int[0] : activateSeqs;
        this.summonSeqs = summonSeqs == null ? new int[0] : summonSeqs;
        int[] actionSeqs = IntStream.concat(Arrays.stream(this.activateSeqs), Arrays.stream(this.summonSeqs)).toArray();
        int[] actionCodes = IntStream.concat(
                Arrays.stream(activateCodes == null ? new int[0] : activateCodes), Arrays.stream(summonCodes == null ? new int[0] : summonCodes)
            )
            .toArray();
        List<PileOrder.Entry> entries = PileOrder.sorted(actionSeqs, actionCodes, allCodes);
        this.hidden = Math.max(0, hidden);
        this.seqs = new int[entries.size() + this.hidden];
        this.codes = new int[entries.size() + this.hidden];

        for (int i = 0; i < entries.size(); i++) {
            this.seqs[i] = entries.get(i).sequence();
            this.codes[i] = entries.get(i).code();
        }

        for (int i = 0; i < this.hidden; i++) {
            this.seqs[entries.size() + i] = -1;
            this.codes[entries.size() + i] = 0;
        }
    }

    private static String title(int location) {
        return switch (location) {
            case 16 -> "墓地";
            case 64 -> "额外卡组";
            default -> "除外区";
        };
    }

    private String verb() {
        return this.location == 64 ? "特殊召唤" : "操作";
    }

    private static String name(int code) {
        return code <= 0 ? "里侧卡片" : McPanel.displayName(code);
    }

    private boolean actionable(int index) {
        return this.actionSeq(index) >= 0;
    }

    private int actionSeq(int index) {
        int sequence = this.seqs[index];
        return !this.actionAvailable(this.activateSeqs, sequence) && !this.actionAvailable(this.summonSeqs, sequence) ? -1 : sequence;
    }

    private int totalRows() {
        return Math.max(1, (this.codes.length + this.cols - 1) / this.cols);
    }

    private int maxScroll() {
        return Math.max(0, this.totalRows() - Math.max(1, this.visibleRows));
    }

    private int cardX(int index) {
        return this.listLeft + 8 + index % this.cols * 72;
    }

    private int cardY(int index) {
        return this.listTop + 8 + (index / this.cols - this.scroll) * 96;
    }

    private int cardAt(double mouseX, double mouseY) {
        for (int i = 0; i < this.codes.length; i++) {
            int y = this.cardY(i);
            if (y >= this.listTop && y + 78 <= this.listTop + this.listH && McPanel.inside(mouseX, mouseY, this.cardX(i), y, 54, 78)) {
                return i;
            }
        }

        return -1;
    }

    private void refreshActionButton() {
        if (this.activateButton != null) {
            this.activateButton.active = this.actionAvailable(this.activateSeqs, this.selectedSeq);
        }

        if (this.summonButton != null) {
            this.summonButton.active = this.actionAvailable(this.summonSeqs, this.selectedSeq);
        }
    }

    private boolean actionAvailable(int[] candidates, int sequence) {
        if (sequence < 0) {
            return false;
        }

        for (int value : candidates) {
            if (value == sequence) {
                return true;
            }
        }

        return false;
    }

    public boolean canReturnFromCard() {
        if (this.minecraft != null && this.minecraft.level != null && this.minecraft.player != null && this.codes.length != 0) {
            for (DuelFieldEntity field : this.minecraft.level.getEntitiesOfClass(DuelFieldEntity.class, this.minecraft.player.getBoundingBox().inflate(48.0))) {
                if (field.getUUID().equals(this.sourceField) && field.isGameStarted() && !field.hasResult()) {
                    return true;
                }
            }

            return false;
        } else {
            return false;
        }
    }

    private void performAction(boolean summon) {
        if (this.actionAvailable(summon ? this.summonSeqs : this.activateSeqs, this.selectedSeq)) {
            ClientDuelAudio.playUi("SE_DUEL_DECIDE");
            ModNetwork.sendToServer(new ModNetwork.PileAct(this.location, this.selectedSeq, summon));
            this.onClose();
        }
    }

    protected void init() {
        this.panelW = Math.min(Math.max(this.width - 24, 340), 640);
        this.panelH = Math.min(Math.max(this.height - 24, 220), 430);
        this.panelLeft = (this.width - this.panelW) / 2;
        this.panelTop = (this.height - this.panelH) / 2;
        this.hintTop = this.panelTop + 22;
        this.listLeft = this.panelLeft + 10;
        this.listW = this.panelW - 20 - 8;
        this.listTop = this.hintTop + 28 + 4;
        this.listH = this.panelTop + this.panelH - 30 - this.listTop;
        this.cols = Math.max(3, (this.listW - 16 + 18) / 72);
        this.visibleRows = Math.max(1, (this.listH - 16 + 18) / 96);
        this.scroll = Math.min(this.scroll, this.maxScroll());
        int bottom = this.panelTop + this.panelH - 20 - 6;
        int right = this.panelLeft + this.panelW - 10;
        this.addRenderableWidget(Button.builder(Component.literal("关闭"), button -> this.onClose()).bounds(right - 70, bottom, 70, 20).build());
        this.actionsLeft = right - 70 - 8 - 80;
        this.summonButton = (Button)this.addRenderableWidget(
            Button.builder(Component.literal(this.location == 64 ? "特殊召唤" : "召唤"), button -> this.performAction(true))
                .bounds(this.actionsLeft, bottom, 80, 20)
                .build()
        );
        this.activateButton = null;
        if (this.location != 64) {
            this.actionsLeft -= 88;
            this.activateButton = (Button)this.addRenderableWidget(
                Button.builder(Component.literal("发动"), button -> this.performAction(false)).bounds(this.actionsLeft, bottom, 80, 20).build()
            );
        }

        this.refreshActionButton();
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        McPanel.panel(graphics, this.panelLeft, this.panelTop, this.panelLeft + this.panelW, this.panelTop + this.panelH);
        int ready = 0;

        for (int i = 0; i < this.codes.length; i++) {
            if (this.actionable(i)) {
                ready++;
            }
        }

        this.drawTitleBar(graphics, ready);
        boolean riding = RidingDuelUi.active(this.minecraft);
        String hint = riding ? "" : "滚轮翻看 · 左键选中 · 右键看大图";
        if (!hint.isEmpty()) {
            graphics.drawString(this.font, hint, this.listLeft, this.hintTop + 2, 7368816, false);
        }

        graphics.fill(this.listLeft, this.hintTop + 15, this.listLeft + 7, this.hintTop + 22, -10662);
        graphics.drawString(this.font, "可发动", this.listLeft + 11, this.hintTop + 14, 5263440, false);
        int summonLegendX = this.listLeft + 11 + this.font.width("可发动") + 14;
        graphics.fill(summonLegendX, this.hintTop + 15, summonLegendX + 7, this.hintTop + 22, -7019521);
        graphics.drawString(this.font, "可特殊召唤", summonLegendX + 11, this.hintTop + 14, 5263440, false);
        if (this.selectedSeq >= 0) {
            int x = this.listLeft + this.font.width(hint) + 14;
            int room = this.panelLeft + this.panelW - 10 - x;
            if (room > 60) {
                graphics.drawString(this.font, this.trim("已选：" + name(this.codeOf(this.selectedSeq)), room), x, this.hintTop + 2, 2055967, false);
            }
        }

        graphics.enableScissor(this.listLeft - 2, this.listTop - 2, this.listLeft + this.listW + 8, this.listTop + this.listH + 2);
        int hovered = this.cardAt(mouseX, mouseY);

        for (int i = 0; i < this.codes.length; i++) {
            int y = this.cardY(i);
            if (y + 78 >= this.listTop && y <= this.listTop + this.listH) {
                int x = this.cardX(i);
                McPanel.cardInSlot(graphics, this.codes[i], x, y, false);
                boolean canActivate = this.actionAvailable(this.activateSeqs, this.seqs[i]);
                boolean canSummon = this.actionAvailable(this.summonSeqs, this.seqs[i]);
                if (canActivate || canSummon) {
                    this.frame(graphics, x, y, 7, 7, -14405315, 0);
                    if (canActivate && canSummon) {
                        this.frame(graphics, x, y, 6, 3, -7019521, 0);
                        this.frame(graphics, x, y, 3, 3, -10662, 0);
                    } else {
                        this.frame(graphics, x, y, 5, 4, canActivate ? -10662 : -7019521, 0);
                    }
                }

                if (this.selectedSeq >= 0 && this.seqs[i] == this.selectedSeq) {
                    this.frame(graphics, x, y, 8, 1, -1, 0);
                    graphics.drawString(this.font, "✔", x + 54 - 10, y + 2, 16777215, true);
                } else if (i == hovered) {
                    graphics.fill(x, y, x + 54, y + 78, 1090519039);
                }
            }
        }

        graphics.disableScissor();
        this.drawScrollbar(graphics);
        this.drawFooter(graphics);
        if (hovered >= 0) {
            List<FormattedCharSequence> lines = new ArrayList<>();
            boolean back = this.codes[hovered] <= 0;
            lines.add(Component.literal(name(this.codes[hovered])).getVisualOrderText());
            if (back) {
                lines.add(Component.literal("内容未知（这堆里还有 " + this.hidden + " 张里侧）").getVisualOrderText());
            } else if (this.actionable(hovered)) {
                if (this.actionAvailable(this.activateSeqs, this.seqs[hovered])) {
                    lines.add(Component.literal("现在可以发动").getVisualOrderText());
                }

                if (this.actionAvailable(this.summonSeqs, this.seqs[hovered])) {
                    lines.add(Component.literal("现在可以特殊召唤").getVisualOrderText());
                }
            }

            if (!back && this.seqs[hovered] == this.selectedSeq) {
                lines.add(Component.literal("已选中").getVisualOrderText());
            }

            if (!back && !riding) {
                lines.add(Component.literal("右键看大图").getVisualOrderText());
            }

            graphics.renderTooltip(this.font, lines, mouseX, mouseY);
        }

        super.render(graphics, mouseX, mouseY, partialTick);
    }

    private void drawTitleBar(GuiGraphics graphics, int ready) {
        int x1 = this.panelLeft + 1;
        int x2 = this.panelLeft + this.panelW - 1;
        int y2 = this.panelTop + 22;
        graphics.fill(x1, this.panelTop + 1, x2, y2, -7631989);
        graphics.fill(x1, this.panelTop + 1, x2, this.panelTop + 2, -13158601);
        graphics.fill(x1, y2 - 1, x2, y2, -11184811);
        String title = title(this.location);
        graphics.drawString(this.font, title, x1 + 7, this.panelTop + 7, 16777215, true);
        String count = this.hidden > 0 ? this.codes.length + " 张（" + (this.codes.length - this.hidden) + " 张明置）" : this.codes.length + " 张";
        graphics.drawString(this.font, count, x1 + 7 + this.font.width(title) + 8, this.panelTop + 7, 13684944, true);
        if (this.codes.length == 0) {
            this.tag(graphics, "这堆现在是空的", x2 - 7, this.panelTop + 6, -11184811, 13684944);
        } else if (ready > 0) {
            this.tag(graphics, "可以" + this.verb() + " " + ready + " 张", x2 - 7, this.panelTop + 6, this.location == 64 ? -7019521 : -10662, -1447447);
        } else {
            this.tag(graphics, "现在没有可以" + this.verb() + "的卡", x2 - 7, this.panelTop + 6, -11184811, 11579568);
        }
    }

    private void tag(GuiGraphics graphics, String text, int rightX, int y, int border, int textColor) {
        int width = this.font.width(text) + 10;
        int x = rightX - width;
        graphics.fill(x, y, x + width, y + 12, -12829636);
        graphics.fill(x, y, x + width, y + 1, border);
        graphics.fill(x, y + 11, x + width, y + 12, border);
        graphics.fill(x, y, x + 1, y + 12, border);
        graphics.fill(x + width - 1, y, x + width, y + 12, border);
        graphics.drawString(this.font, text, x + 5, y + 2, textColor, true);
    }

    private void frame(GuiGraphics graphics, int x, int y, int out, int thick, int color, int outer) {
        if (outer != 0) {
            int bx0 = x - out - thick;
            int by0 = y - out - thick;
            int bx1 = x + 54 + out + thick;
            int by1 = y + 78 + out + thick;
            graphics.fill(bx0, by0, bx1, by0 + 1, outer);
            graphics.fill(bx0, by1 - 1, bx1, by1, outer);
            graphics.fill(bx0, by0, bx0 + 1, by1, outer);
            graphics.fill(bx1 - 1, by0, bx1, by1, outer);
        }

        int x0 = x - out;
        int y0 = y - out;
        int x1 = x + 54 + out;
        int y1 = y + 78 + out;
        graphics.fill(x0, y0, x1, y0 + thick, color);
        graphics.fill(x0, y1 - thick, x1, y1, color);
        graphics.fill(x0, y0, x0 + thick, y1, color);
        graphics.fill(x1 - thick, y0, x1, y1, color);
    }

    private void drawScrollbar(GuiGraphics graphics) {
        if (this.maxScroll() > 0) {
            int barX = this.listLeft + this.listW + 3;
            int width = 6;
            graphics.fill(barX, this.listTop, barX + width, this.listTop + this.listH, -13750738);
            graphics.fill(barX, this.listTop, barX + width, this.listTop + 1, -13158601);
            int barH = Math.max(14, this.listH * Math.max(1, this.visibleRows) / this.totalRows());
            int barY = this.listTop + (this.listH - barH) * this.scroll / Math.max(1, this.maxScroll());
            graphics.fill(barX + 1, barY, barX + width - 1, barY + barH, -4144960);
            graphics.fill(barX + 1, barY, barX + width - 1, barY + 1, -1);
            graphics.fill(barX + 1, barY + barH - 1, barX + width - 1, barY + barH, -11184811);
        }
    }

    private void drawFooter(GuiGraphics graphics) {
        int footerTop = this.panelTop + this.panelH - 30;
        graphics.fill(this.panelLeft + 1, footerTop, this.panelLeft + this.panelW - 1, footerTop + 1, -11184811);
        String range;
        if (this.codes.length == 0) {
            range = "（空的）";
        } else {
            int first = Math.min(this.codes.length, this.scroll * this.cols + 1);
            int last = Math.min(this.codes.length, (this.scroll + Math.max(1, this.visibleRows)) * this.cols);
            range = "看到第 " + first + "–" + last + " 张 / 共 " + this.codes.length + " 张";
        }

        graphics.drawString(this.font, this.trim(range, this.actionsLeft - this.panelLeft - 18), this.panelLeft + 10, footerTop + 12, 5263440, false);
    }

    private int codeOf(int sequence) {
        for (int i = 0; i < this.seqs.length; i++) {
            if (this.seqs[i] == sequence) {
                return this.codes[i];
            }
        }

        return 0;
    }

    private String trim(String text, int width) {
        return this.font.width(text) <= width ? text : this.font.plainSubstrByWidth(text, Math.max(8, width - 6)) + "…";
    }

    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        this.scroll = Math.max(0, Math.min(this.maxScroll(), this.scroll - (int)Math.signum(delta)));
        return true;
    }

    public boolean mouseClicked(double mouseX, double mouseY, int button) {
        int index = this.cardAt(mouseX, mouseY);
        if (index >= 0) {
            if (this.codes[index] <= 0) {
                if (this.minecraft != null && this.minecraft.player != null) {
                    this.minecraft.player.displayClientMessage(Component.literal("里侧卡片：内容未知（这堆里还有 " + this.hidden + " 张里侧）"), true);
                }

                return true;
            }

            if (button == 0) {
                ClientDuelAudio.playUi("SE_DUEL_SELECT");
                this.selectedSeq = this.seqs[index] == this.selectedSeq ? -1 : this.seqs[index];
                this.refreshActionButton();
                return true;
            }

            if (button == 1) {
                ClientHandBridge.openCard(this.codes[index], this);
                return true;
            }
        }

        return super.mouseClicked(mouseX, mouseY, button);
    }

    public boolean isPauseScreen() {
        return false;
    }
}
