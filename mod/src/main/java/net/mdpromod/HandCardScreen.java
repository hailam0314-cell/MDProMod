package net.mdpromod;

import java.util.ArrayList;
import java.util.List;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraft.util.FormattedCharSequence;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public class HandCardScreen extends Screen {
    private static final int COLOR_PANEL = -3750202;
    private static final int COLOR_LIGHT = -1;
    private static final int COLOR_DARK = -11184811;
    private static final int COLOR_TITLE = 4210752;
    private static final int COLOR_BODY = -13619152;
    private static final float TEXT_SCALE = 0.85F;
    private final int code;
    private final Screen returnScreen;
    private int leftPos;
    private int topPos;
    private int panelW;
    private int panelH;
    private int textTop;
    private int textHeight;
    private int scroll;
    private List<FormattedCharSequence> lines = new ArrayList<>();

    public HandCardScreen(int code) {
        this(code, null);
    }

    public HandCardScreen(int code, Screen returnScreen) {
        super(Component.literal("手牌详情"));
        this.code = code;
        this.returnScreen = returnScreen;
    }

    protected void init() {
        super.init();
        this.panelW = Math.min(Math.max(this.width - 40, 260), 420);
        this.panelH = Math.min(Math.max(this.height - 40, 180), 260);
        this.leftPos = (this.width - this.panelW) / 2;
        this.topPos = (this.height - this.panelH) / 2;
        this.textTop = this.topPos + 26;
        this.textHeight = this.panelH - 40;
        this.rebuildLines();
    }

    private void rebuildLines() {
        this.lines = new ArrayList<>();
        CardInfo info = CardRegistry.get(this.code).orElse(null);
        if (info != null) {
            int wrap = Math.max(60, (int)((this.panelW - 150) / 0.85F));
            String text = info.desc() != null && !info.desc().isBlank() ? info.desc() : "（没有效果文本）";
            this.lines.addAll(this.font.split(Component.literal(text), wrap));
        }
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        graphics.fill(this.leftPos - 1, this.topPos - 1, this.leftPos + this.panelW + 1, this.topPos + this.panelH + 1, -16777216);
        graphics.fill(this.leftPos, this.topPos, this.leftPos + this.panelW, this.topPos + this.panelH, -3750202);
        graphics.fill(this.leftPos, this.topPos, this.leftPos + this.panelW, this.topPos + 1, -1);
        graphics.fill(this.leftPos, this.topPos, this.leftPos + 1, this.topPos + this.panelH, -1);
        graphics.fill(this.leftPos, this.topPos + this.panelH - 1, this.leftPos + this.panelW, this.topPos + this.panelH, -11184811);
        graphics.fill(this.leftPos + this.panelW - 1, this.topPos, this.leftPos + this.panelW, this.topPos + this.panelH, -11184811);
        CardInfo info = CardRegistry.get(this.code).orElse(null);
        if (info == null) {
            graphics.drawString(this.font, Component.literal("未知卡片 #" + this.code), this.leftPos + 10, this.topPos + 10, -13619152, false);
        } else {
            graphics.drawString(this.font, Component.literal(info.name()), this.leftPos + 10, this.topPos + 8, 4210752, false);
            int artW = 96;
            int artH = artW * 256 / 176;
            int artX = this.leftPos + 10;
            int artY = this.topPos + 24;
            graphics.fill(artX - 1, artY - 1, artX + artW + 1, artY + artH + 1, -16777216);
            graphics.blit(CardTextureCache.get(this.code), artX, artY, artW, artH, 0.0F, 0.0F, 176, 256, 176, 256);
            int textX = artX + artW + 12;
            int textW = this.leftPos + this.panelW - 10 - textX;
            graphics.drawString(this.font, Component.literal(info.typeLine()), textX, artY, -13619152, false);
            graphics.drawString(
                this.font,
                Component.literal(info.isMonster() ? "ATK " + info.atk() + " / DEF " + info.def() : info.typeLabel()),
                textX,
                artY + 11,
                -13619152,
                false
            );
            graphics.drawString(this.font, Component.literal("#" + info.code()), textX, artY + 22, -9474193, false);
            int areaY = artY + 36;
            int areaH = this.topPos + this.panelH - 12 - areaY;
            graphics.fill(textX - 2, areaY - 2, textX + textW + 2, areaY + areaH + 2, -7631989);
            graphics.enableScissor(textX, areaY, textX + textW, areaY + areaH);
            graphics.pose().pushPose();
            graphics.pose().scale(0.85F, 0.85F, 1.0F);
            int baseX = (int)(textX / 0.85F);
            int baseY = (int)(areaY / 0.85F);
            int visible = Math.max(1, (int)(areaH / 7.65F));
            int max = Math.max(0, this.lines.size() - visible);
            this.scroll = Math.max(0, Math.min(this.scroll, max));

            for (int i = this.scroll; i < this.lines.size(); i++) {
                int lineY = baseY + (i - this.scroll) * 9;
                if (lineY * 0.85F >= areaY + areaH) {
                    break;
                }

                graphics.drawString(this.font, this.lines.get(i), baseX, lineY, -13619152, false);
            }

            graphics.pose().popPose();
            graphics.disableScissor();
            if (this.lines.size() > visible && !RidingDuelUi.active(this.minecraft)) {
                graphics.drawString(this.font, Component.literal("滚轮翻页"), textX, areaY + areaH + 3, 4210752, false);
            }
        }
    }

    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        this.scroll = this.scroll - (int)Math.signum(delta) * 1;
        return true;
    }

    public boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (button != 1 && button != 0) {
            return super.mouseClicked(mouseX, mouseY, button);
        }

        this.onClose();
        return true;
    }

    public void onClose() {
        ClientDuelAudio.playUi("SE_MENU_CANCEL");
        if (this.minecraft != null) {
            if (this.returnScreen instanceof PileScreen pile && pile.canReturnFromCard()) {
                this.minecraft.setScreen(this.returnScreen);
            } else {
                this.minecraft.setScreen(null);
            }
        }
    }

    public boolean isPauseScreen() {
        return false;
    }
}
