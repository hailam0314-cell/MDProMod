package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

public final class MusicSettingsScreen extends Screen {
    private static final String OFF_KEY = "off";
    private static final int ROWS = 9;
    private static final int BOX_WIDTH = 400;
    private static final int ROW_HEIGHT = 21;
    private static final int BOX_HEIGHT = 285;
    private final String[] keys;
    private final String[] labels;
    private int moment;
    private int page;
    private boolean needsRebuild;

    public MusicSettingsScreen(String[] selected, String[] keys, String[] labels) {
        super(Component.translatable("item.mdpromod.music_settings"));
        this.keys = keys == null ? new String[0] : (String[])keys.clone();
        this.labels = labels == null ? new String[0] : (String[])labels.clone();
    }

    private String momentKey() {
        return DuelBgmRules.MOMENTS.get(this.moment);
    }

    private String currentTrack() {
        return ClientMusicSettingsBridge.trackFor(this.momentKey());
    }

    private int entries() {
        return this.keys.length + 1;
    }

    private int pageCount() {
        return Math.max(1, (this.entries() + 9 - 1) / 9);
    }

    private String keyAt(int index) {
        return index == 0 ? "off" : this.keys[index - 1];
    }

    private String labelAt(int index) {
        if (index == 0) {
            return DuelBgmTracks.label("off");
        } else {
            return index - 1 < this.labels.length ? this.labels[index - 1] : this.keys[index - 1];
        }
    }

    protected void init() {
        this.page = Math.max(0, Math.min(this.page, this.pageCount() - 1));
        int left = (this.width - 400) / 2;
        int top = this.height / 2 - 142;
        int tabWidth = 352 / DuelBgmRules.MOMENTS.size();

        for (int i = 0; i < DuelBgmRules.MOMENTS.size(); i++) {
            int index = i;
            String key = DuelBgmRules.MOMENTS.get(i);
            String label = (i == this.moment ? "▶ " : "") + DuelBgmRules.momentLabel(key);
            this.addRenderableWidget(Button.builder(Component.literal(label), button -> {
                this.moment = index;
                this.page = 0;
                this.requestRebuild();
            }).bounds(left + 24 + i * tabWidth, top + 24, tabWidth - 2, 20).build());
        }

        int y = top + 62;

        for (int row = 0; row < 9; row++) {
            int index = this.page * 9 + row;
            if (index >= this.entries()) {
                break;
            }

            String key = this.keyAt(index);
            String label = this.labelAt(index);
            if (DuelBgmRules.localChoice(key).equals(this.currentTrack())) {
                label = "✓ " + label;
            }

            this.addRenderableWidget(Button.builder(Component.literal(label), button -> this.choose(key)).bounds(left + 24, y, 310, 19).build());
            this.addRenderableWidget(Button.builder(Component.literal("♪"), button -> this.audition(key)).bounds(left + 400 - 62, y, 38, 19).build());
            y += 21;
        }

        int navY = top + 285 - 34;
        this.addRenderableWidget(Button.builder(Component.literal("◀"), button -> this.turn(-1)).bounds(left + 24, navY, 34, 20).build());
        this.addRenderableWidget(Button.builder(Component.literal("▶"), button -> this.turn(1)).bounds(left + 62, navY, 34, 20).build());
        this.addRenderableWidget(
            Button.builder(Component.literal(ClientDuelAudio.isPreviewing() ? "■ 停止试听" : "♪ 试听"), button -> this.togglePreview())
                .bounds(left + 104, navY, 110, 20)
                .build()
        );
        this.addRenderableWidget(Button.builder(Component.translatable("gui.done"), button -> this.onClose()).bounds(left + 400 - 124, navY, 100, 20).build());
    }

    private void turn(int delta) {
        int next = Math.max(0, Math.min(this.pageCount() - 1, this.page + delta));
        if (next != this.page) {
            this.page = next;
            this.requestRebuild();
        }
    }

    private void choose(String key) {
        ClientMusicSettingsBridge.apply(this.momentKey(), key);
        ModNetwork.sendToServer(new ModNetwork.MusicSettingsAction(this.momentKey(), key));
        this.requestRebuild();
    }

    private void audition(String key) {
        ClientDuelAudio.preview(key);
        this.requestRebuild();
    }

    private void togglePreview() {
        if (ClientDuelAudio.isPreviewing()) {
            ClientDuelAudio.stopPreview();
        } else {
            ClientDuelAudio.preview(this.currentTrack());
        }

        this.requestRebuild();
    }

    private void requestRebuild() {
        this.needsRebuild = true;
    }

    public void tick() {
        if (this.needsRebuild) {
            this.needsRebuild = false;
            this.rebuildWidgets();
        }
    }

    public boolean keyPressed(int keyCode, int scanCode, int modifiers) {
        if (ClientDuelAudio.isPreviewing()) {
            ClientDuelAudio.stopPreview();
            this.requestRebuild();
        }

        return super.keyPressed(keyCode, scanCode, modifiers);
    }

    public void onClose() {
        ClientDuelAudio.stopPreview();
        super.onClose();
    }

    public void removed() {
        ClientDuelAudio.stopPreview();
        super.removed();
    }

    public boolean isPauseScreen() {
        return false;
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.renderBackground(graphics);
        int left = (this.width - 400) / 2;
        int top = this.height / 2 - 142;
        McPanel.panel(graphics, left, top, left + 400, top + 285);
        graphics.drawCenteredString(this.font, this.title, this.width / 2, top + 8, 4210752);
        graphics.drawCenteredString(
            this.font,
            DuelBgmRules.momentLabel(this.momentKey())
                + "："
                + DuelBgmTracks.label(this.currentTrack())
                + "　（第 "
                + (this.page + 1)
                + "/"
                + this.pageCount()
                + " 页，滚轮翻页）",
            this.width / 2,
            top + 47,
            5592405
        );
        super.render(graphics, mouseX, mouseY, partialTick);
    }

    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        this.turn(delta < 0.0 ? 1 : -1);
        return true;
    }
}
