package net.mdpromod;

import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.EditBox;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.util.FormattedCharSequence;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;

/**
 * 卡盒编辑器（人用版）：
 * 左 = 卡面详情（大图 + 小字效果文本，滚轮翻页），中 = 当前卡组（主/额外/副三区，每行十张），
 * 右 = 卡池检索（只留搜索框和「查找」，详细条件在第二层界面）。
 */
@OnlyIn(Dist.CLIENT)
public class CardCaseScreen extends Screen {
    private static final int GAP = 2;
    private static final int SECTION_GAP = 4;
    private static final int ART_W = 176;
    private static final int ART_H = 256;
    private static final int DECK_COLUMNS = 10;
    private static final int ROW_H = 22;
    private static final float TEXT_SCALE = 0.72F;

    private static final int COLOR_PANEL = 0xFFC6C6C6;
    private static final int COLOR_LIGHT = 0xFFFFFFFF;
    private static final int COLOR_DARK = 0xFF555555;
    private static final int COLOR_SLOT = 0xFF8B8B8B;
    private static final int COLOR_SLOT_DARK = 0xFF373737;
    private static final int COLOR_TEXT = 0xFFFFFFFF;
    private static final int COLOR_TITLE = 0x404040;
    /** 标题右边那半句（当前禁卡表）：比标题淡一点。 */
    private static final int COLOR_TITLE_DIM = 0x6A6A6A;
    /** 真的开着卡表时换个颜色，好和"未启用"一眼区分。 */
    private static final int COLOR_TITLE_BANLIST = 0x2F5F8F;
    private static final int COLOR_BODY = 0xFF303030;
    private static final int COLOR_HOVER = 0x50FFFFFF;
    private static final int COLOR_ACTIVE = 0x3300A000;
    private static final int COLOR_GOOD = 0x1F6F1F;
    private static final int COLOR_BAD = 0x9C1B1B;

    private static final String[] ZONE_NAMES = {"主卡组", "额外卡组", "副卡组"};
    private static final int[] ZONE_CAP = {DeckRules.MAIN_MAX, DeckRules.EXTRA_MAX, DeckRules.SIDE_MAX};

    private final DeckData deck = new DeckData();
    private final CardFilter filter = new CardFilter();
    private final List<CardInfo> results = new ArrayList<>();
    private final List<List<Integer>> zoneCodes = List.of(new ArrayList<>(), new ArrayList<>(), new ArrayList<>());
    private final List<List<Integer>> zoneCounts = List.of(new ArrayList<>(), new ArrayList<>(), new ArrayList<>());
    private final int[] zoneScroll = new int[3];
    private final int[] zoneRows = new int[3];
    private final int[] sectionY = new int[3];
    private final int[] sectionGridY = new int[3];
    private final int[] sectionGridH = new int[3];

    private int leftPos;
    private int topPos;
    private int panelW;
    private int panelH;
    private int contentY;
    private int contentH;
    private int statusY;

    private int detailX;
    private int detailW;
    private int detailArtW;
    private int detailArtH;
    private int detailTextTop;
    private int detailTextH;

    private int centerX;
    private int centerW;
    private int cellW;
    private int cellH;

    private int poolX;
    private int poolW;
    private int poolListY;
    private int poolListH;
    private int poolRows;
    private int poolScroll;

    private int activeZone;
    private int selectedCode = -1;
    private int detailScroll;
    private int detailTextHeight;
    private List<FormattedCharSequence> detailLines = new ArrayList<>();
    private String query = "";
    private List<String> problems = new ArrayList<>();

    private int hoveredZone = -1;
    private int hoveredZoneIndex = -1;
    private int hoveredPool = -1;

    private EditBox nameBox;
    private EditBox searchBox;

    public CardCaseScreen(DeckData initial) {
        super(Component.literal("卡盒"));
        if (initial != null) {
            deck.mainDeck().addAll(initial.mainDeck());
            deck.extraDeck().addAll(initial.extraDeck());
            deck.sideDeck().addAll(initial.sideDeck());
            deck.setName(initial.name());
            if (!deck.mainDeck().isEmpty()) selectedCode = deck.mainDeck().get(0);
        }
    }

    // ------------------------------------------------------------ 初始化

