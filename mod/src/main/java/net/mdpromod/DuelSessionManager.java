package net.mdpromod;

import com.mojang.logging.LogUtils;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.HashMap;
import java.util.HashSet;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.UUID;
import java.util.concurrent.ThreadLocalRandom;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceKey;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.EntityType;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.level.ItemLike;
import net.minecraft.world.level.Level;
import net.minecraft.world.phys.Vec3;
import org.slf4j.Logger;

final class DuelSessionManager {
    private static final Logger LOGGER = LogUtils.getLogger();
    private DuelHostProcess host;
    private MinecraftServer server;
    private ResourceKey<Level> fieldDimension;
    private UUID fieldId;
    private UUID ownerId;
    private UUID seat1Id;
    private final int[][] lastHand = new int[][]{new int[0], new int[0]};
    private final int[] lastTurn = new int[]{-1, -1};
    private final boolean[] handActive = new boolean[]{false, false};
    private int[] previewHand;
    private int[] lastPlaceHints = new int[0];
    private final Set<Integer> cardSelection = new LinkedHashSet<>();
    private String lastSelectSignature = "";
    private int[] lastSelectHints = new int[0];
    private int[] lastPileHints = new int[0];
    private String lastDecisionSignature = "";
    private int lastChainVersion = -1;
    private int[] lastChainMarks = new int[0];
    private int[] lastTargetMarks = new int[0];
    private int[] lastChainMarkAddrs = new int[0];
    private int[] lastTargetMarkAddrs = new int[0];
    private String pendingTiming = "default";
    private final ExtraDeckTracker extraTracker = new ExtraDeckTracker();
    private final ExtraDeckTracker extraTracker2 = new ExtraDeckTracker(1);
    private List<Integer> extraBase = List.of();
    private int lastPhaseTurn = -1;
    private String lastPhaseName = "";
    private String lastChainChoiceSignature = "";
    private String lastUnselectSignature = "";
    private String lastPositionSignature = "";
    private String lastAnnounceSignature = "";
    private String lastSortSignature = "";
    private String lastOptionSignature = "";
    private String lastSumSignature = "";
    private String lastUnknownPromptSignature = "";
    private DuelSessionManager.PendingActivation pendingActivation;
    private DeckData pendingDeck;
    private final Map<UUID, Map<String, DeckData>> fieldDecks = new HashMap<>();
    private DuelAnimationScene scene;
    private DuelSessionManager.StartStage startStage = DuelSessionManager.StartStage.IDLE;
    private final int[] rpsPicks = new int[]{0, 0};
    private int rpsWinner = -1;
    private long startDeadline;
    private static final long RPS_TIMEOUT_MS = 30000L;
    private static final long RPS_RESEND_MS = 5000L;
    private long lastRpsPromptAt;
    private UUID rpsFallbackId;
    private int rpsFallbackSeat = -1;
    private Path exportDir;
    private static final long TP_TIMEOUT_MS = 30000L;

    synchronized void start(MinecraftServer server, String deck0, String deck1) throws Exception {
        this.start(server, deck0, deck1, false);
    }

    synchronized void start(MinecraftServer server, String deck0, String deck1, boolean auto) throws Exception {
        this.start(server, deck0, deck1, auto, null);
    }

    static Path runtimeRoot() {
        return ModPaths.runtimeRoot();
    }

    synchronized void start(MinecraftServer server, String deck0, String deck1, boolean auto, DuelSessionManager.AiProfile ai) throws Exception {
        this.start(server, deck0, deck1, auto, ai, true);
    }

    synchronized void start(MinecraftServer server, String deck0, String deck1, boolean auto, DuelSessionManager.AiProfile ai, boolean withAi) throws Exception {
        this.start(server, deck0, deck1, auto, ai, withAi, withAi ? 1 : -1);
    }

    synchronized void start(MinecraftServer server, String deck0, String deck1, boolean auto, DuelSessionManager.AiProfile ai, boolean withAi, int aiSeat) throws Exception {
        if (this.host != null) {
            if (this.host.isRunning() && this.host.duelActive()) {
                throw new IllegalStateException("A duel is already active");
            }

            this.host.close();
            this.host = null;
        }

        this.clearVisuals();
        this.pendingActivation = null;
        this.previewHand = null;
        Path root = runtimeRoot();
        Path dotnet = Path.of(System.getProperty("mdpromod.dotnet", "D:\\MDProMod\\tools\\dotnet\\dotnet.exe"));
        Path dll = Path.of(System.getProperty("mdpromod.duelHost", "D:\\MDProMod\\tools\\duel_host\\bin\\Release\\net8.0\\duel_host.dll"));
        this.host = new DuelHostProcess(LOGGER, dotnet, dll, root);
        this.host.setTiming(this.pendingTiming);

        try {
            this.extraTracker.reset(extrasOf(DeckData.fromYdk(Path.of(deck0))));
        } catch (Exception ex) {
            LOGGER.warn("读不到开局卡组 {}，额外卡组界面退回卡组清单：{}", deck0, ex.getMessage());
            this.extraTracker.stop();
        }

        try {
            this.extraTracker2.reset(extrasOf(DeckData.fromYdk(Path.of(deck1))));
        } catch (Exception ex) {
            this.extraTracker2.stop();
        }

        try {
            String humanDeck = aiSeat == 0 ? deck1 : deck0;
            this.extraBase = extrasOf(DeckData.fromYdk(Path.of(humanDeck)));
        } catch (Exception ex) {
            this.extraBase = List.of();
        }

        this.host.start(deck0, deck1, auto, ai == null ? null : ai.name(), ai == null ? null : ai.dialog(), withAi, aiSeat);
        this.server = server;
        this.scene = new DuelAnimationScene();
        this.scene.initialize(this.host.initialMain(0), this.host.initialExtra(0), this.host.initialMain(1), this.host.initialExtra(1));
        LOGGER.info("Started MDProMod duel on server {} (auto={})", server.getServerDirectory(), auto);
    }

    synchronized void startForField(MinecraftServer server, DuelFieldEntity field, ServerPlayer owner) throws Exception {
        if (field == null) {
            throw new IllegalArgumentException("No duel field");
        }

        String aiKey = field.aiKey();
        boolean withAi = aiKey != null && !aiKey.isBlank();
        int aiSide = withAi ? Math.max(0, field.aiSide()) : -1;
        DeckData deck0 = this.boundDeck(field, "p1");
        DeckData deck1 = this.boundDeck(field, "p2");
        if (!withAi && deck0 == null && this.pendingDeck != null) {
            deck0 = this.pendingDeck;
        }

        if (DuelStart.requiresPlayerDeck(0, aiSide) && deck0 == null) {
            throw new IllegalStateException("该决斗盘近半场还没有卡组");
        }

        if (DuelStart.requiresPlayerDeck(1, aiSide) && deck1 == null) {
            throw new IllegalStateException("等待对手选择卡组");
        }

        if (aiSide != 0 && deck0 != null) {
            List<String> problems0 = DeckRules.problems(deck0);
            if (!problems0.isEmpty()) {
                throw new IllegalStateException("卡组还不能用于决斗：" + String.join("；", problems0));
            }
        }

        if (aiSide != 1 && deck1 != null) {
            List<String> problems1 = DeckRules.problems(deck1);
            if (!problems1.isEmpty()) {
                throw new IllegalStateException("对手的卡组还不能用于决斗：" + String.join("；", problems1));
            }
        }

        // Each duel field writes its duel decks into its own folder: with several duels running at once, two decks
        // of the same name would otherwise overwrite each other's file while a duel host is still starting up.
        Path deckDir = ModPaths.deckDir().resolveSibling("duel-decks").resolve(field.getUUID().toString());
        Files.createDirectories(deckDir);
        this.exportDir = deckDir;
        Path exported0 = deck0 == null ? null : CardCaseItem.exportYdk(deck0, deckDir);
        Path exported1 = deck1 == null
            ? null
            : CardCaseItem.exportYdk(DeckData.of(deck1.name() + " (p2)", deck1.mainArray(), deck1.extraArray(), deck1.sideArray()), deckDir);
        this.pendingTiming = field.timingMode().hostKey();
        if (withAi) {
            AiDeckCatalog.Entry ai = AiDeckCatalog.find(runtimeRoot(), aiKey);
            String aiFile = ai == null ? "Data/Windbot/Decks/" + aiKey + ".ydk" : ai.file();
            String file0 = aiSide == 0 ? aiFile : exported0.toAbsolutePath().toString();
            String file1 = aiSide == 1 ? aiFile : exported1.toAbsolutePath().toString();
            this.start(server, file0, file1, false, new DuelSessionManager.AiProfile(aiKey, field.aiName(), field.aiDialog()), true, aiSide);
            this.linkField(server, field, this.playerForSeat(server, DuelStart.humanSeat(aiSide)));
        } else {
            this.start(server, exported0.toAbsolutePath().toString(), exported1.toAbsolutePath().toString(), false, null, false, -1);
            this.linkField(server, field, this.playerForSeat(server, 0));
        }
    }

    synchronized void startFromField(ServerPlayer owner, DuelFieldEntity field) throws Exception {
        if (owner == null) {
            throw new IllegalArgumentException("No player");
        }

        this.startForField(owner.server, field, owner);
        field.setGameStarted(true);
    }

    synchronized boolean requestDuelStart(ServerPlayer trigger) {
        return this.server == null ? false : this.requestDuelStart(this.linkedField(this.server), trigger);
    }

    /**
     * Starts rock-paper-scissors for the given field. The field is passed in (instead of taking whatever field the
     * session was last linked to) so that a start request can never act on another duel field.
     */
    synchronized boolean requestDuelStart(DuelFieldEntity target, ServerPlayer trigger) {
        if (target == null) {
            return false;
        }

        this.attach(target);
        DuelFieldEntity field = this.linkedField(this.server);
        if (field == target && !field.isGameStarted() && !field.hasResult()) {
            if (this.startStage != DuelSessionManager.StartStage.IDLE) {
                return true;
            }

            if (!field.isReadyToStart()) {
                return false;
            }

            // Who plays is decided by the field, not by which seat happens to hold a human: a human on the far half
            // against an AI on the near half (their own choice, or the board was turned because the AI went first in
            // the duel before) used to be taken for a player duel and waited 30 s for a second human to pick.
            boolean ai = field.aiSide() >= 0 && field.aiKey() != null && !field.aiKey().isBlank();
            boolean pvp = !ai && this.ownerId != null && this.seat1Id != null;
            if (!pvp && !ai) {
                return false;
            }

            this.startStage = DuelSessionManager.StartStage.RPS;
            this.rpsPicks[0] = 0;
            this.rpsPicks[1] = 0;
            this.rpsWinner = -1;
            this.lastRpsPromptAt = System.currentTimeMillis();
            this.rpsFallbackId = trigger == null ? null : trigger.getUUID();
            this.startDeadline = System.currentTimeMillis() + 30000L;
            int aiSeat = ai ? field.aiSide() : -1;
            this.rpsFallbackSeat = ai ? 1 - aiSeat : -1;
            if (aiSeat >= 0) {
                this.rpsPicks[aiSeat] = randomHand();
            }

            for (int seat = 0; seat < 2; seat++) {
                ServerPlayer who = this.rpsPicks[seat] == 0 ? this.startPlayer(seat) : null;
                if (who != null) {
                    ModNetwork.sendTo(who, new ModNetwork.RpsPrompt());
                    LOGGER.info("RPS prompt -> seat {} ({})", seat, who.getScoreboardName());
                }
            }

            LOGGER.info("Duel start: RPS begins on field {} (pvp={}, aiSeat={})", new Object[]{this.fieldId, pvp, aiSeat});
            return true;
        } else {
            return false;
        }
    }

    private static int randomHand() {
        return 1 + ThreadLocalRandom.current().nextInt(3);
    }

    /**
     * The player who answers for a seat while the duel is being started: the seated player, or, when the human
     * seat of an AI duel has nobody bound to it (deck placed by command), the player who asked to start.
     */
    private ServerPlayer startPlayer(int seat) {
        ServerPlayer who = this.playerForSeat(seat);
        if (who == null && seat == this.rpsFallbackSeat && this.rpsFallbackId != null && this.server != null) {
            who = this.server.getPlayerList().getPlayer(this.rpsFallbackId);
        }

        return who;
    }

