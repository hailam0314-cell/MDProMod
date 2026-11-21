package net.mdpromod;

import java.util.UUID;
import java.util.LinkedHashSet;
import java.util.List;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

final class DuelControlScreen extends Screen {
    private final UUID duel;
    private ModNetwork.HandSync shown;
    private int placePage;
    private final LinkedHashSet<Integer> picked = new LinkedHashSet<>();
    private int left, top, panelWidth;
    private long submitted = -1;
    private boolean fieldTab, battle;
    private int selectedSlot = -1;

    DuelControlScreen(UUID duel) {
        super(Component.literal("对局操作"));
        this.duel = duel;
    }

    boolean isCurrent() {
        return ClientHandBridge.field() != null && duel.equals(ClientHandBridge.snapshot().duel());
    }

    @Override
    public boolean isPauseScreen() {
        return false;
    }

    @Override
    protected void init() {
        clearWidgets();
        if (!isCurrent()) return;
        ModNetwork.HandSync update = ClientHandBridge.snapshot();
        if (shown == null || shown.prompt() != update.prompt()) {
            submitted = -1;
            picked.clear();
            placePage = 0;
        }
        shown = update;
        boolean inBattle = "BP".equals(ClientHandBridge.field().currentPhase(ClientHandBridge.field().playerSideForTurn()));
        if (inBattle && !battle && shown.yourTurn() && !shown.monsters().isEmpty()) fieldTab = true;
        battle = inBattle;
        panelWidth = Math.min(348, width - 12);
        left = (width - panelWidth) / 2;
        top = height - 146;
        button(left + panelWidth - 42, top + 3, 38, "关闭", this::onClose);
        if (shown.places().length > 0) {
            addPlaces();
        } else {
            tab(left + 6, 68, "手牌 " + shown.cards().length, false);
            tab(left + 78, 92, "场上怪兽 " + shown.monsters().size(), true);
            addCards();
            int x = left + 6;
            for (int i = 0; i < 3; i++) {
                if ((shown.phases() & (1 << i)) == 0) continue;
                int kind = ModNetwork.PanelAction.BATTLE + i;
                String text = switch (kind) {
                    case ModNetwork.PanelAction.BATTLE -> "进入战斗阶段";
                    case ModNetwork.PanelAction.MAIN2 -> "进入主要阶段 2";
                    default -> "结束回合";
                };
                int w = font.width(text) + 14;
                button(x, top + 120, w, text, () -> send(kind, 0, 0));
                x += w + 4;
            }
        }
    }

    private void tab(int x, int w, String label, boolean field) {
        Button tab = Button.builder(Component.literal(label), b -> { fieldTab = field; init(); })
            .bounds(x, top + 17, w, 16).build();
        tab.active = fieldTab != field;
        addRenderableWidget(tab);
    }

    private void addCards() {
        List<ModNetwork.PanelMonster> monsters = shown.monsters();
        int size = fieldTab ? monsters.size() : shown.cards().length;
        int selected = ClientHandBridge.selected();
        if (fieldTab) {
            selected = 0;
            for (int i = 0; i < size; i++) {
                if (monsters.get(i).slot() == selectedSlot) { selected = i; break; }
                if (monsters.get(selected).actions() == 0 && monsters.get(i).actions() != 0) selected = i;
            }
            selectedSlot = size == 0 ? -1 : monsters.get(selected).slot();
        }
        int capacity = Math.max(1, (panelWidth - 48) / 36);
        int start = selected / capacity * capacity;
        int count = Math.min(capacity, size - start);
        int x = left + (panelWidth - count * 36) / 2;
        for (int i = start; i < start + count; i++) {
            int code = fieldTab ? monsters.get(i).code() : shown.cards()[i];
            int mask = fieldTab ? monsters.get(i).actions() : shown.actions()[i];
            addRenderableWidget(new CardButton(x, top + 35, i, code, mask != 0, i == selected));
            x += 36;
        }
        if (start > 0) button(left + 3, top + 50, 18, "‹", () -> select(start - 1));
        if (start + count < size) button(left + panelWidth - 21, top + 50, 18, "›", () -> select(start + count));
        if (count == 0) return;
        x = left + 6;
        int mask = fieldTab ? monsters.get(selected).actions() : shown.actions()[selected];
        int index = fieldTab ? selectedSlot : selected;
        int location = fieldTab ? 4 : 2;
        int code = fieldTab ? monsters.get(selected).code() : shown.cards()[selected];
        for (int i = 0; i < ClientCardMenu.LABELS.length; i++) {
            if ((mask & 1 << i) == 0) continue;
            int kind = i;
            String label = i == ModNetwork.CARD_MENU_POSITION ? "变更表示" : ClientCardMenu.LABELS[i];
            int w = font.width(label) + 14;
            button(x, top + 98, w, label, () -> send(new ModNetwork.PanelAction(duel, shown.prompt(), kind, index, code, new int[0], location)));
            x += w + 4;
        }
    }

