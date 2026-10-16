package net.mdpromod;

import com.mojang.logging.LogUtils;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceKey;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.level.Level;
import org.slf4j.Logger;

import java.nio.file.Files;
import java.nio.file.Path;
import java.util.HashMap;
import java.util.Map;
import java.util.UUID;

/** 管理无头决斗主机进程，并把实时统计推给已绑定的决斗盘实体。 */
final class DuelSessionManager {
    private static final Logger LOGGER = LogUtils.getLogger();
    private DuelHostProcess host;
    private MinecraftServer server;
    private ResourceKey<Level> fieldDimension;
    private UUID fieldId;
    private UUID ownerId;
    private int[] lastHand = new int[0];
    private int lastTurn = -1;
    private boolean handActive;
    private int[] previewHand;
    private int[] lastPlaceHints = new int[0];
    private final java.util.Set<Integer> cardSelection = new java.util.LinkedHashSet<>();
    private String lastSelectSignature = "";
    private int[] lastSelectHints = new int[0];
    private int[] lastPileHints = new int[0];
    private String lastDecisionSignature = "";
    private int lastChainVersion = -1;
    private int[] lastChainMarks = new int[0];
    private int[] lastTargetMarks = new int[0];
    private int[] lastChainMarkAddrs = new int[0];
    private int[] lastTargetMarkAddrs = new int[0];
    /** 玩家选的时点：跟着决斗盘存着，开局时带给宿主。 */
    private String pendingTiming = "default";
    /** 当前额外卡组：核心会压缩序号，界面不能直接用卡组原始清单（见 ExtraDeckTracker）。 */
    private final ExtraDeckTracker extraTracker = new ExtraDeckTracker();
    /** 最近一次放到近半场的额外卡组：重开一局时用它把清单恢复成整副。 */
    private java.util.List<Integer> extraBase = java.util.List.of();
    /** 上一次播报过的阶段/回合：只在真的换阶段时才提示一次。 */
    private int lastPhaseTurn = -1;
    private String lastPhaseName = "";
    private String lastChainChoiceSignature = "";
    private String lastUnselectSignature = "";
    private String lastPositionSignature = "";
    private String lastSortSignature = "";
    private String lastOptionSignature = "";
    /** 上一次提醒过的"没有界面"的窗口签名，避免每 tick 刷屏。 */
    private String lastUnknownPromptSignature = "";
    private DeckData pendingDeck;
    /**
     * Decks bound through the in-world card case.  DuelFieldEntity keeps the
     * compact render state (counts/name), while the session manager keeps the
     * actual card list needed when the host process is started.  This also
     * avoids using one stale global pending deck when more than one field is
     * present in a world.
     */
    private final Map<UUID, Map<String, DeckData>> fieldDecks = new HashMap<>();
    private DuelAnimationScene scene;

    synchronized void start(MinecraftServer server, String deck0, String deck1) throws Exception {
        start(server, deck0, deck1, false);
    }

    synchronized void start(MinecraftServer server, String deck0, String deck1, boolean auto) throws Exception {
        start(server, deck0, deck1, auto, null);
    }

    /** 远半场那个人机的身份：卡组键 / 显示名 / 人格。放的是玩家自己的卡组时传 null。 */
    record AiProfile(String key, String name, String dialog) {
    }

    /** 运行时根目录：宿主的工作目录（卡组、cards.cdb、bot.conf 都在这里）。 */
    static Path runtimeRoot() {
        return ModPaths.runtimeRoot();
    }

    synchronized void start(MinecraftServer server, String deck0, String deck1, boolean auto,
                            AiProfile ai) throws Exception {
        if (host != null) {
            // 进程活着但没建起对局（例如卡组文件读不到）时允许重开，
            // 否则玩家会卡在「A duel is already active」上没法重试。
            if (host.isRunning() && host.duelActive()) {
                throw new IllegalStateException("A duel is already active");
            }
            host.close();
            host = null;
        }
        clearVisuals();
        previewHand = null;
        Path root = runtimeRoot();
        Path dotnet = Path.of(System.getProperty("mdpromod.dotnet", "D:\\MDProMod\\tools\\dotnet\\dotnet.exe"));
        Path dll = Path.of(System.getProperty("mdpromod.duelHost", "D:\\MDProMod\\tools\\duel_host\\bin\\Release\\net8.0\\duel_host.dll"));
        host = new DuelHostProcess(LOGGER, dotnet, dll, root);
        // 带上玩家选的时点（决斗盘存着的那档）
        host.setTiming(pendingTiming);
        // 额外卡组记账：给"查看额外卡组"一份和核心序号对齐的当前清单（召唤后会前移）
        try {
            extraBase = extrasOf(DeckData.fromYdk(Path.of(deck0)));
            extraTracker.reset(extraBase);
        } catch (Exception ex) {
            LOGGER.warn("读不到开局卡组 {}，额外卡组界面退回卡组清单：{}", deck0, ex.getMessage());
            extraTracker.stop();
        }
        host.start(deck0, deck1, auto, ai == null ? null : ai.name(),
                ai == null ? null : ai.dialog());
        this.server = server;
        scene = new DuelAnimationScene();
        scene.initialize(host.initialMain(0), host.initialExtra(0), host.initialMain(1), host.initialExtra(1));
        LOGGER.info("Started MDProMod duel on server {} (auto={})", server.getServerDirectory(), auto);
    }

    /**
     * Start the host for an already prepared duel field.  This is the entry
     * point used by the physical "start duel" control; it deliberately uses
     * the deck captured when the card case was placed on the field instead of
     * searching the player's inventory again.
     */
    synchronized void startForField(MinecraftServer server, DuelFieldEntity field,
                                    net.minecraft.server.level.ServerPlayer owner) throws Exception {
        if (field == null) throw new IllegalArgumentException("No duel field");
        DeckData deck = boundDeck(field, "p1");
        if (deck == null && pendingDeck != null) deck = pendingDeck;
        if (deck == null) {
            throw new IllegalStateException("该决斗盘近半场还没有卡组");
        }
        java.util.List<String> problems = DeckRules.problems(deck);
        if (!problems.isEmpty()) {
            throw new IllegalStateException("卡组还不能用于决斗：" + String.join("；", problems));
        }
        // 卡组放配置目录（config/mdpromod/decks）：换机器、删掉模组资源都不该影响玩家的卡组
        Path deckDir = ModPaths.deckDir();
        Files.createDirectories(deckDir);
        Path exported = CardCaseItem.exportYdk(deck, deckDir);
        // 时点跟着决斗盘走：开局时把盘上那一档带给宿主
        pendingTiming = field.timingMode().hostKey();
        // 远半场必须是玩家放上去的「人机卡组」，用它选的卡组和人格开打；没放就不让开始。
        String aiKey = field.aiKey();
        if (aiKey == null || aiKey.isBlank()) {
            throw new IllegalStateException("等待对手选择卡组");
        }
        AiDeckCatalog.Entry ai = AiDeckCatalog.find(runtimeRoot(), aiKey);
        String aiFile = ai == null ? "Data/Windbot/Decks/" + aiKey + ".ydk" : ai.file();
        start(server, exported.toAbsolutePath().toString(), aiFile, false,
                new AiProfile(aiKey, field.aiName(), field.aiDialog()));
        linkField(server, field, owner);
    }

    /** Compatibility entry point for the physical field control. */
    synchronized void startFromField(net.minecraft.server.level.ServerPlayer owner,
                                     DuelFieldEntity field) throws Exception {
        if (owner == null) throw new IllegalArgumentException("No player");
        startForField(owner.server, field, owner);
        field.setGameStarted(true);
    }

    synchronized DeckData boundDeck(DuelFieldEntity field, String side) {
        if (field == null) return null;
        Map<String, DeckData> decks = fieldDecks.get(field.getUUID());
        if (decks == null) return null;
        return decks.get(side == null ? "p1" : side);
    }

