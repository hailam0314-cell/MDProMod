package net.mdpromod;

import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.network.chat.Component;
import net.minecraft.util.FormattedCharSequence;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

import java.util.ArrayList;
import java.util.List;

/**
 * 手牌 HUD（可视化稿的「悬浮扇形」方案）：
 * 卡图浮在快捷栏上方，悬停抬起放大并出 tooltip，左键选中、右键看大图，
 * 对手回合整条压暗；牌多时自动收紧间距。
 */
@OnlyIn(Dist.CLIENT)
public final class HandHudOverlay {
    private static final int CARD_W = 34;
    private static final int CARD_H = 49;
    private static final int HOTBAR_CLEARANCE = 26;
    private static final int COLOR_LIGHT = 0xFFFFFFFF;
    private static final int COLOR_DARK = 0xFF373737;
    private static final int COLOR_SLOT = 0xFF8B8B8B;
    private static final int COLOR_GOLD = 0xFFFF55;
    private static final int COLOR_DIM = 0x90000000;

    private HandHudOverlay() {
    }

    private static int[] layout(Minecraft minecraft, int count) {
        int spacing = count <= 5 ? CARD_W + 4 : count <= 7 ? CARD_W - 8 : CARD_W - 14;
        int total = CARD_W + (count - 1) * spacing;
        int startX = (minecraft.getWindow().getGuiScaledWidth() - total) / 2;
        int baseY = minecraft.getWindow().getGuiScaledHeight() - HOTBAR_CLEARANCE - CARD_H;
        return new int[]{startX, baseY, spacing};
    }

    public static double[] mousePosition(Minecraft minecraft) {
        double width = minecraft.getWindow().getGuiScaledWidth();
        double height = minecraft.getWindow().getGuiScaledHeight();
        double x = minecraft.mouseHandler.xpos() * width / minecraft.getWindow().getScreenWidth();
        double y = minecraft.mouseHandler.ypos() * height / minecraft.getWindow().getScreenHeight();
        return new double[]{x, y};
    }

    private static int hoverIndex(int count, int[] box, double[] mouse) {
        for (int i = count - 1; i >= 0; i--) {
            int x = box[0] + i * box[2];
            if (mouse[0] >= x && mouse[0] < x + CARD_W
                    && mouse[1] >= box[1] - 6 && mouse[1] < box[1] + CARD_H) {
                return i;
            }
        }
        return -1;
    }

    public static void render(GuiGraphics graphics) {
        if (!ClientHandBridge.active()) return;
        int[] cards = ClientHandBridge.cards();
        if (cards.length == 0) return;
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.player == null || minecraft.screen != null || minecraft.options.hideGui) return;

        int[] box = layout(minecraft, cards.length);
        double[] mouse = mousePosition(minecraft);
        int hovered = hoverIndex(cards.length, box, mouse);
        for (int i = 0; i < cards.length; i++) {
            drawCard(graphics, cards[i], box[0] + i * box[2], box[1],
                    i == hovered, i == ClientHandBridge.selected(), !ClientHandBridge.yourTurn());
        }

        String label = "手牌 " + cards.length + (ClientHandBridge.yourTurn() ? "" : " · 对手回合");
        graphics.drawString(minecraft.font, Component.literal(label),
                box[0], box[1] - 13, ClientHandBridge.yourTurn() ? 0xFFFFFF : 0xB0B0B0, true);
        if (hovered >= 0) {
            renderTooltip(graphics, cards[hovered], minecraft, mouse);
        }
    }

    private static void drawCard(GuiGraphics graphics, int code, int x, int y,
                                 boolean hover, boolean selected, boolean dim) {
        float scale = hover ? 1.25F : 1.0F;
        graphics.pose().pushPose();
        graphics.pose().translate(x + CARD_W / 2.0, y + CARD_H, 0.0D);
        graphics.pose().scale(scale, scale, 1.0F);
        graphics.pose().translate(-(CARD_W / 2.0), -CARD_H + (hover ? -4 : 0), 0.0D);

        graphics.fill(0, 0, CARD_W, CARD_H, COLOR_SLOT);
        graphics.fill(0, 0, CARD_W, 1, COLOR_DARK);
        graphics.fill(0, 0, 1, CARD_H, COLOR_DARK);
        graphics.fill(0, CARD_H - 1, CARD_W, CARD_H, COLOR_LIGHT);
        graphics.fill(CARD_W - 1, 0, CARD_W, CARD_H, COLOR_LIGHT);
        graphics.blit(CardTextureCache.get(code), 1, 1, CARD_W - 2, CARD_H - 2,
                0.0F, 0.0F, 176, 256, 176, 256);
        if (dim) {
            graphics.fill(0, 0, CARD_W, CARD_H, COLOR_DIM);
        }
        if (selected) {
            graphics.fill(0, 0, CARD_W, 1, COLOR_GOLD);
            graphics.fill(0, CARD_H - 1, CARD_W, CARD_H, COLOR_GOLD);
            graphics.fill(0, 0, 1, CARD_H, COLOR_GOLD);
            graphics.fill(CARD_W - 1, 0, CARD_W, CARD_H, COLOR_GOLD);
        }
        graphics.pose().popPose();
    }

    private static void renderTooltip(GuiGraphics graphics, int code, Minecraft minecraft, double[] mouse) {
        CardInfo info = CardRegistry.get(code).orElse(null);
        if (info == null) return;
        List<FormattedCharSequence> lines = new ArrayList<>();
        lines.add(Component.literal(info.name()).getVisualOrderText());
        lines.add(Component.literal(info.typeLine()).getVisualOrderText());
        if (info.isMonster()) {
            lines.add(Component.literal("ATK " + info.atk() + " / DEF " + info.def()).getVisualOrderText());
        }
        graphics.renderTooltip(minecraft.font, lines, (int) mouse[0], (int) mouse[1]);
    }

    /** 返回 true 表示这次点击被手牌吃掉了。 */
    public static boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (!ClientHandBridge.active()) return false;
        int[] cards = ClientHandBridge.cards();
        if (cards.length == 0) return false;
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.player == null || minecraft.screen != null) return false;
        int[] box = layout(minecraft, cards.length);
        int index = hoverIndex(cards.length, box, new double[]{mouseX, mouseY});
        if (index < 0) return false;
        if (button == 0) {
            ClientHandBridge.setSelected(index);
            minecraft.player.displayClientMessage(Component.literal(
                    "选中：" + CardRegistry.get(cards[index]).map(CardInfo::name).orElse("#" + cards[index])), true);
            return true;
        }
        if (button == 1) {
            minecraft.setScreen(new HandCardScreen(cards[index]));
            return true;
        }
        return false;
    }
}