    /** Seat a player answers for during the start, or -1 when the player has no part in it. */
    private int startSeatOf(ServerPlayer player) {
        UUID id = player.getUUID();
        if (id.equals(this.ownerId)) {
            return 0;
        } else if (id.equals(this.seat1Id)) {
            return 1;
        } else {
            return this.rpsFallbackSeat >= 0 && id.equals(this.rpsFallbackId) && this.playerForSeat(this.rpsFallbackSeat) == null
                ? this.rpsFallbackSeat
                : -1;
        }
    }

    synchronized boolean isStarting() {
        return this.startStage != DuelSessionManager.StartStage.IDLE;
    }

    synchronized void answerRps(ServerPlayer player, int hand) {
        if (this.startStage == DuelSessionManager.StartStage.RPS && player != null) {
            if (hand >= 1 && hand <= 3) {
                int seat = this.startSeatOf(player);
                if (seat >= 0 && this.rpsPicks[seat] == 0) {
                    this.rpsPicks[seat] = hand;
                    LOGGER.info("RPS pick: seat {} ({})", seat, player.getScoreboardName());
                    this.resolveRps();
                }
            }
        }
    }

    synchronized void answerTp(ServerPlayer player, boolean first) {
        if (this.startStage == DuelSessionManager.StartStage.CHOOSE_TP && player != null) {
            if (this.startSeatOf(player) == this.rpsWinner) {
                this.chooseTp(this.rpsWinner, first);
            }
        }
    }

    private void resolveRps() {
        if (this.rpsPicks[0] != 0 && this.rpsPicks[1] != 0) {
            int a = this.rpsPicks[0];
            int b = this.rpsPicks[1];
            this.broadcast("猜拳：" + this.seatLabel(0) + " " + DuelStart.handName(a) + " vs " + this.seatLabel(1) + " " + DuelStart.handName(b));
            int winner = DuelStart.winner(a, b);
            if (winner < 0) {
                this.broadcast("平局，再来一次");
                this.restartRps();
            } else {
                this.rpsWinner = winner;
                this.startStage = DuelSessionManager.StartStage.CHOOSE_TP;
                this.startDeadline = System.currentTimeMillis() + 30000L;
                this.lastRpsPromptAt = System.currentTimeMillis();
                ServerPlayer winnerPlayer = this.startPlayer(winner);
                if (winnerPlayer != null) {
                    ModNetwork.sendTo(winnerPlayer, new ModNetwork.TpPrompt());
                } else {
                    this.chooseTp(winner, ThreadLocalRandom.current().nextBoolean());
                }
            }
        }
    }

    private void restartRps() {
        this.rpsPicks[0] = 0;
        this.rpsPicks[1] = 0;
        DuelFieldEntity field = this.linkedField(this.server);
        int aiSeat = field == null ? -1 : field.aiSide();
        if (aiSeat >= 0) {
            this.rpsPicks[aiSeat] = randomHand();
        }

        this.lastRpsPromptAt = System.currentTimeMillis();
        this.startDeadline = System.currentTimeMillis() + 30000L;

        for (int seat = 0; seat < 2; seat++) {
            ServerPlayer who = this.rpsPicks[seat] == 0 ? this.startPlayer(seat) : null;
            if (who != null) {
                ModNetwork.sendTo(who, new ModNetwork.RpsPrompt());
            }
        }
    }

    private void chooseTp(int winnerSeat, boolean chooseFirst) {
        if (this.startStage == DuelSessionManager.StartStage.CHOOSE_TP) {
            this.startStage = DuelSessionManager.StartStage.IDLE;
            int firstSeat = DuelStart.firstSeat(winnerSeat, chooseFirst);
            this.broadcast(this.seatLabel(winnerSeat) + " 选择了" + (chooseFirst ? "先攻" : "后攻"));
            this.finishDuelStart(firstSeat);
        }
    }

    private void finishDuelStart(int firstSeat) {
        DuelFieldEntity field = this.linkedField(this.server);
        if (field != null) {
            boolean flipped = false;
            String firstLabel = this.seatLabel(firstSeat);

            try {
                if (DuelStart.needsFlip(firstSeat)) {
                    this.flipField(field);
                    flipped = true;
                }

                this.startForField(this.server, field, this.playerForSeat(0));
                field.setGameStarted(true);
                this.broadcast("决斗开始：" + firstLabel + " 先手");
            } catch (Exception ex) {
                if (flipped) {
                    this.flipField(field);
                    LOGGER.warn("开局失败后已恢复翻场前的卡组与座位");
                }

                LOGGER.warn("开局失败：{}", ex.getMessage());
                this.broadcast("开局失败：" + ex.getMessage());
                this.startStage = DuelSessionManager.StartStage.IDLE;
            }
        }
    }

    private void flipField(DuelFieldEntity field) {
        field.rotateHalfTurn();
        field.swapSides();
        UUID oldOwner = this.ownerId;
        this.ownerId = this.seat1Id;
        this.seat1Id = oldOwner;
        Map<String, DeckData> decks = this.fieldDecks.get(field.getUUID());
        if (decks != null) {
            DeckData p1 = decks.remove("p1");
            DeckData p2 = decks.remove("p2");
            if (p2 != null) {
                decks.put("p1", p2);
            }

            if (p1 != null) {
                decks.put("p2", p1);
            }
        }

        int aiSide = field.aiSide();
        this.pendingDeck = this.boundDeck(field, aiSide == 0 ? "p2" : "p1");
        LOGGER.info("Duel start: field flipped so seat 0 is the (old) p2 side");
    }

    private void resendRpsIfNeeded() {
        long now = System.currentTimeMillis();
        if (now - this.lastRpsPromptAt >= 5000L) {
            this.lastRpsPromptAt = now;
            if (this.startStage == DuelSessionManager.StartStage.CHOOSE_TP) {
                // The winner was asked only once: a question closed or covered by another screen never came back.
                ServerPlayer winnerPlayer = this.startPlayer(this.rpsWinner);
                if (winnerPlayer != null) {
                    ModNetwork.sendTo(winnerPlayer, new ModNetwork.TpPrompt());
                    LOGGER.info("TP prompt resent -> seat {} ({})", this.rpsWinner, winnerPlayer.getScoreboardName());
                }

                return;
            }

            for (int seat = 0; seat < 2; seat++) {
                if (this.rpsPicks[seat] == 0) {
                    ServerPlayer who = this.startPlayer(seat);
                    if (who != null) {
                        ModNetwork.sendTo(who, new ModNetwork.RpsPrompt());
                        LOGGER.info("RPS prompt resent -> seat {} ({})", seat, who.getScoreboardName());
                    }
                }
            }
        }
    }

    private void tickStartStage() {
        if (this.startStage != DuelSessionManager.StartStage.IDLE) {
            this.resendRpsIfNeeded();
        }

        if (this.startStage != DuelSessionManager.StartStage.IDLE && System.currentTimeMillis() >= this.startDeadline) {
            if (this.startStage == DuelSessionManager.StartStage.RPS) {
                for (int seat = 0; seat < 2; seat++) {
                    if (this.rpsPicks[seat] == 0) {
                        this.rpsPicks[seat] = randomHand();
                        ServerPlayer who = this.playerForSeat(seat);
                        if (who != null) {
                            who.displayClientMessage(Component.literal("猜拳超时，已随机替你出拳"), false);
                        }
                    }
                }

                this.resolveRps();
            } else if (this.startStage == DuelSessionManager.StartStage.CHOOSE_TP) {
                ServerPlayer winnerPlayer = this.startPlayer(this.rpsWinner);
                if (winnerPlayer != null) {
                    winnerPlayer.displayClientMessage(Component.literal("选先后超时，已随机"), false);
                }

                this.chooseTp(this.rpsWinner, ThreadLocalRandom.current().nextBoolean());
            }
        }
    }

    private String seatLabel(int seat) {
        ServerPlayer who = this.playerForSeat(seat);
        if (who != null) {
            return who.getScoreboardName();
        }

        DuelFieldEntity field = this.linkedField(this.server);
        return field != null && field.aiSide() == seat ? "人机" : "座位 " + seat;
    }

    private void broadcast(String text) {
        for (int seat = 0; seat < 2; seat++) {
            ServerPlayer who = this.playerForSeat(seat);
            if (who != null) {
                who.displayClientMessage(Component.literal(text), false);
            }
        }
    }

    synchronized DeckData boundDeck(DuelFieldEntity field, String side) {
        if (field == null) {
            return null;
        }

        Map<String, DeckData> decks = this.fieldDecks.get(field.getUUID());
        return decks == null ? null : decks.get(side == null ? "p1" : side);
    }

    private int askedSeat() {
        return this.host == null ? -1 : this.host.promptPlayer();
    }

    synchronized boolean isSecondPlayer(ServerPlayer player, DuelFieldEntity field) {
        if (player == null) {
            return false;
        } else if (this.seat1Id != null) {
            return this.seat1Id.equals(player.getUUID());
        } else {
            return this.ownerId != null ? !this.ownerId.equals(player.getUUID()) : field != null && "p2".equals(field.playerSide(player));
        }
    }

    synchronized boolean isOwner(ServerPlayer player) {
        return player != null && this.ownerId != null && this.ownerId.equals(player.getUUID());
    }

    synchronized boolean sideTakenByOther(ServerPlayer player, String side) {
        if (player == null) {
            return false;
        }

        UUID id = player.getUUID();
        UUID holder = "p2".equals(side) ? this.seat1Id : this.ownerId;
        return holder != null && !holder.equals(id);
    }

    private ServerPlayer playerForSeat(MinecraftServer server, int seat) {
        UUID id = seat == 1 ? this.seat1Id : this.ownerId;
        return id != null && server != null ? server.getPlayerList().getPlayer(id) : null;
    }

    private ServerPlayer playerForSeat(int seat) {
        return this.playerForSeat(this.server, seat);
    }

    private ServerPlayer playerForSeat() {
        int seat = this.askedSeat();
        return this.playerForSeat(seat < 0 ? 0 : seat);
    }

    private int seatOf(ServerPlayer player) {
        return player != null && this.seat1Id != null && this.seat1Id.equals(player.getUUID()) ? 1 : 0;
    }

    private boolean isAsking(ServerPlayer player) {
        int asked = this.askedSeat();
        return asked < 0 || asked == this.seatOf(player);
    }

    private boolean refuseIfNotAsked(ServerPlayer player) {
        if (player != null && !this.isAsking(player)) {
            player.displayClientMessage(Component.literal("现在不是你的操作"), true);
            return true;
        } else {
            return false;
        }
    }

    private boolean isParticipant(ServerPlayer player) {
        if (player == null) {
            return false;
        }

        UUID id = player.getUUID();
        return this.ownerId != null && this.ownerId.equals(id) ? true : this.seat1Id != null && this.seat1Id.equals(id);
    }

    synchronized void requestPromptResend(ServerPlayer player) {
        if (player != null && this.host != null && this.host.isRunning()) {
            int asked = this.host.promptPlayer();
            if (asked >= 0 && asked == this.seatOf(player) && this.isParticipant(player)) {
                if (this.pendingActivation != null
                    && this.pendingActivation.host() == this.host
                    && this.pendingActivation.player().equals(player.getUUID())
                    && this.pendingActivation.seq() == this.host.promptSeq()) {
                    this.sendActivationChoices(player, this.pendingActivation);
                } else {
                    this.lastSelectSignature = "";
                    this.lastUnselectSignature = "";
                    this.lastPositionSignature = "";
                    this.lastSortSignature = "";
                    this.lastOptionSignature = "";
                    this.lastSumSignature = "";
                    this.lastChainChoiceSignature = "";
                    this.lastDecisionSignature = "";
                    this.lastPlaceHints = new int[0];
                    LOGGER.info("Prompt resend requested by {} (function {})", player.getScoreboardName(), this.host.promptFunction());
                }
            }
        }
    }

    synchronized void bindSecondPlayer(ServerPlayer player, DuelFieldEntity field, DeckData deck) {
        this.bindCaseSide(player, field, deck, "p2");
    }