    /** 把正在进行的对局绑到某个决斗盘上，并记住手牌要推给谁。 */
    synchronized void linkField(MinecraftServer server, DuelFieldEntity field,
                                 net.minecraft.server.level.ServerPlayer owner) {
        if (host != null && host.isRunning() && this.ownerId != null && owner != null
                && (!this.ownerId.equals(owner.getUUID()) || this.fieldId != null && !this.fieldId.equals(field.getUUID()))) {
            owner.displayClientMessage(net.minecraft.network.chat.Component.literal("该对局已绑定玩家和决斗盘"), true);
            return;
        }
        if (this.fieldId != null && !this.fieldId.equals(field.getUUID())) clearVisuals();
        this.server = server;
        this.fieldDimension = field.level().dimension();
        this.fieldId = field.getUUID();
        this.ownerId = owner == null ? null : owner.getUUID();
        this.lastHand = new int[0];
        this.lastTurn = -1;
        this.handActive = false;
        if (host != null) {
            if (scene == null) {
                scene = new DuelAnimationScene();
                scene.initialize(host.initialMain(0), host.initialExtra(0), host.initialMain(1), host.initialExtra(1));
            }
            // 远半场只认玩家放上去的「人机卡组」，这里不再自动补预设卡组
        }
        LOGGER.info("Duel field linked: {} @ {} owner={}", fieldId, fieldDimension.location(), ownerId);
    }

    synchronized UUID linkedField() {
        return fieldId;
    }

