package net.mdpromod;

import com.google.gson.JsonArray;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Locale;
import java.util.Set;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;
import org.slf4j.Logger;

final class DuelHostProcess implements AutoCloseable {
    private final Logger logger;
    private final Path dotnet;
    private final Path hostDll;
    private final Path runtimeRoot;
    private final AtomicBoolean running = new AtomicBoolean();
    private Process process;
    private BufferedWriter input;
    private volatile boolean ready;
    private volatile boolean waiting;
    private volatile boolean ended;
    private volatile boolean duelActive;
    private volatile int lastFunction = -1;
    private volatile String lastName = "";
    private volatile Integer winner;
    private volatile String lastError = "";
    private volatile String notice = "";
    private volatile long lastRetryNoticeAt;
    private volatile String promptSummary = "";
    private volatile String promptResponseKind = "";
    private volatile String promptJson = "";
    private volatile boolean promptToBattle;
    private volatile boolean promptToEnd;
    private volatile boolean promptToMain2;
    private volatile int promptPlayer = -1;
    private final DuelVisualState visualState = new DuelVisualState();
    private final int[] initialMain = new int[2];
    private final int[] initialExtra = new int[2];
    private volatile int turnPlayer;
    private final int[] lifePoints = new int[]{8000, 8000};
    private volatile int phaseCode;
    private final List<int[]> chainLinks = new ArrayList<>();
    private volatile int chainResolving;
    private volatile int chainVersion;
    private volatile String timingMode = "default";
    private static final int IDLE_COMMAND = 11;
    private static final int BATTLE_COMMAND = 10;
    private String currentMoment = "";

    int[] chainCodes() {
        int[] codes = new int[this.chainLinks.size()];

        for (int i = 0; i < codes.length; i++) {
            codes[i] = this.chainLinks.get(i)[0];
        }

        return codes;
    }

    int chainResolving() {
        return this.chainResolving;
    }

    int chainVersion() {
        return this.chainVersion;
    }

    int[] chainMarkAddresses() {
        return this.visualState.chainMarkAddresses();
    }

    int[] targetMarkAddresses() {
        return this.visualState.targetMarkAddresses();
    }

    boolean duelActive() {
        return this.duelActive;
    }

    String lastError() {
        return this.lastError;
    }

    String consumeNotice() {
        String value = this.notice;
        this.notice = "";
        return value == null ? "" : value;
    }

    DuelHostProcess(Logger logger, Path dotnet, Path hostDll, Path runtimeRoot) {
        this.logger = logger;
        this.dotnet = dotnet;
        this.hostDll = hostDll;
        this.runtimeRoot = runtimeRoot;
    }

    synchronized void start(String deck0, String deck1) throws IOException {
        this.start(deck0, deck1, false);
    }

    synchronized void start(String deck0, String deck1, boolean auto) throws IOException {
        this.start(deck0, deck1, auto, null, null);
    }

    synchronized void start(String deck0, String deck1, boolean auto, String aiName, String aiDialog) throws IOException {
        this.start(deck0, deck1, auto, aiName, aiDialog, true);
    }

    synchronized void start(String deck0, String deck1, boolean auto, String aiName, String aiDialog, boolean withAi) throws IOException {
        this.start(deck0, deck1, auto, aiName, aiDialog, withAi, withAi ? 1 : -1);
    }

    synchronized void start(String deck0, String deck1, boolean auto, String aiName, String aiDialog, boolean withAi, int aiSeat) throws IOException {
        if (this.running.get()) {
            throw new IllegalStateException("Duel Host is already running");
        }

        if (!ModBundle.ready()) {
            throw new IOException(
                ModBundle.state() == ModBundle.State.FAILED ? "资源解包失败（看日志里的 mdpromod-unpack），起不了决斗宿主" : "资源还在解包（" + ModBundle.progressText() + "），稍等一下再开始决斗"
            );
        }

        this.ready = false;
        this.waiting = false;
        this.ended = false;
        this.duelActive = false;
        this.lastFunction = -1;
        this.lastName = "";
        this.winner = null;
        this.lastError = "";
        this.promptSummary = "";
        this.promptResponseKind = "";
        this.promptJson = "";
        this.clearPhaseOptions();
        this.lifePoints[0] = 8000;
        this.lifePoints[1] = 8000;
        this.phaseCode = 1;
        this.turnPlayer = 0;
        ProcessBuilder builder = new ProcessBuilder(this.dotnet.toString(), this.hostDll.toString(), "--root", this.runtimeRoot.toString())
            .directory(this.runtimeRoot.toFile())
            .redirectErrorStream(true);
        Path runtimeHome = this.dotnet.getParent();
        if (runtimeHome != null && Files.isDirectory(runtimeHome)) {
            builder.environment().put("DOTNET_ROOT", runtimeHome.toString());
        }

        this.process = builder.start();
        this.input = new BufferedWriter(new OutputStreamWriter(this.process.getOutputStream(), StandardCharsets.UTF_8));
        this.running.set(true);
        Thread reader = new Thread(this::readLoop, "mdpromod-duel-host");
        reader.setDaemon(true);
        reader.start();
        this.sendStart(deck0, deck1, auto, aiName, aiDialog, withAi, aiSeat);
    }

    synchronized void sendStart(String deck0, String deck1) throws IOException {
        this.sendStart(deck0, deck1, false, null, null);
    }

    synchronized void sendStart(String deck0, String deck1, boolean auto) throws IOException {
        this.sendStart(deck0, deck1, auto, null, null);
    }

    synchronized void sendStart(String deck0, String deck1, boolean auto, String aiName, String aiDialog) throws IOException {
        this.sendStart(deck0, deck1, auto, aiName, aiDialog, true);
    }

    synchronized void sendStart(String deck0, String deck1, boolean auto, String aiName, String aiDialog, boolean withAi) throws IOException {
        this.sendStart(deck0, deck1, auto, aiName, aiDialog, withAi, withAi ? 1 : -1);
    }