    synchronized void bindCaseSide(ServerPlayer player, DuelFieldEntity field, DeckData deck, String side) {
        if (player != null && field != null && deck != null && side != null) {
            boolean p2 = "p2".equals(side);
            UUID id = player.getUUID();
            if (p2) {
                if (id.equals(this.ownerId)) {
                    // The near seat can be held without a deck there: placing the AI deck links its player to the
                    // board. Clearing that half would throw the AI deck away the moment the player sits down opposite.
                    if (field.aiSide() == 0) {
                        this.ownerId = null;
                    } else {
                        this.releaseSide(field, "p1");
                    }
                }

                this.seat1Id = id;
            } else {
                if (id.equals(this.seat1Id)) {
                    if (field.aiSide() == 1) {
                        this.seat1Id = null;
                    } else {
                        this.releaseSide(field, "p2");
                    }
                }

                this.ownerId = id;
            }

            field.bindDeck(side, deck);
            field.setSeatId(side, id);
            this.fieldDecks.computeIfAbsent(field.getUUID(), ignored -> new HashMap<>()).put(side, deck);
            if (!p2) {
                this.pendingDeck = deck;
                this.extraBase = List.copyOf(deck.extraDeck());
                this.extraTracker.reset(this.extraBase);
            } else {
                this.extraTracker2.reset(List.copyOf(deck.extraDeck()));
            }

            LOGGER.info("Card case bound: {} -> {} ({})", new Object[]{player.getName().getString(), side, id});
            MDProMod.duels().vacateOthers(player, this);
        }
    }

    synchronized void clearCaseSide(ServerPlayer player, DuelFieldEntity field, String side) {
        if (field != null && side != null) {
            this.releaseSide(field, side);
            LOGGER.info("Card case cleared: {} (by {})", side, player == null ? "-" : player.getName().getString());
        }
    }

    synchronized void bindAiSeat(DuelFieldEntity field) {
        this.bindAiSeat(field, "p2");
    }

    synchronized void bindAiSeat(DuelFieldEntity field, String side) {
        boolean p2 = "p2".equals(side);
        UUID holder = p2 ? this.seat1Id : this.ownerId;
        if (holder != null) {
            LOGGER.info("AI deck takes {}; releasing human binding {}", side, holder);
            if (field == null) {
                if (p2) {
                    this.seat1Id = null;
                } else {
                    this.ownerId = null;
                }
            } else {
                this.releaseSide(field, p2 ? "p2" : "p1");
            }
        }
    }

    private void releaseSide(DuelFieldEntity field, String side) {
        boolean p2 = "p2".equals(side);
        field.unbindDeck(side);
        Map<String, DeckData> decks = this.fieldDecks.get(field.getUUID());
        if (decks != null) {
            decks.remove(side);
        }

        if (this.fieldId == null || field.getUUID().equals(this.fieldId)) {
            if (p2) {
                this.seat1Id = null;
                this.extraTracker2.stop();
            } else {
                this.ownerId = null;
                this.pendingDeck = null;
                this.extraBase = List.of();
                this.extraTracker.stop();
            }
        }
    }

    private void pushHandForSeat(int seat) {
        ServerPlayer viewer = this.playerForSeat(seat);
        if (viewer != null) {
            boolean running = this.host != null && this.host.isRunning();
            boolean opening = this.scene != null && this.scene.openingBusy();
            int[] hand = opening
                ? new int[0]
                : (
                    this.previewHand != null && seat == 0
                        ? this.previewHand
                        : (this.scene != null ? this.scene.hand(seat) : (running ? this.host.handOf(seat) : new int[0]))
                );
            int turn = running ? this.host.turnPlayer() : 0;
            boolean active = running && !opening;
            if (this.handActive[seat] != active || this.lastTurn[seat] != turn || !Arrays.equals(hand, this.lastHand[seat])) {
                this.handActive[seat] = active;
                this.lastTurn[seat] = turn;
                this.lastHand[seat] = hand;
                ModNetwork.sendTo(viewer, new ModNetwork.HandSync(hand, active, running && turn == seat));
            }
        }
    }

    synchronized void linkField(MinecraftServer server, DuelFieldEntity field, ServerPlayer owner) {
        UUID linkedId = owner == null ? null : owner.getUUID();
        if (this.host != null && this.host.isRunning() && linkedId != null) {
            UUID engineHolder = this.ownerId != null ? this.ownerId : this.seat1Id;
            if (engineHolder != null && !engineHolder.equals(linkedId) || this.fieldId != null && !this.fieldId.equals(field.getUUID())) {
                owner.displayClientMessage(Component.literal("该对局已绑定玩家和决斗盘"), true);
                return;
            }
        }

        if (this.fieldId != null && !this.fieldId.equals(field.getUUID())) {
            this.clearVisuals();
        }

        this.server = server;
        this.fieldDimension = field.level().dimension();
        this.fieldId = field.getUUID();
        // Linking only fills an empty near seat. It used to overwrite it, so anybody right-clicking the board with
        // an empty hand took the seat of the player whose deck was already lying there.
        if (this.ownerId == null && DuelStart.shouldSetOwnerSeat(linkedId, this.seat1Id)) {
            this.ownerId = linkedId;
        }

        for (int seat = 0; seat < 2; seat++) {
            this.lastHand[seat] = new int[0];
            this.lastTurn[seat] = -1;
            this.handActive[seat] = false;
        }

        if (this.host != null && this.scene == null) {
            this.scene = new DuelAnimationScene();
            this.scene.initialize(this.host.initialMain(0), this.host.initialExtra(0), this.host.initialMain(1), this.host.initialExtra(1));
        }

        LOGGER.info(
            "Duel field linked: {} @ {} seat0={} seat1={} viewer={}",
            new Object[]{this.fieldId, this.fieldDimension.location(), this.ownerId, this.seat1Id, linkedId}
        );
    }

    synchronized UUID linkedField() {
        return this.fieldId;
    }