    /** Network/engine work never blocks the Minecraft tick; only presentation is paced. */
    synchronized void tickField() {
        if (server == null) return;
        // 宿主的错误 / "应答被拒" 直接说给玩家听，别让它只留在日志里
        if (host != null) {
            String notice = host.consumeNotice();
            if (!notice.isEmpty() && ownerId != null) {
                var ownerPlayer = server.getPlayerList().getPlayer(ownerId);
                if (ownerPlayer != null) {
                    ownerPlayer.displayClientMessage(net.minecraft.network.chat.Component.literal(notice), false);
                }
            }
        }
        boolean running = host != null && host.isRunning();
        if (fieldId != null && fieldDimension != null) {
            ServerLevel level = server.getLevel(fieldDimension);
            if (level != null && level.getEntity(fieldId) instanceof DuelFieldEntity field) {
                if (scene != null) {
                    if (host != null) {
                        java.util.List<DuelVisualState.Event> events = host.drainEvents();
                        trackExtraDeck(events);
                        scene.enqueue(events);
                    }
                    scene.tick(level, field, ownerId);
                }
                if (host != null) {
                    boolean caughtUp = scene == null || !scene.busy();
                    if (caughtUp) {
                        field.setLifePoints("p1", host.lifePoints(0));
                        field.setLifePoints("p2", host.lifePoints(1));
                        field.setTurnSide(host.turnPlayer());
                        String phase = phaseName(host.phaseCode());
                        field.setCurrentPhase(host.turnPlayer() == 1 ? "p2" : "p1", phase);
                        // 进入新阶段时说一声「对方进入准备阶段」——MD 的时机提示就是这个意思
                        if (host.turnPlayer() != lastPhaseTurn || !phase.equals(lastPhaseName)) {
                            lastPhaseTurn = host.turnPlayer();
                            lastPhaseName = phase;
                            var who = ownerId == null ? null : server.getPlayerList().getPlayer(ownerId);
                            if (who != null) {
                                who.displayClientMessage(net.minecraft.network.chat.Component.literal(
                                        DuelTiming.phaseEntry(host.turnPlayer() == 1, phase)), true);
                            }
                        }
                    }
                    // 这一局分出胜负（或宿主进程已经结束）：把结果写到右下角的按钮上，
                    // 金色=本侧胜利 / 灰色=本侧失败；右键它就能回到「准备」开下一局。
                    if (caughtUp && field.duelResult() == 0 && field.isGameStarted() && host.duelActive()
                            && (host.isEnded() || !host.isRunning())) {
                        Integer winner = host.getWinner();
                        boolean iWon = winner != null && (winner & 1) == 0;
                        field.setDuelResult(iWon ? 1 : 2);
                        field.setOpening(false);
                        field.setPhasePrompt(-1, false, false, false);
                        var resultPlayer = ownerId == null ? null : server.getPlayerList().getPlayer(ownerId);
                        if (resultPlayer != null) {
                            resultPlayer.displayClientMessage(net.minecraft.network.chat.Component.literal(
                                    iWon ? "决斗胜利！右键右下角的金色按钮可以开新一局。"
                                            : "决斗失败。右键右下角的灰色按钮可以开新一局。"), false);
                        }
                    }
                    // 开局失败（例如卡组文件读不到）时退回「准备」，把原因说清楚，允许重试
                    if (host.isRunning() && !host.duelActive() && field.isGameStarted()
                            && !host.lastError().isEmpty()) {
                        field.setGameStarted(false);
                        var ownerPlayer = ownerId == null ? null : server.getPlayerList().getPlayer(ownerId);
                        if (ownerPlayer != null) {
                            ownerPlayer.displayClientMessage(
                                    net.minecraft.network.chat.Component.literal("开局失败：" + host.lastError()),
                                    false);
                        }
                    }
                }
                // 开局先演发牌：这期间右下角保持「游戏中」，阶段键全部锁住，
                // 初始手牌落位之后才把决策点和阶段键放出来。
                boolean opening = scene != null && scene.openingBusy();
                // 演出优先：动画队列没放完之前，弹窗和连锁显示都先压住（等画面跟上再问玩家）
                boolean reveal = !opening && (scene == null || !scene.busy());
                field.setOpening(opening);
                if (host != null && reveal) {
                    // 阶段键是否可以按，完全跟着核心当前决策点给出的允许位
                    field.setPhasePrompt(host.promptPlayer(), host.promptToBattle(),
                            host.promptToEnd(), host.promptToMain2());
                } else {
                    field.setPhasePrompt(-1, false, false, false);
                }
                // 落点提示：核心问落点时把可用格子同步给客户端（含"清空"）
                int[] hints = host != null && !opening ? host.placeAddresses() : new int[0];
                if (reveal && !java.util.Arrays.equals(hints, lastPlaceHints)) {
                    lastPlaceHints = hints;
                    var hintPlayer = ownerId == null ? null : server.getPlayerList().getPlayer(ownerId);
                    if (hintPlayer != null) {
                        ModNetwork.sendTo(hintPlayer, new ModNetwork.PlaceHint(field.getId(), hints));
                        // 只有 1 个合法落点（场地魔法区这类）时最容易让人以为卡住了：直接说出来
                        if (hints.length > 0) {
                            StringBuilder where = new StringBuilder();
                            for (int i = 0; i < hints.length; i++) {
                                if (i > 0) where.append("、");
                                where.append(placeLabel(hints[i]));
                            }
                            hintPlayer.displayClientMessage(net.minecraft.network.chat.Component.literal(
                                    "请左键点绿色的格子来选定落点：" + where), false);
                        }
                    }
                }
                // 选卡提示（弃牌等）：把候选做成"仅自己可见"的发光标记，选择进度也同步过去
                if (host != null && reveal && host.unselectPrompt()) {
                    // 融合素材那类"一张一张点 + 完成"：列表每次都推最新的（选/取消后核心会重问）
                    String signature = host.promptSeq() + ":u:" + java.util.Arrays.toString(host.unselectSelectCodes())
                            + ":" + java.util.Arrays.toString(host.unselectChosenCodes());
                    var chooser = ownerId == null ? null : server.getPlayerList().getPlayer(ownerId);
                    if (chooser != null && !signature.equals(lastUnselectSignature)) {
                        lastUnselectSignature = signature;
                        ModNetwork.sendTo(chooser, new ModNetwork.UnselectList(
                                host.unselectSelectCodes(), host.unselectChosenCodes(),
                                host.selectMin(), host.selectMax(), host.unselectFinishable(),
                                host.unselectSelectAddresses(), host.unselectChosenAddresses()));
                        // 场上/手牌的素材要"看得见"：可选的泛绿光，已选的转金光（只有自己可见）
                        int[] selectable = resolveCardEntities(level, field, host.unselectSelectAddresses());
                        int[] selected = resolveCardEntities(level, field, host.unselectChosenAddresses());
                        ModNetwork.sendTo(chooser, new ModNetwork.SelectHint(selectable, selected));
                    }
                } else if (host == null || !host.unselectPrompt()) {
                    lastUnselectSignature = "";
                }
                // 选表示形式（融合/同步召唤后问攻守）
                if (host != null && reveal && host.positionPrompt()) {
                    int[] values = host.positionValues();
                    String signature = host.promptSeq() + ":p:" + java.util.Arrays.toString(values);
                    var chooser = ownerId == null ? null : server.getPlayerList().getPlayer(ownerId);
                    if (chooser != null && !signature.equals(lastPositionSignature)) {
                        lastPositionSignature = signature;
                        String[] labels = new String[values.length];
                        for (int i = 0; i < values.length; i++) {
                            labels[i] = switch (values[i]) {
                                case 1 -> "攻击表示";
                                case 4 -> "守备表示";
                                case 2 -> "里侧攻击";
                                case 8 -> "里侧守备";
                                default -> "表示 " + values[i];
                            };
                        }
                        ModNetwork.sendTo(chooser, new ModNetwork.PositionList(values, labels));
                    }
                } else if (host == null || !host.positionPrompt()) {
                    lastPositionSignature = "";
                }
                // 排序（把卡组里的若干张按玩家想要的顺序排好）
                if (host != null && reveal && host.sortPrompt()) {
                    int[] codes = host.sortCodes();
                    String signature = host.promptSeq() + ":s:" + java.util.Arrays.toString(codes);
                    var sorter = ownerId == null ? null : server.getPlayerList().getPlayer(ownerId);
                    if (sorter != null && !signature.equals(lastSortSignature)) {
                        lastSortSignature = signature;
                        ModNetwork.sendTo(sorter, new ModNetwork.SortList(codes));
                    }
                } else if (host == null || !host.sortPrompt()) {
                    lastSortSignature = "";
                }
                // 选项（核心给若干处理方式，选一个）
                if (host != null && reveal && host.optionPrompt()) {
                    int[] values = host.optionValues();
                    String signature = host.promptSeq() + ":o:" + java.util.Arrays.toString(values);
                    var chooser = ownerId == null ? null : server.getPlayerList().getPlayer(ownerId);
                    if (chooser != null && !signature.equals(lastOptionSignature)) {
                        lastOptionSignature = signature;
                        ModNetwork.sendTo(chooser, new ModNetwork.OptionList(values, host.optionLabels()));
                    }
                } else if (host == null || !host.optionPrompt()) {
                    lastOptionSignature = "";
                }
                if (host != null && reveal && host.hasSelectCardPrompt() && !host.unselectPrompt()) {
                    // 序号 + 形状：核心重问同一条提示（应答被拒时会重发）也要重新弹界面
                    String signature = host.promptSeq() + ":" + host.selectSignature();
                    if (!signature.equals(lastSelectSignature)) {
                        lastSelectSignature = signature;
                        cardSelection.clear();
                        // 同时把候选列成清单：卡组/额外卡组的卡没有世界实体，只能点清单
                        var chooser = ownerId == null ? null : server.getPlayerList().getPlayer(ownerId);
                        if (chooser != null) {
                            ModNetwork.sendTo(chooser, new ModNetwork.SelectList(
                                    host.selectCodes(), host.selectMin(), host.selectMax(),
                                    host.selectAddrs()));
                        }
                    }
                    int[] selectable = host.selectableAddresses();
                    int[] entities = resolveCardEntities(level, field, selectable);
                    int[] selected = resolveCardEntities(level, field, packedSelection());
                    if (!java.util.Arrays.equals(entities, lastSelectHints)
                            || !java.util.Arrays.equals(selected, lastSelectHints)) {
                        lastSelectHints = entities;
                        var selectPlayer = ownerId == null ? null : server.getPlayerList().getPlayer(ownerId);
                        if (selectPlayer != null) {
                            ModNetwork.sendTo(selectPlayer, new ModNetwork.SelectHint(entities, selected));
                        }
                    }
                } else if (lastSelectHints.length > 0 && (host == null || !host.unselectPrompt())) {
                    lastSelectHints = new int[0];
                    lastSelectSignature = "";
                    cardSelection.clear();
                    var selectPlayer = ownerId == null ? null : server.getPlayerList().getPlayer(ownerId);
                    if (selectPlayer != null) {
                        ModNetwork.sendTo(selectPlayer, new ModNetwork.SelectHint(new int[0], new int[0]));
                    }
                }
                // 卡堆发光：额外卡组有可特召的卡、墓地/除外有可发动的卡
                int[] piles = host != null && !opening ? host.glowingPileZones() : new int[0];
                if (reveal && !java.util.Arrays.equals(piles, lastPileHints)) {
                    lastPileHints = piles;
                    var pilePlayer = ownerId == null ? null : server.getPlayerList().getPlayer(ownerId);
                    if (pilePlayer != null) {
                        ModNetwork.sendTo(pilePlayer, new ModNetwork.PileHint(field.getId(), piles));
                    }
                }
                // "是否发动"这类是和否决定：弹窗给玩家，别让对局停在这儿
                // 连锁演出：链上每多一张/解决一张，就把最新状态广播给场地附近的玩家
                if (host != null && reveal && host.chainVersion() != lastChainVersion) {
                    lastChainVersion = host.chainVersion();
                    ModNetwork.sendNear(level, field.position(), 96,
                            new ModNetwork.ChainState(field.getId(), host.chainCodes(), host.chainResolving()));
                }
                // 卡面标记：连锁里发动的卡（橙红）、被连锁中其他卡点名的卡（青蓝）。
                // 位置会随卡片移动，所以每 tick 都对一次实体，只有变了才广播。
                int[] chainMarks = host == null ? new int[0]
                        : resolveCardEntities(level, field, host.chainMarkAddresses());
                int[] targetMarks = host == null ? new int[0]
                        : resolveCardEntities(level, field, host.targetMarkAddresses());
                int[] chainMarkAddrs = host == null ? new int[0] : host.chainMarkAddresses();
                int[] targetMarkAddrs = host == null ? new int[0] : host.targetMarkAddresses();
                boolean clearingMarks = chainMarks.length == 0 && targetMarks.length == 0;
                if (reveal || clearingMarks) {
                    if (!java.util.Arrays.equals(chainMarks, lastChainMarks)
                            || !java.util.Arrays.equals(targetMarks, lastTargetMarks)
                            || !java.util.Arrays.equals(chainMarkAddrs, lastChainMarkAddrs)
                            || !java.util.Arrays.equals(targetMarkAddrs, lastTargetMarkAddrs)) {
                        lastChainMarks = chainMarks;
                        lastTargetMarks = targetMarks;
                        lastChainMarkAddrs = chainMarkAddrs;
                        lastTargetMarkAddrs = targetMarkAddrs;
                        ModNetwork.sendNear(level, field.position(), 96,
                                new ModNetwork.CardMarks(chainMarks, targetMarks,
                                        chainMarkAddrs, targetMarkAddrs));
                    }
                }
                // "要不要接连锁"：有候选卡时弹窗（空窗口由 host 自动放弃，不会到这里）
                if (host != null && reveal && host.chainChoicePrompt()) {
                    int[] codes = host.chainChoiceCodes();
                    // 链上还没有东西 = 这次问的是"要不要发动效果"（转阶段的诱发效果那种），
                    // 措辞不能说成"已经发动了，是否连锁"
                    boolean firstActivation = host.chainCodes().length == 0;
                    String signature = host.promptSeq() + ":"
                            + java.util.Arrays.toString(codes) + ":" + host.chainForced() + ":"
                            + firstActivation;
                    var chooser = ownerId == null ? null : server.getPlayerList().getPlayer(ownerId);
                    if (chooser != null && !signature.equals(lastChainChoiceSignature)) {
                        lastChainChoiceSignature = signature;
                        // 时机说明：连锁栈最上面那张就是"刚发动、要被连锁"的卡
                        // 全时点下"没有可接的卡"的空窗口也会停下来问：那时没有候选，措辞单独说
                        String question = codes.length == 0
                                ? DuelTiming.passQuestion()
                                : firstActivation
                                        ? DuelTiming.openQuestion(chainCandidateNames(host))
                                        : DuelTiming.chainQuestion(chainTriggerName(host));
                        ModNetwork.sendTo(chooser, new ModNetwork.ChainChoice(codes, host.chainForced(),
                                question, firstActivation, host.chainChoiceAddrs()));
                        // 有候选没解析出卡号时也要能答：说明一下，别让玩家以为界面坏了
                        if (host.chainChoiceHasUnknown()) {
                            chooser.displayClientMessage(net.minecraft.network.chat.Component.literal(
                                    "连锁窗口有 " + codes.length + " 张可选（部分卡面没认出来，仍可按下标选择或放弃）"),
                                    false);
                        }
                    }
                } else if (host == null || !host.chainChoicePrompt()) {
                    lastChainChoiceSignature = "";
                }
                if (host != null && reveal) {
                    int function = host.decisionFunction();
                    var decider = ownerId == null ? null : server.getPlayerList().getPlayer(ownerId);
                    String signature = function < 0 ? ""
                            : host.promptSeq() + ":" + host.decisionSignature();
                    if (function < 0) {
                        // 决定已经答完/消失：清掉去重标记，同一个效果下次还能弹
                        lastDecisionSignature = "";
                    } else if (decider != null && !signature.equals(lastDecisionSignature)) {
                        lastDecisionSignature = signature;
                        int code = host.decisionCardCode();
                        String name = code > 0
                                ? CardRegistry.get(code).map(CardInfo::name).orElse("#" + code) : "这张卡";
                        String question = function == 12
                                ? DuelTiming.activateQuestion(host.turnPlayer() == 1,
                                        phaseName(host.phaseCode()), name)
                                : (host.decisionDescription().isEmpty() ? "是否确认？" : "是否确认当前操作？");
                        ModNetwork.sendTo(decider, new ModNetwork.DecisionPrompt(function, name, question));
                    }
                }
                // 兜底提醒：核心在等一个模组还没有界面的窗口时，至少说出来，别让对局静默停住
                if (host != null && reveal) {
                    int pending = host.promptFunction();
                    var who = ownerId == null ? null : server.getPlayerList().getPlayer(ownerId);
                    String signature = pending < 0 ? "" : pending + ":" + host.promptSummary();
                    if (pending >= 0 && !isRenderedPrompt(pending)) {
                        if (who != null && !signature.equals(lastUnknownPromptSignature)) {
                            lastUnknownPromptSignature = signature;
                            who.displayClientMessage(net.minecraft.network.chat.Component.literal(
                                    "核心在等一个还没有界面的窗口（函数 " + pending + "）："
                                            + host.promptSummary() + " —— 现在会一直等在这里"), false);
                        }
                    } else {
                        lastUnknownPromptSignature = "";
                    }
                }
                if (previewHand != null && !running) reconcilePreview(level, field);
            }
        }
        pushHand(running || previewHand != null || scene != null && scene.busy());
    }

