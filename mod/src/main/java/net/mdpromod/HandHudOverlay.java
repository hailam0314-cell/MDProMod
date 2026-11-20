package net.mdpromod;

import java.util.ArrayList;
import java.util.List;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.network.chat.Component;
import net.minecraft.util.FormattedCharSequence;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public final class HandHudOverlay {
    private static final int CARD_W = 34;
    private static final int CARD_H = 49;
    private static final int HOTBAR_CLEARANCE = 26;
    private static final int COLOR_LIGHT = -1;
    private static final int COLOR_DARK = -13158601;
    private static final int COLOR_SLOT = -7631989;
    private static final int COLOR_GOLD = 16777045;
    private static final int COLOR_DIM = -1879048192;

    private HandHudOverlay() {
    }

    private static int[] layout(Minecraft minecraft, int count) {
        int spacing = count <= 5 ? 38 : (count <= 7 ? 26 : 20);
        int total = 34 + (count - 1) * spacing;
        int startX = (minecraft.getWindow().getGuiScaledWidth() - total) / 2;
        int baseY = minecraft.getWindow().getGuiScaledHeight() - 26 - 49;
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
            if (mouse[0] >= x && mouse[0] < x + 34 && mouse[1] >= box[1] - 6 && mouse[1] < box[1] + 49) {
                return i;
            }
        }

        return -1;
    }

    public static void render(GuiGraphics graphics) {
        if (ClientHandBridge.active()) {
            int[] cards = ClientHandBridge.cards();
            if (cards.length != 0) {
                Minecraft minecraft = Minecraft.getInstance();
                if (minecraft.player != null && minecraft.screen == null && !minecraft.options.hideGui) {
                    int[] box = layout(minecraft, cards.length);
                    double[] mouse = mousePosition(minecraft);
                    int hovered = hoverIndex(cards.length, box, mouse);

                    for (int i = 0; i < cards.length; i++) {
                        drawCard(graphics, cards[i], box[0] + i * box[2], box[1], i == hovered, i == ClientHandBridge.selected(), !ClientHandBridge.yourTurn());
                    }

                    String label = "手牌 " + cards.length + (ClientHandBridge.yourTurn() ? "" : " · 对手回合");
                    graphics.drawString(
                        minecraft.font, Component.literal(label), box[0], box[1] - 13, ClientHandBridge.yourTurn() ? 16777215 : 11579568, true
                    );
                    if (hovered >= 0) {
                        renderTooltip(graphics, cards[hovered], minecraft, mouse);
                    }
                }
            }
        }
    }

    private static void drawCard(GuiGraphics graphics, int code, int x, int y, boolean hover, boolean selected, boolean dim) {
        float scale = hover ? 1.25F : 1.0F;
        graphics.pose().pushPose();
        graphics.pose().translate(x + 17.0, y + 49, 0.0);
        graphics.pose().scale(scale, scale, 1.0F);
        graphics.pose().translate(-17.0, -49 + (hover ? -4 : 0), 0.0);
        graphics.fill(0, 0, 34, 49, -7631989);
        graphics.fill(0, 0, 34, 1, -13158601);
        graphics.fill(0, 0, 1, 49, -13158601);
        graphics.fill(0, 48, 34, 49, -1);
        graphics.fill(33, 0, 34, 49, -1);
        graphics.blit(CardTextureCache.get(code), 1, 1, 32, 47, 0.0F, 0.0F, 176, 256, 176, 256);
        if (dim) {
            graphics.fill(0, 0, 34, 49, -1879048192);
        }

        if (selected) {
            graphics.fill(0, 0, 34, 1, 16777045);
            graphics.fill(0, 48, 34, 49, 16777045);
            graphics.fill(0, 0, 1, 49, 16777045);
            graphics.fill(33, 0, 34, 49, 16777045);
        }

        graphics.pose().popPose();
    }

    private static void renderTooltip(GuiGraphics graphics, int code, Minecraft minecraft, double[] mouse) {
        CardInfo info = CardRegistry.get(code).orElse(null);
        if (info != null) {
            List<FormattedCharSequence> lines = new ArrayList<>();
            lines.add(Component.literal(info.name()).getVisualOrderText());
            lines.add(Component.literal(info.typeLine()).getVisualOrderText());
            if (info.isMonster()) {
                lines.add(Component.literal("ATK " + info.atk() + " / DEF " + info.def()).getVisualOrderText());
            }

            graphics.renderTooltip(minecraft.font, lines, (int)mouse[0], (int)mouse[1]);
        }
    }

    public static boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (!ClientHandBridge.active()) {
            return false;
        }

        int[] cards = ClientHandBridge.cards();
        if (cards.length == 0) {
            return false;
        }

        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.player != null && minecraft.screen == null) {
            int[] box = layout(minecraft, cards.length);
            int index = hoverIndex(cards.length, box, new double[]{mouseX, mouseY});
            if (index < 0) {
                return false;
            } else if (button == 0) {
                ClientHandBridge.setSelected(index);
                minecraft.player.displayClientMessage(Component.literal("选中：" + CardRegistry.get(cards[index]).map(CardInfo::name).orElse("#" + cards[index])), true);
                return true;
            } else if (button == 1) {
                minecraft.setScreen(new HandCardScreen(cards[index]));
                return true;
            } else {
                return false;
            }
        } else {
            return false;
        }
    }
}