    synchronized void tickField() {
        if (this.server != null) {
            this.tickStartStage();
            if (this.host != null) {
                String notice = this.host.consumeNotice();
                if (!notice.isEmpty()) {
                    ServerPlayer noticePlayer = this.playerForSeat();
                    if (noticePlayer != null) {
                        noticePlayer.displayClientMessage(Component.literal(notice), false);
                    }
                }
            }

            boolean running = this.host != null && this.host.isRunning();
            if (this.fieldId != null && this.fieldDimension != null) {
                ServerLevel level = this.server.getLevel(this.fieldDimension);
                if (level != null && level.getEntity(this.fieldId) instanceof DuelFieldEntity field) {
                    if (this.scene != null) {
                        if (this.host != null) {
                            List<DuelVisualState.Event> events = this.host.drainEvents();
                            this.trackExtraDeck(events);
                            this.scene.enqueue(events);
                        }

                        this.scene.tick(level, field, this.ownerId, this.seat1Id);
                    }

                    if (this.host != null) {
                        boolean caughtUp = this.scene == null || !this.scene.busy();
                        if (caughtUp) {
                            field.setLifePoints("p1", this.host.lifePoints(0));
                            field.setLifePoints("p2", this.host.lifePoints(1));
                            field.setTurnSide(this.host.turnPlayer());
                            String phase = phaseName(this.host.phaseCode());
                            field.setCurrentPhase(this.host.turnPlayer() == 1 ? "p2" : "p1", phase);
                            if (this.host.turnPlayer() != this.lastPhaseTurn || !phase.equals(this.lastPhaseName)) {
                                this.lastPhaseTurn = this.host.turnPlayer();
                                this.lastPhaseName = phase;

                                for (int seat = 0; seat < 2; seat++) {
                                    ServerPlayer who = this.playerForSeat(seat);
                                    if (who != null) {
                                        who.displayClientMessage(Component.literal(DuelTiming.phaseEntry(this.host.turnPlayer() != seat, phase)), true);
                                    }
                                }
                            }
                        }

                        if (caughtUp
                            && field.duelResult() == 0
                            && field.isGameStarted()
                            && this.host.duelActive()
                            && (this.host.isEnded() || !this.host.isRunning())) {
                            Integer winner = this.host.getWinner();
                            field.setOpening(false);
                            field.setPhasePrompt(-1, false, false, false);

                            for (int seat = 0; seat < 2; seat++) {
                                ServerPlayer resultPlayer = this.playerForSeat(seat);
                                if (resultPlayer != null) {
                                    boolean iWon = winner != null && (winner & 1) == seat;
                                    resultPlayer.displayClientMessage(Component.literal(iWon ? "决斗胜利！右键右下角的金色按钮可以开新一局。" : "决斗失败。右键右下角的灰色按钮可以开新一局。"), false);
                                }
                            }

                            if (winner != null) {
                                field.setDuelResult((winner & 1) + 1);
                            } else {
                                // A draw, or a duel host that died: there is no result to show, and the board would
                                // stay "in a duel" that nobody can surrender, restart or pick up.
                                LOGGER.warn("Duel on field {} ended without a winner; back to preparation", this.fieldId);
                                this.abandon(field, "这一局没有分出胜负（平局或决斗核心中断），决斗盘已回到准备状态");
                                return;
                            }
                        }

                        if (this.host.isRunning() && !this.host.duelActive() && field.isGameStarted() && !this.host.lastError().isEmpty()) {
                            field.setGameStarted(false);

                            for (int seat = 0; seat < 2; seat++) {
                                ServerPlayer failPlayer = this.playerForSeat(seat);
                                if (failPlayer != null) {
                                    failPlayer.displayClientMessage(Component.literal("开局失败：" + this.host.lastError()), false);
                                }
                            }
                        }

                        if (!this.host.isProcessAlive() && !this.host.duelActive() && field.isGameStarted() && !field.hasResult()) {
                            String error = this.host.lastError();
                            LOGGER.warn("Duel host of field {} exited before the duel began: {}", this.fieldId, error);
                            this.abandon(field, "开局失败：" + (error.isEmpty() ? "决斗核心没有启动" : error));
                            return;
                        }
                    }

                    boolean opening = this.scene != null && this.scene.openingBusy();
                    boolean reveal = !opening && (this.scene == null || !this.scene.busy());
                    field.setOpening(opening);
                    if (this.host != null && reveal) {
                        field.setPhasePrompt(this.host.promptPlayer(), this.host.promptToBattle(), this.host.promptToEnd(), this.host.promptToMain2());
                    } else {
                        field.setPhasePrompt(-1, false, false, false);
                    }

                    int[] hints = this.host != null && !opening ? this.host.placeAddresses() : new int[0];
                    if (reveal && !Arrays.equals(hints, this.lastPlaceHints)) {
                        this.lastPlaceHints = hints;
                        ServerPlayer hintPlayer = this.playerForSeat();
                        if (hintPlayer != null) {
                            ModNetwork.sendTo(hintPlayer, new ModNetwork.PlaceHint(field.getId(), hints));
                            if (hints.length > 0) {
                                StringBuilder where = new StringBuilder();

                                for (int i = 0; i < hints.length; i++) {
                                    if (i > 0) {
                                        where.append("、");
                                    }

                                    where.append(DuelLocation.label(hints[i], this.seatOf(hintPlayer)));
                                }

                                hintPlayer.displayClientMessage(Component.literal("请左键点绿色的格子来选定落点：" + where), false);
                            }
                        }
                    }

                    if (this.host != null && reveal && this.host.unselectPrompt()) {
                        String signature = this.host.promptSeq()
                            + ":u:"
                            + Arrays.toString(this.host.unselectSelectCodes())
                            + ":"
                            + Arrays.toString(this.host.unselectChosenCodes())
                            + ":"
                            + this.host.hintSelectText();
                        ServerPlayer chooser = this.playerForSeat();
                        if (chooser != null && !signature.equals(this.lastUnselectSignature)) {
                            this.lastUnselectSignature = signature;
                            ModNetwork.sendTo(
                                chooser,
                                new ModNetwork.UnselectList(
                                    this.host.unselectSelectCodes(),
                                    this.host.unselectChosenCodes(),
                                    this.host.selectMin(),
                                    this.host.selectMax(),
                                    this.host.unselectFinishable(),
                                    this.host.unselectSelectAddresses(),
                                    this.host.unselectChosenAddresses(),
                                    this.seatOf(chooser),
                                    this.host.hintSelectText()
                                )
                            );
                            int[] selectable = this.resolveCardEntities(level, field, this.host.unselectSelectAddresses());
                            int[] selected = this.resolveCardEntities(level, field, this.host.unselectChosenAddresses());
                            ModNetwork.sendTo(chooser, new ModNetwork.SelectHint(selectable, selected));
                        }
                    } else if (this.host == null || !this.host.unselectPrompt()) {
                        this.lastUnselectSignature = "";
                    }

                    if (this.host != null && reveal && this.host.positionPrompt()) {
                        int[] values = this.host.positionValues();
                        String signature = this.host.promptSeq() + ":p:" + Arrays.toString(values);
                        ServerPlayer chooser = this.playerForSeat();
                        if (chooser != null && !signature.equals(this.lastPositionSignature)) {
                            this.lastPositionSignature = signature;
                            String[] labels = new String[values.length];

                            for (int i = 0; i < values.length; i++) {
                                labels[i] = switch (values[i]) {
                                    case 1 -> "攻击表示";
                                    case 2 -> "里侧攻击";
                                    default -> "表示 " + values[i];
                                    case 4 -> "守备表示";
                                    case 8 -> "里侧守备";
                                };
                            }

                            ModNetwork.sendTo(chooser, new ModNetwork.PositionList(values, labels));
                        }
                    } else if (this.host == null || !this.host.positionPrompt()) {
                        this.lastPositionSignature = "";
                    }

                    if (this.host != null && reveal && this.host.sortPrompt()) {
                        int[] codes = this.host.sortCodes();
                        String signature = this.host.promptSeq() + ":s:" + Arrays.toString(codes);
                        ServerPlayer sorter = this.playerForSeat();
                        if (sorter != null && !signature.equals(this.lastSortSignature)) {
                            this.lastSortSignature = signature;
                            ModNetwork.sendTo(sorter, new ModNetwork.SortList(codes));
                        }
                    } else if (this.host == null || !this.host.sortPrompt()) {
                        this.lastSortSignature = "";
                    }

                    if (this.host != null && reveal && this.host.optionPrompt()) {
                        int[] values = this.host.optionValues();
                        String signature = this.host.promptSeq() + ":o:" + Arrays.toString(values);
                        ServerPlayer chooser = this.playerForSeat();
                        if (chooser != null && !signature.equals(this.lastOptionSignature)) {
                            this.lastOptionSignature = signature;
                            ModNetwork.sendTo(chooser, new ModNetwork.OptionList(values, this.host.optionLabels()));
                        }
                    } else if (this.host == null || !this.host.optionPrompt()) {
                        this.lastOptionSignature = "";
                    }

                    if (this.host != null && reveal) {
                        int announce = this.host.announceFunction();
                        if (announce < 0) {
                            this.lastAnnounceSignature = "";
                        } else {
                            int[] values = this.host.announceValues();
                            String signature = this.host.promptSeq() + ":a:" + announce + ":" + Arrays.toString(values);
                            ServerPlayer chooser = this.playerForSeat();
                            if (chooser != null && !signature.equals(this.lastAnnounceSignature)) {
                                this.lastAnnounceSignature = signature;
                                ModNetwork.sendTo(
                                    chooser, new ModNetwork.AnnounceList(this.host.announceKind(), this.host.announcePick(), values, this.host.announceHint())
                                );
                            }
                        }
                    }

                    if (this.host != null && reveal && this.host.hasSelectCardPrompt() && !this.host.unselectPrompt()) {
                        String signature = this.host.promptSeq() + ":" + this.host.selectSignature() + ":" + this.host.hintSelectText();
                        if (!signature.equals(this.lastSelectSignature)) {
                            this.lastSelectSignature = signature;
                            this.cardSelection.clear();
                            ServerPlayer chooser = this.playerForSeat();
                            if (chooser != null) {
                                ModNetwork.sendTo(
                                    chooser,
                                    new ModNetwork.SelectList(
                                        this.host.selectCodes(),
                                        this.host.selectMin(),
                                        this.host.selectMax(),
                                        this.host.selectAddrs(),
                                        this.seatOf(chooser),
                                        this.host.hintSelectText(),
                                        this.host.selectCancelable(),
                                        this.host.selectFieldFirst()
                                    )
                                );
                            }
                        }

                        int[] selectable = this.host.selectableAddresses();
                        int[] entities = this.resolveCardEntities(level, field, selectable);
                        int[] selected = this.resolveCardEntities(level, field, this.packedSelection());
                        if (!Arrays.equals(entities, this.lastSelectHints) || !Arrays.equals(selected, this.lastSelectHints)) {
                            this.lastSelectHints = entities;
                            ServerPlayer selectPlayer = this.playerForSeat();
                            if (selectPlayer != null) {
                                ModNetwork.sendTo(selectPlayer, new ModNetwork.SelectHint(entities, selected));
                            }
                        }
                    } else if (this.lastSelectHints.length > 0 && (this.host == null || !this.host.unselectPrompt())) {
                        this.lastSelectHints = new int[0];
                        this.lastSelectSignature = "";
                        this.cardSelection.clear();
                        ServerPlayer selectPlayer = this.playerForSeat();
                        if (selectPlayer != null) {
                            ModNetwork.sendTo(selectPlayer, new ModNetwork.SelectHint(new int[0], new int[0]));
                        }
                    }

                    int[] piles = this.host != null && !opening ? this.host.glowingPileZones() : new int[0];
                    if (reveal && !Arrays.equals(piles, this.lastPileHints)) {
                        this.lastPileHints = piles;
                        ServerPlayer pilePlayer = this.playerForSeat();
                        if (pilePlayer != null) {
                            ModNetwork.sendTo(pilePlayer, new ModNetwork.PileHint(field.getId(), piles));
                        }
                    }

                    if (this.host != null && reveal && this.host.chainVersion() != this.lastChainVersion) {
                        this.lastChainVersion = this.host.chainVersion();
                        ModNetwork.sendNear(
                            level, field.position(), 96.0, new ModNetwork.ChainState(field.getId(), this.host.chainCodes(), this.host.chainResolving())
                        );
                    }

                    int[] chainMarks = this.host == null ? new int[0] : this.resolveCardEntities(level, field, this.host.chainMarkAddresses());
                    int[] targetMarks = this.host == null ? new int[0] : this.resolveCardEntities(level, field, this.host.targetMarkAddresses());
                    int[] chainMarkAddrs = this.host == null ? new int[0] : this.host.chainMarkAddresses();
                    int[] targetMarkAddrs = this.host == null ? new int[0] : this.host.targetMarkAddresses();
                    boolean clearingMarks = chainMarks.length == 0 && targetMarks.length == 0;
                    if ((reveal || clearingMarks)
                        && (
                            !Arrays.equals(chainMarks, this.lastChainMarks)
                                || !Arrays.equals(targetMarks, this.lastTargetMarks)
                                || !Arrays.equals(chainMarkAddrs, this.lastChainMarkAddrs)
                                || !Arrays.equals(targetMarkAddrs, this.lastTargetMarkAddrs)
                        )) {
                        this.lastChainMarks = chainMarks;
                        this.lastTargetMarks = targetMarks;
                        this.lastChainMarkAddrs = chainMarkAddrs;
                        this.lastTargetMarkAddrs = targetMarkAddrs;
                        ModNetwork.sendNear(level, field.position(), 96.0, new ModNetwork.CardMarks(chainMarks, targetMarks, chainMarkAddrs, targetMarkAddrs));
                    }

                    if (this.host != null && reveal && this.host.chainChoicePrompt()) {
                        int[] codes = this.host.chainChoiceCodes();
                        boolean firstActivation = this.host.chainCodes().length == 0;
                        String signature = this.host.promptSeq() + ":" + Arrays.toString(codes) + ":" + this.host.chainForced() + ":" + firstActivation;
                        ServerPlayer chooser = this.playerForSeat();
                        if (chooser != null && !signature.equals(this.lastChainChoiceSignature)) {
                            this.lastChainChoiceSignature = signature;
                            String question = codes.length == 0
                                ? DuelTiming.passQuestion()
                                : (
                                    firstActivation
                                        ? DuelTiming.openQuestion(chainCandidateNames(this.host))
                                        : DuelTiming.chainQuestion(chainTriggerName(this.host))
                                );
                            String moment = this.host.chainTimingText();
                            if (moment.isEmpty()) {
                                moment = this.host.currentMomentText();
                            }

                            if (!moment.isEmpty()) {
                                question = "【" + moment + "】" + question;
                            }

                            ModNetwork.sendTo(
                                chooser,
                                new ModNetwork.ChainChoice(
                                    codes,
                                    this.host.chainForced(),
                                    question,
                                    firstActivation,
                                    this.host.chainChoiceAddrs(),
                                    this.seatOf(chooser),
                                    this.host.chainChoiceDescriptions()
                                )
                            );
                            if (this.host.chainChoiceHasUnknown()) {
                                chooser.displayClientMessage(Component.literal("连锁窗口有 " + codes.length + " 张可选（部分卡面没认出来，仍可按下标选择或放弃）"), false);
                            }
                        }
                    } else if (this.host == null || !this.host.chainChoicePrompt()) {
                        this.lastChainChoiceSignature = "";
                    }

                    if (this.host != null && reveal && this.host.sumPrompt()) {
                        ServerPlayer chooser = this.playerForSeat();
                        String signature = this.host.promptSeq()
                            + ":sum:"
                            + this.host.sumTarget()
                            + ":"
                            + Arrays.toString(this.host.sumMustCodes())
                            + ":"
                            + Arrays.toString(this.host.sumCodes());
                        if (!signature.equals(this.lastSumSignature)) {
                            if (this.host.sumMustMatches() && this.answerSumImplicit(this.host)) {
                                this.lastSumSignature = signature;
                            } else if (chooser != null) {
                                this.lastSumSignature = signature;
                                ModNetwork.sendTo(
                                    chooser,
                                    new ModNetwork.SumChoice(
                                        this.host.promptSeq(),
                                        this.host.sumOverflow(),
                                        this.host.sumTarget(),
                                        this.host.sumMin(),
                                        this.host.sumMax(),
                                        this.seatOf(chooser),
                                        this.host.sumMustCodes(),
                                        this.host.sumMustAddrs(),
                                        this.host.sumMustLevel1(),
                                        this.host.sumMustLevel2(),
                                        this.host.sumCodes(),
                                        this.host.sumAddrs(),
                                        this.host.sumLevel1(),
                                        this.host.sumLevel2()
                                    )
                                );
                            }
                        }
                    } else if (this.host == null || !this.host.sumPrompt()) {
                        this.lastSumSignature = "";
                    }

                    if (this.host != null && reveal) {
                        int function = this.host.decisionFunction();
                        ServerPlayer decider = this.playerForSeat();
                        String signature = function < 0 ? "" : this.host.promptSeq() + ":" + this.host.decisionSignature();
                        if (function < 0) {
                            this.lastDecisionSignature = "";
                        } else if (decider != null && !signature.equals(this.lastDecisionSignature)) {
                            this.lastDecisionSignature = signature;
                            int code = this.host.decisionCardCode();
                            String name = code > 0 ? CardRegistry.get(code).map(CardInfo::name).orElse("#" + code) : "这张卡";
                            int askedNow = Math.max(0, this.askedSeat());
                            int descriptionId = parseDescription(this.host.decisionDescription());
                            String rawDescription = descriptionId > 0 ? SystemStrings.get(descriptionId) : "";
                            String described = descriptionId > 0 ? SystemStrings.format(descriptionId, name, name) : "";
                            String question = function == 12
                                ? (
                                    !rawDescription.contains("%ls")
                                        ? DuelTiming.activateQuestion(this.host.turnPlayer() != askedNow, phaseName(this.host.phaseCode()), name)
                                        : described
                                )
                                : (described.isEmpty() ? "是否确认当前操作？" : described);
                            if (function == 12) {
                                String moment = this.host.chainTimingText();
                                if (moment.isEmpty()) {
                                    moment = this.host.currentMomentText();
                                }

                                if (!moment.isEmpty()) {
                                    question = "【" + moment + "】" + (!rawDescription.contains("%ls") ? "是否发动「" + name + "」的效果？" : described);
                                }
                            }

                            ModNetwork.sendTo(decider, new ModNetwork.DecisionPrompt(function, name, question));
                        }
                    }

                    if (this.host != null && reveal) {
                        int pending = this.host.promptFunction();
                        ServerPlayer who = this.playerForSeat();
                        String signature = pending < 0 ? "" : pending + ":" + this.host.promptSummary();
                        if (pending < 0 || isRenderedPrompt(pending)) {
                            this.lastUnknownPromptSignature = "";
                        } else if (who != null && !signature.equals(this.lastUnknownPromptSignature)) {
                            this.lastUnknownPromptSignature = signature;
                            who.displayClientMessage(Component.literal("核心在等一个还没有界面的窗口（函数 " + pending + "）：" + this.host.promptSummary() + " —— 现在会一直等在这里"), false);
                        }
                    }

                    if (this.previewHand != null && !running) {
                        this.reconcilePreview(level, field);
                    }
                }
            }

            this.pushHand();
        }
    }

