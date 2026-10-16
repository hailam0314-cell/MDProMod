package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraft.util.FormattedCharSequence;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

import java.util.ArrayList;
import java.util.List;

/** 右键手牌弹出的大图详情：左边卡图，右边效果文本（滚轮翻页）。 */
@OnlyIn(Dist.CLIENT)
public class HandCardScreen extends Screen {
    private static final int COLOR_PANEL = 0xFFC6C6C6;
    private static final int COLOR_LIGHT = 0xFFFFFFFF;
    private static final int COLOR_DARK = 0xFF555555;
    private static final int COLOR_TITLE = 0x404040;
    private static final int COLOR_BODY = 0xFF303030;
    private static final float TEXT_SCALE = 0.85F;

    private final int code;
    private int leftPos;
    private int topPos;
    private int panelW;
    private int panelH;
    private int textTop;
    private int textHeight;
    private int scroll;
    private List<FormattedCharSequence> lines = new ArrayList<>();

    public HandCardScreen(int code) {
        super(Component.literal("手牌详情"));
        this.code = code;
    }

    @Override
    protected void init() {
        super.init();
        panelW = Math.min(Math.max(this.width - 40, 260), 420);
        panelH = Math.min(Math.max(this.height - 40, 180), 260);
        leftPos = (this.width - panelW) / 2;
        topPos = (this.height - panelH) / 2;
        textTop = topPos + 26;
        textHeight = panelH - 40;
        rebuildLines();
    }

    private void rebuildLines() {
        lines = new ArrayList<>();
        CardInfo info = CardRegistry.get(code).orElse(null);
        if (info == null) return;
        int wrap = Math.max(60, (int) ((panelW - 150) / TEXT_SCALE));
        String text = info.desc() == null || info.desc().isBlank() ? "（没有效果文本）" : info.desc();
        lines.addAll(this.font.split(Component.literal(text), wrap));
    }

    @Override
    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        renderBackground(graphics);
        graphics.fill(leftPos - 1, topPos - 1, leftPos + panelW + 1, topPos + panelH + 1, 0xFF000000);
        graphics.fill(leftPos, topPos, leftPos + panelW, topPos + panelH, COLOR_PANEL);
        graphics.fill(leftPos, topPos, leftPos + panelW, topPos + 1, COLOR_LIGHT);
        graphics.fill(leftPos, topPos, leftPos + 1, topPos + panelH, COLOR_LIGHT);
        graphics.fill(leftPos, topPos + panelH - 1, leftPos + panelW, topPos + panelH, COLOR_DARK);
        graphics.fill(leftPos + panelW - 1, topPos, leftPos + panelW, topPos + panelH, COLOR_DARK);

        CardInfo info = CardRegistry.get(code).orElse(null);
        if (info == null) {
            graphics.drawString(this.font, Component.literal("未知卡片 #" + code), leftPos + 10, topPos + 10, COLOR_BODY, false);
            return;
        }
        graphics.drawString(this.font, Component.literal(info.name()), leftPos + 10, topPos + 8, COLOR_TITLE, false);

        int artW = 96;
        int artH = artW * 256 / 176;
        int artX = leftPos + 10;
        int artY = topPos + 24;
        graphics.fill(artX - 1, artY - 1, artX + artW + 1, artY + artH + 1, 0xFF000000);
        graphics.blit(CardTextureCache.get(code), artX, artY, artW, artH, 0.0F, 0.0F, 176, 256, 176, 256);

        int textX = artX + artW + 12;
        int textW = leftPos + panelW - 10 - textX;
        graphics.drawString(this.font, Component.literal(info.typeLine()),
                textX, artY, COLOR_BODY, false);
        graphics.drawString(this.font, Component.literal(info.isMonster()
                        ? "ATK " + info.atk() + " / DEF " + info.def() : info.typeLabel()),
                textX, artY + 11, COLOR_BODY, false);
        graphics.drawString(this.font, Component.literal("#" + info.code()),
                textX, artY + 22, 0xFF6F6F6F, false);

        int areaY = artY + 36;
        int areaH = topPos + panelH - 12 - areaY;
        graphics.fill(textX - 2, areaY - 2, textX + textW + 2, areaY + areaH + 2, 0xFF8B8B8B);
        graphics.enableScissor(textX, areaY, textX + textW, areaY + areaH);
        graphics.pose().pushPose();
        graphics.pose().scale(TEXT_SCALE, TEXT_SCALE, 1.0F);
        int baseX = (int) (textX / TEXT_SCALE);
        int baseY = (int) (areaY / TEXT_SCALE);
        int visible = Math.max(1, (int) (areaH / (9 * TEXT_SCALE)));
        int max = Math.max(0, lines.size() - visible);
        scroll = Math.max(0, Math.min(scroll, max));
        for (int i = scroll; i < lines.size(); i++) {
            int lineY = baseY + (i - scroll) * 9;
            if (lineY * TEXT_SCALE >= areaY + areaH) break;
            graphics.drawString(this.font, lines.get(i), baseX, lineY, COLOR_BODY, false);
        }
        graphics.pose().popPose();
        graphics.disableScissor();
        if (lines.size() > visible) {
            graphics.drawString(this.font, Component.literal("滚轮翻页"),
                    textX, areaY + areaH + 3, COLOR_TITLE, false);
        }
    }

    @Override
    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        scroll -= (int) Math.signum(delta) * 1;
        return true;
    }

    @Override
    public boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (button == 1 || button == 0) {
            onClose();
            return true;
        }
        return super.mouseClicked(mouseX, mouseY, button);
    }

    @Override
    public void onClose() {
        ClientDuelAudio.playUi("SE_MENU_CANCEL");
        if (this.minecraft != null) {
            this.minecraft.setScreen(null);
        }
    }

    @Override
    public boolean isPauseScreen() {
        return false;
    }
}