    /**
     * 连锁栈最上面那张（也就是刚发动、正被询问要不要连锁的那张）的卡名。
     * 场地效果会带 0x80000000 标记，读的时候要掩掉。
     */
    private static String chainTriggerName(DuelHostProcess host) {
        if (host == null) return "";
        int[] codes = host.chainCodes();
        if (codes.length == 0) return "";
        int code = codes[codes.length - 1] & 0x7FFFFFFF;
        return code <= 0 ? "" : CardRegistry.get(code).map(CardInfo::name).orElse("");
    }

    /** 连锁窗口里候选卡的卡名（解析不出来的留空，问句会退化成"有 N 张"）。 */
    private static String[] chainCandidateNames(DuelHostProcess host) {
        int[] codes = host == null ? new int[0] : host.chainChoiceCodes();
        String[] names = new String[codes.length];
        for (int i = 0; i < codes.length; i++) {
            int code = codes[i] & 0x7FFFFFFF;
            names[i] = code <= 0 ? "" : CardRegistry.get(code).map(CardInfo::name).orElse("");
        }
        return names;
    }

    private static String phaseName(int code) {
        return switch (code & 0xFFFF) {
            case 0x01 -> "DP";
            case 0x02 -> "SP";
            case 0x04 -> "M1";
            case 0x08, 0x10, 0x20, 0x40, 0x80 -> "BP";
            case 0x100 -> "M2";
            case 0x200 -> "EP";
            default -> "DP";
        };
    }

    /** 决斗盘菜单上的按钮：action 0 = 投降，1 = 换时点（value = {@link DuelTimingMode} 序号）。 */
    /**
     * 客户端按了决斗盘上的某个控件（阶段键 / 确认键）：核对之后交给决斗盘执行。
     * 命中判定在客户端做（更准），这里只认"控件存在 + 是自己那侧的"。
     */
    synchronized void handleFieldClick(net.minecraft.server.level.ServerPlayer player, String controlId) {
        if (player == null || controlId == null || controlId.isBlank()) return;
        DuelFieldEntity field = linkedField(player.server);
        if (field == null) return;
        DuelFieldLayout.Control control = DuelFieldLayout.controlById(controlId).orElse(null);
        if (control == null) return;
        if (!field.ownsControl(control)) {
            // 自己那侧 = p1 那半场（看归属，不看站位）；对面的键按了也白按
            player.displayClientMessage(Component.literal("这是对面那半场的按键"), true);
            return;
        }
        field.activateControl(player, control);
    }

    /** 决斗盘菜单上的按钮：action 0 = 投降，1 = 换时点（value = {@link DuelTimingMode} 序号）。 */
    synchronized void handleFieldAction(net.minecraft.server.level.ServerPlayer player,
                                        int action, int value) {
        if (player == null) return;
        if (action == 1) {
            DuelTimingMode mode = DuelTimingMode.byOrdinal(value);
            DuelFieldEntity field = linkedField(player.server);
            if (field != null) field.setTimingMode(mode);
            if (host != null && host.isRunning()) {
                try {
                    host.setTiming(mode.hostKey());
                } catch (Exception ex) {
                    LOGGER.warn("切换时点失败: {}", ex.getMessage());
                }
            }
            player.displayClientMessage(Component.literal(
                    "时点：" + mode.label() + "（" + mode.detail() + "）"), true);
            return;
        }
        if (action != 0) return;
        DuelFieldEntity field = linkedField(player.server);
        if (host == null || !host.isRunning() || !host.duelActive()
                || field == null || field.hasResult()) {
            player.displayClientMessage(Component.literal("现在没有进行中的决斗"), true);
            return;
        }
        try {
            host.surrender();
            player.displayClientMessage(Component.literal("已认输，这一局判给对方"), false);
        } catch (Exception ex) {
            player.displayClientMessage(Component.literal("投降失败：" + ex.getMessage()), false);
        }
    }

    /** 本会话绑定的决斗盘实体；找不到就返回 null。 */
    private DuelFieldEntity linkedField(MinecraftServer server) {
        if (server == null || fieldId == null || fieldDimension == null) return null;
        ServerLevel level = server.getLevel(fieldDimension);
        if (level == null) return null;
        return level.getEntity(fieldId) instanceof DuelFieldEntity field ? field : null;
    }