    private static String chainTriggerName(DuelHostProcess host) {
        if (host == null) {
            return "";
        }

        int[] codes = host.chainCodes();
        if (codes.length == 0) {
            return "";
        }

        int code = codes[codes.length - 1] & 2147483647;
        return code <= 0 ? "" : CardRegistry.get(code).map(CardInfo::name).orElse("");
    }

    private static String[] chainCandidateNames(DuelHostProcess host) {
        int[] codes = host == null ? new int[0] : host.chainChoiceCodes();
        String[] names = new String[codes.length];

        for (int i = 0; i < codes.length; i++) {
            int code = codes[i] & 2147483647;
            names[i] = code <= 0 ? "" : CardRegistry.get(code).map(CardInfo::name).orElse("");
        }

        return names;
    }

    private static String phaseName(int code) {
        return switch (code & 65535) {
            case 1 -> "DP";
            case 2 -> "SP";
            case 4 -> "M1";
            case 8, 16, 32, 64, 128 -> "BP";
            case 256 -> "M2";
            case 512 -> "EP";
            default -> "DP";
        };
    }

    private static int parseDescription(String text) {
        if (text != null && !text.isBlank()) {
            try {
                return Integer.parseInt(text.trim());
            } catch (NumberFormatException ignored) {
                return 0;
            }
        } else {
            return 0;
        }
    }

    synchronized void handleFieldClick(ServerPlayer player, String controlId) {
        if (player != null && controlId != null && !controlId.isBlank()) {
            DuelFieldEntity field = this.linkedField(player.server);
            if (field != null) {
                DuelFieldLayout.Control control = DuelFieldLayout.controlById(controlId).orElse(null);
                if (control != null) {
                    if (!field.ownsControl(player, control)) {
                        player.displayClientMessage(Component.literal("这是对面那半场的按键"), true);
                    } else {
                        field.activateControl(player, control);
                    }
                }
            }
        }
    }

    synchronized void handleFieldAction(ServerPlayer player, int action, int value) {
        if (player != null) {
            if (action == 1) {
                if (this.isParticipant(player)) {
                    DuelTimingMode mode = DuelTimingMode.byOrdinal(value);
                    DuelFieldEntity field = this.linkedField(player.server);
                    if (field != null) {
                        field.setTimingMode(mode);
                    }

                    if (this.host != null && this.host.isRunning()) {
                        try {
                            this.host.setTiming(mode.hostKey());
                        } catch (Exception ex) {
                            LOGGER.warn("切换时点失败: {}", ex.getMessage());
                        }
                    }

                    player.displayClientMessage(Component.literal("时点：" + mode.label() + "（" + mode.detail() + "）"), true);
                }
            } else if (action == 0) {
                if (this.isParticipant(player)) {
                    DuelFieldEntity field = this.linkedField(player.server);
                    if (this.host != null && this.host.isRunning() && this.host.duelActive() && field != null && !field.hasResult()) {
                        try {
                            this.host.surrender(this.seatOf(player));
                            player.displayClientMessage(Component.literal("已认输，这一局判给对方"), false);
                        } catch (Exception ex) {
                            player.displayClientMessage(Component.literal("投降失败：" + ex.getMessage()), false);
                        }
                    } else {
                        player.displayClientMessage(Component.literal("现在没有进行中的决斗"), true);
                    }
                }
            }
        }
    }

    private DuelFieldEntity linkedField(MinecraftServer server) {
        if (server != null && this.fieldId != null && this.fieldDimension != null) {
            ServerLevel level = server.getLevel(this.fieldDimension);
            if (level == null) {
                return null;
            } else {
                return level.getEntity(this.fieldId) instanceof DuelFieldEntity field ? field : null;
            }
        } else {
            return null;
        }
    }

    synchronized boolean hasLiveDuel(DuelFieldEntity field) {
        return field != null && this.host != null && this.host.isRunning() && this.fieldId != null && this.fieldId.equals(field.getUUID());
    }

    synchronized void handleFieldClick(ServerPlayer player, DuelFieldEntity field, DuelFieldLayout.Control control) {
        if (!field.ownsControl(player, control)) {
            player.displayClientMessage(Component.literal("这是对面那半场的按键"), true);
        } else {
            field.activateControl(player, control);
        }
    }

    synchronized boolean reclaimField(ServerPlayer player, DuelFieldEntity field) {
        if (player != null && field != null) {
            UUID id = player.getUUID();
            boolean seatsFree = this.ownerId == null && this.seat1Id == null;
            boolean mine = seatsFree
                || id.equals(this.ownerId)
                || field.placer() != null && field.placer().equals(id)
                || player.hasPermissions(2);
            if (!mine) {
                player.displayClientMessage(Component.literal("这个决斗盘绑在别人身上，让本人（或放置它的人、管理员）来收"), true);
                return false;
            }

            if (this.isStarting() || this.duelInProgress(field)) {
                player.displayClientMessage(Component.literal("正在决斗，先投降或打完这一局再收（右下角菜单里有投降）"), true);
                return false;
            }

            this.stop();
            this.fieldDecks.remove(field.getUUID());
            if (field.getUUID().equals(this.fieldId)) {
                this.fieldId = null;
                this.fieldDimension = null;
                this.ownerId = null;
                this.seat1Id = null;
                this.pendingDeck = null;
            }

            this.purgeCards(field);
            field.discard();
            MDProMod.duels().forget(field.getUUID(), this);
            ItemStack stack = new ItemStack((ItemLike)ModItems.DUEL_FIELD_ITEM.get());
            if (!player.getInventory().add(stack)) {
                player.drop(stack, false);
            }

            player.displayClientMessage(Component.literal("已收回决斗盘（卡盒和里面的卡组都还在你身上）"), false);
            return true;
        } else {
            return false;
        }
    }

    synchronized void onPlayerLeft(ServerPlayer player) {
        if (player != null) {
            UUID id = player.getUUID();
            boolean isOwner = this.ownerId != null && this.ownerId.equals(id);
            boolean isSecond = this.seat1Id != null && this.seat1Id.equals(id);
            if (isOwner || isSecond) {
                if (this.startStage != DuelSessionManager.StartStage.IDLE) {
                    this.startStage = DuelSessionManager.StartStage.IDLE;
                    this.broadcast("对手离开了游戏，开局取消");
                }

                int seat = isSecond ? 1 : 0;
                String side = isSecond ? "p2" : "p1";
                DuelFieldEntity field = this.linkedField(player.server);
                boolean active = this.host != null && this.host.isRunning() && this.host.duelActive() && !this.host.isEnded();
                if (active) {
                    try {
                        this.host.surrender(seat);
                        LOGGER.info("Player {} left mid-duel: seat {} forfeits", player.getScoreboardName(), seat);
                    } catch (Exception ex) {
                        LOGGER.warn("退出判负失败（{}）：{}", player.getScoreboardName(), ex.getMessage());
                    }

                    ServerPlayer opponent = this.playerForSeat(1 - seat);
                    if (opponent != null) {
                        opponent.displayClientMessage(Component.literal("对手退出了游戏，这一局判你胜"), false);
                    }
                }

                if (field != null) {
                    this.releaseSide(field, side);
                } else if (isSecond) {
                    this.seat1Id = null;
                    this.extraTracker2.stop();
                } else {
                    this.ownerId = null;
                    this.pendingDeck = null;
                    this.extraBase = List.of();
                    this.extraTracker.stop();
                }

                LOGGER.info("Player {} unbound from duel field (side {})", player.getScoreboardName(), side);
                if (this.ownerId == null && this.seat1Id == null) {
                    // Nobody is left to finish or acknowledge this duel. The board is saved with the world, the duel
                    // host is not: a board left "in a duel" could not be used, reset or picked up after coming back.
                    if (field != null && (field.isGameStarted() || field.hasResult())) {
                        this.abandon(field, null);
                    } else {
                        this.stop();
                    }
                }
            }
        }
    }

    private static boolean isRenderedPrompt(int function) {
        return switch (function) {
            case 10, 11, 12, 13, 14, 15, 16, 18, 19, 20, 23, 24, 25, 26, 140, 141, 142, 143 -> true;
            default -> false;
        };
    }

    private void reconcilePreview(ServerLevel level, DuelFieldEntity field) {
        Map<Integer, HandCardEntity> existing = new HashMap<>();

        for (HandCardEntity card : level.getEntitiesOfClass(HandCardEntity.class, field.getBoundingBox().inflate(28.0))) {
            if (card.belongsTo(field.getUUID(), 0)) {
                existing.put(card.getIndex(), card);
            }
        }

        for (int i = 0; i < this.previewHand.length; i++) {
            Vec3 at = DuelAnimationScene.handPoint(field, 0, i, this.previewHand.length);
            HandCardEntity card = existing.remove(i);
            if (card == null) {
                card = (HandCardEntity)((EntityType)ModEntities.HAND_CARD.get()).create(level);
                if (card == null) {
                    continue;
                }

                card.configure(field.getUUID(), this.ownerIdText(), 0, i);
                card.moveTo(at.x, at.y, at.z, field.getYRot(), 0.0F);
                level.addFreshEntity(card);
            } else {
                card.moveTo(at.x, at.y, at.z, field.getYRot(), 0.0F);
            }

            card.setPrivateCode(this.previewHand[i]);
            card.setCardCode(0);
            ServerPlayer owner = this.playerForSeat(0);
            if (owner != null) {
                ModNetwork.sendTo(owner, new ModNetwork.CardKnowledge(card.getId(), this.previewHand[i]));
            }
        }

        for (HandCardEntity unused : existing.values()) {
            unused.discard();
        }
    }

    private void clearVisuals() {
        if (this.server != null && this.fieldDimension != null && this.fieldId != null) {
            ServerLevel level = this.server.getLevel(this.fieldDimension);
            if (level != null && level.getEntity(this.fieldId) instanceof DuelFieldEntity field) {
                if (this.scene != null) {
                    this.scene.clear(level, field);
                }

                for (HandCardEntity hand : level.getEntitiesOfClass(HandCardEntity.class, field.getBoundingBox().inflate(28.0))) {
                    if (hand.fieldId().equals(this.fieldId.toString())) {
                        hand.discard();
                    }
                }

                for (DuelCardEntity card : level.getEntitiesOfClass(DuelCardEntity.class, field.getBoundingBox().inflate(28.0))) {
                    if (card.belongsTo(this.fieldId)) {
                        card.discard();
                    }
                }
            }
        }

        this.scene = null;
    }

    private String ownerIdText() {
        return this.ownerId == null ? "" : this.ownerId.toString();
    }

    synchronized void previewHand(ServerPlayer player, int[] cards) {
        this.server = player.server;
        this.ownerId = player.getUUID();
        this.previewHand = cards;
    }

    synchronized void clearPreviewHand() {
        this.previewHand = null;
        if (this.host == null) {
            this.clearVisuals();
        }
    }