    private final class CardButton extends Button {
        private final int code;
        private final boolean actionable, selected;

        CardButton(int x, int y, int index, int code, boolean actionable, boolean selected) {
            super(x, y, 34, 49, Component.literal(cardName(code)), b -> select(index), DEFAULT_NARRATION);
            this.code = code;
            this.actionable = actionable;
            this.selected = selected;
        }

        @Override
        public void renderWidget(GuiGraphics g, int mx, int my, float dt) {
            HandHudOverlay.drawCard(g, code, getX(), getY(), false, selected, false);
            if (actionable) g.fill(getX() + 1, getY() + 46, getX() + 33, getY() + 48, 0xff61daba);
        }

        @Override
        public boolean mouseClicked(double x, double y, int button) {
            if (button == 1 && isMouseOver(x, y)) {
                ClientHandBridge.openCard(code, DuelControlScreen.this);
                return true;
            }
            return super.mouseClicked(x, y, button);
        }
    }

    private void select(int index) {
        if (fieldTab) selectedSlot = shown.monsters().get(index).slot();
        else ClientHandBridge.setSelected(index);
        init();
    }

    private void addPlaces() {
        int[] places = shown.places();
        int columns = Math.max(1, Math.min(5, (panelWidth - 10) / 57));
        int perPage = columns * 3;
        placePage = Math.min(placePage, (places.length - 1) / perPage);
        int start = placePage * perPage;
        int stride = (panelWidth - 10) / columns;
        for (int i = start; i < Math.min(start + perPage, places.length); i++) {
            int addr = places[i];
            int offset = i - start;
            int x = left + 6 + offset % columns * stride;
            String label = DuelLocation.label(addr, shown.seat());
            String shortLabel = label.replace("自己", "我").replace("对方", "敌");
            button(x, top + 43 + offset / columns * 24, stride - 2, (picked.contains(addr) ? "✓" : "") + shortLabel,
                () -> pickPlace(addr));
        }
        if (start > 0) button(left + 6, top + 120, 48, "上一页", () -> { placePage--; init(); });
        if (start + perPage < places.length) button(left + panelWidth - 54, top + 120, 48, "下一页", () -> { placePage++; init(); });
    }

    private void pickPlace(int address) {
        if (!picked.remove(address)) picked.add(address);
        if (picked.size() == shown.placeCount()) {
            int[] addresses = picked.stream().mapToInt(Integer::intValue).toArray();
            send(new ModNetwork.PanelAction(duel, shown.prompt(), ModNetwork.PanelAction.PLACE, addresses[0], 0,
                java.util.Arrays.copyOfRange(addresses, 1, addresses.length)));
        } else {
            init();
        }
    }

    private void button(int x, int y, int w, String label, Runnable click) {
        addRenderableWidget(Button.builder(Component.literal(label), b -> click.run()).bounds(x, y, w, 18).build());
    }

    private void send(int kind, int index, int code) {
        send(new ModNetwork.PanelAction(duel, shown.prompt(), kind, index, code));
    }