    @Override
    protected void init() {
        super.init();
        layout();

        this.nameBox = new EditBox(this.font, leftPos + 34, topPos + 4, 110, 14, Component.literal("卡组名"));
        this.nameBox.setMaxLength(32);
        this.nameBox.setHint(Component.literal("卡组名"));
        this.nameBox.setValue(deck.name());
        addRenderableWidget(this.nameBox);

        addRenderableWidget(Button.builder(Component.literal("完成"), b -> onClose())
                .bounds(leftPos + panelW - 52, topPos + 3, 46, 16).build());

        this.searchBox = new EditBox(this.font, poolX + 3, contentY + 3,
                Math.max(48, poolW - 66), 15, Component.literal("搜索"));
        this.searchBox.setMaxLength(24);
        this.searchBox.setHint(Component.literal("卡名 / 卡号"));
        this.searchBox.setValue(query);
        this.searchBox.setResponder(value -> {
            query = value;
            poolScroll = 0;
            rebuildResults();
        });
        addRenderableWidget(this.searchBox);

        addRenderableWidget(Button.builder(Component.literal("查找"), b -> {
            if (this.minecraft != null) {
                this.minecraft.setScreen(new CardSearchScreen(this, filter));
            }
        }).bounds(poolX + poolW - 60, contentY + 3, 56, 15).build());

        rebuildDeckViews();
        rebuildResults();
        rebuildDetailLines();
        refreshProblems();
        ModNetwork.sendToServer(new ModNetwork.CardCaseAction(3, 0, 0, ""));
    }

    private void layout() {
        panelW = Math.min(Math.max(this.width - 8, 260), Math.min(900, this.width));
        panelH = Math.min(Math.max(this.height - 8, 170), Math.min(520, this.height));
        leftPos = (this.width - panelW) / 2;
        topPos = (this.height - panelH) / 2;

        int pad = 5;
        int columnGap = 5;
        contentY = topPos + 22;
        statusY = topPos + panelH - 14;
        contentH = Math.max(90, statusY - contentY - 4);

        int usable = panelW - pad * 2 - columnGap * 2;
        int detailWish = clamp((int) (usable * 0.20), 88, 132);
        int poolWish = clamp((int) (usable * 0.24), 104, 190);
        int centerWish = usable - detailWish - poolWish;
        if (centerWish < DECK_COLUMNS * 14) {
            int deficit = DECK_COLUMNS * 14 - centerWish;
            int shrink = Math.min(deficit, detailWish - 78);
            detailWish -= shrink;
            deficit -= shrink;
            shrink = Math.min(deficit, poolWish - 92);
            poolWish -= shrink;
            centerWish = Math.max(40, usable - detailWish - poolWish);
        }

        detailX = leftPos + pad;
        detailW = detailWish;
        centerX = detailX + detailW + columnGap;
        centerW = centerWish;
        poolX = centerX + centerW + columnGap;
        poolW = poolWish;

        // 左：详情
        int artTop = contentY + 16;
        int textBottom = contentY + contentH - 5;
        int textBlock = 30;
        int minTextH = 46;
        detailArtW = Math.min(detailW - 10, 112);
        detailArtH = detailArtW * ART_H / ART_W;
        int maxArtH = Math.max(36, textBottom - minTextH - artTop - textBlock);
        if (detailArtH > maxArtH) {
            detailArtH = maxArtH;
            detailArtW = detailArtH * ART_W / ART_H;
        }
        detailTextTop = artTop + detailArtH + textBlock;
        detailTextH = Math.max(0, textBottom - detailTextTop);

        // 中：三区
        int gridX = centerX + 3;
        int gridW = centerW - 6;
        cellW = clamp((gridW - (DECK_COLUMNS - 1) * GAP) / DECK_COLUMNS, 12, 46);
        cellH = cellW * ART_H / ART_W;
        int available = contentH - SECTION_GAP * 2;
        int mainH = Math.max(48, (int) (available * 0.52));
        int extraH = Math.max(34, (int) ((available - mainH) * 0.5));
        int sideH = Math.max(34, available - mainH - extraH);
        int[] heights = {mainH, extraH, sideH};
        int y = contentY;
        for (int i = 0; i < 3; i++) {
            sectionY[i] = y;
            sectionGridY[i] = y + 12;
            sectionGridH[i] = Math.max(20, heights[i] - 12);
            zoneRows[i] = Math.max(1, (sectionGridH[i] + GAP) / (cellH + GAP));
            y += heights[i] + SECTION_GAP;
        }

        // 右：检索结果列表
        poolListY = contentY + 21;
        poolListH = Math.max(24, statusY - 4 - poolListY);
        poolRows = Math.max(1, poolListH / ROW_H);
    }

    private static int clamp(int value, int min, int max) {
        return Math.max(min, Math.min(max, value));
    }