    synchronized void animationDemo(ServerPlayer player, DuelFieldEntity field) {
        if (this.isRunning()) {
            throw new IllegalStateException("请先结束当前对局，再播放动画演示");
        }

        this.stop();
        this.linkField(player.server, field, player);
        this.scene = new DuelAnimationScene();
        this.scene.initialize(40, 15, 40, 15);
        DeckData demoDeck = DeckData.of("动画演示", new int[40], new int[15], new int[0]);
        if (!field.hasDeck("p1")) {
            field.bindDeck("p1", demoDeck);
        }

        if (!field.hasDeck("p2")) {
            field.bindDeck("p2", demoDeck);
        }

        this.scene.enqueue(DuelAnimationDemo.events());
    }

    synchronized void setPendingDeck(DeckData deck) {
        this.pendingDeck = deck;
        if (this.fieldId != null && deck != null) {
            this.fieldDecks.computeIfAbsent(this.fieldId, ignored -> new HashMap<>()).put("p1", deck);
        }
    }

    synchronized void setPendingDeck(DuelFieldEntity field, String side, DeckData deck) {
        if (field != null && deck != null) {
            this.fieldDecks.computeIfAbsent(field.getUUID(), ignored -> new HashMap<>()).put(side == null ? "p1" : side, deck);
            this.pendingDeck = deck;
            if (!"p2".equals(side)) {
                this.extraBase = List.copyOf(deck.extraDeck());
                this.extraTracker.reset(this.extraBase);
            }
        }
    }

    synchronized DeckData pendingDeck() {
        return this.pendingDeck;
    }

    private void pushHand() {
        this.pushHandForSeat(0);
        this.pushHandForSeat(1);
    }

    synchronized void sendHandSnapshot(ServerPlayer player, int[] cards, boolean active, int turn) {
        this.server = player.server;
        this.ownerId = player.getUUID();
        this.lastHand[0] = cards;
        this.lastTurn[0] = turn;
        this.handActive[0] = active;
        ModNetwork.sendTo(player, new ModNetwork.HandSync(cards, active, turn == 0));
    }

    synchronized void stop() {
        if (this.host != null) {
            this.host.close();
        }

        this.host = null;
        this.previewHand = null;
        this.clearVisuals();
        this.pushHand();
        this.deleteExportedDecks();
    }

    private void deleteExportedDecks() {
        Path dir = this.exportDir;
        this.exportDir = null;
        if (dir != null) {
            try (java.util.stream.Stream<Path> files = Files.list(dir)) {
                for (Path file : files.toList()) {
                    Files.deleteIfExists(file);
                }

                Files.deleteIfExists(dir);
            } catch (Exception ex) {
                LOGGER.debug("Could not remove duel decks in {}: {}", dir, ex.toString());
            }
        }
    }

    // ------------------------------------------------------------------ several duels at once / stale boards

    /** Points this session at a duel field without touching seats or a running duel. */
    synchronized void attach(DuelFieldEntity field) {
        if (field != null && field.level() instanceof ServerLevel level) {
            if (this.fieldId == null || this.fieldId.equals(field.getUUID())) {
                this.server = level.getServer();
                this.fieldDimension = level.dimension();
                this.fieldId = field.getUUID();
            }
        }
    }

    synchronized boolean isLinkedTo(UUID field) {
        return field != null && field.equals(this.fieldId);
    }

    /** True when the player sits at this session's board. */
    synchronized boolean involves(ServerPlayer player) {
        return this.isParticipant(player) || player != null && this.isStarting() && this.startSeatOf(player) >= 0;
    }

    /** The duel field entity of this session, when it is loaded. */
    synchronized DuelFieldEntity field(MinecraftServer server) {
        return this.linkedField(server);
    }

    /** True when the player sits here and the duel engine is waiting for that player. */
    synchronized boolean isAskingPlayer(ServerPlayer player) {
        return this.host != null && this.host.isRunning() && this.isParticipant(player) && this.isAsking(player);
    }

    /** A duel that is being started or played; such a session must not be taken over or thrown away. */
    synchronized boolean isBusy() {
        return this.startStage != DuelSessionManager.StartStage.IDLE
            || this.host != null && this.host.isRunning() && this.host.duelActive() && !this.host.isEnded();
    }

    private boolean duelInProgress(DuelFieldEntity field) {
        return field.isGameStarted()
            && !field.hasResult()
            && this.host != null
            && this.host.isRunning()
            && field.getUUID().equals(this.fieldId);
    }

    /** Nothing in here is worth keeping: no duel, no seats, no decks. */
    synchronized boolean isDisposable() {
        return this.host == null
            && this.scene == null
            && this.ownerId == null
            && this.seat1Id == null
            && this.previewHand == null
            && this.startStage == DuelSessionManager.StartStage.IDLE
            && this.fieldDecks.values().stream().allMatch(Map::isEmpty);
    }

    /** Takes the player off this board (used when the player sits down at another one). */
    synchronized void vacate(ServerPlayer player) {
        if (player == null || this.isBusy()) {
            return;
        }

        UUID id = player.getUUID();
        DuelFieldEntity field = this.linkedField(player.server);
        for (String side : new String[]{"p1", "p2"}) {
            boolean p2 = "p2".equals(side);
            if (id.equals(p2 ? this.seat1Id : this.ownerId)) {
                if (field != null) {
                    this.releaseSide(field, side);
                } else if (p2) {
                    this.seat1Id = null;
                    this.extraTracker2.stop();
                } else {
                    this.ownerId = null;
                    this.pendingDeck = null;
                    this.extraBase = List.of();
                    this.extraTracker.stop();
                }

                LOGGER.info("Player {} moved to another duel field; side {} of {} released", new Object[]{player.getScoreboardName(), side, this.fieldId});
            }
        }
    }

    /**
     * Repairs a board whose saved state no longer matches any duel.
     *
     * <p>A duel field is saved with the world ("in a duel", decks lying on both halves), the duel itself is not: the
     * duel host is a process of the running server and the decks are kept in memory. After leaving a single-player
     * world or restarting a server, the board still said "in a duel", and every way out asked for a running duel:
     * the buttons did nothing, surrendering answered "no duel in progress" and picking the board up answered
     * "duel in progress".
     */
    synchronized void reconcile(DuelFieldEntity field) {
        if (field == null || this.host != null || this.startStage != DuelSessionManager.StartStage.IDLE) {
            return;
        }

        if (field.isGameStarted() || field.hasResult()) {
            LOGGER.info("Duel field {} was left in a duel that no longer exists; back to preparation", field.getUUID());
            this.abandon(field, null);
        }

        if (this.scene != null) {
            return;
        }

        for (String side : new String[]{"p1", "p2"}) {
            int seat = "p2".equals(side) ? 1 : 0;
            if (field.hasDeck(side) && field.aiSide() != seat && this.boundDeck(field, side) == null) {
                LOGGER.info("Duel field {}: the deck on side {} is not known any more; side cleared", field.getUUID(), side);
                this.releaseSide(field, side);
            }
        }
    }

    /** Ends whatever is going on at this board and puts it back into preparation, decks still in place. */
    private void abandon(DuelFieldEntity field, String notice) {
        this.startStage = DuelSessionManager.StartStage.IDLE;
        if (notice != null) {
            this.broadcast(notice);
        }

        this.stop();
        if (field != null) {
            this.purgeCards(field);
            this.resetFieldState(field);
        }
    }

    /** Called when the server stops: no board may be saved "in a duel". */
    synchronized void shutdown() {
        DuelFieldEntity field = this.server == null ? null : this.linkedField(this.server);
        if (field != null && (field.isGameStarted() || field.hasResult() || this.startStage != DuelSessionManager.StartStage.IDLE)) {
            this.abandon(field, null);
        } else {
            this.startStage = DuelSessionManager.StartStage.IDLE;
            this.stop();
        }

        this.server = null;
        this.fieldId = null;
        this.fieldDimension = null;
        this.ownerId = null;
        this.seat1Id = null;
        this.pendingDeck = null;
        this.pendingActivation = null;
        this.fieldDecks.clear();
        this.extraBase = List.of();
        this.extraTracker.stop();
        this.extraTracker2.stop();
    }

    /** The field entity was removed from the world (picked up, /kill): its duel goes with it. */
    synchronized void fieldRemoved(DuelFieldEntity field) {
        this.startStage = DuelSessionManager.StartStage.IDLE;
        this.stop();
        this.purgeCards(field);
        this.fieldDecks.remove(field.getUUID());
        if (field.getUUID().equals(this.fieldId)) {
            this.fieldId = null;
            this.fieldDimension = null;
            this.ownerId = null;
            this.seat1Id = null;
            this.pendingDeck = null;
        }
    }

    private void purgeCards(DuelFieldEntity field) {
        if (field.level() instanceof ServerLevel level) {
            UUID id = field.getUUID();

            for (HandCardEntity hand : level.getEntitiesOfClass(HandCardEntity.class, field.getBoundingBox().inflate(28.0))) {
                if (id.toString().equals(hand.fieldId())) {
                    hand.discard();
                }
            }

            for (DuelCardEntity card : level.getEntitiesOfClass(DuelCardEntity.class, field.getBoundingBox().inflate(28.0))) {
                if (card.belongsTo(id)) {
                    card.discard();
                }
            }
        }
    }

    private void resetFieldState(DuelFieldEntity field) {
        field.setGameStarted(false);
        field.setDuelResult(0);
        field.setOpening(false);
        field.setPhasePrompt(-1, false, false, false);
        field.setLifePoints("p1", 8000);
        field.setLifePoints("p2", 8000);
        field.setTurnSide(0);
        field.setCurrentPhase("p1", "DP");
        field.setCurrentPhase("p2", "DP");
        int[] main = new int[2];
        int[] extra = new int[2];

        for (int seat = 0; seat < 2; seat++) {
            String side = seat == 1 ? "p2" : "p1";
            if (field.hasDeck(side)) {
                DeckData deck = this.boundDeck(field, side);
                if (deck == null && field.aiSide() == seat) {
                    try {
                        deck = AiDeckCatalog.deck(runtimeRoot(), field.aiKey());
                    } catch (Exception ignored) {
                    }
                }

                main[seat] = deck == null ? field.mainCount(side) : deck.mainDeck().size();
                extra[seat] = deck == null ? field.extraCount(side) : deck.extraDeck().size();
            }
        }

        field.updateVisualCounts(main, extra, new int[]{0, 0}, new int[]{0, 0});
    }

    synchronized void resetForNewDuel(ServerPlayer player, DuelFieldEntity field) {
        this.stop();
        if (field != null) {
            boolean humanIsP1 = this.ownerId != null || this.seat1Id == null;
            String keepSide = humanIsP1 ? "p1" : "p2";
            String dropSide = humanIsP1 ? "p2" : "p1";
            List<Integer> base = this.extraBase.isEmpty() ? extrasOf(this.boundDeck(field, keepSide)) : this.extraBase;
            ExtraDeckTracker keepTracker = humanIsP1 ? this.extraTracker : this.extraTracker2;
            if (!base.isEmpty()) {
                keepTracker.reset(base);
            }

            field.setGameStarted(false);
            field.setDuelResult(0);
            field.setOpening(false);
            field.setPhasePrompt(-1, false, false, false);
            field.setLifePoints("p1", 8000);
            field.setLifePoints("p2", 8000);
            field.setTurnSide(0);
            field.setCurrentPhase("p1", "DP");
            field.setCurrentPhase("p2", "DP");
            field.unbindDeck(dropSide);
            if (humanIsP1) {
                this.seat1Id = null;
            } else {
                this.ownerId = null;
            }

            int[] mainCounts = new int[]{0, 0};
            int[] extraCounts = new int[]{0, 0};
            mainCounts[humanIsP1 ? 0 : 1] = field.mainCount(keepSide);
            extraCounts[humanIsP1 ? 0 : 1] = field.extraCount(keepSide);
            field.updateVisualCounts(mainCounts, extraCounts, new int[]{0, 0}, new int[]{0, 0});
            if (player != null) {
                player.displayClientMessage(Component.literal("已回到准备：想换卡组就用卡盒右键决斗盘，点绿色按钮开始新一局"), false);
            }
        }
    }

