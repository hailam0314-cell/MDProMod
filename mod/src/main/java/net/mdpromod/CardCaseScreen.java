package net.mdpromod;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Map.Entry;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.EditBox;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.util.FormattedCharSequence;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public class CardCaseScreen extends Screen {
    private static final int GAP = 2;
    private static final int SECTION_GAP = 4;
    private static final int ART_W = 176;
    private static final int ART_H = 256;
    private static final int DECK_COLUMNS = 10;
    private static final int ROW_H = 22;
    private static final float TEXT_SCALE = 0.72F;
    private static final int COLOR_PANEL = -3750202;
    private static final int COLOR_LIGHT = -1;
    private static final int COLOR_DARK = -11184811;
    private static final int COLOR_SLOT = -7631989;
    private static final int COLOR_SLOT_DARK = -13158601;
    private static final int COLOR_TEXT = -1;
    private static final int COLOR_TITLE = 4210752;
    private static final int COLOR_TITLE_DIM = 6974058;
    private static final int COLOR_TITLE_BANLIST = 3104655;
    private static final int COLOR_BODY = -13619152;
    private static final int COLOR_HOVER = 1358954495;
    private static final int COLOR_ACTIVE = 855678976;
    private static final int COLOR_GOOD = 2060063;
    private static final int COLOR_BAD = 10230555;
    private static final String[] ZONE_NAMES = new String[]{"主卡组", "额外卡组", "副卡组"};
    private static final int[] ZONE_CAP = new int[]{60, 15, 15};
    private final DeckData deck = new DeckData();
    private final CardFilter filter = new CardFilter();
    private final List<CardInfo> results = new ArrayList<>();
    private final List<CardInfo> relatedResults = new ArrayList<>();
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
    private int detailButtonsY;
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
    private int relatedCode = -1;
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
    private Button addOneButton;
    private Button removeOneButton;
    private Button relatedButton;
    private int nameBoxX;
    private int nameBoxW;
    private int banlistW;

    public CardCaseScreen(DeckData initial) {
        super(Component.literal("卡盒"));
        if (initial != null) {
            this.deck.mainDeck().addAll(initial.mainDeck());
            this.deck.extraDeck().addAll(initial.extraDeck());
            this.deck.sideDeck().addAll(initial.sideDeck());
            this.deck.setName(initial.name());
            this.deck.setCardBack(initial.cardBack());
            if (!this.deck.mainDeck().isEmpty()) {
                this.selectedCode = this.deck.mainDeck().get(0);
            }
        }
    }

    protected void init() {
        super.init();
        this.layout();
        this.banlistW = clamp(this.panelW - 34 - 4 - 52 - 102 - 4 - 58, 32, 100);
        this.nameBoxX = this.leftPos + 34 + this.banlistW + 4;
        this.nameBoxW = clamp(this.panelW - (this.nameBoxX - this.leftPos) - 3 - 32 - 2 - 32 - 34 - 58, 52, 110);
        this.nameBox = new EditBox(this.font, this.nameBoxX, this.topPos + 4, this.nameBoxW, 14, Component.literal("卡组名"));
        this.nameBox.setMaxLength(32);
        this.nameBox.setHint(Component.literal("卡组名"));
        this.nameBox.setValue(this.deck.name());
        this.addRenderableWidget(this.nameBox);
        this.addRenderableWidget(
            Button.builder(Component.literal("导入"), b -> this.openImport())
                .bounds(this.nameBoxX + this.nameBoxW + 3, this.topPos + 3, 32, 16)
                .build()
        );
        this.addRenderableWidget(
            Button.builder(Component.literal("导出"), b -> this.copyDeckCode())
                .bounds(this.nameBoxX + this.nameBoxW + 37, this.topPos + 3, 32, 16)
                .build()
        );
        this.addRenderableWidget(
            Button.builder(Component.literal("完成"), b -> this.onClose()).bounds(this.leftPos + this.panelW - 52, this.topPos + 3, 46, 16).build()
        );
        this.addRenderableWidget(Button.builder(Component.literal("卡背"), b -> {
            this.sendName();
            this.minecraft.setScreen(new CardBackScreen(this, this.deck.cardBack()));
        }).bounds(this.nameBoxX + this.nameBoxW + 71, this.topPos + 3, 32, 16).build());
        int detailInnerX = this.detailX + 4;
        int detailInnerW = Math.max(48, this.detailW - 8);
        int smallButtonW = 20;
        int relatedW = detailInnerW - smallButtonW * 2 - 4;
        this.addOneButton = (Button)this.addRenderableWidget(
            Button.builder(Component.literal("+"), b -> this.addSelectedCard()).bounds(detailInnerX, this.detailButtonsY, smallButtonW, 16).build()
        );
        this.removeOneButton = (Button)this.addRenderableWidget(
            Button.builder(Component.literal("-"), b -> this.removeSelectedCard())
                .bounds(detailInnerX + smallButtonW + 2, this.detailButtonsY, smallButtonW, 16)
                .build()
        );
        this.relatedButton = (Button)this.addRenderableWidget(
            Button.builder(Component.literal("相关"), b -> this.toggleRelated())
                .bounds(detailInnerX + smallButtonW * 2 + 4, this.detailButtonsY, relatedW, 16)
                .build()
        );
        int findW = this.poolW >= 110 ? 46 : 38;
        this.searchBox = new EditBox(this.font, this.poolX + 3, this.contentY + 3, Math.max(40, this.poolW - findW - 8), 15, Component.literal("搜索"));
        this.searchBox.setMaxLength(32);
        this.searchBox.setHint(Component.empty());
        this.searchBox.setValue(this.query);
        this.searchBox.setResponder(value -> {
            this.query = value;
            this.poolScroll = 0;
            this.rebuildResults();
        });
        this.addRenderableWidget(this.searchBox);
        this.addRenderableWidget(Button.builder(Component.literal("查找"), b -> {
            if (this.minecraft != null) {
                this.minecraft.setScreen(new CardSearchScreen(this, this.filter));
            }
        }).bounds(this.poolX + this.poolW - findW - 3, this.contentY + 3, findW, 15).build());
        this.rebuildDeckViews();
        this.rebuildResults();
        this.rebuildDetailLines();
        this.refreshProblems();
        ModNetwork.sendToServer(new ModNetwork.CardCaseAction(3, 0, 0, ""));
    }

    private void layout() {
        this.panelW = Math.min(Math.max(this.width - 8, 260), Math.min(900, this.width));
        this.panelH = Math.min(Math.max(this.height - 8, 170), Math.min(520, this.height));
        this.leftPos = (this.width - this.panelW) / 2;
        this.topPos = (this.height - this.panelH) / 2;
        int pad = 5;
        int columnGap = 5;
        this.contentY = this.topPos + 22;
        this.statusY = this.topPos + this.panelH - 14;
        this.contentH = Math.max(90, this.statusY - this.contentY - 4);
        int usable = this.panelW - pad * 2 - columnGap * 2;
        int detailWish = clamp((int)(usable * 0.2), 88, 132);
        int poolWish = clamp((int)(usable * 0.24), 104, 190);
        int centerWish = usable - detailWish - poolWish;
        if (centerWish < 140) {
            int deficit = 140 - centerWish;
            int shrink = Math.min(deficit, detailWish - 78);
            detailWish -= shrink;
            deficit -= shrink;
            shrink = Math.min(deficit, poolWish - 92);
            poolWish -= shrink;
            centerWish = Math.max(40, usable - detailWish - poolWish);
        }

        this.detailX = this.leftPos + pad;
        this.detailW = detailWish;
        this.centerX = this.detailX + this.detailW + columnGap;
        this.centerW = centerWish;
        this.poolX = this.centerX + this.centerW + columnGap;
        this.poolW = poolWish;
        this.detailButtonsY = this.contentY + this.contentH - 20;
        int artTop = this.contentY + 4;
        int textBottom = this.detailButtonsY - 6;
        int textBlock = 30;
        int minTextH = 34;
        this.detailArtW = Math.min(this.detailW - 10, 96);
        this.detailArtH = this.detailArtW * 256 / 176;
        int maxArtH = Math.max(36, textBottom - minTextH - artTop - textBlock);
        if (this.detailArtH > maxArtH) {
            this.detailArtH = maxArtH;
            this.detailArtW = this.detailArtH * 176 / 256;
        }

        this.detailTextTop = artTop + this.detailArtH + textBlock;
        this.detailTextH = Math.max(0, textBottom - this.detailTextTop);
        int gridX = this.centerX + 3;
        int gridW = this.centerW - 6;
        this.cellW = clamp((gridW - 18) / 10, 12, 46);
        this.cellH = this.cellW * 256 / 176;
        int available = this.contentH - 8;
        int mainH = Math.max(48, (int)(available * 0.52));
        int extraH = Math.max(34, (int)((available - mainH) * 0.5));
        int sideH = Math.max(34, available - mainH - extraH);
        int[] heights = new int[]{mainH, extraH, sideH};
        int y = this.contentY;

        for (int i = 0; i < 3; i++) {
            this.sectionY[i] = y;
            this.sectionGridY[i] = y + 12;
            this.sectionGridH[i] = Math.max(20, heights[i] - 12);
            this.zoneRows[i] = Math.max(1, (this.sectionGridH[i] + 2) / (this.cellH + 2));
            y += heights[i] + 4;
        }

        this.poolListY = this.contentY + 21;
        this.poolListH = Math.max(24, this.statusY - 4 - this.poolListY);
        this.poolRows = Math.max(1, this.poolListH / 22);
    }

    private static int clamp(int value, int min, int max) {
        return Math.max(min, Math.min(max, value));
    }

    public void applyDeck(DeckData incoming) {
        this.deck.mainDeck().clear();
        this.deck.mainDeck().addAll(incoming.mainDeck());
        this.deck.extraDeck().clear();
        this.deck.extraDeck().addAll(incoming.extraDeck());
        this.deck.sideDeck().clear();
        this.deck.sideDeck().addAll(incoming.sideDeck());
        this.deck.setName(incoming.name());
        this.deck.setCardBack(incoming.cardBack());
        if (this.nameBox != null && !this.nameBox.isFocused()) {
            this.nameBox.setValue(this.deck.name());
        }

        this.rebuildDeckViews();
        this.rebuildDetailLines();
        this.refreshProblems();
    }

    private List<Integer> zoneList(int zone) {
        return switch (zone) {
            case 1 -> this.deck.extraDeck();
            case 2 -> this.deck.sideDeck();
            default -> this.deck.mainDeck();
        };
    }

    private void rebuildDeckViews() {
        for (int zone = 0; zone < 3; zone++) {
            List<Integer> codes = this.zoneCodes.get(zone);
            List<Integer> counts = this.zoneCounts.get(zone);
            codes.clear();
            counts.clear();
            Map<Integer, Integer> grouped = new LinkedHashMap<>();

            for (int code : this.zoneList(zone)) {
                grouped.merge(code, 1, Integer::sum);
            }

            for (Entry<Integer, Integer> entry : grouped.entrySet()) {
                codes.add(entry.getKey());
                counts.add(entry.getValue());
            }

            this.zoneScroll[zone] = this.clampScroll(this.zoneScroll[zone], codes.size(), this.zoneRows[zone] * 10);
        }
    }

    private void rebuildResults() {
        this.results.clear();
        String needle = this.query.trim();

        for (CardInfo info : this.relatedCode > 0 ? this.relatedResults : CardRegistry.all()) {
            if ((this.relatedCode > 0 || this.filter.matches(info)) && (needle.isEmpty() || CardNameSearch.matches(info, needle))) {
                this.results.add(info);
            }
        }
        Comparator<CardInfo> comparator = switch (this.filter.sortMode) {
            case 1 -> Comparator.<CardInfo>comparingInt(c -> -c.level()).thenComparingInt(CardInfo::code);
            case 2 -> Comparator.<CardInfo>comparingInt(c -> -c.atk()).thenComparingInt(CardInfo::code);
            case 3 -> Comparator.<CardInfo>comparingInt(c -> -c.def()).thenComparingInt(CardInfo::code);
            case 4 -> Comparator.comparingInt(CardInfo::code);
            default -> Comparator.comparingInt(CardCaseScreen::kindRank).thenComparingInt(CardInfo::code);
        };
        this.results.sort(comparator);
        this.poolScroll = this.clampScroll(this.poolScroll, this.results.size(), this.poolRows);
    }

    private void toggleRelated() {
        if (this.relatedCode > 0) {
            this.relatedCode = -1;
            this.relatedResults.clear();
        } else {
            if (this.selectedCode <= 0) {
                this.feedback("先选中一张卡");
                return;
            }

            this.relatedCode = this.selectedCode;
            this.relatedResults.clear();
            this.relatedResults.addAll(RelatedCards.find(this.relatedCode));
        }

        this.query = "";
        if (this.searchBox != null) {
            this.searchBox.setValue("");
        }

        this.poolScroll = 0;
        this.rebuildResults();
        this.updateDetailButtons();
    }

    private void updateDetailButtons() {
        boolean hasCard = this.selectedCard() != null;
        if (this.addOneButton != null) {
            this.addOneButton.active = hasCard;
        }

        if (this.removeOneButton != null) {
            this.removeOneButton.active = hasCard && this.containsAnywhere(this.selectedCode);
        }

        if (this.relatedButton != null) {
            this.relatedButton.setMessage(Component.literal(this.relatedCode > 0 ? "返回" : "相关"));
            this.relatedButton.active = this.relatedCode > 0 || hasCard;
        }
    }

    private boolean containsAnywhere(int code) {
        for (int zone = 0; zone < 3; zone++) {
            if (this.zoneList(zone).contains(code)) {
                return true;
            }
        }

        return false;
    }

    private void addSelectedCard() {
        if (this.selectedCode > 0) {
            this.addCard(this.selectedCode);
        }
    }

    private void removeSelectedCard() {
        if (this.selectedCode > 0) {
            this.removeAnywhere(this.selectedCode);
        }
    }

    private void openImport() {
        if (this.minecraft != null) {
            this.sendName();
            this.minecraft.setScreen(new DeckImportScreen(this, this::importDeck));
        }
    }

    private void copyDeckCode() {
        if (this.minecraft != null) {
            try {
                String code = DeckShareCode.exportUrl(this.deck);
                this.minecraft.keyboardHandler.setClipboard(code);
                this.feedback("卡组码已复制到剪贴板");
            } catch (RuntimeException error) {
                this.feedback("导出失败：" + error.getMessage());
            }
        }
    }

    private void importDeck(DeckData imported) {
        if (imported != null) {
            String currentName = this.deck.name();
            this.deck.mainDeck().clear();
            this.deck.mainDeck().addAll(imported.mainDeck());
            this.deck.extraDeck().clear();
            this.deck.extraDeck().addAll(imported.extraDeck());
            this.deck.sideDeck().clear();
            this.deck.sideDeck().addAll(imported.sideDeck());
            this.deck.setName(currentName);
            this.selectedCode = !this.deck.mainDeck().isEmpty()
                ? this.deck.mainDeck().get(0)
                : (!this.deck.extraDeck().isEmpty() ? this.deck.extraDeck().get(0) : (!this.deck.sideDeck().isEmpty() ? this.deck.sideDeck().get(0) : -1));
            this.relatedCode = -1;
            this.relatedResults.clear();
            this.detailScroll = 0;
            this.poolScroll = 0;
            this.rebuildDeckViews();
            this.rebuildResults();
            this.rebuildDetailLines();
            this.refreshProblems();
            ModNetwork.sendToServer(new ModNetwork.CardCaseReplace(this.deck.name(), this.deck.mainArray(), this.deck.extraArray(), this.deck.sideArray()));
            int unknown = 0;

            for (int code : this.deck.mainArray()) {
                if (!CardRegistry.contains(code)) {
                    unknown++;
                }
            }

            for (int code : this.deck.extraArray()) {
                if (!CardRegistry.contains(code)) {
                    unknown++;
                }
            }

            for (int code : this.deck.sideArray()) {
                if (!CardRegistry.contains(code)) {
                    unknown++;
                }
            }

            this.feedback(
                "已导入：主 "
                    + this.deck.mainDeck().size()
                    + " / 额外 "
                    + this.deck.extraDeck().size()
                    + " / 副 "
                    + this.deck.sideDeck().size()
                    + (unknown == 0 ? "" : "，其中 " + unknown + " 张本地卡库找不到")
            );
        }
    }

    private static int kindRank(CardInfo info) {
        if (info.isExtraDeck()) {
            return 2;
        } else if (info.isMonster()) {
            return 0;
        } else if (info.isSpell()) {
            return 3;
        } else {
            return info.isTrap() ? 4 : 5;
        }
    }

    private void rebuildDetailLines() {
        this.detailLines = new ArrayList<>();
        this.detailTextHeight = 0;
        if (this.selectedCode > 0) {
            CardInfo info = CardRegistry.get(this.selectedCode).orElse(null);
            if (info != null) {
                String text = info.desc() != null && !info.desc().isBlank() ? info.desc() : "（这张卡没有效果文本）";
                int wrap = Math.max(40, (int)((this.detailW - 12) / 0.72F));
                this.detailLines.addAll(this.font.split(Component.literal(text), wrap));
                this.detailTextHeight = this.detailLines.size();
                this.detailScroll = this.clampScroll(this.detailScroll, this.detailTextHeight, this.visibleDetailLines());
            }
        }
    }

    private int visibleDetailLines() {
        return Math.max(1, (int)(this.detailTextH / 6.4800005F));
    }

    private void refreshProblems() {
        this.problems = DeckRules.problems(this.deck);
    }

    private CardInfo selectedCard() {
        return this.selectedCode > 0 ? CardRegistry.get(this.selectedCode).orElse(null) : null;
    }

    private void addCard(int code) {
        CardInfo info = CardRegistry.get(code).orElse(null);
        if (info != null) {
            int zone = this.activeZone;
            if (info.isExtraDeck() && zone == 0) {
                zone = 1;
                this.feedback("「" + info.name() + "」是额外卡组卡，已放进额外卡组");
            }

            if (!info.isExtraDeck() && zone == 1) {
                zone = 0;
                this.feedback("「" + info.name() + "」不能放进额外卡组，已放进主卡组");
            }

            List<Integer> target = this.zoneList(zone);
            if (target.size() >= ZONE_CAP[zone]) {
                this.feedback(ZONE_NAMES[zone] + "最多 " + ZONE_CAP[zone] + " 张");
            } else {
                int limit = DeckRules.limitOf(code);
                if (DeckRules.copies(this.deck, code) >= limit) {
                    String reason = DeckRules.limitSuffix(code);
                    String mates = DeckRules.groupMateText(this.deck, code);
                    this.feedback(limit == 0 ? "「" + info.name() + "」是禁止卡" + reason + mates : "「" + info.name() + "」最多放 " + limit + " 张" + reason + mates);
                } else {
                    target.add(code);
                    this.activeZone = zone;
                    this.selectedCode = code;
                    this.rebuildDeckViews();
                    this.rebuildDetailLines();
                    this.refreshProblems();
                    ModNetwork.sendToServer(new ModNetwork.CardCaseAction(0, zone, code, ""));
                }
            }
        }
    }

    private void removeCard(int code, int zone) {
        List<Integer> target = this.zoneList(zone);
        if (!target.remove(Integer.valueOf(code))) {
            this.feedback("「" + DeckRules.displayName(code) + "」不在" + ZONE_NAMES[zone] + "里");
        } else {
            this.selectedCode = code;
            this.rebuildDeckViews();
            this.rebuildDetailLines();
            this.refreshProblems();
            ModNetwork.sendToServer(new ModNetwork.CardCaseAction(1, zone, code, ""));
        }
    }

    private void removeAnywhere(int code) {
        int[] order = new int[]{this.activeZone, 0, 1, 2};

        for (int zone : order) {
            if (this.zoneList(zone).remove(Integer.valueOf(code))) {
                this.selectedCode = code;
                this.rebuildDeckViews();
                this.rebuildDetailLines();
                this.refreshProblems();
                ModNetwork.sendToServer(new ModNetwork.CardCaseAction(1, zone, code, ""));
                return;
            }
        }

        this.feedback("卡组里没有「" + DeckRules.displayName(code) + "」");
    }

    private void sendName() {
        if (this.nameBox != null) {
            String value = this.nameBox.getValue().trim();
            if (!value.isEmpty() && !value.equals(this.deck.name())) {
                this.deck.setName(value);
                ModNetwork.sendToServer(new ModNetwork.CardCaseAction(2, 0, 0, value));
            }
        }
    }

    private void feedback(String message) {
        if (this.minecraft != null && this.minecraft.player != null) {
            this.minecraft.player.displayClientMessage(Component.literal(message), true);
        }
    }

    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        this.updateDetailButtons();
        this.renderBackground(graphics);
        this.drawWindow(graphics);
        this.drawDetail(graphics);
        this.drawCenter(graphics, mouseX, mouseY);
        this.drawPool(graphics, mouseX, mouseY);
        this.drawStatus(graphics);
        super.render(graphics, mouseX, mouseY, partialTick);
        CardInfo hovered = this.hoveredCard();
        if (hovered != null) {
            List<FormattedCharSequence> lines = new ArrayList<>();
            lines.add(Component.literal(hovered.name()).getVisualOrderText());
            lines.add(Component.literal(hovered.typeLine() + "  #" + hovered.code()).getVisualOrderText());
            String stats = hovered.isMonster() ? "ATK " + hovered.atk() + " / DEF " + hovered.def() : hovered.typeLabel();
            lines.add(
                Component.literal(stats + " · 卡组内 " + DeckRules.copies(this.deck, hovered.code()) + " 张 · " + DeckRules.limitText(hovered.code())).getVisualOrderText()
            );
            graphics.renderTooltip(this.font, lines, mouseX, mouseY);
        }
    }

    private CardInfo hoveredCard() {
        if (this.hoveredPool >= 0 && this.hoveredPool < this.results.size()) {
            return this.results.get(this.hoveredPool);
        }

        if (this.hoveredZone >= 0 && this.hoveredZoneIndex >= 0) {
            List<Integer> codes = this.zoneCodes.get(this.hoveredZone);
            if (this.hoveredZoneIndex < codes.size()) {
                return CardRegistry.get(codes.get(this.hoveredZoneIndex)).orElse(null);
            }
        }

        return null;
    }

    private void drawWindow(GuiGraphics graphics) {
        graphics.fill(this.leftPos - 1, this.topPos - 1, this.leftPos + this.panelW + 1, this.topPos + this.panelH + 1, -16777216);
        graphics.fill(this.leftPos, this.topPos, this.leftPos + this.panelW, this.topPos + this.panelH, -3750202);
        graphics.fill(this.leftPos, this.topPos, this.leftPos + this.panelW, this.topPos + 1, -1);
        graphics.fill(this.leftPos, this.topPos, this.leftPos + 1, this.topPos + this.panelH, -1);
        graphics.fill(this.leftPos, this.topPos + this.panelH - 1, this.leftPos + this.panelW, this.topPos + this.panelH, -11184811);
        graphics.fill(this.leftPos + this.panelW - 1, this.topPos, this.leftPos + this.panelW, this.topPos + this.panelH, -11184811);
        graphics.drawString(this.font, Component.literal("卡盒"), this.leftPos + 8, this.topPos + 7, 4210752, false);
        int banlistX = this.leftPos + 8 + this.font.width("卡盒") + 8;
        graphics.drawString(
            this.font,
            Component.literal(this.trim(DeckRules.banlistStatus(), this.banlistW)),
            banlistX,
            this.topPos + 7,
            DeckRules.banlists().isEmpty() ? 6974058 : 3104655,
            false
        );
    }

    private void drawDetail(GuiGraphics graphics) {
        this.drawFrame(graphics, this.detailX, this.contentY, this.detailW, this.contentH, null);
        CardInfo info = this.selectedCard();
        int artX = this.detailX + (this.detailW - this.detailArtW) / 2;
        int artY = this.contentY + 4;
        this.drawSlot(graphics, artX, artY, this.detailArtW, this.detailArtH);
        if (info == null) {
            graphics.drawString(this.font, Component.literal("点一张卡看详情"), this.detailX + 5, artY + 4, 4210752, false);
        } else {
            ResourceLocation art = CardTextureCache.get(info.code());
            graphics.blit(art, artX + 1, artY + 1, this.detailArtW - 2, this.detailArtH - 2, 0.0F, 0.0F, 176, 256, 176, 256);
            int textY = artY + this.detailArtH + 3;
            int textX = this.detailX + 5;
            int textW = this.detailW - 10;
            this.drawFitText(graphics, info.name(), textX, textY, textW, -13619152, 0.55F);
            textY += 9;
            this.drawFitText(graphics, info.typeLine(), textX, textY, textW, -13619152, 0.6F);
            textY += 9;
            String stats = info.isMonster() ? "ATK " + info.atk() + " / DEF " + info.def() : "";
            if (!stats.isEmpty()) {
                this.drawFitText(graphics, stats, textX, textY, textW, -13619152, 0.6F);
            }

            int areaY = this.detailTextTop;
            int areaH = this.detailTextH;
            if (areaH >= 8) {
                this.drawSlot(graphics, this.detailX + 4, areaY - 2, this.detailW - 8, areaH + 4);
                graphics.enableScissor(this.detailX + 5, areaY, this.detailX + this.detailW - 5, areaY + areaH);
                graphics.pose().pushPose();
                graphics.pose().scale(0.72F, 0.72F, 1.0F);
                int baseX = (int)((this.detailX + 6) / 0.72F);
                int baseY = (int)(areaY / 0.72F);

                for (int i = this.detailScroll; i < this.detailLines.size(); i++) {
                    int lineY = baseY + (i - this.detailScroll) * 9;
                    if (lineY * 0.72F >= areaY + areaH) {
                        break;
                    }

                    graphics.drawString(this.font, this.detailLines.get(i), baseX, lineY, -13619152, false);
                }

                graphics.pose().popPose();
                graphics.disableScissor();
            }
        }
    }

    private void drawCenter(GuiGraphics graphics, int mouseX, int mouseY) {
        this.hoveredZone = -1;
        this.hoveredZoneIndex = -1;

        for (int zone = 0; zone < 3; zone++) {
            int y = this.sectionY[zone];
            int height = this.sectionGridY[zone] + this.sectionGridH[zone] - y + 2;
            this.drawFrame(graphics, this.centerX, y, this.centerW, height, null);
            boolean active = zone == this.activeZone;
            if (active) {
                graphics.fill(this.centerX + 1, y + 1, this.centerX + this.centerW - 1, y + 12, 855678976);
            }

            String header = (active ? "▶ " : "") + ZONE_NAMES[zone] + " " + this.zoneList(zone).size() + "/" + ZONE_CAP[zone];
            graphics.drawString(this.font, Component.literal(header), this.centerX + 5, y + 2, active ? -14725345 : 4210752, false);
            String hint = "左键选中 · 右键移除一张";
            graphics.drawString(this.font, Component.literal(hint), this.centerX + this.centerW - 5 - this.font.width(hint), y + 2, 4210752, false);
            int gridX = this.centerX + 3;
            int gridY = this.sectionGridY[zone];
            int gridW = this.centerW - 6;
            int gridH = this.sectionGridH[zone];
            this.drawSlot(graphics, gridX, gridY, gridW, gridH);
            List<Integer> codes = this.zoneCodes.get(zone);
            if (codes.isEmpty()) {
                graphics.drawString(this.font, Component.literal("（空）"), gridX + 4, gridY + 4, 4210752, false);
            } else if (gridW >= 24 && gridH >= 16) {
                graphics.enableScissor(gridX + 1, gridY + 1, gridX + gridW - 1, gridY + gridH - 1);

                for (int row = 0; row < this.zoneRows[zone]; row++) {
                    for (int col = 0; col < 10; col++) {
                        int index = this.zoneScroll[zone] + row * 10 + col;
                        if (index >= codes.size()) {
                            break;
                        }

                        int x = gridX + 1 + col * (this.cellW + 2);
                        int cellY = gridY + 1 + row * (this.cellH + 2);
                        int code = codes.get(index);
                        boolean hover = this.inside(mouseX, mouseY, x, cellY, this.cellW, this.cellH);
                        if (hover) {
                            this.hoveredZone = zone;
                            this.hoveredZoneIndex = index;
                        }

                        CardInfo info = CardRegistry.get(code).orElse(null);
                        if (info != null) {
                            this.drawCell(
                                graphics, info, x, cellY, this.cellW, this.cellH, this.zoneCounts.get(zone).get(index), code == this.selectedCode, hover
                            );
                        }
                    }
                }

                graphics.disableScissor();
                int total = codes.size();
                int visible = this.zoneRows[zone] * 10;
                if (total > visible) {
                    String page = this.zoneScroll[zone] / 10 + 1 + "/" + (total + 10 - 1) / 10;
                    graphics.drawString(
                        this.font, Component.literal(page), this.centerX + this.centerW - 5 - this.font.width(page), gridY + gridH - 10, -1, true
                    );
                }
            }
        }
    }

    private void drawPool(GuiGraphics graphics, int mouseX, int mouseY) {
        this.drawFrame(graphics, this.poolX, this.contentY, this.poolW, this.contentH, null);
        this.drawSlot(graphics, this.poolX + 3, this.poolListY, this.poolW - 6, this.poolListH);
        this.hoveredPool = -1;
        if (this.results.isEmpty()) {
            graphics.drawString(this.font, Component.literal("没找到卡片"), this.poolX + 7, this.poolListY + 4, 4210752, false);
        } else if (this.poolW >= 60 && this.poolListH >= 16) {
            graphics.enableScissor(this.poolX + 4, this.poolListY + 1, this.poolX + this.poolW - 4, this.poolListY + this.poolListH - 1);

            for (int row = 0; row < this.poolRows; row++) {
                int index = this.poolScroll + row;
                if (index >= this.results.size()) {
                    break;
                }

                CardInfo info = this.results.get(index);
                int rowY = this.poolListY + 1 + row * 22;
                boolean hover = this.inside(mouseX, mouseY, this.poolX + 4, rowY, this.poolW - 8, 22);
                if (hover) {
                    this.hoveredPool = index;
                    graphics.fill(this.poolX + 4, rowY, this.poolX + this.poolW - 4, rowY + 22, 1358954495);
                }

                ResourceLocation art = CardTextureCache.get(info.code());
                graphics.blit(art, this.poolX + 5, rowY + 2, 14, 20, 0.0F, 0.0F, 176, 256, 176, 256);
                graphics.drawString(this.font, this.trim(info.name(), this.poolW - 40), this.poolX + 22, rowY + 3, -13619152, false);
                graphics.drawString(this.font, "#" + info.code(), this.poolX + 22, rowY + 12, -9474193, false);
                int copies = DeckRules.copies(this.deck, info.code());
                if (copies > 0) {
                    String count = "x" + copies;
                    graphics.drawString(this.font, count, this.poolX + this.poolW - 6 - this.font.width(count), rowY + 7, -1, true);
                }
            }

            graphics.disableScissor();
        }
    }

    private void drawStatus(GuiGraphics graphics) {
        String counters = "主 " + this.deck.mainDeck().size() + "/60  额外 " + this.deck.extraDeck().size() + "/15  副 " + this.deck.sideDeck().size() + "/15";
        counters = this.trim(counters, Math.max(70, this.panelW / 2));
        int countersX = this.leftPos + this.panelW - 8 - this.font.width(counters);
        graphics.drawString(this.font, Component.literal(counters), countersX, this.statusY + 3, 4210752, false);
        String line = this.problems.isEmpty() ? "卡组合法 · 改动即时保存" : this.problems.get(0);
        graphics.drawString(
            this.font,
            Component.literal(this.trim(line, countersX - this.leftPos - 16)),
            this.leftPos + 8,
            this.statusY + 3,
            this.problems.isEmpty() ? 2060063 : 10230555,
            false
        );
    }

    private void drawFrame(GuiGraphics graphics, int x, int y, int w, int h, String title) {
        graphics.fill(x, y, x + w, y + h, -3750202);
        graphics.fill(x, y, x + w, y + 1, -1);
        graphics.fill(x, y, x + 1, y + h, -1);
        graphics.fill(x, y + h - 1, x + w, y + h, -11184811);
        graphics.fill(x + w - 1, y, x + w, y + h, -11184811);
        if (title != null) {
            graphics.drawString(this.font, Component.literal(title), x + 5, y + 3, 4210752, false);
        }
    }

    private void drawSlot(GuiGraphics graphics, int x, int y, int w, int h) {
        if (w > 2 && h > 2) {
            graphics.fill(x, y, x + w, y + h, -7631989);
            graphics.fill(x, y, x + w, y + 1, -13158601);
            graphics.fill(x, y, x + 1, y + h, -13158601);
            graphics.fill(x, y + h - 1, x + w, y + h, -1);
            graphics.fill(x + w - 1, y, x + w, y + h, -1);
        }
    }

    private void drawCell(GuiGraphics graphics, CardInfo info, int x, int y, int w, int h, int count, boolean selected, boolean hover) {
        if (w > 2 && h > 2) {
            this.drawSlot(graphics, x, y, w, h);
            ResourceLocation art = CardTextureCache.get(info.code());
            graphics.blit(art, x + 1, y + 1, w - 2, h - 2, 0.0F, 0.0F, 176, 256, 176, 256);
            int limit = DeckRules.limitOf(info.code());
            if (limit < 3 && w > 12 && h > 12) {
                String badge = limit == 0 ? "禁" : Integer.toString(limit);
                int badgeW = this.font.width(badge) + 4;
                graphics.fill(x + 1, y + 1, x + 1 + badgeW, y + 9, limit == 2 ? -8431088 : -8450032);
                graphics.drawString(this.font, badge, x + 3, y + 2, -1, false);
            }

            if (count > 1 && w > 10) {
                String text = Integer.toString(count);
                graphics.drawString(this.font, text, x + w - 2 - this.font.width(text), y + h - 9, -1, true);
            }

            if (hover) {
                graphics.fill(x, y, x + w, y + h, 1358954495);
            }

            if (selected) {
                graphics.fill(x - 1, y - 1, x + w + 1, y, -1);
                graphics.fill(x - 1, y + h, x + w + 1, y + h + 1, -1);
                graphics.fill(x - 1, y, x, y + h, -1);
                graphics.fill(x + w, y, x + w + 1, y + h, -1);
            }
        }
    }

    private boolean inside(double mouseX, double mouseY, int x, int y, int w, int h) {
        return mouseX >= x && mouseX < x + w && mouseY >= y && mouseY < y + h;
    }

    private int clampScroll(int value, int lines, int visibleLines) {
        return Math.max(0, Math.min(value, Math.max(0, lines - visibleLines)));
    }

    private String trim(String text, int width) {
        if (width <= 8) {
            return "";
        } else {
            return this.font.width(text) <= width ? text : this.font.plainSubstrByWidth(text, width - 6) + "…";
        }
    }

    private void drawFitText(GuiGraphics graphics, String text, int x, int y, int maxWidth, int color, float minScale) {
        if (text != null && !text.isEmpty() && maxWidth > 4) {
            int width = this.font.width(text);
            float scale = width <= maxWidth ? 1.0F : Math.max(minScale, (float)maxWidth / width);
            if (scale >= 0.999F) {
                graphics.drawString(this.font, text, x, y, color, false);
            } else {
                String shown = text;
                if (width * scale > maxWidth) {
                    shown = this.trim(text, maxWidth);
                    int trimmed = this.font.width(shown);
                    scale = trimmed <= maxWidth ? 1.0F : Math.max(minScale, (float)maxWidth / trimmed);
                }

                graphics.pose().pushPose();
                graphics.pose().translate(x, y, 0.0);
                graphics.pose().scale(scale, scale, 1.0F);
                graphics.drawString(this.font, shown, 0, 0, color, false);
                graphics.pose().popPose();
            }
        }
    }

    public boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (super.mouseClicked(mouseX, mouseY, button)) {
            return true;
        }

        for (int zone = 0; zone < 3; zone++) {
            int headerY = this.sectionY[zone];
            if (this.inside(mouseX, mouseY, this.centerX, headerY, this.centerW, 12)) {
                this.activeZone = zone;
                return true;
            }
        }

        for (int zone = 0; zone < 3; zone++) {
            int gridX = this.centerX + 3;
            int gridY = this.sectionGridY[zone];
            List<Integer> codes = this.zoneCodes.get(zone);

            for (int row = 0; row < this.zoneRows[zone]; row++) {
                for (int col = 0; col < 10; col++) {
                    int index = this.zoneScroll[zone] + row * 10 + col;
                    if (index >= codes.size()) {
                        break;
                    }

                    int x = gridX + 1 + col * (this.cellW + 2);
                    int y = gridY + 1 + row * (this.cellH + 2);
                    if (this.inside(mouseX, mouseY, x, y, this.cellW, this.cellH)) {
                        int code = codes.get(index);
                        this.selectedCode = code;
                        this.activeZone = zone;
                        this.rebuildDetailLines();
                        if (button == 1) {
                            this.removeCard(code, zone);
                        }

                        return true;
                    }
                }
            }
        }

        if (this.inside(mouseX, mouseY, this.poolX + 4, this.poolListY + 1, this.poolW - 8, this.poolListH - 2)) {
            int row = (int)((mouseY - this.poolListY - 1.0) / 22.0);
            int index = this.poolScroll + row;
            if (row >= 0 && row < this.poolRows && index < this.results.size()) {
                CardInfo info = this.results.get(index);
                this.selectedCode = info.code();
                this.rebuildDetailLines();
                if (button == 0) {
                    this.addCard(info.code());
                } else if (button == 1) {
                    this.removeAnywhere(info.code());
                }

                return true;
            }
        }

        return false;
    }

    public boolean mouseScrolled(double mouseX, double mouseY, double delta) {
        if (this.inside(mouseX, mouseY, this.detailX, this.detailTextTop - 2, this.detailW, this.detailTextH + 4)) {
            this.detailScroll = this.clampScroll(this.detailScroll - (int)Math.signum(delta) * 2, this.detailTextHeight, this.visibleDetailLines());
            return true;
        }

        for (int zone = 0; zone < 3; zone++) {
            if (this.inside(
                mouseX, mouseY, this.centerX, this.sectionY[zone], this.centerW, this.sectionGridY[zone] + this.sectionGridH[zone] - this.sectionY[zone]
            )) {
                this.zoneScroll[zone] = this.clampScroll(
                    this.zoneScroll[zone] - (int)Math.signum(delta) * 10, this.zoneCodes.get(zone).size(), this.zoneRows[zone] * 10
                );
                return true;
            }
        }

        if (this.inside(mouseX, mouseY, this.poolX, this.poolListY, this.poolW, this.poolListH)) {
            this.poolScroll = this.clampScroll(this.poolScroll - (int)Math.signum(delta), this.results.size(), this.poolRows);
            return true;
        } else {
            return super.mouseScrolled(mouseX, mouseY, delta);
        }
    }

    public boolean keyPressed(int keyCode, int scanCode, int modifiers) {
        boolean enter = keyCode == 257 || keyCode == 335;
        if (enter && this.nameBox != null && this.nameBox.isFocused()) {
            this.sendName();
            this.nameBox.setFocused(false);
            return true;
        } else if (enter && this.searchBox != null && this.searchBox.isFocused()) {
            this.searchBox.setFocused(false);
            return true;
        } else {
            return super.keyPressed(keyCode, scanCode, modifiers);
        }
    }

    public void onClose() {
        this.sendName();
        super.onClose();
    }

    public boolean isPauseScreen() {
        return false;
    }
}