    // ------------------------------------------------------------ 数据

    public void applyDeck(DeckData incoming) {
        deck.mainDeck().clear();
        deck.mainDeck().addAll(incoming.mainDeck());
        deck.extraDeck().clear();
        deck.extraDeck().addAll(incoming.extraDeck());
        deck.sideDeck().clear();
        deck.sideDeck().addAll(incoming.sideDeck());
        deck.setName(incoming.name());
        if (nameBox != null && !nameBox.isFocused()) nameBox.setValue(deck.name());
        rebuildDeckViews();
        rebuildDetailLines();
        refreshProblems();
    }

    private List<Integer> zoneList(int zone) {
        return switch (zone) {
            case 1 -> deck.extraDeck();
            case 2 -> deck.sideDeck();
            default -> deck.mainDeck();
        };
    }

    private void rebuildDeckViews() {
        for (int zone = 0; zone < 3; zone++) {
            List<Integer> codes = zoneCodes.get(zone);
            List<Integer> counts = zoneCounts.get(zone);
            codes.clear();
            counts.clear();
            Map<Integer, Integer> grouped = new LinkedHashMap<>();
            for (int code : zoneList(zone)) grouped.merge(code, 1, Integer::sum);
            for (Map.Entry<Integer, Integer> entry : grouped.entrySet()) {
                codes.add(entry.getKey());
                counts.add(entry.getValue());
            }
            zoneScroll[zone] = clampScroll(zoneScroll[zone], codes.size(), zoneRows[zone] * DECK_COLUMNS);
        }
    }

    private void rebuildResults() {
        results.clear();
        String needle = query.trim().toLowerCase(Locale.ROOT);
        for (CardInfo info : CardRegistry.all()) {
            if (!filter.matches(info)) continue;
            if (!needle.isEmpty()) {
                boolean byName = info.name().toLowerCase(Locale.ROOT).contains(needle);
                boolean byCode = Integer.toString(info.code()).startsWith(needle);
                if (!byName && !byCode) continue;
            }
            results.add(info);
        }
        Comparator<CardInfo> comparator = switch (filter.sortMode) {
            case 1 -> Comparator.comparingInt((CardInfo c) -> -c.level()).thenComparingInt(CardInfo::code);
            case 2 -> Comparator.comparingInt((CardInfo c) -> -c.atk()).thenComparingInt(CardInfo::code);
            case 3 -> Comparator.comparingInt((CardInfo c) -> -c.def()).thenComparingInt(CardInfo::code);
            case 4 -> Comparator.comparingInt(CardInfo::code);
            default -> Comparator.comparingInt(CardCaseScreen::kindRank).thenComparingInt(CardInfo::code);
        };
        results.sort(comparator);
        poolScroll = clampScroll(poolScroll, results.size(), poolRows);
    }

    private static int kindRank(CardInfo info) {
        if (info.isExtraDeck()) return 2;
        if (info.isMonster()) return 0;
        if (info.isSpell()) return 3;
        if (info.isTrap()) return 4;
        return 5;
    }

    private void rebuildDetailLines() {
        detailLines = new ArrayList<>();
        detailTextHeight = 0;
        if (selectedCode <= 0) return;
        CardInfo info = CardRegistry.get(selectedCode).orElse(null);
        if (info == null) return;
        String text = info.desc() == null || info.desc().isBlank() ? "（这张卡没有效果文本）" : info.desc();
        int wrap = Math.max(40, (int) ((detailW - 12) / TEXT_SCALE));
        detailLines.addAll(this.font.split(Component.literal(text), wrap));
        detailTextHeight = detailLines.size();
        detailScroll = clampScroll(detailScroll, detailTextHeight, visibleDetailLines());
    }

    private int visibleDetailLines() {
        return Math.max(1, (int) (detailTextH / (9 * TEXT_SCALE)));
    }

    private void refreshProblems() {
        problems = DeckRules.problems(deck);
    }

    private CardInfo selectedCard() {
        return selectedCode > 0 ? CardRegistry.get(selectedCode).orElse(null) : null;
    }