    synchronized boolean isRunning() {
        return this.host != null && this.host.isRunning();
    }

    synchronized boolean isEnded() {
        return this.host != null && this.host.isEnded();
    }

    synchronized String statusSummary() {
        return this.host == null ? "no duel host" : this.host.statusSummary();
    }

    synchronized String statsSummary() {
        return this.host == null ? "no duel host" : this.host.statsSummary();
    }

    synchronized String promptSummary() {
        return this.host == null ? "no duel host" : this.host.promptSummary();
    }

    synchronized String promptResponseKind() {
        return this.host == null ? "" : this.host.promptResponseKind();
    }

    synchronized void respond(int value) throws Exception {
        if (this.host == null) {
            throw new IllegalStateException("No duel host");
        }

        this.host.respond(value);
    }

    synchronized void respondBytes(String hex) throws Exception {
        if (this.host == null) {
            throw new IllegalStateException("No duel host");
        }

        this.host.respondBytes(hex);
    }

    synchronized String placeOptions() {
        return this.host == null ? "no duel host" : this.host.placeOptions();
    }

    synchronized void respondPlace(int index) throws Exception {
        if (this.host == null) {
            throw new IllegalStateException("No duel host");
        }

        this.host.respondPlace(index);
    }

    synchronized void submitPhase(DuelFieldEntity field, ServerPlayer player, String phase) throws Exception {
        if (this.host == null || !this.host.isRunning()) {
            throw new IllegalStateException("当前没有进行中的决斗");
        }

        if (!this.isAsking(player)) {
            throw new IllegalStateException("现在不是你的操作");
        }

        int value = this.host.phaseResponse(phase);
        if (value < 0) {
            throw new IllegalStateException("现在还不能切换到该阶段");
        }

        this.host.respond(value);
        LOGGER.info(
            "Phase click: field={} player={} phase={} response={}",
            new Object[]{field == null ? "-" : field.getUUID(), player == null ? "-" : player.getScoreboardName(), phase, value}
        );
    }

    synchronized boolean hasPlacePrompt() {
        return this.host != null && this.host.isRunning() && this.host.hasPlacePrompt();
    }

    synchronized boolean submitCardAction(ServerPlayer player, int controller, int location, int sequence, String... kinds) throws Exception {
        if (this.host == null || !this.host.isRunning()) {
            throw new IllegalStateException("当前没有进行中的决斗");
        }

        if (!this.isAsking(player)) {
            throw new IllegalStateException("现在不是你的操作");
        }

        if (Arrays.asList(kinds).contains("activate")) {
            List<DuelHostProcess.Activation> choices = this.host.activationChoices(controller, location, sequence);
            if (choices.size() > 1) {
                this.pendingActivation = new DuelSessionManager.PendingActivation(
                    this.host, player.getUUID(), this.host.promptSeq(), controller, location, sequence
                );
                this.sendActivationChoices(player, this.pendingActivation);
                return true;
            }
        }

        int value = this.host.promptAction(controller, location, sequence, kinds);
        if (value < 0) {
            return false;
        }

        this.host.respond(value);
        LOGGER.info(
            "Card action: player={} card=({},{},{}) kinds={} response={}",
            new Object[]{player == null ? "-" : player.getScoreboardName(), controller, location, sequence, String.join("/", kinds), value}
        );
        return true;
    }

    private void sendActivationChoices(ServerPlayer player, DuelSessionManager.PendingActivation pending) {
        List<DuelHostProcess.Activation> choices = this.host.activationChoices(pending.controller(), pending.location(), pending.sequence());
        if (!choices.isEmpty()) {
            ModNetwork.sendTo(
                player,
                new ModNetwork.EffectChoices(
                    pending.seq(),
                    choices.get(0).code(),
                    choices.stream().mapToInt(DuelHostProcess.Activation::response).toArray(),
                    choices.stream().map(DuelHostProcess.Activation::text).toArray(String[]::new)
                )
            );
        }
    }

    synchronized void answerEffect(ServerPlayer player, long seq, int value) {
        DuelSessionManager.PendingActivation pending = this.pendingActivation;
        if (this.host != null
            && this.host.isRunning()
            && pending != null
            && pending.host() == this.host
            && pending.player().equals(player.getUUID())
            && pending.seq() == seq
            && seq == this.host.promptSeq()
            && this.isAsking(player)) {
            if (value == -1) {
                this.pendingActivation = null;
            } else if (!this.host
                .activationChoices(pending.controller(), pending.location(), pending.sequence())
                .stream()
                .noneMatch(choice -> choice.response() == value)) {
                try {
                    this.host.respond(value);
                    this.pendingActivation = null;
                } catch (Exception ex) {
                    player.displayClientMessage(Component.literal("发动失败：" + ex.getMessage()), true);
                }
            }
        }
    }

    synchronized boolean submitPlaceFor(int controller, int location, int sequence) throws Exception {
        if (this.host != null && this.host.isRunning()) {
            int index = this.host.placeIndex(controller, location, sequence);
            if (index < 0) {
                return false;
            }

            this.host.respondPlace(index);
            LOGGER.info("Place choice: controller={} location={} sequence={} index={}", new Object[]{controller, location, sequence, index});
            return true;
        } else {
            return false;
        }
    }

    synchronized void offerCardActions(ServerPlayer player, int entityId, int[][] candidates) {
        if (this.host != null && this.host.isRunning() && candidates != null) {
            if (!this.refuseIfNotAsked(player)) {
                if (this.host.hasSelectCardPrompt()) {
                    if (candidates.length > 0 && candidates[0] != null && !this.toggleCardSelect(player, candidates[0][0], candidates[0][1], candidates[0][2])) {
                        player.displayClientMessage(Component.literal("这张卡不在可选的候选里"), true);
                    }
                } else {
                    int mask = 0;

                    for (int[] address : candidates) {
                        if (address != null) {
                            if (this.host.promptAction(address[0], address[1], address[2], "summon") >= 0) {
                                mask |= 1;
                            }

                            if (this.host.promptAction(address[0], address[1], address[2], "spsummon") >= 0) {
                                mask |= 32;
                            }

                            if (this.host.promptAction(address[0], address[1], address[2], "mset") >= 0
                                || this.host.promptAction(address[0], address[1], address[2], "sset") >= 0) {
                                mask |= 2;
                            }

                            if (this.host.promptAction(address[0], address[1], address[2], "activate") >= 0) {
                                mask |= 4;
                            }

                            if (this.host.promptAction(address[0], address[1], address[2], "attack") >= 0) {
                                mask |= 8;
                            }

                            if (this.host.promptAction(address[0], address[1], address[2], "position") >= 0) {
                                mask |= 16;
                            }
                        }
                    }

                    if (mask == 0) {
                        String reason = !this.host.isWaiting()
                            ? "现在不是你的决策点（对手回合或演出中）"
                            : (this.host.chainChoicePrompt() ? "有连锁窗口要先答完" : "核心当前没给出候选：" + this.host.promptSummary());
                        player.displayClientMessage(Component.literal("这张卡没有可做的动作 —— " + reason), false);
                    } else {
                        ModNetwork.sendTo(player, new ModNetwork.CardActions(entityId, mask));
                    }
                }
            }
        }
    }

    private int[] resolveCardEntities(ServerLevel level, DuelFieldEntity field, int[] addresses) {
        if (addresses != null && addresses.length != 0) {
            List<Integer> ids = new ArrayList<>();
            List<HandCardEntity> hands = level.getEntitiesOfClass(HandCardEntity.class, field.getBoundingBox().inflate(28.0));
            List<DuelCardEntity> cards = level.getEntitiesOfClass(DuelCardEntity.class, field.getBoundingBox().inflate(28.0));

            for (int packed : addresses) {
                int controller = packed >> 16 & 1;
                int location = packed >> 8 & 0xFF;
                int sequence = packed & 0xFF;
                if (location == 2) {
                    for (HandCardEntity hand : hands) {
                        if (hand.side() == controller && hand.getIndex() == sequence) {
                            ids.add(hand.getId());
                        }
                    }
                } else {
                    String zoneId = DuelFieldLayout.zoneIdFor(controller, location, sequence);
                    if (zoneId != null) {
                        for (DuelCardEntity card : cards) {
                            if (zoneId.equals(card.getZoneId())) {
                                ids.add(card.getId());
                            }
                        }
                    }
                }
            }

            return ids.stream().mapToInt(Integer::intValue).toArray();
        } else {
            return new int[0];
        }
    }

    private int[] packedSelection() {
        return this.cardSelection.stream().mapToInt(Integer::intValue).toArray();
    }

    synchronized void openPile(ServerPlayer player, DuelFieldEntity field, int side, int location) {
        if (this.host != null && this.host.isRunning()) {
            int viewerSide = this.viewerSeat(player);
            boolean canAct = side == viewerSide && viewerSide == this.host.promptPlayer();
            int[][] activations = canAct && location != 64 ? this.host.pileActions(location, "activate") : new int[0][];
            int[][] summons = canAct ? this.host.pileActions(location, "spsummon") : new int[0][];
            int[] known = this.allCodes(side, location, viewerSide);
            int hidden = 0;
            if (side != viewerSide) {
                hidden = Math.max(0, this.host.count(side, location) - known.length);
            }

            ModNetwork.sendTo(
                player,
                new ModNetwork.PileView(
                    location,
                    Arrays.stream(activations).mapToInt(action -> action[0]).toArray(),
                    Arrays.stream(activations).mapToInt(action -> action[1]).toArray(),
                    Arrays.stream(summons).mapToInt(action -> action[0]).toArray(),
                    Arrays.stream(summons).mapToInt(action -> action[1]).toArray(),
                    known,
                    hidden,
                    field.getUUID()
                )
            );
        }
    }

    private int viewerSeat(ServerPlayer player) {
        return player == null ? -1 : DuelStart.viewerSeat(player.getUUID(), this.ownerId, this.seat1Id);
    }

    private void trackExtraDeck(List<DuelVisualState.Event> events) {
        for (DuelVisualState.Event event : events) {
            DuelVisualState.Address from = event.from();
            DuelVisualState.Address to = event.to();
            if (from != null && to != null && event.card() != null) {
                this.extraTracker.move(from.controller(), from.location(), to.controller(), to.location(), event.card().code());
                this.extraTracker2.move(from.controller(), from.location(), to.controller(), to.location(), event.card().code());
            }
        }
    }

    private static List<Integer> extrasOf(DeckData deck) {
        return deck == null ? List.of() : List.copyOf(deck.extraDeck());
    }

    private int[] allCodes(int side, int location, int viewerSide) {
        boolean mine = side == viewerSide;
        ExtraDeckTracker tracker = side == 1 ? this.extraTracker2 : this.extraTracker;
        if (mine && location == 64 && tracker.tracking()) {
            List<Integer> live = tracker.codes();
            if (this.host != null && this.host.isRunning()) {
                int engineCount = this.host.count(side, 64);
                if (live.size() != engineCount) {
                    LOGGER.warn("座位 {} 的额外卡组记账与核心不一致：界面 {} 张 / 核心 {} 张（界面序号可能错位）", new Object[]{side, live.size(), engineCount});
                }
            }

            return live.stream().mapToInt(Integer::intValue).toArray();
        } else if (!mine) {
            return this.scene == null ? new int[0] : this.scene.publicCodesIn(side, location);
        } else if (location == 64) {
            DeckData deck = this.boundDeckForSide(side);
            return deck == null ? new int[0] : deck.extraDeck().stream().mapToInt(Integer::intValue).toArray();
        } else {
            return this.scene == null ? new int[0] : this.scene.codesIn(side, location);
        }
    }

    private DeckData boundDeckForSide(int side) {
        if (side == 0) {
            DeckData deck = this.fieldId == null ? null : this.fieldDecks.getOrDefault(this.fieldId, Map.of()).getOrDefault("p1", null);
            return deck == null ? this.pendingDeck : deck;
        } else {
            return this.fieldId == null ? null : this.fieldDecks.getOrDefault(this.fieldId, Map.of()).getOrDefault("p2", null);
        }
    }

