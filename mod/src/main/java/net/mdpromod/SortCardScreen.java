package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

import java.util.ArrayList;
import java.util.List;

/**
 * 排序界面（MC 风格卡图）：按你想要的顺序依次点卡，卡上会标出第几个；
 * 再点一次取消这一步；全部点完自动提交。
 */
public class SortCardScreen extends Screen {
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

    @Override
    protected void init() {
        panelW = Math.min(Math.max(this.width - 40, 320), codes.length * (McPanel.CARD_W + GAP) + 24);
        panelH = Math.min(Math.max(this.height - 40, 180), McPanel.CARD_H + 70);
        panelLeft = (this.width - panelW) / 2;
        panelTop = (this.height - panelH) / 2;
    }

    private int cardLeft() {
        int total = codes.length * (McPanel.CARD_W + GAP) - GAP;
        return panelLeft + (panelW - total) / 2;
    }

    private int cardTop() {
        return panelTop + 30;
    }

    private void pick(int index) {
        ClientDuelAudio.playUi("SE_DUEL_SELECT");
        if (!order.remove(Integer.valueOf(index))) {
            order.add(index);
        }
        if (order.size() == codes.length) {
            ClientDuelAudio.playUi("SE_DUEL_DECIDE");
            int[] result = order.stream().mapToInt(Integer::intValue).toArray();
            ModNetwork.sendToServer(new ModNetwork.SortOrder(result));
            onClose();
        }
    }

    @Override
    public boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (button == 0) {
            for (int i = 0; i < codes.length; i++) {
                int x = cardLeft() + i * (McPanel.CARD_W + GAP);
                int y = cardTop();
                if (McPanel.inside(mouseX, mouseY, x, y, McPanel.CARD_W, McPanel.CARD_H)) {
                    pick(i);
                    return true;
                }
            }
        }
        return super.mouseClicked(mouseX, mouseY, button);
    }

    @Override
    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        renderBackground(graphics);
        McPanel.panel(graphics, panelLeft, panelTop, panelLeft + panelW, panelTop + panelH);
        graphics.drawCenteredString(font, "按想要的顺序依次点卡（再点一次取消）", panelLeft + panelW / 2,
                panelTop + 8, 0x404040);
        for (int i = 0; i < codes.length; i++) {
            int x = cardLeft() + i * (McPanel.CARD_W + GAP);
            int y = cardTop();
            boolean picked = order.contains(i);
            McPanel.cardInSlot(graphics, codes[i], x, y, picked);
            if (picked) {
                graphics.drawString(font, Integer.toString(order.indexOf(i) + 1),
                        x + 2, y + 2, 0xFFFF00);
            }
            if (McPanel.inside(mouseX, mouseY, x, y, McPanel.CARD_W, McPanel.CARD_H)) {
                graphics.renderTooltip(font, Component.literal(name(codes[i])), mouseX, mouseY);
            }
        }
        super.render(graphics, mouseX, mouseY, partialTick);
    }

    @Override
    public boolean isPauseScreen() {
        return false;
    }

    @Override
    public boolean shouldCloseOnEsc() {
        return false;
    }
}
