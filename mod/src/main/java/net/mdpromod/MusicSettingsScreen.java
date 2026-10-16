package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

/**
 * 「音乐设置」界面：按**时机**分栏（决斗 / 王牌登场 / 低 LP 高潮），每栏各选一首。
 *
 * <ul>
 *   <li>上面三个是栏目标签；下面列出该栏可选的全部曲目，当前那首打 ✓。</li>
 *   <li>点曲目名 = 设为该栏的曲子；点右侧 ♪ = 单独试听这一首。</li>
 *   <li>底部「试听」按钮试听当前栏已设好的那首。</li>
 *   <li>**关闭界面、按任意键、或开始下一首试听，都会结束当前试听。**</li>
 * </ul>
 */
public final class MusicSettingsScreen extends Screen {

    private static final String OFF_KEY = "off";
    private static final int ROWS = 9;
    private static final int BOX_WIDTH = 400;
    private static final int ROW_HEIGHT = 21;
    private static final int BOX_HEIGHT = ROWS * ROW_HEIGHT + 96;

    private final String[] keys;
    private final String[] labels;
    private int moment;
    private int page;
    /**
     * 控件列表不能在按钮回调里重建——那一刻 MC 正在遍历 children。
     * 统一打标记，等 tick 里再重建。
     */
    private boolean needsRebuild;

    public MusicSettingsScreen(String[] selected, String[] keys, String[] labels) {
        super(Component.translatable("item.mdpromod.music_settings"));
        this.keys = keys == null ? new String[0] : keys.clone();
        this.labels = labels == null ? new String[0] : labels.clone();
    }

    private String momentKey() {
        return DuelBgmRules.MOMENTS.get(moment);
    }

    private String currentTrack() {
        return ClientMusicSettingsBridge.trackFor(momentKey());
    }

    private int entries() {
        return keys.length + 1;
    }

    private int pageCount() {
        return Math.max(1, (entries() + ROWS - 1) / ROWS);
    }

    private String keyAt(int index) {
        return index == 0 ? OFF_KEY : keys[index - 1];
    }

    private String labelAt(int index) {
        if (index == 0) return DuelBgmTracks.label(OFF_KEY);
        return index - 1 < labels.length ? labels[index - 1] : keys[index - 1];
    }

    @Override
    protected void init() {
        page = Math.max(0, Math.min(page, pageCount() - 1));
        int left = (width - BOX_WIDTH) / 2;
        int top = height / 2 - BOX_HEIGHT / 2;

        // 栏目标签
        int tabWidth = (BOX_WIDTH - 48) / DuelBgmRules.MOMENTS.size();
        for (int i = 0; i < DuelBgmRules.MOMENTS.size(); i++) {
            final int index = i;
            String key = DuelBgmRules.MOMENTS.get(i);
            String label = (i == moment ? "▶ " : "") + DuelBgmRules.momentLabel(key);
            addRenderableWidget(Button.builder(Component.literal(label), button -> {
                moment = index;
                page = 0;
                requestRebuild();
            }).bounds(left + 24 + i * tabWidth, top + 24, tabWidth - 2, 20).build());
        }

        // 曲目行：[名字] [♪]
        int y = top + 62;
        for (int row = 0; row < ROWS; row++) {
            int index = page * ROWS + row;
            if (index >= entries()) break;
            final String key = keyAt(index);
            String label = labelAt(index);
            if (DuelBgmRules.localChoice(key).equals(currentTrack())) {
                label = "✓ " + label;
            }
            addRenderableWidget(Button.builder(Component.literal(label), button -> choose(key))
                    .bounds(left + 24, y, BOX_WIDTH - 90, 19).build());
            addRenderableWidget(Button.builder(Component.literal("♪"), button -> audition(key))
                    .bounds(left + BOX_WIDTH - 62, y, 38, 19).build());
            y += ROW_HEIGHT;
        }

        // 底部：翻页 / 试听 / 关闭
        int navY = top + BOX_HEIGHT - 34;
        addRenderableWidget(Button.builder(Component.literal("◀"), button -> turn(-1))
                .bounds(left + 24, navY, 34, 20).build());
        addRenderableWidget(Button.builder(Component.literal("▶"), button -> turn(1))
                .bounds(left + 62, navY, 34, 20).build());
        addRenderableWidget(Button.builder(
                        Component.literal(ClientDuelAudio.isPreviewing() ? "■ 停止试听" : "♪ 试听"),
                        button -> togglePreview())
                .bounds(left + 104, navY, 110, 20).build());
        addRenderableWidget(Button.builder(Component.translatable("gui.done"), button -> onClose())
                .bounds(left + BOX_WIDTH - 124, navY, 100, 20).build());
    }

    private void turn(int delta) {
        int next = Math.max(0, Math.min(pageCount() - 1, page + delta));
        if (next == page) return;
        page = next;
        requestRebuild();
    }

    /** 把这一首设为当前栏目的曲子（不动试听）。 */
    private void choose(String key) {
        ClientMusicSettingsBridge.apply(momentKey(), key);
        ModNetwork.sendToServer(new ModNetwork.MusicSettingsAction(momentKey(), key));
        requestRebuild();
    }

    /** 试听某一首；开新的一首会先结束上一首。 */
    private void audition(String key) {
        ClientDuelAudio.preview(key);
        requestRebuild();
    }

    private void togglePreview() {
        if (ClientDuelAudio.isPreviewing()) {
            ClientDuelAudio.stopPreview();
        } else {
            ClientDuelAudio.preview(currentTrack());
        }
        requestRebuild();
    }

    private void requestRebuild() {
        needsRebuild = true;
    }

    @Override
    public void tick() {
        if (needsRebuild) {
            needsRebuild = false;
            rebuildWidgets();
        }
    }

    @Override
    public boolean keyPressed(int keyCode, int scanCode, int modifiers) {
        // 「按别的键位都结束试听」：任何按键先停掉，再走原本的处理
        if (ClientDuelAudio.isPreviewing()) {
            ClientDuelAudio.stopPreview();
            requestRebuild();
        }
        return super.keyPressed(keyCode, scanCode, modifiers);
    }

    @Override
    public void onClose() {
        ClientDuelAudio.stopPreview();
        super.onClose();
    }

    @Override
    public void removed() {
        // 被别的界面顶掉（按 E 开背包等）也要停
        ClientDuelAudio.stopPreview();
        super.removed();
    }

    @Override
    public boolean isPauseScreen() {
        return false;
    }

    @Override
    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        renderBackground(graphics);
        int left = (width - BOX_WIDTH) / 2;
        int top = height / 2 - BOX_HEIGHT / 2;
        McPanel.panel(graphics, left, top, left + BOX_WIDTH, top + BOX_HEIGHT);
        graphics.drawCenteredString(font, title, width / 2, top + 8, 0x404040);
        graphics.drawCenteredString(font,
                DuelBgmRules.momentLabel(momentKey()) + "：" + DuelBgmTracks.label(currentTrack())
                        + "　（第 " + (page + 1) + "/" + pageCount() + " 页，滚轮翻页）",
                width / 2, top + 47, 0x555555);
        super.render(graphics, mouseX, mouseY, partialTick);
    }

    @Override
    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        turn(delta < 0 ? 1 : -1);
        return true;
    }
}