    synchronized void answerSelect(ServerPlayer player, int[] indices) {
        if (this.host != null && this.host.isRunning() && this.host.hasSelectCardPrompt()) {
            if (!this.refuseIfNotAsked(player)) {
                int count = this.host.selectCodes().length;
                if (indices != null && indices.length == 1 && indices[0] == -1) {
                    if (this.host.selectCancelable()) {
                        try {
                            this.host.respond(-1);
                            this.lastSelectSignature = "";
                            this.cardSelection.clear();
                        } catch (Exception ex) {
                            player.displayClientMessage(Component.literal("取消选择失败：" + ex.getMessage()), true);
                        }
                    }
                } else if (indices != null
                    && (this.host.promptFunction() == 20 || indices.length >= this.host.selectMin())
                    && indices.length <= this.host.selectMax()) {
                    Set<Integer> unique = new HashSet<>();

                    for (int index : indices) {
                        if (index < 0 || index >= count || !unique.add(index)) {
                            return;
                        }
                    }

                    StringBuilder hex = new StringBuilder();
                    int used = 0;
                    StringBuilder body = new StringBuilder();

                    for (int index : indices) {
                        if (index >= 0 && index < count) {
                            body.append(String.format("%02X", index));
                            used++;
                        }
                    }

                    if (used != 0 || this.host.selectMin() <= 0) {
                        hex.append(String.format("%02X", used)).append(body);

                        try {
                            this.host.respondBytes(hex.toString());
                            LOGGER.info("Select list answered: {} -> {}", player == null ? "-" : player.getScoreboardName(), hex);
                            this.lastSelectSignature = "";
                            this.cardSelection.clear();
                        } catch (Exception ex) {
                            player.displayClientMessage(Component.literal("选卡失败：" + ex.getMessage()), true);
                        }
                    }
                }
            }
        }
    }

    private boolean answerSumImplicit(DuelHostProcess host) {
        int must = host.sumMustCount();
        if (must <= 0) {
            return false;
        }

        StringBuilder hex = new StringBuilder();
        hex.append(String.format("%02X", must));

        for (int i = 0; i < must; i++) {
            hex.append(String.format("%02X", i));
        }

        try {
            host.respondBytes(hex.toString());
            LOGGER.info("Sum auto-answered: {} -> {}", host.promptSummary(), hex);
            return true;
        } catch (Exception ex) {
            LOGGER.warn("Sum auto-answer failed: {}", ex.toString());
            return false;
        }
    }

    synchronized void answerSum(ServerPlayer player, long seq, int[] chosen) {
        if (this.host != null && this.host.isRunning() && this.host.sumPrompt()) {
            if (this.host.promptSeq() == seq) {
                if (!this.refuseIfNotAsked(player)) {
                    int selectable = this.host.sumCodes().length;
                    int must = this.host.sumMustCount();
                    int[] picked = chosen == null ? new int[0] : chosen;
                    if (picked.length >= this.host.sumMin() && picked.length <= this.host.sumMax()) {
                        Set<Integer> unique = new HashSet<>();

                        for (int index : picked) {
                            if (index < 0 || index >= selectable || !unique.add(index)) {
                                return;
                            }
                        }

                        StringBuilder hex = new StringBuilder();
                        hex.append(String.format("%02X", must + picked.length));

                        for (int i = 0; i < must; i++) {
                            hex.append(String.format("%02X", i));
                        }

                        for (int index : picked) {
                            hex.append(String.format("%02X", index));
                        }

                        try {
                            this.host.respondBytes(hex.toString());
                            this.lastSumSignature = "";
                            LOGGER.info("Sum answered: {} -> {}", player == null ? "-" : player.getScoreboardName(), hex);
                        } catch (Exception ex) {
                            player.displayClientMessage(Component.literal("选择素材失败：" + ex.getMessage()), true);
                        }
                    }
                }
            }
        }
    }

    synchronized void unselectPick(ServerPlayer player, int index) {
        if (this.host != null && this.host.isRunning() && this.host.unselectPrompt()) {
            if (!this.refuseIfNotAsked(player)) {
                try {
                    if (index < 0) {
                        this.host.respond(-1);
                    } else {
                        this.host.respondBytes(String.format("01%02X", index));
                    }

                    this.lastUnselectSignature = "";
                } catch (Exception ex) {
                    player.displayClientMessage(Component.literal("选素材失败：" + ex.getMessage()), true);
                }
            }
        }
    }

    synchronized void answerOption(ServerPlayer player, int value) {
        if (this.host != null && this.host.isRunning() && this.host.optionPrompt()) {
            if (!this.refuseIfNotAsked(player)) {
                try {
                    this.host.respond(value);
                    this.lastOptionSignature = "";
                    LOGGER.info("Option answered: {} -> {}", player == null ? "-" : player.getScoreboardName(), value);
                } catch (Exception ex) {
                    player.displayClientMessage(Component.literal("选项应答失败：" + ex.getMessage()), true);
                }
            }
        }
    }

    synchronized void answerAnnounce(ServerPlayer player, int value) {
        if (this.host != null && this.host.isRunning() && this.host.announceFunction() >= 0) {
            if (!this.refuseIfNotAsked(player)) {
                try {
                    this.host.respond(value);
                    this.lastAnnounceSignature = "";
                    LOGGER.info("Announce answered: {} -> {} ({})", new Object[]{player == null ? "-" : player.getScoreboardName(), value, this.host.announceKind()});
                } catch (Exception ex) {
                    player.displayClientMessage(Component.literal("宣言失败：" + ex.getMessage()), true);
                }
            }
        }
    }

    synchronized void answerSort(ServerPlayer player, int[] order) {
        if (this.host != null && this.host.isRunning() && this.host.sortPrompt()) {
            if (!this.refuseIfNotAsked(player)) {
                int total = this.host.sortCodes().length;
                if (order != null && order.length == total) {
                    StringBuilder hex = new StringBuilder();

                    for (int index : order) {
                        if (index < 0 || index >= total) {
                            return;
                        }

                        hex.append(String.format("%02X", index));
                    }

                    try {
                        this.host.respondBytes(hex.toString());
                        this.lastSortSignature = "";
                        LOGGER.info("Sort answered: {}", hex);
                    } catch (Exception ex) {
                        player.displayClientMessage(Component.literal("排序失败：" + ex.getMessage()), true);
                    }
                }
            }
        }
    }

    synchronized void answerPosition(ServerPlayer player, int value) {
        if (this.host != null && this.host.isRunning() && this.host.positionPrompt()) {
            if (!this.refuseIfNotAsked(player)) {
                try {
                    this.host.respond(value);
                    this.lastPositionSignature = "";
                } catch (Exception ex) {
                    player.displayClientMessage(Component.literal("选表示失败：" + ex.getMessage()), true);
                }
            }
        }
    }

    synchronized void answerChain(ServerPlayer player, int index) {
        if (this.host != null && this.host.isRunning()) {
            if (!this.refuseIfNotAsked(player)) {
                try {
                    this.host.respond(index < 0 ? -1 : index);
                    this.lastChainChoiceSignature = "";
                    LOGGER.info("Chain answer: {} -> {}", player == null ? "-" : player.getScoreboardName(), index);
                } catch (Exception ex) {
                    player.displayClientMessage(Component.literal("连锁回答失败：" + ex.getMessage()), true);
                }
            }
        }
    }

    synchronized void answerDecision(ServerPlayer player, int value) {
        if (this.host != null && this.host.isRunning()) {
            if (this.host.decisionFunction() >= 0) {
                if (!this.refuseIfNotAsked(player)) {
                    try {
                        this.host.respond(value == 1 ? 1 : 0);
                        this.lastDecisionSignature = "";
                        LOGGER.info("Decision answered: {} -> {}", player == null ? "-" : player.getScoreboardName(), value);
                    } catch (Exception ex) {
                        player.displayClientMessage(Component.literal("回答失败：" + ex.getMessage()), true);
                    }
                }
            }
        }
    }

    synchronized void performPileAct(ServerPlayer player, int location, int sequence, boolean summon) {
        if (this.host != null && this.host.isRunning()) {
            if (!this.refuseIfNotAsked(player)) {
                if (location == 16 || location == 32 || location == 64) {
                    try {
                        String kind = summon ? "spsummon" : "activate";
                        if (!this.submitCardAction(player, this.seatOf(player), location, sequence, kind)) {
                            player.displayClientMessage(Component.literal("这张卡现在不能执行该动作"), true);
                        }
                    } catch (Exception ex) {
                        player.displayClientMessage(Component.literal("操作失败：" + ex.getMessage()), true);
                    }
                }
            }
        }
    }

    synchronized boolean toggleCardSelect(ServerPlayer player, int controller, int location, int sequence) {
        if (this.host == null || !this.host.isRunning() || !this.host.hasSelectCardPrompt()) {
            return false;
        }

        if (this.refuseIfNotAsked(player)) {
            return false;
        }

        int index = this.host.selectCardIndex(controller, location, sequence);
        if (index < 0) {
            return false;
        }

        if (this.host.unselectPrompt()) {
            try {
                this.host.respondBytes(String.format("01%02X", index));
                player.displayClientMessage(Component.literal("已选素材（在素材界面里点已选的卡可取消，按完成结束）"), true);
            } catch (Exception ex) {
                player.displayClientMessage(Component.literal("选素材失败：" + ex.getMessage()), true);
            }

            return true;
        } else {
            int packed = controller << 16 | location << 8 | sequence;
            int min = Math.max(1, this.host.selectMin());
            int max = Math.max(min, this.host.selectMax());
            if (!this.cardSelection.remove(packed)) {
                if (this.cardSelection.size() >= max) {
                    return true;
                }

                this.cardSelection.add(packed);
            }

            player.displayClientMessage(Component.literal("已选 " + this.cardSelection.size() + " 张（需要 " + min + ".." + max + "）"), true);
            int need = this.host.selectMin() == this.host.selectMax() ? min : max;
            if (this.cardSelection.size() >= need) {
                List<Integer> indices = new ArrayList<>();

                for (int address : this.cardSelection) {
                    int found = this.host.selectCardIndex(address >> 16 & 1, address >> 8 & 0xFF, address & 0xFF);
                    if (found >= 0) {
                        indices.add(found);
                    }
                }

                Collections.sort(indices);
                StringBuilder hex = new StringBuilder(String.format("%02X", indices.size()));

                for (int value : indices) {
                    hex.append(String.format("%02X", value));
                }

                try {
                    this.host.respondBytes(hex.toString());
                    LOGGER.info("Card selection sent: {}", hex);
                } catch (Exception ex) {
                    LOGGER.warn("Card selection failed: {}", ex.getMessage());
                }

                this.cardSelection.clear();
            }

            return true;
        }
    }

    synchronized void performCardAction(ServerPlayer player, int entityId, int kind) {
        if (this.host != null && this.host.isRunning()) {
            if (!this.refuseIfNotAsked(player)) {
                Entity entity = player.level().getEntity(entityId);
                List<int[]> candidates = new ArrayList<>();
                if (entity instanceof HandCardEntity hand && !hand.ownerId().isEmpty() && hand.ownerId().equals(player.getUUID().toString())) {
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
                    case 0 -> new String[]{"summon"};
                    case 1 -> new String[]{"mset", "sset"};
                    case 2 -> new String[]{"activate"};
                    case 3 -> new String[]{"attack"};
                    case 4 -> new String[]{"position"};
                    case 5 -> new String[]{"spsummon"};
                    default -> new String[0];
                };
                if (kinds.length != 0) {
                    try {
                        for (int[] address : candidates) {
                            if (address != null && this.submitCardAction(player, address[0], address[1], address[2], kinds)) {
                                return;
                            }
                        }

                        player.displayClientMessage(Component.literal("这个动作现在做不了（规则核心没有提供候选）"), true);
                    } catch (Exception ex) {
                        player.displayClientMessage(Component.literal("操作失败：" + ex.getMessage()), true);
                    }
                }
            }
        }
    }

    record AiProfile(String key, String name, String dialog) {
    }

    private record PendingActivation(DuelHostProcess host, UUID player, long seq, int controller, int location, int sequence) {
    }

    private enum StartStage {
        IDLE,
        RPS,
        CHOOSE_TP;
    }
}