    /**
     * 这块盘上还有没有真的在跑的对局。
     * 重启 / 崩溃之后决斗盘可能还留着"进行中"的标记，但服务端这边已经没有宿主了——
     * 那种不算，否则玩家会卡在一个点不动的假状态里。
     */
    synchronized boolean hasLiveDuel(DuelFieldEntity field) {
        return field != null && host != null && host.isRunning()
                && fieldId != null && fieldId.equals(field.getUUID());
    }

    /**
     * 空手蹲下右键收回决斗盘：停宿主、收掉场上的卡与这场对局的记录、把决斗盘物品还给玩家。
     *
     * <p>正在决斗（已开局且还没有结果）时不动，只提醒一句——免得手一滑把一局删了；
     * 开局后的卡组记录清掉，但**卡盒和里面的卡组都还在玩家身上**，所以什么都不丢。
     *
     * @return true = 收回了；false = 因为还在决斗（或者不是本人的盘）没收
     */
    synchronized boolean reclaimField(net.minecraft.server.level.ServerPlayer player,
                                      DuelFieldEntity field) {
        if (player == null || field == null) return false;
        if (ownerId != null && !ownerId.equals(player.getUUID())) {
            player.displayClientMessage(Component.literal("这个决斗盘绑在别人身上，让本人来收"), true);
            return false;
        }
        if (field.isGameStarted() && !field.hasResult()) {
            player.displayClientMessage(Component.literal(
                    "正在决斗，先投降或打完这一局再收（右下角菜单里有投降）"), true);
            return false;
        }
        stop();                       // 关宿主进程 + 收掉场上的卡、手牌和演出
        fieldDecks.remove(field.getUUID());
        if (field.getUUID().equals(fieldId)) {
            fieldId = null;
            ownerId = null;
        }
        field.discard();
        net.minecraft.world.item.ItemStack stack =
                new net.minecraft.world.item.ItemStack(ModItems.DUEL_FIELD_ITEM.get());
        if (!player.getInventory().add(stack)) player.drop(stack, false);
        player.displayClientMessage(Component.literal(
                "已收回决斗盘（卡盒和里面的卡组都还在你身上）"), false);
        return true;
    }

    /** 把打包的落点地址说成人话（自己/对方的怪兽区 3、场地魔法区……）。 */
    private static String placeLabel(int packed) {
        int controller = (packed >> 16) & 1;
        int location = (packed >> 8) & 0xFF;
        int sequence = packed & 0xFF;
        String who = controller == 0 ? "自己" : "对方";
        return switch (location) {
            case 4 -> sequence < 5 ? who + "怪兽区" + (sequence + 1) : "额外怪兽区";
            case 8 -> sequence == 5 ? who + "场地魔法区" : who + "魔陷区" + (sequence + 1);
            case 1 -> who + "卡组";
            case 16 -> who + "墓地";
            case 32 -> who + "除外区";
            case 64 -> who + "额外卡组";
            default -> who + "区域" + location + "-" + sequence;
        };
    }

    /**
     * 这些功能号的窗口模组已经有界面（或已经有操作方式）：
     * 10/11 阶段按钮、12/13 是/否、14 选项、15/20 选卡（含祭品）、16 连锁、
     * 18/24 落点、19 表示形式、25 排序、26 一张一张选。
     */
    private static boolean isRenderedPrompt(int function) {
        return switch (function) {
            case 10, 11, 12, 13, 14, 15, 16, 18, 19, 20, 24, 25, 26 -> true;
            default -> false;
        };
    }

    private void reconcilePreview(ServerLevel level, DuelFieldEntity field) {
        Map<Integer, HandCardEntity> existing = new HashMap<>();
        for (HandCardEntity card : level.getEntitiesOfClass(HandCardEntity.class, field.getBoundingBox().inflate(28)))
            if (card.belongsTo(field.getUUID(), 0)) existing.put(card.getIndex(), card);
        for (int i = 0; i < previewHand.length; i++) {
            var at = DuelAnimationScene.handPoint(field, 0, i, previewHand.length);
            HandCardEntity card = existing.remove(i);
            if (card == null) {
                card = ModEntities.HAND_CARD.get().create(level);
                if (card == null) continue;
                card.configure(field.getUUID(), ownerIdText(), 0, i);
                card.moveTo(at.x, at.y, at.z, field.getYRot(), 0);
                level.addFreshEntity(card);
            } else card.moveTo(at.x, at.y, at.z, field.getYRot(), 0);
            card.setPrivateCode(previewHand[i]); card.setCardCode(0);
            var owner = ownerId == null ? null : server.getPlayerList().getPlayer(ownerId);
            if (owner != null) ModNetwork.sendTo(owner, new ModNetwork.CardKnowledge(card.getId(), previewHand[i]));
        }
        for (var unused : existing.values()) unused.discard();
    }

    private void clearVisuals() {
        if (server != null && fieldDimension != null && fieldId != null) {
            ServerLevel level = server.getLevel(fieldDimension);
            if (level != null && level.getEntity(fieldId) instanceof DuelFieldEntity field) {
                if (scene != null) scene.clear(level, field);
                for (var hand : level.getEntitiesOfClass(HandCardEntity.class, field.getBoundingBox().inflate(28)))
                    if (hand.fieldId().equals(fieldId.toString())) hand.discard();
                for (var card : level.getEntitiesOfClass(DuelCardEntity.class, field.getBoundingBox().inflate(28)))
                    if (card.belongsTo(fieldId)) card.discard();
            }
        }
        scene = null;
    }
    private String ownerIdText() {
        return ownerId == null ? "" : ownerId.toString();
    }

    /** 调试用：不跑对局也能在世界里摆一排手牌实体。 */
    synchronized void previewHand(net.minecraft.server.level.ServerPlayer player, int[] cards) {
        this.server = player.server;
        this.ownerId = player.getUUID();
        this.previewHand = cards;
    }

    synchronized void clearPreviewHand() {
        this.previewHand = null;
        if (host == null) clearVisuals();
    }

    /** Visual-only rehearsal, using the same event-to-animation path as a live duel. */
    synchronized void animationDemo(net.minecraft.server.level.ServerPlayer player, DuelFieldEntity field) {
        if (isRunning()) throw new IllegalStateException("请先结束当前对局，再播放动画演示");
        stop();
        linkField(player.server, field, player);
        scene = new DuelAnimationScene();
        scene.initialize(40, 15, 40, 15);
        DeckData demoDeck = DeckData.of("动画演示", new int[40], new int[15], new int[0]);
        if (!field.hasDeck("p1")) field.bindDeck("p1", demoDeck);
        if (!field.hasDeck("p2")) field.bindDeck("p2", demoDeck);
        scene.enqueue(DuelAnimationDemo.events());
    }

    /** 玩家用卡盒右键放下的卡组，决斗开始时可以直接拿来用。 */
    synchronized void setPendingDeck(DeckData deck) {
        this.pendingDeck = deck;
        // Existing callers predate per-field storage.  When a field has
        // already been linked (the normal empty-hand bind + card-case flow),
        // retain the same deck under that field's near-side slot as well.
        if (fieldId != null && deck != null) {
            fieldDecks.computeIfAbsent(fieldId, ignored -> new HashMap<>())
                    .put("p1", deck);
        }
    }

    /** Record a deck for an explicit field side (used by multiplayer UI). */
    synchronized void setPendingDeck(DuelFieldEntity field, String side, DeckData deck) {
        if (field == null || deck == null) return;
        fieldDecks.computeIfAbsent(field.getUUID(), ignored -> new HashMap<>())
                .put(side == null ? "p1" : side, deck);
        this.pendingDeck = deck;
        // 近半场的额外卡组记一份：界面要跟着核心的序号走（召唤后序号会前移）
        if (!"p2".equals(side)) {
            extraBase = java.util.List.copyOf(deck.extraDeck());
            extraTracker.reset(extraBase);
        }
    }

