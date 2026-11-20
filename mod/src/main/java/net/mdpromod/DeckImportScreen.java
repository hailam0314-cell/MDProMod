package net.mdpromod;

import java.util.ArrayList;
import java.util.List;
import java.util.function.Consumer;
import net.minecraft.Util;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public final class DeckImportScreen extends Screen {
    private static final int PREFERRED_W = 620;
    private static final int PREFERRED_H = 380;
    private static final int TEXT_COLOR = 2105376;
    private static final int MUTED_COLOR = 6316128;
    private static final int ERROR_COLOR = 11018272;
    private static final int GOOD_COLOR = 1534764;
    private final Screen parent;
    private final Consumer<DeckData> importer;
    private final StringBuilder text = new StringBuilder();
    private int left;
    private int top;
    private int panelW;
    private int panelH;
    private int textX;
    private int textY;
    private int textW;
    private int textH;
    private int lineHeight;
    private int visibleLines;
    private int scrollLine;
    private int cursor;
    private String status = "";
    private boolean statusGood;

    public DeckImportScreen(Screen parent, Consumer<DeckData> importer) {
        super(Component.literal("导入卡组码"));
        this.parent = parent;
        this.importer = importer;
    }

    protected void init() {
        this.panelW = Math.min(Math.max(this.width - 20, 320), 620);
        this.panelH = Math.min(Math.max(this.height - 20, 240), 380);
        this.left = (this.width - this.panelW) / 2;
        this.top = (this.height - this.panelH) / 2;
        this.textX = this.left + 12;
        this.textY = this.top + 46;
        this.textW = this.panelW - 30;
        this.textH = this.panelH - 104;
        this.lineHeight = 10;
        this.visibleLines = Math.max(1, this.textH / this.lineHeight);
        this.cursor = Math.min(this.cursor, this.text.length());
        int buttonY = this.top + this.panelH - 26;
        this.addRenderableWidget(Button.builder(Component.literal("粘贴"), b -> this.paste()).bounds(this.left + 12, buttonY, 60, 20).build());
        this.addRenderableWidget(Button.builder(Component.literal("清空"), b -> {
            this.text.setLength(0);
            this.cursor = 0;
            this.scrollLine = 0;
            this.status = "";
        }).bounds(this.left + 76, buttonY, 60, 20).build());
        this.addRenderableWidget(Button.builder(Component.literal("取消"), b -> this.back()).bounds(this.left + this.panelW - 162, buttonY, 70, 20).build());
        this.addRenderableWidget(Button.builder(Component.literal("导入"), b -> this.importDeck()).bounds(this.left + this.panelW - 88, buttonY, 76, 20).build());
    }

    private void paste() {
        if (this.minecraft != null) {
            this.insert(this.minecraft.keyboardHandler.getClipboard());
            this.status = "";
        }
    }

    private void importDeck() {
        try {
            DeckData deck = DeckShareCode.decode(this.text.toString(), "导入卡组");
            if (deck.mainDeck().isEmpty() && deck.extraDeck().isEmpty() && deck.sideDeck().isEmpty()) {
                throw new IllegalArgumentException("卡组码里没有卡片");
            }

            this.status = "导入成功";
            this.statusGood = true;
            if (this.importer != null) {
                this.importer.accept(deck);
            }

            this.back();
        } catch (RuntimeException error) {
            this.statusGood = false;
            this.status = error.getMessage() == null ? "导入失败" : error.getMessage();
        }
    }

    private void back() {
        if (this.minecraft != null) {
            this.minecraft.setScreen(this.parent);
        }
    }

    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        if (inside(mouseX, mouseY, this.textX, this.textY, this.textW, this.textH)) {
            int max = Math.max(0, this.buildLines().size() - this.visibleLines);
            this.scrollLine = Math.max(0, Math.min(max, this.scrollLine - (int)Math.signum(delta) * 2));
            return true;
        } else {
            return super.mouseScrolled(mouseX, mouseY, delta);
        }
    }

    public boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (super.mouseClicked(mouseX, mouseY, button)) {
            return true;
        }

        if (button == 0 && inside(mouseX, mouseY, this.textX, this.textY, this.textW, this.textH)) {
            List<DeckImportScreen.VisualLine> lines = this.buildLines();
            int row = Math.max(0, Math.min(this.visibleLines - 1, (int)((mouseY - this.textY) / this.lineHeight)));
            int lineIndex = Math.min(lines.size() - 1, this.scrollLine + row);
            if (lineIndex >= 0) {
                DeckImportScreen.VisualLine line = lines.get(lineIndex);
                this.cursor = this.cursorAt(line, (int)mouseX - this.textX - 3);
                this.ensureCursorVisible();
            }

            return true;
        } else {
            return false;
        }
    }

    public boolean keyPressed(int keyCode, int scanCode, int modifiers) {
        boolean control = (modifiers & 2) != 0;
        if (control && keyCode == 86) {
            this.paste();
            return true;
        }

        if (control && keyCode == 67) {
            if (this.minecraft != null) {
                this.minecraft.keyboardHandler.setClipboard(this.text.toString());
            }

            return true;
        } else {
            if (control && keyCode == 65) {
                this.cursor = this.text.length();
                this.ensureCursorVisible();
                return true;
            }

            if (control && keyCode == 257) {
                this.importDeck();
                return true;
            }

            switch (keyCode) {
                case 257:
                case 335:
                    this.insert("\n");
                    return true;
                case 259:
                    if (this.cursor > 0) {
                        this.text.deleteCharAt(this.cursor - 1);
                        this.cursor--;
                        this.changed();
                    }

                    return true;
                case 261:
                    if (this.cursor < this.text.length()) {
                        this.text.deleteCharAt(this.cursor);
                        this.changed();
                    }

                    return true;
                case 262:
                    if (this.cursor < this.text.length()) {
                        this.cursor++;
                    }

                    this.ensureCursorVisible();
                    return true;
                case 263:
                    if (this.cursor > 0) {
                        this.cursor--;
                    }

                    this.ensureCursorVisible();
                    return true;
                case 264:
                    this.moveVertical(1);
                    return true;
                case 265:
                    this.moveVertical(-1);
                    return true;
                case 266:
                    this.scrollLine = Math.max(0, this.scrollLine - this.visibleLines);
                    return true;
                case 267:
                    int max = Math.max(0, this.buildLines().size() - this.visibleLines);
                    this.scrollLine = Math.min(max, this.scrollLine + this.visibleLines);
                    return true;
                case 268:
                    this.cursor = this.lineForCursor().start();
                    this.ensureCursorVisible();
                    return true;
                case 269:
                    this.cursor = this.lineForCursor().end();
                    this.ensureCursorVisible();
                    return true;
                default:
                    return super.keyPressed(keyCode, scanCode, modifiers);
            }
        }
    }

    public boolean charTyped(char codePoint, int modifiers) {
        if (codePoint >= ' ' && codePoint != 127) {
            this.insert(String.valueOf(codePoint));
            return true;
        } else {
            return super.charTyped(codePoint, modifiers);
        }
    }

    private void insert(String value) {
        if (value != null && !value.isEmpty()) {
            String normalized = value.replace("\r\n", "\n").replace('\r', '\n');
            int room = Math.max(0, 32768 - this.text.length());
            if (room > 0) {
                if (normalized.length() > room) {
                    normalized = normalized.substring(0, room);
                }

                this.text.insert(this.cursor, normalized);
                this.cursor = this.cursor + normalized.length();
                this.changed();
            }
        }
    }

    private void changed() {
        this.status = "";
        this.ensureCursorVisible();
    }

    private void moveVertical(int direction) {
        List<DeckImportScreen.VisualLine> lines = this.buildLines();
        int current = this.lineIndexForCursor(lines);
        int target = Math.max(0, Math.min(lines.size() - 1, current + direction));
        DeckImportScreen.VisualLine from = lines.get(Math.max(0, Math.min(lines.size() - 1, current)));
        DeckImportScreen.VisualLine to = lines.get(target);
        int columnWidth = Math.max(0, this.font.width(this.text.substring(from.start(), this.cursor)));
        this.cursor = this.cursorAt(to, columnWidth);
        this.ensureCursorVisible();
    }

    private void ensureCursorVisible() {
        List<DeckImportScreen.VisualLine> lines = this.buildLines();
        int line = this.lineIndexForCursor(lines);
        if (line < this.scrollLine) {
            this.scrollLine = line;
        }

        if (line >= this.scrollLine + this.visibleLines) {
            this.scrollLine = line - this.visibleLines + 1;
        }

        int max = Math.max(0, lines.size() - this.visibleLines);
        this.scrollLine = Math.max(0, Math.min(max, this.scrollLine));
    }

    private int lineIndexForCursor(List<DeckImportScreen.VisualLine> lines) {
        for (int i = 0; i < lines.size(); i++) {
            DeckImportScreen.VisualLine line = lines.get(i);
            if (this.cursor <= line.end() || i == lines.size() - 1) {
                return i;
            }
        }

        return 0;
    }

    private DeckImportScreen.VisualLine lineForCursor() {
        List<DeckImportScreen.VisualLine> lines = this.buildLines();
        return lines.get(this.lineIndexForCursor(lines));
    }

    private int cursorAt(DeckImportScreen.VisualLine line, int relativeX) {
        if (relativeX <= 0) {
            return line.start();
        }

        String value = this.text.substring(line.start(), line.end());
        int best = line.start();

        for (int i = 1; i <= value.length(); i++) {
            int width = this.font.width(value.substring(0, i));
            if (width > relativeX) {
                break;
            }

            best = line.start() + i;
        }

        return best;
    }

    private List<DeckImportScreen.VisualLine> buildLines() {
        List<DeckImportScreen.VisualLine> lines = new ArrayList<>();
        int start = 0;
        int width = 0;

        for (int i = 0; i < this.text.length(); i++) {
            char value = this.text.charAt(i);
            if (value == '\n') {
                lines.add(new DeckImportScreen.VisualLine(start, i));
                start = i + 1;
                width = 0;
            } else {
                int charWidth = this.font.width(String.valueOf(value));
                if (width > 0 && width + charWidth > this.textW - 10) {
                    lines.add(new DeckImportScreen.VisualLine(start, i));
                    start = i;
                    width = 0;
                }

                width += charWidth;
            }
        }

        lines.add(new DeckImportScreen.VisualLine(start, this.text.length()));
        return lines;
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        McPanel.panel(graphics, this.left, this.top, this.left + this.panelW, this.top + this.panelH);
        graphics.drawCenteredString(this.font, Component.literal("导入卡组码"), this.left + this.panelW / 2, this.top + 8, 3158064);
        graphics.drawString(this.font, "支持 MDPro 分享链接、纯 d 码或 #main 开头的 YDK 文本", this.textX, this.top + 24, 6316128, false);
        graphics.fill(this.textX - 2, this.textY - 2, this.textX + this.textW + 2, this.textY + this.textH + 2, -11184811);
        graphics.fill(this.textX, this.textY, this.textX + this.textW, this.textY + this.textH, -526345);
        graphics.fill(this.textX, this.textY, this.textX + this.textW, this.textY + 1, -4671304);
        graphics.enableScissor(this.textX + 1, this.textY + 1, this.textX + this.textW - 1, this.textY + this.textH - 1);
        List<DeckImportScreen.VisualLine> lines = this.buildLines();
        int maxScroll = Math.max(0, lines.size() - this.visibleLines);
        this.scrollLine = Math.max(0, Math.min(maxScroll, this.scrollLine));

        for (int row = 0; row < this.visibleLines; row++) {
            int index = this.scrollLine + row;
            if (index >= lines.size()) {
                break;
            }

            DeckImportScreen.VisualLine line = lines.get(index);
            graphics.drawString(
                this.font, this.text.substring(line.start(), line.end()), this.textX + 4, this.textY + 3 + row * this.lineHeight, 2105376, false
            );
        }

        graphics.disableScissor();
        if (maxScroll > 0) {
            int trackTop = this.textY + 2;
            int trackH = this.textH - 4;
            graphics.fill(this.textX + this.textW - 5, trackTop, this.textX + this.textW - 2, trackTop + trackH, -7566196);
            int thumbH = Math.max(12, trackH * this.visibleLines / Math.max(this.visibleLines, lines.size()));
            int thumbY = trackTop + (trackH - thumbH) * this.scrollLine / Math.max(1, maxScroll);
            graphics.fill(this.textX + this.textW - 5, thumbY, this.textX + this.textW - 2, thumbY + thumbH, -723724);
        }

        if (Util.getMillis() / 500L % 2L == 0L) {
            DeckImportScreen.VisualLine line = this.lineForCursor();
            int cursorWidth = this.font.width(this.text.substring(line.start(), Math.min(this.cursor, line.end())));
            int cursorLine = this.lineIndexForCursor(lines) - this.scrollLine;
            if (cursorLine >= 0 && cursorLine < this.visibleLines) {
                graphics.fill(
                    this.textX + 4 + Math.min(cursorWidth, this.textW - 12),
                    this.textY + 1 + cursorLine * this.lineHeight,
                    this.textX + 5 + Math.min(cursorWidth, this.textW - 12),
                    this.textY + 9 + cursorLine * this.lineHeight,
                    -14671840
                );
            }
        }

        if (!this.status.isEmpty()) {
            graphics.drawString(
                this.font,
                this.font.plainSubstrByWidth(this.status, this.panelW - 24),
                this.textX,
                this.top + this.panelH - 39,
                this.statusGood ? 1534764 : 11018272,
                false
            );
        } else {
            graphics.drawString(this.font, "Ctrl+V 粘贴，Ctrl+Enter 导入，滚轮上下浏览", this.textX, this.top + this.panelH - 39, 6316128, false);
        }

        super.render(graphics, mouseX, mouseY, partialTick);
    }

    public void onClose() {
        this.back();
    }

    public boolean isPauseScreen() {
        return false;
    }

    private static boolean inside(double mouseX, double mouseY, int x, int y, int width, int height) {
        return mouseX >= x && mouseX < x + width && mouseY >= y && mouseY < y + height;
    }

    private record VisualLine(int start, int end) {
    }
}