    private void addCard(int code) {
        CardInfo info = CardRegistry.get(code).orElse(null);
        if (info == null) return;
        int zone = activeZone;
        if (info.isExtraDeck() && zone == 0) {
            zone = 1;
            feedback("「" + info.name() + "」是额外卡组卡，已放进额外卡组");
        }
        if (!info.isExtraDeck() && zone == 1) {
            zone = 0;
            feedback("「" + info.name() + "」不能放进额外卡组，已放进主卡组");
        }
        List<Integer> target = zoneList(zone);
        if (target.size() >= ZONE_CAP[zone]) {
            feedback(ZONE_NAMES[zone] + "最多 " + ZONE_CAP[zone] + " 张");
            return;
        }
        int limit = DeckRules.limitOf(code);
        if (DeckRules.copies(deck, code) >= limit) {
            String reason = DeckRules.limitSuffix(code);
            String mates = DeckRules.groupMateText(deck, code);
            feedback(limit == 0 ? "「" + info.name() + "」是禁止卡" + reason + mates
                    : "「" + info.name() + "」最多放 " + limit + " 张" + reason + mates);
            return;
        }
        target.add(code);
        activeZone = zone;
        selectedCode = code;
        rebuildDeckViews();
        rebuildDetailLines();
        refreshProblems();
        ModNetwork.sendToServer(new ModNetwork.CardCaseAction(0, zone, code, ""));
    }

    private void removeCard(int code, int zone) {
        List<Integer> target = zoneList(zone);
        if (!target.remove(Integer.valueOf(code))) {
            feedback("「" + DeckRules.displayName(code) + "」不在" + ZONE_NAMES[zone] + "里");
            return;
        }
        selectedCode = code;
        rebuildDeckViews();
        rebuildDetailLines();
        refreshProblems();
        ModNetwork.sendToServer(new ModNetwork.CardCaseAction(1, zone, code, ""));
    }

    /** 从包里移除一张：先看当前区，再依次找主 / 额外 / 副。 */
    private void removeAnywhere(int code) {
        int[] order = {activeZone, 0, 1, 2};
        for (int zone : order) {
            if (zoneList(zone).remove(Integer.valueOf(code))) {
                selectedCode = code;
                rebuildDeckViews();
                rebuildDetailLines();
                refreshProblems();
                ModNetwork.sendToServer(new ModNetwork.CardCaseAction(1, zone, code, ""));
                return;
            }
        }
        feedback("卡组里没有「" + DeckRules.displayName(code) + "」");
    }

    private void sendName() {
        if (nameBox == null) return;
        String value = nameBox.getValue().trim();
        if (value.isEmpty() || value.equals(deck.name())) return;
        deck.setName(value);
        ModNetwork.sendToServer(new ModNetwork.CardCaseAction(2, 0, 0, value));
    }

    private void feedback(String message) {
        if (this.minecraft != null && this.minecraft.player != null) {
            this.minecraft.player.displayClientMessage(Component.literal(message), true);
        }
    }

    // ------------------------------------------------------------ 绘制

    @Override
    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        renderBackground(graphics);
        drawWindow(graphics);
        drawDetail(graphics);
        drawCenter(graphics, mouseX, mouseY);
        drawPool(graphics, mouseX, mouseY);
        drawStatus(graphics);
        super.render(graphics, mouseX, mouseY, partialTick);