    synchronized void sendStart(String deck0, String deck1, boolean auto, String aiName, String aiDialog, boolean withAi, int aiSeat) throws IOException {
        JsonObject command = new JsonObject();
        command.addProperty("cmd", "start");
        command.addProperty("deck0", deck0);
        command.addProperty("auto", auto);
        String deck1File = deck1 == null ? "" : deck1;
        command.addProperty("deck1", deck1File);
        if (withAi) {
            String fallbackDeck = System.getProperty("mdpromod.aiDeck", "AI_BlueEyes");
            String fallbackFile = System.getProperty("mdpromod.aiDeckFile", "Data/Windbot/Decks/" + fallbackDeck + ".ydk");
            String chosenAiFile = aiSeat == 0 ? deck0 : deck1;
            String aiDeckFile = chosenAiFile != null && !chosenAiFile.isBlank() ? chosenAiFile : fallbackFile;
            String aiDeck = aiDeckFile.replace('\\', '/');
            aiDeck = aiDeck.substring(aiDeck.lastIndexOf(47) + 1).replaceFirst("\\.ydk$", "");
            command.addProperty("ai", true);
            command.addProperty("aiSeat", aiSeat == 0 ? 0 : 1);
            command.addProperty("aiName", aiName != null && !aiName.isBlank() ? aiName : System.getProperty("mdpromod.aiName", "MDProBot"));
            command.addProperty("aiDeck", aiDeck);
            command.addProperty("aiDeckFile", aiDeckFile);
        }

        command.addProperty("autoPassChain", Boolean.parseBoolean(System.getProperty("mdpromod.autoPassChain", "false")));
        command.addProperty("aiDialog", aiDialog != null && !aiDialog.isBlank() ? aiDialog : System.getProperty("mdpromod.aiDialog", "cirno.zh-CN"));
        command.addProperty("timing", this.timingMode);
        this.visualState.reset();
        this.initializeDeck(0, deck0);
        this.initializeDeck(1, deck1File);
        this.send(command);
    }

    synchronized void stop() throws IOException {
        if (this.running.get()) {
            JsonObject command = new JsonObject();
            command.addProperty("cmd", "stop");
            this.send(command);
            this.close();
        }
    }

    synchronized void setTiming(String mode) throws IOException {
        this.timingMode = mode != null && !mode.isBlank() ? mode : "default";
        if (this.running.get()) {
            JsonObject command = new JsonObject();
            command.addProperty("cmd", "timing");
            command.addProperty("mode", this.timingMode);
            this.send(command);
        }
    }

    synchronized void surrender() throws IOException {
        this.surrender(0);
    }

    synchronized void surrender(int seat) throws IOException {
        if (!this.running.get()) {
            throw new IllegalStateException("没有进行中的决斗");
        }

        JsonObject command = new JsonObject();
        command.addProperty("cmd", "surrender");
        command.addProperty("player", seat == 1 ? 1 : 0);
        this.send(command);
    }

    synchronized void respond(int value) throws IOException {
        JsonObject command = new JsonObject();
        command.addProperty("cmd", "response");
        command.addProperty("value", value);
        this.send(command);
    }

    synchronized void respondBytes(String hex) throws IOException {
        JsonObject command = new JsonObject();
        command.addProperty("cmd", "responseBytes");
        command.addProperty("hex", hex);
        this.send(command);
    }