    synchronized DeckData pendingDeck() {
        return pendingDeck;
    }

    /** 手牌有变化时才发快照给玩家客户端。 */
    private void pushHand(boolean running) {
        if (ownerId == null) return;
        net.minecraft.server.level.ServerPlayer owner = server.getPlayerList().getPlayer(ownerId);
        if (owner == null) return;
        // 开局发牌演出期间先不亮手牌 HUD：等牌真的落到手牌位再出现
        boolean opening = scene != null && scene.openingBusy();
        int[] hand = opening ? new int[0] : previewHand != null ? previewHand
                : scene != null ? scene.hand(0) : running && host != null ? host.handOf(0) : new int[0];
        int turn = running && host != null ? host.turnPlayer() : 0;
        boolean active = running && !opening;
        if (handActive == active && lastTurn == turn && java.util.Arrays.equals(hand, lastHand)) return;
        lastHand = hand;
        lastTurn = turn;
        handActive = active;
        ModNetwork.sendTo(owner, new ModNetwork.HandSync(hand, active, turn));
    }

    /** 调试用：直接推一份手牌快照（不依赖对局）。 */
    synchronized void sendHandSnapshot(net.minecraft.server.level.ServerPlayer player,
                                       int[] cards, boolean active, int turn) {
        this.server = player.server;
        this.ownerId = player.getUUID();
        this.lastHand = cards;
        this.lastTurn = turn;
        this.handActive = active;
        ModNetwork.sendTo(player, new ModNetwork.HandSync(cards, active, turn));
    }

    synchronized void stop() {
        if (host != null) host.close();
        host = null;
        previewHand = null;
        clearVisuals();
        pushHand(false);
    }