        CardInfo hovered = hoveredCard();
        if (hovered != null) {
            List<FormattedCharSequence> lines = new ArrayList<>();
            lines.add(Component.literal(hovered.name()).getVisualOrderText());
            lines.add(Component.literal(hovered.typeLine() + "  #" + hovered.code()).getVisualOrderText());
            String stats = hovered.isMonster()
                    ? "ATK " + hovered.atk() + " / DEF " + hovered.def()
                    : hovered.typeLabel();
            lines.add(Component.literal(stats + " · 卡组内 " + DeckRules.copies(deck, hovered.code())
                    + " 张 · " + DeckRules.limitText(hovered.code())).getVisualOrderText());
            graphics.renderTooltip(this.font, lines, mouseX, mouseY);
        }
    }

    private CardInfo hoveredCard() {
        if (hoveredPool >= 0 && hoveredPool < results.size()) return results.get(hoveredPool);
        if (hoveredZone >= 0 && hoveredZoneIndex >= 0) {
            List<Integer> codes = zoneCodes.get(hoveredZone);
            if (hoveredZoneIndex < codes.size()) {
                return CardRegistry.get(codes.get(hoveredZoneIndex)).orElse(null);
            }
        }
        return null;
    }

    private void drawWindow(GuiGraphics graphics) {
        graphics.fill(leftPos - 1, topPos - 1, leftPos + panelW + 1, topPos + panelH + 1, 0xFF000000);
        graphics.fill(leftPos, topPos, leftPos + panelW, topPos + panelH, COLOR_PANEL);
        graphics.fill(leftPos, topPos, leftPos + panelW, topPos + 1, COLOR_LIGHT);
        graphics.fill(leftPos, topPos, leftPos + 1, topPos + panelH, COLOR_LIGHT);
        graphics.fill(leftPos, topPos + panelH - 1, leftPos + panelW, topPos + panelH, COLOR_DARK);
        graphics.fill(leftPos + panelW - 1, topPos, leftPos + panelW, topPos + panelH, COLOR_DARK);
        graphics.drawString(this.font, Component.literal("卡盒"), leftPos + 8, topPos + 7, COLOR_TITLE, false);
        // 当前生效的禁卡表（在 banlist.conf 里开关）：一眼能看出卡组受哪张表管
        int banlistX = leftPos + 8 + this.font.width("卡盒") + 8;
        graphics.drawString(this.font,
                Component.literal(trim(DeckRules.banlistStatus(), leftPos + panelW - 8 - banlistX)),
                banlistX, topPos + 7,
                DeckRules.banlists().isEmpty() ? COLOR_TITLE_DIM : COLOR_TITLE_BANLIST, false);
    }

    private void drawDetail(GuiGraphics graphics) {
        drawFrame(graphics, detailX, contentY, detailW, contentH, "卡片详情");
        CardInfo info = selectedCard();
        int artX = detailX + (detailW - detailArtW) / 2;
        int artY = contentY + 14;
        drawSlot(graphics, artX, artY, detailArtW, detailArtH);
        if (info == null) {
            graphics.drawString(this.font, Component.literal("点一张卡看详情"),
                    detailX + 5, artY + 4, COLOR_TITLE, false);
            return;
        }
        ResourceLocation art = CardTextureCache.get(info.code());
        graphics.blit(art, artX + 1, artY + 1, detailArtW - 2, detailArtH - 2,
                0.0F, 0.0F, ART_W, ART_H, ART_W, ART_H);

        int textY = artY + detailArtH + 3;
        int textX = detailX + 5;
        int textW = detailW - 10;
        drawFitText(graphics, info.name(), textX, textY, textW, COLOR_BODY, 0.55F);
        textY += 10;
        drawFitText(graphics, info.typeLine(), textX, textY, textW, COLOR_BODY, 0.6F);
        textY += 10;
        String stats = info.isMonster()
                ? "ATK " + info.atk() + " / DEF " + info.def()
                : "";
        if (!stats.isEmpty()) {
            drawFitText(graphics, stats, textX, textY, textW, COLOR_BODY, 0.6F);
        }

        int areaY = detailTextTop;
        int areaH = detailTextH;
        if (areaH < 8) return;
        drawSlot(graphics, detailX + 4, areaY - 2, detailW - 8, areaH + 4);
        graphics.enableScissor(detailX + 5, areaY, detailX + detailW - 5, areaY + areaH);
        graphics.pose().pushPose();
        graphics.pose().scale(TEXT_SCALE, TEXT_SCALE, 1.0F);
        int baseX = (int) ((detailX + 6) / TEXT_SCALE);
        int baseY = (int) (areaY / TEXT_SCALE);
        for (int i = detailScroll; i < detailLines.size(); i++) {
            int lineY = baseY + (i - detailScroll) * 9;
            if (lineY * TEXT_SCALE >= areaY + areaH) break;
            graphics.drawString(this.font, detailLines.get(i), baseX, lineY, COLOR_BODY, false);
        }
        graphics.pose().popPose();
        graphics.disableScissor();
    }

    private void drawCenter(GuiGraphics graphics, int mouseX, int mouseY) {
        hoveredZone = -1;
        hoveredZoneIndex = -1;
        for (int zone = 0; zone < 3; zone++) {
            int y = sectionY[zone];
            int height = sectionGridY[zone] + sectionGridH[zone] - y + 2;
            drawFrame(graphics, centerX, y, centerW, height, null);
            boolean active = zone == activeZone;
            if (active) {
                graphics.fill(centerX + 1, y + 1, centerX + centerW - 1, y + 12, COLOR_ACTIVE);
            }
            String header = (active ? "▶ " : "") + ZONE_NAMES[zone] + " "
                    + zoneList(zone).size() + "/" + ZONE_CAP[zone];
            graphics.drawString(this.font, Component.literal(header), centerX + 5, y + 2,
                    active ? 0xFF1F4F1F : COLOR_TITLE, false);
            String hint = "左键选中 · 右键移除一张";
            graphics.drawString(this.font, Component.literal(hint),
                    centerX + centerW - 5 - this.font.width(hint), y + 2, COLOR_TITLE, false);

            int gridX = centerX + 3;
            int gridY = sectionGridY[zone];
            int gridW = centerW - 6;
            int gridH = sectionGridH[zone];
            drawSlot(graphics, gridX, gridY, gridW, gridH);

            List<Integer> codes = zoneCodes.get(zone);
            if (codes.isEmpty()) {
                graphics.drawString(this.font, Component.literal("（空）"), gridX + 4, gridY + 4, COLOR_TITLE, false);
                continue;
            }
            if (gridW < 24 || gridH < 16) continue;
            graphics.enableScissor(gridX + 1, gridY + 1, gridX + gridW - 1, gridY + gridH - 1);
            for (int row = 0; row < zoneRows[zone]; row++) {
                for (int col = 0; col < DECK_COLUMNS; col++) {
                    int index = zoneScroll[zone] + row * DECK_COLUMNS + col;
                    if (index >= codes.size()) break;
                    int x = gridX + 1 + col * (cellW + GAP);
                    int cellY = gridY + 1 + row * (cellH + GAP);
                    int code = codes.get(index);
                    boolean hover = inside(mouseX, mouseY, x, cellY, cellW, cellH);
                    if (hover) {
                        hoveredZone = zone;
                        hoveredZoneIndex = index;
                    }
                    CardInfo info = CardRegistry.get(code).orElse(null);
                    if (info != null) {
                        drawCell(graphics, info, x, cellY, cellW, cellH,
                                zoneCounts.get(zone).get(index), code == selectedCode, hover);
                    }
                }
            }
            graphics.disableScissor();
            int total = codes.size();
            int visible = zoneRows[zone] * DECK_COLUMNS;
            if (total > visible) {
                String page = (zoneScroll[zone] / DECK_COLUMNS + 1) + "/"
                        + ((total + DECK_COLUMNS - 1) / DECK_COLUMNS);
                graphics.drawString(this.font, Component.literal(page),
                        centerX + centerW - 5 - this.font.width(page), gridY + gridH - 10, COLOR_TEXT, true);
            }
        }
    }

    private void drawPool(GuiGraphics graphics, int mouseX, int mouseY) {
        drawFrame(graphics, poolX, contentY, poolW, contentH, null);
        String note = results.size() + " 张";
        graphics.drawString(this.font, Component.literal(note),
                poolX + 4, poolListY - 10, COLOR_TITLE, false);
        String rightNote = filter.activeCount() > 0
                ? "条件 " + filter.activeCount() + " 项"
                : "左键加入·右键移除";
        graphics.drawString(this.font, Component.literal(rightNote),
                poolX + poolW - 4 - this.font.width(rightNote), poolListY - 10,
                filter.activeCount() > 0 ? 0xFF1F4F1F : COLOR_TITLE, false);
        drawSlot(graphics, poolX + 3, poolListY, poolW - 6, poolListH);
        hoveredPool = -1;
        if (results.isEmpty()) {
            graphics.drawString(this.font, Component.literal("没找到卡片"),
                    poolX + 7, poolListY + 4, COLOR_TITLE, false);
            return;
        }
        if (poolW < 60 || poolListH < 16) return;
        graphics.enableScissor(poolX + 4, poolListY + 1, poolX + poolW - 4, poolListY + poolListH - 1);
        for (int row = 0; row < poolRows; row++) {
            int index = poolScroll + row;
            if (index >= results.size()) break;
            CardInfo info = results.get(index);
            int rowY = poolListY + 1 + row * ROW_H;
            boolean hover = inside(mouseX, mouseY, poolX + 4, rowY, poolW - 8, ROW_H);
            if (hover) {
                hoveredPool = index;
                graphics.fill(poolX + 4, rowY, poolX + poolW - 4, rowY + ROW_H, COLOR_HOVER);
            }
            ResourceLocation art = CardTextureCache.get(info.code());
            graphics.blit(art, poolX + 5, rowY + 2, 14, 20, 0.0F, 0.0F, ART_W, ART_H, ART_W, ART_H);
            graphics.drawString(this.font, trim(info.name(), poolW - 40), poolX + 22, rowY + 3,
                    COLOR_BODY, false);
            graphics.drawString(this.font, "#" + info.code(), poolX + 22, rowY + 12, 0xFF6F6F6F, false);
            int copies = DeckRules.copies(deck, info.code());
            if (copies > 0) {
                String count = "x" + copies;
                graphics.drawString(this.font, count, poolX + poolW - 6 - this.font.width(count),
                        rowY + 7, COLOR_TEXT, true);
            }
        }
        graphics.disableScissor();
    }

    private void drawStatus(GuiGraphics graphics) {
        String counters = "主 " + deck.mainDeck().size() + "/60  额外 " + deck.extraDeck().size()
                + "/15  副 " + deck.sideDeck().size() + "/15";
        counters = trim(counters, Math.max(70, panelW / 2));
        int countersX = leftPos + panelW - 8 - this.font.width(counters);
        graphics.drawString(this.font, Component.literal(counters), countersX, statusY + 3, COLOR_TITLE, false);
        String line = problems.isEmpty() ? "卡组合法 · 改动即时保存" : problems.get(0);
        graphics.drawString(this.font, Component.literal(trim(line, countersX - leftPos - 16)),
                leftPos + 8, statusY + 3, problems.isEmpty() ? COLOR_GOOD : COLOR_BAD, false);
    }

    private void drawFrame(GuiGraphics graphics, int x, int y, int w, int h, String title) {
        graphics.fill(x, y, x + w, y + h, COLOR_PANEL);
        graphics.fill(x, y, x + w, y + 1, COLOR_LIGHT);
        graphics.fill(x, y, x + 1, y + h, COLOR_LIGHT);
        graphics.fill(x, y + h - 1, x + w, y + h, COLOR_DARK);
        graphics.fill(x + w - 1, y, x + w, y + h, COLOR_DARK);
        if (title != null) {
            graphics.drawString(this.font, Component.literal(title), x + 5, y + 3, COLOR_TITLE, false);
        }
    }

    private void drawSlot(GuiGraphics graphics, int x, int y, int w, int h) {
        if (w <= 2 || h <= 2) return;
        graphics.fill(x, y, x + w, y + h, COLOR_SLOT);
        graphics.fill(x, y, x + w, y + 1, COLOR_SLOT_DARK);
        graphics.fill(x, y, x + 1, y + h, COLOR_SLOT_DARK);
        graphics.fill(x, y + h - 1, x + w, y + h, COLOR_LIGHT);
        graphics.fill(x + w - 1, y, x + w, y + h, COLOR_LIGHT);
    }

    private void drawCell(GuiGraphics graphics, CardInfo info, int x, int y, int w, int h,
                          int count, boolean selected, boolean hover) {
        if (w <= 2 || h <= 2) return;
        drawSlot(graphics, x, y, w, h);
        ResourceLocation art = CardTextureCache.get(info.code());
        graphics.blit(art, x + 1, y + 1, w - 2, h - 2, 0.0F, 0.0F, ART_W, ART_H, ART_W, ART_H);
        int limit = DeckRules.limitOf(info.code());
        if (limit < 3 && w > 12 && h > 12) {
            String badge = limit == 0 ? "禁" : Integer.toString(limit);
            int badgeW = this.font.width(badge) + 4;
            graphics.fill(x + 1, y + 1, x + 1 + badgeW, y + 9,
                    limit == 2 ? 0xFF7F5A10 : 0xFF7F1010);
            graphics.drawString(this.font, badge, x + 3, y + 2, COLOR_TEXT, false);
        }
        if (count > 1 && w > 10) {
            String text = Integer.toString(count);
            graphics.drawString(this.font, text, x + w - 2 - this.font.width(text), y + h - 9, COLOR_TEXT, true);
        }
        if (hover) graphics.fill(x, y, x + w, y + h, COLOR_HOVER);
        if (selected) {
            graphics.fill(x - 1, y - 1, x + w + 1, y, COLOR_LIGHT);
            graphics.fill(x - 1, y + h, x + w + 1, y + h + 1, COLOR_LIGHT);
            graphics.fill(x - 1, y, x, y + h, COLOR_LIGHT);
            graphics.fill(x + w, y, x + w + 1, y + h, COLOR_LIGHT);
        }
    }

    // ------------------------------------------------------------ 交互

    private boolean inside(double mouseX, double mouseY, int x, int y, int w, int h) {
        return mouseX >= x && mouseX < x + w && mouseY >= y && mouseY < y + h;
    }

    private int clampScroll(int value, int lines, int visibleLines) {
        return Math.max(0, Math.min(value, Math.max(0, lines - visibleLines)));
    }

    private String trim(String text, int width) {
        if (width <= 8) return "";
        return this.font.width(text) <= width ? text : this.font.plainSubstrByWidth(text, width - 6) + "…";
    }

    /**
     * 文字按可用宽度自动缩放：够宽就原样画，太长就等比缩小到放得下，
     * 缩到 minScale 还放不下才退回省略号。
     */
    private void drawFitText(GuiGraphics graphics, String text, int x, int y, int maxWidth,
                             int color, float minScale) {
        if (text == null || text.isEmpty() || maxWidth <= 4) return;
        int width = this.font.width(text);
        float scale = width <= maxWidth ? 1.0F : Math.max(minScale, maxWidth / (float) width);
        if (scale >= 0.999F) {
            graphics.drawString(this.font, text, x, y, color, false);
            return;
        }
        String shown = text;
        if (width * scale > maxWidth) {
            shown = trim(text, maxWidth);
            int trimmed = this.font.width(shown);
            scale = trimmed <= maxWidth ? 1.0F : Math.max(minScale, maxWidth / (float) trimmed);
        }
        graphics.pose().pushPose();
        graphics.pose().translate(x, y, 0.0D);
        graphics.pose().scale(scale, scale, 1.0F);
        graphics.drawString(this.font, shown, 0, 0, color, false);
        graphics.pose().popPose();
    }

    @Override
    public boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (super.mouseClicked(mouseX, mouseY, button)) return true;
        for (int zone = 0; zone < 3; zone++) {
            int headerY = sectionY[zone];
            if (inside(mouseX, mouseY, centerX, headerY, centerW, 12)) {
                activeZone = zone;
                return true;
            }
        }
        for (int zone = 0; zone < 3; zone++) {
            int gridX = centerX + 3;
            int gridY = sectionGridY[zone];
            List<Integer> codes = zoneCodes.get(zone);
            for (int row = 0; row < zoneRows[zone]; row++) {
                for (int col = 0; col < DECK_COLUMNS; col++) {
                    int index = zoneScroll[zone] + row * DECK_COLUMNS + col;
                    if (index >= codes.size()) break;
                    int x = gridX + 1 + col * (cellW + GAP);
                    int y = gridY + 1 + row * (cellH + GAP);
                    if (inside(mouseX, mouseY, x, y, cellW, cellH)) {
                        int code = codes.get(index);
                        selectedCode = code;
                        activeZone = zone;
                        rebuildDetailLines();
                        if (button == 1) removeCard(code, zone);
                        return true;
                    }
                }
            }
        }
        if (inside(mouseX, mouseY, poolX + 4, poolListY + 1, poolW - 8, poolListH - 2)) {
            int row = (int) ((mouseY - poolListY - 1) / ROW_H);
            int index = poolScroll + row;
            if (row >= 0 && row < poolRows && index < results.size()) {
                CardInfo info = results.get(index);
                selectedCode = info.code();
                rebuildDetailLines();
                if (button == 0) addCard(info.code());
                else if (button == 1) removeAnywhere(info.code());
                return true;
            }
        }
        return false;
    }

    @Override
    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        if (inside(mouseX, mouseY, detailX, detailTextTop - 2, detailW, detailTextH + 4)) {
            detailScroll = clampScroll(detailScroll - (int) Math.signum(delta) * 2,
                    detailTextHeight, visibleDetailLines());
            return true;
        }
        for (int zone = 0; zone < 3; zone++) {
            if (inside(mouseX, mouseY, centerX, sectionY[zone], centerW,
                    sectionGridY[zone] + sectionGridH[zone] - sectionY[zone])) {
                zoneScroll[zone] = clampScroll(zoneScroll[zone]
                                - (int) Math.signum(delta) * DECK_COLUMNS,
                        zoneCodes.get(zone).size(), zoneRows[zone] * DECK_COLUMNS);
                return true;
            }
        }
        if (inside(mouseX, mouseY, poolX, poolListY, poolW, poolListH)) {
            poolScroll = clampScroll(poolScroll - (int) Math.signum(delta), results.size(), poolRows);
            return true;
        }
        return super.mouseScrolled(mouseX, mouseY, delta);
    }

    @Override
    public boolean keyPressed(int keyCode, int scanCode, int modifiers) {
        boolean enter = keyCode == 257 || keyCode == 335;
        if (enter && nameBox != null && nameBox.isFocused()) {
            sendName();
            nameBox.setFocused(false);
            return true;
        }
        if (enter && searchBox != null && searchBox.isFocused()) {
            searchBox.setFocused(false);
            return true;
        }
        return super.keyPressed(keyCode, scanCode, modifiers);
    }

    @Override
    public void onClose() {
        sendName();
        super.onClose();
    }

    @Override
    public boolean isPauseScreen() {
        return false;
    }
}