    private void send(ModNetwork.PanelAction action) {
        if (!isCurrent() || shown != ClientHandBridge.snapshot() || shown.prompt() <= 0 || submitted == shown.prompt()) return;
        submitted = shown.prompt();
        ModNetwork.sendToServer(action);
    }

    @Override
    public void tick() {
        if (!isCurrent()) onClose();
        else if (shown != ClientHandBridge.snapshot()) init();
    }

    private static String cardName(int code) {
        return CardRegistry.get(code).map(CardInfo::name).orElse("#" + code);
    }

    private static String phaseTitle() {
        DuelFieldEntity field = ClientHandBridge.field();
        return (ClientHandBridge.yourTurn() ? "我的回合" : "对手回合") + " · "
            + DuelTiming.phaseLabel(field.currentPhase(field.playerSideForTurn()));
    }

    @Override
    public void render(GuiGraphics g, int mx, int my, float dt) {
        if (ClientBattleCamera.showing(this)) return;
        if (!isCurrent()) return;
        if (shown != ClientHandBridge.snapshot()) init();
        g.fill(left, top, left + panelWidth, height - 3, 0xeb172129);
        g.drawString(font, phaseTitle(), left + 6, top + 5, 0xfff5dc9a);
        if (shown.places().length > 0) {
            String caption = shown.placeCount() > 1 ? "选择落点  " + picked.size() + " / " + shown.placeCount() : "选择落点";
            g.drawString(font, caption, left + 6, top + 22, -1);
        } else {
            String caption = "";
            if (fieldTab) {
                for (var monster : shown.monsters()) {
                    if (monster.slot() != selectedSlot) continue;
                    String pos = switch (monster.position()) {
                        case 1 -> "攻击表示";
                        case 2 -> "里侧攻击";
                        case 4 -> "守备表示";
                        default -> "里侧守备";
                    };
                    caption = DuelLocation.monsterZone(monster.slot(), true) + " · " + cardName(monster.code()) + " · " + pos;
                }
            } else if (shown.cards().length > 0) {
                caption = cardName(shown.cards()[ClientHandBridge.selected()]) + "  ·  右键查看详情";
            }
            g.drawString(font, font.plainSubstrByWidth(caption, panelWidth - 12), left + 6, top + 87, 0xffc9d8de);
        }
        super.render(g, mx, my, dt);
    }

    @Override
    public boolean mouseClicked(double x, double y, int button) {
        return ClientBattleCamera.showing(this) || super.mouseClicked(x, y, button);
    }

    @Override
    public boolean keyPressed(int key, int scan, int modifiers) {
        if (ClientBattleCamera.showing(this)) {
            if (key == org.lwjgl.glfw.GLFW.GLFW_KEY_ESCAPE) ClientBattleCamera.restore(this.minecraft);
            return true;
        }
        return super.keyPressed(key, scan, modifiers);
    }

    @Override
    public void removed() {
        ClientBattleCamera.restore(this.minecraft);
        super.removed();
    }

    static void renderHud(GuiGraphics g) {
        Minecraft mc = Minecraft.getInstance();
        if (mc.screen != null || mc.options.hideGui || mc.player == null) return;
        int[] cards = ClientHandBridge.cards();
        int center = mc.getWindow().getGuiScaledWidth() / 2;
        int y = mc.getWindow().getGuiScaledHeight() - (mc.gameMode != null && mc.gameMode.canHurtPlayer() ? 88 : 62);
        int count = Math.min(9, cards.length);
        int x = center - (count * 22) / 2;
        g.pose().pushPose();
        g.pose().scale(0.625F, 0.625F, 1);
        for (int i = 0; i < count; i++) {
            HandHudOverlay.drawCard(g, cards[i], (int)((x + i * 22) / 0.625F), (int)(y / 0.625F), false, false, false);
        }
        g.pose().popPose();
        String label = phaseTitle() + "  ·  手牌 " + cards.length + "  [" + ClientModEvents.DUEL_PANEL.getTranslatedKeyMessage().getString() + "] 对局操作";
        g.drawCenteredString(mc.font, label, center, y - 12, -1);
    }
}