    /**
     * 决胜之后重开：收掉上一局的宿主进程与场面，把决斗盘退回「准备」。
     * 本侧卡组留着（按钮显示绿色「准备」），对面卸掉等点按钮时自动补 AI 卡组，
     * 然后就能开新一局。玩家想换卡组的话，直接用卡盒右键决斗盘即可。
     */
    synchronized void resetForNewDuel(net.minecraft.server.level.ServerPlayer player,
                                      DuelFieldEntity field) {
        stop();
        // 新一局：额外卡组恢复成整副（没记过就用盘上绑着的那副）
        java.util.List<Integer> base = extraBase.isEmpty() ? extrasOf(boundDeck(field, "p1")) : extraBase;
        if (!base.isEmpty()) extraTracker.reset(base);
        if (field == null) return;
        field.setGameStarted(false);
        field.setDuelResult(0);
        field.setOpening(false);
        field.setPhasePrompt(-1, false, false, false);
        field.setLifePoints("p1", 8000);
        field.setLifePoints("p2", 8000);
        field.setTurnSide(0);
        field.setCurrentPhase("p1", "DP");
        field.setCurrentPhase("p2", "DP");
        field.unbindDeck("p2");
        field.updateVisualCounts(
                new int[]{field.mainCount("p1"), 0},
                new int[]{field.extraCount("p1"), 0},
                new int[]{0, 0},
                new int[]{0, 0});
        if (player != null) {
            player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                    "已回到准备：想换卡组就用卡盒右键决斗盘，点绿色按钮开始新一局"), false);
        }
    }

    synchronized boolean isRunning() {
        return host != null && host.isRunning();
    }

    synchronized boolean isEnded() {
        return host != null && host.isEnded();
    }

    synchronized String statusSummary() {
        return host == null ? "no duel host" : host.statusSummary();
    }

    synchronized String statsSummary() {
        return host == null ? "no duel host" : host.statsSummary();
    }

    synchronized String promptSummary() {
        return host == null ? "no duel host" : host.promptSummary();
    }

    synchronized String promptResponseKind() {
        return host == null ? "" : host.promptResponseKind();
    }

    synchronized void respond(int value) throws Exception {
        if (host == null) throw new IllegalStateException("No duel host");
        host.respond(value);
    }

    synchronized void respondBytes(String hex) throws Exception {
        if (host == null) throw new IllegalStateException("No duel host");
        host.respondBytes(hex);
    }

    synchronized String placeOptions() {
        return host == null ? "no duel host" : host.placeOptions();
    }

    synchronized void respondPlace(int index) throws Exception {
        if (host == null) throw new IllegalStateException("No duel host");
        host.respondPlace(index);
    }

    /**
     * 阶段按钮点击：把 抽卡/准备/主要1/战斗/主要2/结束 翻译成核心当前决策点允许的应答。
     * 核心只允许 主要阶段→战斗/结束 与 战斗阶段→主要2/结束 这四种转移。
     */
    synchronized void submitPhase(DuelFieldEntity field, net.minecraft.server.level.ServerPlayer player,
                                  String phase) throws Exception {
        if (host == null || !host.isRunning()) throw new IllegalStateException("当前没有进行中的决斗");
        int value = host.phaseResponse(phase);
        if (value < 0) throw new IllegalStateException("现在还不能切换到该阶段");
        host.respond(value);
        LOGGER.info("Phase click: field={} player={} phase={} response={}",
                field == null ? "-" : field.getUUID(),
                player == null ? "-" : player.getScoreboardName(), phase, value);
    }

    /** 当前是否在等玩家给一张卡选落点。 */
    synchronized boolean hasPlacePrompt() {
        return host != null && host.isRunning() && host.hasPlacePrompt();
    }

    /**
     * 卡片点击：把"这张卡现在能做什么"翻译成核心应答。
     * kinds 按优先级排列（如 summon → spsummon → activate）；返回 false 表示当前做不了。
     */
    synchronized boolean submitCardAction(net.minecraft.server.level.ServerPlayer player,
                                          int controller, int location, int sequence, String... kinds)
            throws Exception {
        if (host == null || !host.isRunning()) throw new IllegalStateException("当前没有进行中的决斗");
        int value = host.promptAction(controller, location, sequence, kinds);
        if (value < 0) return false;
        host.respond(value);
        LOGGER.info("Card action: player={} card=({},{},{}) kinds={} response={}",
                player == null ? "-" : player.getScoreboardName(), controller, location, sequence,
                String.join("/", kinds), value);
        return true;
    }

    /** 选落点：把 (座位, 区域类型, 序号) 送回去，只在核心正在问落点时有效。 */
    synchronized boolean submitPlaceFor(int controller, int location, int sequence) throws Exception {
        if (host == null || !host.isRunning()) return false;
        int index = host.placeIndex(controller, location, sequence);
        if (index < 0) return false;
        host.respondPlace(index);
        LOGGER.info("Place choice: controller={} location={} sequence={} index={}",
                controller, location, sequence, index);
        return true;
    }

    /** 左键点卡 → 算出这张卡现在能做的动作，发给客户端弹按钮。 */
    synchronized void offerCardActions(net.minecraft.server.level.ServerPlayer player, int entityId,
                                       int[][] candidates) {
        if (host == null || !host.isRunning() || candidates == null) return;
        // 核心正在等选卡（弃牌/选素材）时，不弹动作菜单：点卡就是"选/取消选这张"
        if (host.hasSelectCardPrompt()) {
            if (candidates.length > 0 && candidates[0] != null) {
                if (!toggleCardSelect(player, candidates[0][0], candidates[0][1], candidates[0][2])) {
                    player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                            "这张卡不在可选的候选里"), true);
                }
            }
            return;
        }
        int mask = 0;
        for (int[] address : candidates) {
            if (address == null) continue;
            if (host.promptAction(address[0], address[1], address[2], "summon") >= 0
                    )
                mask |= 1 << ModNetwork.CARD_MENU_SUMMON;
            if (host.promptAction(address[0], address[1], address[2], "spsummon") >= 0)
                mask |= 1 << ModNetwork.CARD_MENU_SPSUMMON;
            if (host.promptAction(address[0], address[1], address[2], "mset") >= 0
                    || host.promptAction(address[0], address[1], address[2], "sset") >= 0)
                mask |= 1 << ModNetwork.CARD_MENU_SET;
            if (host.promptAction(address[0], address[1], address[2], "activate") >= 0)
                mask |= 1 << ModNetwork.CARD_MENU_ACTIVATE;
            if (host.promptAction(address[0], address[1], address[2], "attack") >= 0)
                mask |= 1 << ModNetwork.CARD_MENU_ATTACK;
            if (host.promptAction(address[0], address[1], address[2], "position") >= 0)
                mask |= 1 << ModNetwork.CARD_MENU_POSITION;
        }
        if (mask == 0) {
            // 说清楚"为什么没按钮"：这句话以前只发在动作条上，一闪就没了，玩家会觉得是坏了
            String reason = !host.isWaiting()
                    ? "现在不是你的决策点（对手回合或演出中）"
                    : host.chainChoicePrompt()
                    ? "有连锁窗口要先答完"
                    : "核心当前没给出候选：" + host.promptSummary();
            player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                    "这张卡没有可做的动作 —— " + reason), false);
            return;
        }
        ModNetwork.sendTo(player, new ModNetwork.CardActions(entityId, mask));
    }

    /** 客户端点了动作按钮：按类型再核一次候选（菜单可能已过期），然后发送应答。 */
    /** 把打包地址解析成世界实体的 id（移动的实体用 id 定位，客户端据此发光）。 */
    private int[] resolveCardEntities(ServerLevel level, DuelFieldEntity field, int[] addresses) {
        if (addresses == null || addresses.length == 0) return new int[0];
        java.util.List<Integer> ids = new java.util.ArrayList<>();
        var hands = level.getEntitiesOfClass(HandCardEntity.class, field.getBoundingBox().inflate(28));
        var cards = level.getEntitiesOfClass(DuelCardEntity.class, field.getBoundingBox().inflate(28));
        for (int packed : addresses) {
            int controller = (packed >> 16) & 1;
            int location = (packed >> 8) & 0xFF;
            int sequence = packed & 0xFF;
            if (location == 2) {
                for (HandCardEntity hand : hands) {
                    if (hand.side() == controller && hand.getIndex() == sequence) ids.add(hand.getId());
                }
            } else {
                String zoneId = DuelFieldLayout.zoneIdFor(controller, location, sequence);
                if (zoneId == null) continue;
                for (DuelCardEntity card : cards) {
                    if (zoneId.equals(card.getZoneId())) ids.add(card.getId());
                }
            }
        }
        return ids.stream().mapToInt(Integer::intValue).toArray();
    }

    private int[] packedSelection() {
        return cardSelection.stream().mapToInt(Integer::intValue).toArray();
    }

    /**
     * 点选卡（弃牌、选素材那类）：切换这张卡的选中状态。
     * 选够张数（min == max 时）就把 responseBytes 发回核心。
     */
    /** 打开卡堆交互界面：额外卡组列出可特召的卡，墓地/除外列出可发动的卡，另附全部卡。 */
    synchronized void openPile(net.minecraft.server.level.ServerPlayer player,
                               DuelFieldEntity field, int side, int location) {
        if (host == null || !host.isRunning()) return;
        // 动作按钮只对自己的卡堆有意义（核心给的候选列表也只写自己这一侧）
        int[][] actions = side == 0 ? host.pileActions(location) : new int[0][];
        int[] sequences = new int[actions.length];
        int[] codes = new int[actions.length];
        for (int i = 0; i < actions.length; i++) {
            sequences[i] = actions[i][0];
            codes[i] = actions[i][1];
        }
        ModNetwork.sendTo(player, new ModNetwork.PileView(location, sequences, codes, allCodes(side, location)));
    }

    /** 额外卡组的进出记账：核心压缩序号后，界面上的"序号"必须跟着它走。 */
    private void trackExtraDeck(java.util.List<DuelVisualState.Event> events) {
        for (DuelVisualState.Event event : events) {
            DuelVisualState.Address from = event.from();
            DuelVisualState.Address to = event.to();
            if (from == null || to == null || event.card() == null) continue;
            extraTracker.move(from.controller(), from.location(), to.controller(), to.location(),
                    event.card().code());
        }
    }

    private static java.util.List<Integer> extrasOf(DeckData deck) {
        return deck == null ? java.util.List.of() : java.util.List.copyOf(deck.extraDeck());
    }

    /** 卡堆的全部内容：额外卡组用绑定的卡组数据（核心只公开表侧额外卡），墓地/除外用演出层。 */
    private int[] allCodes(int side, int location) {
        // 自己的额外卡组：用"当前还在额外卡组里"的那份（召唤后核心会把序号整体前移，
        // 拿卡组原始清单会出现"召掉的卡还在、序号还错位"）
        if (side == 0 && location == 64 && extraTracker.tracking()) {
            java.util.List<Integer> live = extraTracker.codes();
            if (host != null && host.isRunning()) {
                int engineCount = host.count(0, 0x40);
                if (live.size() != engineCount) {
                    LOGGER.warn("额外卡组记账与核心不一致：界面 {} 张 / 核心 {} 张（界面序号可能错位）",
                            live.size(), engineCount);
                }
            }
            return live.stream().mapToInt(Integer::intValue).toArray();
        }
        // 对手的卡堆只列公开信息（公开的墓地 / 表侧除外 / 表侧额外），
        // 否则"看对面的额外卡组"会把 15 张里侧牌全抖出来。
        if (side != 0) {
            return scene == null ? new int[0] : scene.publicCodesIn(side, location);
        }
        String key = "p1";
        if (location == 64) {
            DeckData deck = fieldId == null ? pendingDeck
                    : fieldDecks.getOrDefault(fieldId, Map.of()).getOrDefault(key, pendingDeck);
            if (deck == null) return new int[0];
            return deck.extraDeck().stream().mapToInt(Integer::intValue).toArray();
        }
        return scene == null ? new int[0] : scene.codesIn(side, location);
    }

    /** 卡堆界面里按下"特殊召唤 / 发动"。 */
    /** 玩家在弹窗里回答"是 / 否"。 */
    /**
     * 列表界面选好了卡：把下标发回核心（SelectCard 的应答 = 张数 + 各下标）。
     * 每个下标都会对着当前候选再校一遍，菜单过期就丢弃。
     */
    synchronized void answerSelect(net.minecraft.server.level.ServerPlayer player, int[] indices) {
        if (host == null || !host.isRunning() || !host.hasSelectCardPrompt()) return;
        int count = host.selectCodes().length;
        StringBuilder hex = new StringBuilder();
        int used = 0;
        StringBuilder body = new StringBuilder();
        for (int index : indices) {
            if (index < 0 || index >= count) continue;
            body.append(String.format("%02X", index));
            used++;
        }
        if (used == 0) return;
        hex.append(String.format("%02X", used)).append(body);
        try {
            // ocgcore 的 select_card / select_tribute / select_unselect_card 都只认这个格式：
            //   [张数, 下标…]
            // （返回值从 returns.bvalue[0] 开始校验，发整数会被判非法 → MSG_RETRY）
            host.respondBytes(hex.toString());
            LOGGER.info("Select list answered: {} -> {}", player == null ? "-" : player.getScoreboardName(), hex);
            lastSelectSignature = "";
            cardSelection.clear();
        } catch (Exception ex) {
            player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                    "选卡失败：" + ex.getMessage()), true);
        }
    }

    /** 玩家选了"接连锁的第几张"（-1 = 放弃）。 */
    /** 融合素材那种"一张一张选"：index = 合并列表下标（先可选的、后已选的），-1 = 完成。 */
    synchronized void unselectPick(net.minecraft.server.level.ServerPlayer player, int index) {
        if (host == null || !host.isRunning() || !host.unselectPrompt()) return;
        try {
            if (index < 0) {
                host.respond(-1);
            } else {
                host.respondBytes(String.format("01%02X", index));
            }
            lastUnselectSignature = "";
        } catch (Exception ex) {
            player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                    "选素材失败：" + ex.getMessage()), true);
        }
    }

    /** 选表示形式（攻击/守备）。 */
    /** 排序应答：[张数, 新顺序的下标…]。 */
    /** 选项应答：直接把选项值发回去。 */
    synchronized void answerOption(net.minecraft.server.level.ServerPlayer player, int value) {
        if (host == null || !host.isRunning() || !host.optionPrompt()) return;
        try {
            host.respond(value);
            lastOptionSignature = "";
            LOGGER.info("Option answered: {} -> {}", player == null ? "-" : player.getScoreboardName(), value);
        } catch (Exception ex) {
            player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                    "选项应答失败：" + ex.getMessage()), true);
        }
    }

    synchronized void answerSort(net.minecraft.server.level.ServerPlayer player, int[] order) {
        if (host == null || !host.isRunning() || !host.sortPrompt()) return;
        int total = host.sortCodes().length;
        if (order == null || order.length != total) return;
        // 注意：SortCard 的应答**没有长度前缀**——引擎从 bvalue[0] 开始直接读这个排列
        // （和 SelectCard 的"张数 + 下标"不同，加前缀会被当成下标越界而 RETRY）。
        StringBuilder hex = new StringBuilder();
        for (int index : order) {
            if (index < 0 || index >= total) return;
            hex.append(String.format("%02X", index));
        }
        try {
            host.respondBytes(hex.toString());
            lastSortSignature = "";
            LOGGER.info("Sort answered: {}", hex);
        } catch (Exception ex) {
            player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                    "排序失败：" + ex.getMessage()), true);
        }
    }

    synchronized void answerPosition(net.minecraft.server.level.ServerPlayer player, int value) {
        if (host == null || !host.isRunning() || !host.positionPrompt()) return;
        try {
            host.respond(value);
            lastPositionSignature = "";
        } catch (Exception ex) {
            player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                    "选表示失败：" + ex.getMessage()), true);
        }
    }

    synchronized void answerChain(net.minecraft.server.level.ServerPlayer player, int index) {
        if (host == null || !host.isRunning()) return;
        try {
            // 注意：SelectChain 的应答是**纯下标**（引擎校验 0 <= 值 < 候选数），
            // 不是 (下标<<16)——后者只有下标 0 恰好合法，别的都会被判非法重选。
            host.respond(index < 0 ? -1 : index);
            lastChainChoiceSignature = "";
            LOGGER.info("Chain answer: {} -> {}", player == null ? "-" : player.getScoreboardName(), index);
        } catch (Exception ex) {
            player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                    "连锁回答失败：" + ex.getMessage()), true);
        }
    }

    synchronized void answerDecision(net.minecraft.server.level.ServerPlayer player, int value) {
        if (host == null || !host.isRunning()) return;
        if (host.decisionFunction() < 0) return;
        try {
            host.respond(value == 1 ? 1 : 0);
            lastDecisionSignature = "";
            LOGGER.info("Decision answered: {} -> {}", player == null ? "-" : player.getScoreboardName(), value);
        } catch (Exception ex) {
            player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                    "回答失败：" + ex.getMessage()), true);
        }
    }

    synchronized void performPileAct(net.minecraft.server.level.ServerPlayer player,
                                     int location, int sequence) {
        if (host == null || !host.isRunning()) return;
        try {
            String kind = location == 64 ? "spsummon" : "activate";
            if (!submitCardAction(player, 0, location, sequence, kind)) {
                player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                        "这张卡现在不能执行该动作"), true);
            }
        } catch (Exception ex) {
            player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                    "操作失败：" + ex.getMessage()), true);
        }
    }

    synchronized boolean toggleCardSelect(net.minecraft.server.level.ServerPlayer player,
                                          int controller, int location, int sequence) {
        if (host == null || !host.isRunning() || !host.hasSelectCardPrompt()) return false;
        int index = host.selectCardIndex(controller, location, sequence);
        if (index < 0) return false;
        // 26 号（融合素材那类）核心只接受"一次一张"：直接发这一张的下标，
        // 取消/完成走界面（完成 = -1）。
        if (host.unselectPrompt()) {
            try {
                host.respondBytes(String.format("01%02X", index));
                player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                        "已选素材（在素材界面里点已选的卡可取消，按完成结束）"), true);
            } catch (Exception ex) {
                player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                        "选素材失败：" + ex.getMessage()), true);
            }
            return true;
        }
        int packed = (controller << 16) | (location << 8) | sequence;
        int min = Math.max(1, host.selectMin());
        int max = Math.max(min, host.selectMax());
        if (!cardSelection.remove(packed)) {
            if (cardSelection.size() >= max) return true;
            cardSelection.add(packed);
        }
        player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                "已选 " + cardSelection.size() + " 张（需要 " + min + ".." + max + "）"), true);
        int need = host.selectMin() == host.selectMax() ? min : max;
        if (cardSelection.size() >= need) {
            java.util.List<Integer> indices = new java.util.ArrayList<>();
            for (int address : cardSelection) {
                int found = host.selectCardIndex((address >> 16) & 1, (address >> 8) & 0xFF, address & 0xFF);
                if (found >= 0) indices.add(found);
            }
            java.util.Collections.sort(indices);
            StringBuilder hex = new StringBuilder(String.format("%02X", indices.size()));
            for (int value : indices) hex.append(String.format("%02X", value));
            try {
                host.respondBytes(hex.toString());
                LOGGER.info("Card selection sent: {}", hex);
            } catch (Exception ex) {
                LOGGER.warn("Card selection failed: {}", ex.getMessage());
            }
            cardSelection.clear();
        }
        return true;
    }

    synchronized void performCardAction(net.minecraft.server.level.ServerPlayer player, int entityId, int kind) {
        if (host == null || !host.isRunning()) return;
        net.minecraft.world.entity.Entity entity = player.level().getEntity(entityId);
        java.util.List<int[]> candidates = new java.util.ArrayList<>();
        if (entity instanceof HandCardEntity hand && !hand.ownerId().isEmpty()
                && hand.ownerId().equals(player.getUUID().toString())) {
            candidates.add(new int[]{hand.side(), 2, hand.getIndex()});
        } else if (entity instanceof DuelCardEntity card) {
            String zone = card.getZoneId();
            if (zone.startsWith("shared_emz_")) {
                candidates.add(DuelFieldLayout.engineAddress(zone, 0));
                candidates.add(DuelFieldLayout.engineAddress(zone, 1));
            } else {
                candidates.add(DuelFieldLayout.engineAddress(zone, 0));
            }
        }
        String[] kinds = switch (kind) {
            case ModNetwork.CARD_MENU_SUMMON -> new String[]{"summon"};
            case ModNetwork.CARD_MENU_SPSUMMON -> new String[]{"spsummon"};
            case ModNetwork.CARD_MENU_SET -> new String[]{"mset", "sset"};
            case ModNetwork.CARD_MENU_ACTIVATE -> new String[]{"activate"};
            case ModNetwork.CARD_MENU_ATTACK -> new String[]{"attack"};
            case ModNetwork.CARD_MENU_POSITION -> new String[]{"position"};
            default -> new String[0];
        };
        if (kinds.length == 0) return;
        try {
            for (int[] address : candidates) {
                if (address == null) continue;
                if (submitCardAction(player, address[0], address[1], address[2], kinds)) return;
            }
            player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                    "这个动作现在做不了（规则核心没有提供候选）"), true);
        } catch (Exception ex) {
            player.displayClientMessage(net.minecraft.network.chat.Component.literal(
                    "操作失败：" + ex.getMessage()), true);
        }
    }
}