    synchronized String placeOptions() {
        if (this.promptJson != null && !this.promptJson.isEmpty()) {
            JsonObject prompt = JsonParser.parseString(this.promptJson).getAsJsonObject();
            if (!prompt.has("places")) {
                int function = prompt.has("function") ? prompt.get("function").getAsInt() : -1;
                return "current prompt has no places (function=" + function + ")";
            }

            JsonArray places = prompt.getAsJsonArray("places");
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < places.size(); i++) {
                JsonObject place = places.get(i).getAsJsonObject();
                if (i > 0) {
                    builder.append("; ");
                }

                builder.append("[")
                    .append(i)
                    .append("] player=")
                    .append(place.get("player").getAsInt())
                    .append(" location=")
                    .append(place.get("location").getAsInt())
                    .append(" sequence=")
                    .append(place.get("sequence").getAsInt());
            }

            return builder.toString();
        } else {
            return "no active prompt";
        }
    }

    synchronized void respondPlace(int index) throws IOException {
        if (this.promptJson != null && !this.promptJson.isEmpty()) {
            JsonObject prompt = JsonParser.parseString(this.promptJson).getAsJsonObject();
            if (!prompt.has("places")) {
                throw new IllegalStateException("Current prompt is not a SelectPlace prompt");
            }

            JsonArray places = prompt.getAsJsonArray("places");
            if (index >= 0 && index < places.size()) {
                JsonObject place = places.get(index).getAsJsonObject();
                byte[] bytes = new byte[]{(byte)place.get("player").getAsInt(), (byte)place.get("location").getAsInt(), (byte)place.get("sequence").getAsInt()};
                JsonObject command = new JsonObject();
                command.addProperty("cmd", "responseBytes");
                command.addProperty("hex", bytesToHex(bytes));
                this.send(command);
            } else {
                throw new IllegalArgumentException("Place index out of range: " + index);
            }
        } else {
            throw new IllegalStateException("No active prompt");
        }
    }

    private static String bytesToHex(byte[] bytes) {
        StringBuilder builder = new StringBuilder();

        for (byte value : bytes) {
            builder.append(String.format("%02X", value));
        }

        return builder.toString();
    }

    boolean isRunning() {
        return this.running.get() && this.process != null && this.process.isAlive() && !this.ended;
    }

    boolean isProcessAlive() {
        return this.process != null && this.process.isAlive();
    }

    boolean isReady() {
        return this.ready;
    }

    boolean isWaiting() {
        return this.waiting;
    }

    int promptFunction() {
        return this.waiting ? this.lastFunction : -1;
    }

    long promptSeq() {
        JsonObject prompt = this.currentPrompt();
        return prompt != null && prompt.has("seq") ? prompt.get("seq").getAsLong() : 0L;
    }

    boolean isEnded() {
        return this.ended;
    }

    Integer getWinner() {
        return this.winner;
    }

    String statusSummary() {
        return String.format(
            "alive=%s running=%s ready=%s waiting=%s ended=%s last=%s(%d) winner=%s prompt=%s responseKind=%s error=%s",
            this.isProcessAlive(),
            this.isRunning(),
            this.ready,
            this.waiting,
            this.ended,
            this.lastName,
            this.lastFunction,
            this.winner == null ? "-" : this.winner,
            this.promptSummary,
            this.promptResponseKind,
            this.lastError
        );
    }

    String promptSummary() {
        return this.promptSummary;
    }

    String promptResponseKind() {
        return this.promptResponseKind;
    }

    String promptJson() {
        return this.promptJson;
    }

    String hintSelectText() {
        return this.visualState.hintSelectText();
    }

    private void initializeDeck(int player, String name) throws IOException {
        Path path = Path.of(name);
        if (!path.isAbsolute()) {
            path = this.runtimeRoot.resolve(path);
        }

        DeckData deck = DeckData.fromYdk(path);
        this.initialMain[player] = deck.mainDeck().size();
        this.initialExtra[player] = deck.extraDeck().size();
        this.visualState.initializeCounts(player, this.initialMain[player], this.initialExtra[player]);
    }

    private void applyStats(String name, String hex) {
        try {
            String compact = hex == null ? "" : hex.replaceAll("\\s", "");
            switch (name) {
                case "NewTurn":
                case "NewPhase":
                case "Chaining":
                case "Summoning":
                case "SpSummoning":
                case "FlipSummoning":
                    this.currentMoment = "";
                    break;
                case "Summoned":
                    this.currentMoment = "通常召唤成功时";
                    break;
                case "SpSummoned":
                    this.currentMoment = "特殊召唤成功时";
                    break;
                case "FlipSummoned":
                    this.currentMoment = "反转召唤成功时";
                    break;
                case "Attack":
                    this.currentMoment = "攻击宣言时";
                    break;
                case "DamageStepStart":
                    this.currentMoment = "伤害步骤中";
                    break;
                case "DamageStepEnd":
                    this.currentMoment = "伤害步骤结束时";
                    break;
                case "Battle":
                    this.currentMoment = "伤害计算时";
                    break;
                case "ChainEnd":
                    this.currentMoment = "连锁处理结束时";
                    break;
                case "Move":
                    this.currentMoment = compact.length() >= 28 && Integer.parseInt(compact.substring(20, 22), 16) == 16 ? "送去墓地时" : "";
            }

            if ("LpUpdate".equals(name) && compact.length() >= 12) {
                this.lifePoints[playerByte(compact)] = clampLp(leU32(compact, 4));
                this.logger.info("LP {}: {} / {}", new Object[]{name, this.lifePoints[0], this.lifePoints[1]});
            } else if ("Damage".equals(name) && compact.length() >= 12) {
                int player = playerByte(compact);
                this.lifePoints[player] = clampLp(this.lifePoints[player] - leU32(compact, 4));
                this.logger.info("LP {}: {} / {}", new Object[]{name, this.lifePoints[0], this.lifePoints[1]});
            } else if ("PayLpCost".equals(name) && compact.length() >= 12) {
                int player = playerByte(compact);
                this.lifePoints[player] = clampLp(this.lifePoints[player] - leU32(compact, 4));
                this.logger.info("LP {}: {} / {}", new Object[]{name, this.lifePoints[0], this.lifePoints[1]});
            } else if ("Recover".equals(name) && compact.length() >= 12) {
                int player = playerByte(compact);
                this.lifePoints[player] = clampLp(this.lifePoints[player] + leU32(compact, 4));
                this.logger.info("LP {}: {} / {}", new Object[]{name, this.lifePoints[0], this.lifePoints[1]});
            } else if ("NewPhase".equals(name) && compact.length() >= 6) {
                this.phaseCode = Integer.parseInt(compact.substring(4, 6) + compact.substring(2, 4), 16) & 65535;
            } else if ("Chaining".equals(name) && compact.length() >= 10) {
                int code = Integer.parseInt(compact.substring(8, 10) + compact.substring(6, 8) + compact.substring(4, 6) + compact.substring(2, 4), 16);
                this.chainLinks.add(new int[]{code, this.chainLinks.size() + 1});
                this.chainVersion++;
            } else if ("ChainSolving".equals(name)) {
                this.chainResolving = this.chainLinks.size();
                this.chainVersion++;
            } else if ("ChainSolved".equals(name)) {
                if (!this.chainLinks.isEmpty()) {
                    this.chainLinks.remove(this.chainLinks.size() - 1);
                }

                this.chainResolving = Math.min(this.chainResolving, this.chainLinks.size());
                this.chainVersion++;
            } else if ("ChainEnd".equals(name) && (!this.chainLinks.isEmpty() || this.chainResolving != 0)) {
                this.chainLinks.clear();
                this.chainResolving = 0;
                this.chainVersion++;
            }

            this.visualState.apply(name, hex);
            if ("NewTurn".equals(name) && compact.length() >= 4) {
                this.turnPlayer = Integer.parseInt(compact.substring(2, 4), 16) & 1;
            }
        } catch (RuntimeException ex) {
            this.logger.warn("Invalid duel visual message {}: {}", name, ex.getMessage());
        }
    }

    private static int playerByte(String compact) {
        return Integer.parseInt(compact.substring(2, 4), 16) & 1;
    }

    private static long leU32(String compact, int offset) {
        long value = Long.parseLong(compact.substring(offset, offset + 8), 16);
        return (value & 4278190080L) >>> 24 | (value & 16711680L) >>> 8 | (value & 65280L) << 8 | (value & 255L) << 24;
    }

    private static int clampLp(long value) {
        return (int)Math.max(0L, Math.min(2147483647L, value));
    }

    int[] handOf(int player) {
        return this.visualState.hand(player & 1).stream().mapToInt(DuelVisualState.Card::code).toArray();
    }

    int handSize(int player) {
        return this.visualState.hand(player & 1).size();
    }

    int turnPlayer() {
        return this.turnPlayer;
    }

    int lifePoints(int player) {
        return this.lifePoints[player & 1];
    }

    int phaseCode() {
        return this.phaseCode;
    }

    private static boolean flag(JsonObject message, String key) {
        return message.has(key) && message.get(key).getAsBoolean();
    }

    private void clearPhaseOptions() {
        this.promptToBattle = false;
        this.promptToEnd = false;
        this.promptToMain2 = false;
        this.promptPlayer = -1;
    }

    int promptPlayer() {
        return this.waiting ? this.promptPlayer : -1;
    }

    boolean promptToBattle() {
        return this.waiting && this.promptToBattle;
    }

    boolean promptToEnd() {
        return this.waiting && this.promptToEnd;
    }

    boolean promptToMain2() {
        return this.waiting && this.promptToMain2;
    }

    int phaseResponse(String phase) {
        if (!this.waiting) {
            return -1;
        }

        String wanted = phase == null ? "" : phase.trim().toUpperCase(Locale.ROOT);
        if (this.lastFunction == 11) {
            if ("BP".equals(wanted) && this.promptToBattle) {
                return 6;
            }

            if ("EP".equals(wanted) && this.promptToEnd) {
                return 7;
            }
        } else if (this.lastFunction == 10) {
            if ("M2".equals(wanted) && this.promptToMain2) {
                return 2;
            }

            if ("EP".equals(wanted) && this.promptToEnd) {
                return 3;
            }
        }

        return -1;
    }

    boolean hasPlacePrompt() {
        if (this.waiting && this.promptJson != null && !this.promptJson.isEmpty()) {
            try {
                return JsonParser.parseString(this.promptJson).getAsJsonObject().has("places");
            } catch (RuntimeException ex) {
                return false;
            }
        } else {
            return false;
        }
    }

    int placeIndex(int controller, int location, int sequence) {
        if (!this.hasPlacePrompt()) {
            return -1;
        }

        JsonArray places = JsonParser.parseString(this.promptJson).getAsJsonObject().getAsJsonArray("places");

        for (int i = 0; i < places.size(); i++) {
            JsonObject place = places.get(i).getAsJsonObject();
            if (place.get("player").getAsInt() == controller && place.get("location").getAsInt() == location && place.get("sequence").getAsInt() == sequence) {
                return i;
            }
        }

        return -1;
    }

    int[] placeAddresses() {
        if (!this.hasPlacePrompt()) {
            return new int[0];
        }

        JsonArray places = JsonParser.parseString(this.promptJson).getAsJsonObject().getAsJsonArray("places");
        int[] packed = new int[places.size()];

        for (int i = 0; i < places.size(); i++) {
            JsonObject place = places.get(i).getAsJsonObject();
            packed[i] = place.get("player").getAsInt() << 16 | place.get("location").getAsInt() << 8 | place.get("sequence").getAsInt();
        }

        return packed;
    }

    private JsonObject currentPrompt() {
        if (this.waiting && this.promptJson != null && !this.promptJson.isEmpty()) {
            try {
                return JsonParser.parseString(this.promptJson).getAsJsonObject();
            } catch (RuntimeException ex) {
                return null;
            }
        } else {
            return null;
        }
    }

    boolean hasSelectCardPrompt() {
        JsonObject prompt = this.currentPrompt();
        return prompt != null && prompt.has("cards") && prompt.has("min");
    }

    int selectMin() {
        JsonObject prompt = this.currentPrompt();
        return prompt != null && prompt.has("min") ? prompt.get("min").getAsInt() : 0;
    }

    int selectMax() {
        JsonObject prompt = this.currentPrompt();
        return prompt != null && prompt.has("max") ? prompt.get("max").getAsInt() : 0;
    }

    boolean selectCancelable() {
        JsonObject prompt = this.currentPrompt();
        return (this.lastFunction == 15 || this.lastFunction == 20) && prompt != null && prompt.has("cancelable") && prompt.get("cancelable").getAsBoolean();
    }

    boolean selectFieldFirst() {
        return this.selectCancelable() && this.visualState.hintSelectId() == 575;
    }

    boolean unselectPrompt() {
        JsonObject prompt = this.currentPrompt();
        return this.lastFunction == 26 && prompt != null && prompt.has("cards");
    }

    boolean unselectFinishable() {
        JsonObject prompt = this.currentPrompt();
        return prompt != null && prompt.has("finish") && prompt.get("finish").getAsBoolean();
    }

    private int[] cardCodes(String key) {
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has(key)) {
            JsonArray list = prompt.getAsJsonArray(key);
            int[] codes = new int[list.size()];

            for (int i = 0; i < list.size(); i++) {
                codes[i] = visibleCode(list.get(i).getAsJsonObject(), this.promptSeat());
            }

            return codes;
        } else {
            return new int[0];
        }
    }

    private static int promptCode(JsonObject card) {
        return card != null && card.has("code") ? (int)(card.get("code").getAsLong() & 2147483647L) : 0;
    }

    private int promptSeat() {
        int seat = this.promptPlayer;
        return seat < 0 ? 0 : seat & 1;
    }

    private static int visibleCode(JsonObject card, int viewer) {
        if (card != null && card.has("code")) {
            int controller = card.has("controller") ? card.get("controller").getAsInt() : 0;
            int location = card.has("location") ? card.get("location").getAsInt() : 0;
            int position = card.has("position") ? card.get("position").getAsInt() : 0;
            return CardVisibility.visible(controller, location, position, viewer) ? promptCode(card) : 0;
        } else {
            return 0;
        }
    }

    int[] unselectSelectCodes() {
        return this.cardCodes("cards");
    }

    int[] unselectChosenCodes() {
        return this.cardCodes("unselectCards");
    }

    int[] unselectSelectAddresses() {
        return this.addressesFrom("cards");
    }

    int[] unselectChosenAddresses() {
        return this.addressesFrom("unselectCards");
    }

    private int[] addressesFrom(String key) {
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has(key)) {
            JsonArray list = prompt.getAsJsonArray(key);
            int[] packed = new int[list.size()];

            for (int i = 0; i < list.size(); i++) {
                JsonObject card = list.get(i).getAsJsonObject();
                packed[i] = card.get("controller").getAsInt() << 16 | card.get("location").getAsInt() << 8 | card.get("sequence").getAsInt();
            }

            return packed;
        } else {
            return new int[0];
        }
    }

    boolean optionPrompt() {
        JsonObject prompt = this.currentPrompt();
        return this.lastFunction == 14 && prompt != null && prompt.has("options") && prompt.getAsJsonArray("options").size() > 0;
    }

    int[] optionValues() {
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has("options")) {
            JsonArray options = prompt.getAsJsonArray("options");
            int[] values = new int[options.size()];

            for (int i = 0; i < options.size(); i++) {
                values[i] = options.get(i).getAsJsonObject().get("index").getAsInt();
            }

            return values;
        } else {
            return new int[0];
        }
    }

    String[] optionLabels() {
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has("options")) {
            JsonArray options = prompt.getAsJsonArray("options");
            String[] labels = new String[options.size()];

            for (int i = 0; i < options.size(); i++) {
                JsonObject option = options.get(i).getAsJsonObject();
                String raw = option.has("description") ? SystemStrings.get(option.get("description").getAsInt()) : "";
                if (raw.isEmpty() && option.has("label")) {
                    raw = option.get("label").getAsString();
                }

                labels[i] = raw.isEmpty() ? "选项 " + (i + 1) : raw;
            }

            return labels;
        } else {
            return new String[0];
        }
    }

    int announceFunction() {
        if (this.lastFunction >= 140 && this.lastFunction <= 143) {
            return this.currentPrompt() == null ? -1 : this.lastFunction;
        } else {
            return -1;
        }
    }

    String announceKind() {
        JsonObject prompt = this.currentPrompt();
        return prompt != null && prompt.has("kind") ? prompt.get("kind").getAsString() : "";
    }

    int announcePick() {
        JsonObject prompt = this.currentPrompt();
        return prompt != null && prompt.has("pick") ? Math.max(1, prompt.get("pick").getAsInt()) : 1;
    }

    int[] announceValues() {
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has("values")) {
            JsonArray values = prompt.getAsJsonArray("values");
            int[] out = new int[values.size()];

            for (int i = 0; i < values.size(); i++) {
                out[i] = values.get(i).getAsInt();
            }

            return out;
        } else {
            return new int[0];
        }
    }

    String announceHint() {
        JsonObject prompt = this.currentPrompt();
        return prompt != null && prompt.has("summary") ? prompt.get("summary").getAsString() : "";
    }

    int[] announceOpcodes() {
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has("opcodes")) {
            JsonArray opcodes = prompt.getAsJsonArray("opcodes");
            int[] out = new int[opcodes.size()];

            for (int i = 0; i < opcodes.size(); i++) {
                out[i] = opcodes.get(i).getAsInt();
            }

            return out;
        } else {
            return new int[0];
        }
    }

    boolean sortPrompt() {
        JsonObject prompt = this.currentPrompt();
        return this.lastFunction == 25 && prompt != null && prompt.has("cards") && prompt.getAsJsonArray("cards").size() > 1;
    }

    int[] sortCodes() {
        return this.cardCodes("cards");
    }

    boolean sumPrompt() {
        JsonObject prompt = this.currentPrompt();
        return this.lastFunction == 23 && prompt != null && (prompt.has("mustCards") || prompt.has("cards"));
    }

    boolean sumOverflow() {
        JsonObject prompt = this.currentPrompt();
        return prompt != null && prompt.has("overFlow") && prompt.get("overFlow").getAsBoolean();
    }

    int sumTarget() {
        JsonObject prompt = this.currentPrompt();
        return prompt != null && prompt.has("sum") ? prompt.get("sum").getAsInt() : 0;
    }

    int sumMin() {
        JsonObject prompt = this.currentPrompt();
        return prompt != null && prompt.has("min") ? Math.max(0, prompt.get("min").getAsInt()) : 1;
    }

    int sumMax() {
        JsonObject prompt = this.currentPrompt();
        return prompt != null && prompt.has("max") ? Math.max(1, prompt.get("max").getAsInt()) : 99;
    }

    int sumMustCount() {
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has("mustCards")) {
            return prompt.getAsJsonArray("mustCards").size();
        } else {
            return prompt != null && prompt.has("must") ? prompt.get("must").getAsInt() : 0;
        }
    }

    int[] sumMustCodes() {
        return this.sumCodes("mustCards");
    }

    int[] sumMustAddrs() {
        return this.sumAddrs("mustCards");
    }

    int[] sumMustLevel1() {
        return this.sumLevels("mustCards", false);
    }

    int[] sumMustLevel2() {
        return this.sumLevels("mustCards", true);
    }

    boolean sumMustMatches() {
        int target = this.sumTarget();
        if (target <= 0) {
            return false;
        }

        int first = 0;
        int second = 0;

        for (int level : this.sumMustLevel1()) {
            first += level;
        }

        for (int level : this.sumMustLevel2()) {
            second += level;
        }

        return first == target || second == target;
    }

    int[] sumCodes() {
        return this.sumCodes("cards");
    }

    int[] sumAddrs() {
        return this.sumAddrs("cards");
    }

    int[] sumLevel1() {
        return this.sumLevels("cards", false);
    }

    int[] sumLevel2() {
        return this.sumLevels("cards", true);
    }

    private int[] sumCodes(String key) {
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has(key)) {
            JsonArray cards = prompt.getAsJsonArray(key);
            int[] codes = new int[cards.size()];

            for (int i = 0; i < cards.size(); i++) {
                codes[i] = visibleCode(cards.get(i).getAsJsonObject(), this.promptSeat());
            }

            return codes;
        } else {
            return new int[0];
        }
    }

    private int[] sumAddrs(String key) {
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has(key)) {
            JsonArray cards = prompt.getAsJsonArray(key);
            int[] addrs = new int[cards.size()];

            for (int i = 0; i < cards.size(); i++) {
                JsonObject card = cards.get(i).getAsJsonObject();
                addrs[i] = card.get("controller").getAsInt() << 16 | card.get("location").getAsInt() << 8 | card.get("sequence").getAsInt();
            }

            return addrs;
        } else {
            return new int[0];
        }
    }

    private int[] sumLevels(String key, boolean second) {
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has(key)) {
            JsonArray cards = prompt.getAsJsonArray(key);
            int[] levels = new int[cards.size()];
            String field = second ? "level2" : "level1";

            for (int i = 0; i < cards.size(); i++) {
                JsonObject card = cards.get(i).getAsJsonObject();
                levels[i] = card.has(field) ? card.get(field).getAsInt() : 0;
                if (levels[i] == 0 && card.has("level1")) {
                    levels[i] = card.get("level1").getAsInt();
                }
            }

            return levels;
        } else {
            return new int[0];
        }
    }

    boolean positionPrompt() {
        JsonObject prompt = this.currentPrompt();
        return this.lastFunction == 19 && prompt != null && prompt.has("options");
    }

    int[] positionValues() {
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has("options")) {
            JsonArray options = prompt.getAsJsonArray("options");
            int[] values = new int[options.size()];

            for (int i = 0; i < options.size(); i++) {
                values[i] = options.get(i).getAsJsonObject().get("index").getAsInt();
            }

            return values;
        } else {
            return new int[0];
        }
    }

    int[] selectableAddresses() {
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has("cards")) {
            JsonArray cards = prompt.getAsJsonArray("cards");
            int[] packed = new int[cards.size()];

            for (int i = 0; i < cards.size(); i++) {
                JsonObject card = cards.get(i).getAsJsonObject();
                packed[i] = card.get("controller").getAsInt() << 16 | card.get("location").getAsInt() << 8 | card.get("sequence").getAsInt();
            }

            return packed;
        } else {
            return new int[0];
        }
    }

    int[] selectCodes() {
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has("cards")) {
            JsonArray cards = prompt.getAsJsonArray("cards");
            int[] codes = new int[cards.size()];

            for (int i = 0; i < cards.size(); i++) {
                JsonObject card = cards.get(i).getAsJsonObject();
                codes[i] = visibleCode(card, this.promptSeat());
            }

            return codes;
        } else {
            return new int[0];
        }
    }

    int[] selectAddrs() {
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has("cards")) {
            JsonArray cards = prompt.getAsJsonArray("cards");
            int[] addrs = new int[cards.size()];

            for (int i = 0; i < cards.size(); i++) {
                JsonObject card = cards.get(i).getAsJsonObject();
                int controller = card.has("controller") ? card.get("controller").getAsInt() : 0;
                int location = card.has("location") ? card.get("location").getAsInt() : 0;
                int sequence = card.has("sequence") ? card.get("sequence").getAsInt() : 0;
                addrs[i] = (controller & 1) << 16 | (location & 0xFF) << 8 | sequence & 0xFF;
            }

            return addrs;
        } else {
            return new int[0];
        }
    }

    int selectCardIndex(int controller, int location, int sequence) {
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has("cards")) {
            JsonArray cards = prompt.getAsJsonArray("cards");

            for (int i = 0; i < cards.size(); i++) {
                JsonObject card = cards.get(i).getAsJsonObject();
                if (card.get("controller").getAsInt() == controller
                    && card.get("location").getAsInt() == location
                    && card.get("sequence").getAsInt() == sequence) {
                    return i;
                }
            }

            return -1;
        } else {
            return -1;
        }
    }

    String selectSignature() {
        JsonObject prompt = this.currentPrompt();
        return prompt != null && prompt.has("cards") ? prompt.get("cards").getAsJsonArray().size() + ":" + this.selectMin() + ":" + this.selectMax() : "";
    }

    int decisionFunction() {
        if (!this.waiting) {
            return -1;
        } else {
            return this.lastFunction != 12 && this.lastFunction != 13 ? -1 : this.lastFunction;
        }
    }

    String decisionSignature() {
        JsonObject prompt = this.currentPrompt();
        return prompt == null
            ? ""
            : this.lastFunction
                + ":"
                + (prompt.has("code") ? prompt.get("code").getAsString() : "")
                + ":"
                + (prompt.has("description") ? prompt.get("description").getAsString() : "");
    }

    int decisionCardCode() {
        JsonObject prompt = this.currentPrompt();
        if (prompt == null) {
            return 0;
        }

        if (prompt.has("code")) {
            return promptCode(prompt);
        }

        if (prompt.has("summary")) {
            String summary = prompt.get("summary").getAsString();
            int at = summary.indexOf("card=");
            if (at >= 0) {
                int end = at + 5;

                while (end < summary.length() && Character.isDigit(summary.charAt(end))) {
                    end++;
                }

                try {
                    return Integer.parseInt(summary.substring(at + 5, end));
                } catch (NumberFormatException ignored) {
                    return 0;
                }
            }
        }

        return 0;
    }

    String decisionDescription() {
        JsonObject prompt = this.currentPrompt();
        return prompt != null && prompt.has("description") ? prompt.get("description").getAsString() : "";
    }

    String chainTimingText() {
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has("hints")) {
            JsonArray hints = prompt.getAsJsonArray("hints");
            long a = hints.size() > 0 ? hints.get(0).getAsLong() : 0L;
            long b = hints.size() > 1 ? hints.get(1).getAsLong() : 0L;
            return DuelMoment.describe(a, b, "");
        } else {
            return "";
        }
    }

    String currentMomentText() {
        return this.currentMoment;
    }

    boolean chainChoicePrompt() {
        return this.lastFunction == 16 && this.chainChoiceCount() > 0;
    }

    int chainChoiceCount() {
        JsonObject prompt = this.currentPrompt();
        if (prompt == null) {
            return 0;
        }

        int count = prompt.has("count") ? prompt.get("count").getAsInt() : 0;
        if (prompt.has("cards")) {
            count = Math.max(count, prompt.getAsJsonArray("cards").size());
        }

        return Math.max(0, count);
    }

    int[] chainChoiceCodes() {
        int count = this.chainChoiceCount();
        int[] codes = new int[count];
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has("cards")) {
            JsonArray cards = prompt.getAsJsonArray("cards");

            for (int i = 0; i < cards.size() && i < count; i++) {
                JsonObject card = cards.get(i).getAsJsonObject();
                codes[i] = visibleCode(card, this.promptSeat());
            }

            return codes;
        } else {
            return codes;
        }
    }

    String[] chainChoiceDescriptions() {
        int count = this.chainChoiceCount();
        String[] descriptions = new String[count];
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && prompt.has("cards")) {
            JsonArray cards = prompt.getAsJsonArray("cards");

            for (int i = 0; i < cards.size() && i < count; i++) {
                JsonObject card = cards.get(i).getAsJsonObject();
                long id = card.has("description") ? card.get("description").getAsLong() : 0L;
                if (id <= 0L) {
                    descriptions[i] = "";
                } else {
                    descriptions[i] = effectDescription(card, i);
                }
            }

            return descriptions;
        } else {
            return descriptions;
        }
    }

    boolean chainChoiceHasUnknown() {
        for (int code : this.chainChoiceCodes()) {
            if (code <= 0) {
                return true;
            }
        }

        return false;
    }

    int[] chainChoiceAddrs() {
        return this.addressesFrom("cards");
    }

    boolean chainForced() {
        JsonObject prompt = this.currentPrompt();
        return prompt != null && prompt.has("forced") && prompt.get("forced").getAsBoolean();
    }

    int[] glowingPileZones() {
        JsonObject prompt = this.currentPrompt();
        if (prompt == null) {
            return new int[0];
        }

        Set<Integer> zones = new LinkedHashSet<>();
        this.collectZones(prompt, "spsummonCards", zones, true);
        this.collectZones(prompt, "activateCards", zones, false);
        int[] packed = new int[zones.size()];
        int i = 0;

        for (int zone : zones) {
            packed[i++] = zone;
        }

        return packed;
    }

    private void collectZones(JsonObject prompt, String key, Set<Integer> zones, boolean includeExtra) {
        if (prompt.has(key)) {
            int seat = this.promptSeat();
            JsonArray list = prompt.getAsJsonArray(key);

            for (int i = 0; i < list.size(); i++) {
                JsonObject entry = list.get(i).getAsJsonObject();
                if (entry.has("controller") && entry.get("controller").getAsInt() == seat) {
                    int location = entry.has("location") ? entry.get("location").getAsInt() : -1;
                    if (location == 16 || location == 32 || includeExtra && location == 64) {
                        zones.add((seat & 1) << 16 | location & 0xFF);
                    }
                }
            }
        }
    }

    int[][] pileActions(int location, String kind) {
        JsonObject prompt = this.currentPrompt();
        if (prompt == null) {
            return new int[0][];
        }

        String key = switch (kind) {
            case "spsummon" -> "spsummonCards";
            case "activate" -> "activateCards";
            default -> "";
        };
        if (!prompt.has(key)) {
            return new int[0][];
        }

        JsonArray list = prompt.getAsJsonArray(key);
        List<int[]> out = new ArrayList<>();
        int seat = this.promptSeat();

        for (int i = 0; i < list.size(); i++) {
            JsonObject entry = list.get(i).getAsJsonObject();
            if (entry.get("controller").getAsInt() == seat
                && entry.get("location").getAsInt() == location
                && this.promptAction(seat, location, entry.get("sequence").getAsInt(), kind) >= 0) {
                out.add(new int[]{entry.get("sequence").getAsInt(), promptCode(entry)});
            }
        }

        return out.toArray(new int[0][]);
    }

    int promptAction(int controller, int location, int sequence, String... kinds) {
        if (this.waiting && this.promptJson != null && !this.promptJson.isEmpty()) {
            JsonObject prompt;
            try {
                prompt = JsonParser.parseString(this.promptJson).getAsJsonObject();
            } catch (RuntimeException ex) {
                return -1;
            }

            for (String kind : kinds) {
                String key;
                int type;
                switch (kind) {
                    case "summon":
                        key = "summonCards";
                        type = 0;
                        break;
                    case "spsummon":
                        key = "spsummonCards";
                        type = 1;
                        break;
                    case "position":
                        key = "positionCards";
                        type = 2;
                        break;
                    case "mset":
                        key = "monsterSetCards";
                        type = 3;
                        break;
                    case "sset":
                        key = "spellSetCards";
                        type = 4;
                        break;
                    case "activate":
                        key = "activateCards";
                        type = 5;
                        break;
                    case "attack":
                        key = "attackCards";
                        type = 1;
                        break;
                    default:
                        continue;
                }

                if (this.lastFunction == 11) {
                    if ("attack".equals(kind)) {
                        continue;
                    }
                } else {
                    if (this.lastFunction != 10 || !"attack".equals(kind) && !"activate".equals(kind)) {
                        continue;
                    }

                    if ("activate".equals(kind)) {
                        type = 0;
                    }
                }

                if (prompt.has(key)) {
                    JsonArray list = prompt.getAsJsonArray(key);

                    for (int i = 0; i < list.size(); i++) {
                        JsonObject entry = list.get(i).getAsJsonObject();
                        if (entry.has("controller")
                            && entry.get("controller").getAsInt() == controller
                            && entry.has("location")
                            && entry.get("location").getAsInt() == location
                            && entry.has("sequence")
                            && entry.get("sequence").getAsInt() == sequence
                            && (!entry.has("code") || promptCode(entry) != 0)) {
                            return i << 16 | type;
                        }
                    }
                }
            }

            return -1;
        } else {
            return -1;
        }
    }

    List<DuelHostProcess.Activation> activationChoices(int controller, int location, int sequence) {
        JsonObject prompt = this.currentPrompt();
        if (prompt != null && (this.lastFunction == 11 || this.lastFunction == 10) && controller == this.promptSeat() && prompt.has("activateCards")) {
            JsonArray cards = prompt.getAsJsonArray("activateCards");
            List<DuelHostProcess.Activation> choices = new ArrayList<>();
            int type = this.lastFunction == 11 ? 5 : 0;

            for (int i = 0; i < cards.size(); i++) {
                JsonObject card = cards.get(i).getAsJsonObject();
                if (card.get("controller").getAsInt() == controller
                    && card.get("location").getAsInt() == location
                    && card.get("sequence").getAsInt() == sequence
                    && promptCode(card) != 0) {
                    choices.add(new DuelHostProcess.Activation(i << 16 | type, promptCode(card), effectDescription(card, i)));
                }
            }

            return List.copyOf(choices);
        } else {
            return List.of();
        }
    }

    private static String effectDescription(JsonObject card, int index) {
        long id = card.has("description") ? card.get("description").getAsLong() : 0L;
        String text = card.has("effectText") ? card.get("effectText").getAsString() : "";
        if (text != null && !text.isBlank()) {
            return text;
        }

        if (id > 0L && id < 10000L) {
            text = SystemStrings.get((int)id);
        }

        return text == null ? "" : text;
    }

    String handSummary() {
        return "p1 hand=" + this.handSize(0) + " p2 hand=" + this.handSize(1) + " turn=" + this.turnPlayer;
    }

    List<DuelVisualState.Event> drainEvents() {
        return this.visualState.drainEvents();
    }

    int count(int player, int location) {
        return this.visualState.count(player & 1, location);
    }

    int initialMain(int player) {
        return this.initialMain[player & 1];
    }

    int initialExtra(int player) {
        return this.initialExtra[player & 1];
    }

    int drawCount(int player) {
        return this.initialMain(player) - this.count(player, 1);
    }

    int graveyardCount(int player) {
        return this.count(player, 16);
    }

    int banishedCount(int player) {
        return this.count(player, 32);
    }

    int extraMovedCount(int player) {
        return this.count(player, 64) - this.initialExtra(player);
    }

    String statsSummary() {
        return String.format(
            "p1[deck=%d hand=%d grave=%d banish=%d extra=%d] p2[deck=%d hand=%d grave=%d banish=%d extra=%d]",
            this.count(0, 1),
            this.handSize(0),
            this.graveyardCount(0),
            this.banishedCount(0),
            this.count(0, 64),
            this.count(1, 1),
            this.handSize(1),
            this.graveyardCount(1),
            this.banishedCount(1),
            this.count(1, 64)
        );
    }

    private void send(JsonObject command) throws IOException {
        if (this.input == null) {
            throw new IOException("Duel Host stdin is not available");
        }

        this.input.write(command.toString());
        this.input.newLine();
        this.input.flush();
    }

    private void readLoop() {
        try {
            String line;
            try (BufferedReader reader = new BufferedReader(new InputStreamReader(this.process.getInputStream(), StandardCharsets.UTF_8))) {
                while ((line = reader.readLine()) != null) {
                    try {
                        JsonObject message = JsonParser.parseString(line).getAsJsonObject();
                        String type = message.has("type") ? message.get("type").getAsString() : "unknown";
                        switch (type) {
                            case "ready":
                                this.ready = true;
                                this.lastError = "";
                                break;
                            case "started":
                                this.duelActive = true;
                                this.lastError = "";
                                break;
                            case "state":
                                this.waiting = message.has("waiting") && message.get("waiting").getAsBoolean();
                                if (message.has("function")) {
                                    this.lastFunction = message.get("function").getAsInt();
                                }

                                this.clearPhaseOptions();
                                break;
                            case "prompt":
                                this.promptJson = message.toString();
                                this.promptSummary = message.has("summary") ? message.get("summary").getAsString() : "";
                                this.promptResponseKind = message.has("responseKind") ? message.get("responseKind").getAsString() : "integer";
                                if (message.has("function")) {
                                    this.lastFunction = message.get("function").getAsInt();
                                }

                                this.clearPhaseOptions();
                                if (message.has("player")) {
                                    this.promptPlayer = message.get("player").getAsInt();
                                }

                                this.promptToBattle = flag(message, "toBattle");
                                this.promptToEnd = flag(message, "toEnd");
                                this.promptToMain2 = flag(message, "toMain2");
                                break;
                            case "message":
                                if (message.has("function")) {
                                    this.lastFunction = message.get("function").getAsInt();
                                }

                                if (message.has("name")) {
                                    this.lastName = message.get("name").getAsString();
                                }

                                if (message.has("winner") && !message.get("winner").isJsonNull()) {
                                    this.winner = message.get("winner").getAsInt();
                                }

                                if (message.has("hex")) {
                                    this.applyStats(this.lastName, message.get("hex").getAsString());
                                }
                                break;
                            case "ended":
                                this.ended = true;
                                this.waiting = false;
                                this.clearPhaseOptions();
                                break;
                            case "error":
                                this.lastError = message.has("message") ? message.get("message").getAsString() : "unknown error";
                                this.notice = "对局出错：" + this.lastError;
                                break;
                            case "retry":
                                long now = System.currentTimeMillis();
                                if (now - this.lastRetryNoticeAt > 1500L) {
                                    this.lastRetryNoticeAt = now;
                                    this.notice = "核心判上次应答非法，会重新问一次，请再选一遍";
                                }
                        }

                        this.logger.info("Duel Host {}: {}", type, line);
                    } catch (RuntimeException ex) {
                        this.logger.warn("Duel Host emitted non-JSON output: {}", line);
                    }
                }
            } catch (IOException ex) {
                if (this.running.get()) {
                    this.logger.warn("Duel Host output ended: {}", ex.getMessage());
                }
            }
        } finally {
            this.running.set(false);
        }
    }

    @Override
    public synchronized void close() {
        this.running.set(false);
        if (this.input != null) {
            try {
                this.input.close();
            } catch (IOException var3) {
            }

            this.input = null;
        }

        if (this.process != null) {
            try {
                if (!this.process.waitFor(2L, TimeUnit.SECONDS)) {
                    this.process.destroy();
                    if (!this.process.waitFor(2L, TimeUnit.SECONDS)) {
                        this.process.destroyForcibly();
                    }
                }
            } catch (InterruptedException ex) {
                Thread.currentThread().interrupt();
                this.process.destroyForcibly();
            }

            this.process = null;
        }

        this.ready = false;
        this.waiting = false;
        this.ended = false;
        this.duelActive = false;
        this.lastFunction = -1;
        this.lastName = "";
        this.winner = null;
        this.lastError = "";
        this.promptSummary = "";
        this.promptResponseKind = "";
        this.promptJson = "";
        this.clearPhaseOptions();
    }

    record Activation(int response, int code, String text) {
    }
}
