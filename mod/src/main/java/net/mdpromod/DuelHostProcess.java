package net.mdpromod;

import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import com.google.gson.JsonArray;
import net.minecraft.network.chat.Component;
import org.slf4j.Logger;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.nio.charset.StandardCharsets;
import java.nio.file.Path;
import java.util.concurrent.atomic.AtomicBoolean;

/** Small JSON-lines bridge to the D-drive headless duel host. */
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
    /** 宿主进程内是否真的建起了一局（收到 started 事件）。 */
    private volatile boolean duelActive;
    private volatile int lastFunction = -1;
    private volatile String lastName = "";
    private volatile Integer winner;
    private volatile String lastError = "";
    /** 需要让玩家看见的宿主消息（错误 / 应答被拒），由 tickField 取走显示一次。 */
    private volatile String notice = "";
    private volatile long lastRetryNoticeAt;
    private volatile String promptSummary = "";
    private volatile String promptResponseKind = "";
    private volatile String promptJson = "";
    /** 当前决策点允许的阶段转移：只在 waiting 有效。 */
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
    /** 当前连锁：每项 {卡号, 链号}；解析按"解决一个消一个"维护。 */
    private final java.util.List<int[]> chainLinks = new java.util.ArrayList<>();
    private volatile int chainResolving;
    private volatile int chainVersion;
    /** 时点模式（发给宿主的模式名）；下一局开始时会一并带上。 */
    private volatile String timingMode = "default";

    int[] chainCodes() {
        int[] codes = new int[chainLinks.size()];
        for (int i = 0; i < codes.length; i++) codes[i] = chainLinks.get(i)[0];
        return codes;
    }

    int chainResolving() { return chainResolving; }
    int chainVersion() { return chainVersion; }

    /** 连锁里发动、且还在场上的卡（打包地址），客户端据此在卡面打「连锁中」标记。 */
    int[] chainMarkAddresses() { return visualState.chainMarkAddresses(); }

    /** 当前连锁里被点名、且还在场上的卡（打包地址）。 */
    int[] targetMarkAddresses() { return visualState.targetMarkAddresses(); }

    boolean duelActive() { return duelActive; }

    String lastError() { return lastError; }

    /** 取走一条要显示给玩家的宿主消息（没有就返回空串）。 */
    String consumeNotice() {
        String value = notice;
        notice = "";
        return value == null ? "" : value;
    }
    /** 决策点功能号：11 = SelectIdleCmd（主要阶段），10 = SelectBattleCmd（战斗阶段）。 */
    private static final int IDLE_COMMAND = 11;
    private static final int BATTLE_COMMAND = 10;

    DuelHostProcess(Logger logger, Path dotnet, Path hostDll, Path runtimeRoot) {
        this.logger = logger;
        this.dotnet = dotnet;
        this.hostDll = hostDll;
        this.runtimeRoot = runtimeRoot;
    }

    synchronized void start(String deck0, String deck1) throws IOException {
        start(deck0, deck1, false);
    }

    synchronized void start(String deck0, String deck1, boolean auto) throws IOException {
        start(deck0, deck1, auto, null, null);
    }

    /**
     * @param deck1   远半场（人机）的卡组文件；null/空就用系统属性里的默认人机
     * @param aiName  人机显示名（来自 bot.conf 的 {@code Name=}）
     * @param aiDialog 人机人格（来自 bot.conf 的 {@code Dialog=}）
     */
    synchronized void start(String deck0, String deck1, boolean auto, String aiName,
                            String aiDialog) throws IOException {
        if (running.get()) throw new IllegalStateException("Duel Host is already running");
        // 宿主和它的 cards.cdb / script.zip 都在首次启动解出来的资源目录里，没解完就别起进程
        if (!ModBundle.ready()) {
            throw new IOException(ModBundle.state() == ModBundle.State.FAILED
                    ? "资源解包失败（看日志里的 mdpromod-unpack），起不了决斗宿主"
                    : "资源还在解包（" + ModBundle.progressText() + "），稍等一下再开始决斗");
        }
        ready = false;
        waiting = false;
        ended = false;
        duelActive = false;
        lastFunction = -1;
        lastName = "";
        winner = null;
        lastError = "";
        promptSummary = "";
        promptResponseKind = "";
        promptJson = "";
        clearPhaseOptions();
        lifePoints[0] = 8000;
        lifePoints[1] = 8000;
        phaseCode = 1;
        turnPlayer = 0;
        ProcessBuilder builder = new ProcessBuilder(dotnet.toString(), hostDll.toString(),
                "--root", runtimeRoot.toString())
                .directory(runtimeRoot.toFile())
                .redirectErrorStream(true);
        // 随包自带的 .NET 运行时：apphost 不会自己找同目录的 host/fxr，必须显式给 DOTNET_ROOT。
        // 装系统 dotnet 的情况（dotnet.exe 在安装根下）父目录同样就是运行时根，给它也一样。
        Path runtimeHome = dotnet.getParent();
        if (runtimeHome != null && java.nio.file.Files.isDirectory(runtimeHome)) {
            builder.environment().put("DOTNET_ROOT", runtimeHome.toString());
        }
        process = builder.start();
        input = new BufferedWriter(new OutputStreamWriter(process.getOutputStream(), StandardCharsets.UTF_8));
        running.set(true);
        Thread reader = new Thread(this::readLoop, "mdpromod-duel-host");
        reader.setDaemon(true);
        reader.start();
        sendStart(deck0, deck1, auto, aiName, aiDialog);
    }

    synchronized void sendStart(String deck0, String deck1) throws IOException {
        sendStart(deck0, deck1, false, null, null);
    }

    synchronized void sendStart(String deck0, String deck1, boolean auto) throws IOException {
        sendStart(deck0, deck1, auto, null, null);
    }

    synchronized void sendStart(String deck0, String deck1, boolean auto, String aiName,
                                String aiDialog) throws IOException {
        JsonObject command = new JsonObject();
        command.addProperty("cmd", "start");
        command.addProperty("deck0", deck0);
        // 对手默认交给内置 WindBot（路线 A）：p1 用 AI 卡组，人类继续走 JSON 协议
        String fallbackDeck = System.getProperty("mdpromod.aiDeck", "AI_BlueEyes");
        String fallbackFile = System.getProperty("mdpromod.aiDeckFile",
                "Data/Windbot/Decks/" + fallbackDeck + ".ydk");
        // 玩家在「人机卡组」里选的那副优先；没选才回落到系统属性
        String aiDeckFile = deck1 == null || deck1.isBlank() ? fallbackFile : deck1;
        String aiDeck = aiDeckFile.replace('\\', '/');
        aiDeck = aiDeck.substring(aiDeck.lastIndexOf('/') + 1).replaceFirst("\\.ydk$", "");
        command.addProperty("deck1", aiDeckFile);
        command.addProperty("auto", auto);
        command.addProperty("ai", true);
        command.addProperty("aiSeat", 1);
        command.addProperty("aiName", aiName == null || aiName.isBlank()
                ? System.getProperty("mdpromod.aiName", "MDProBot") : aiName);
        command.addProperty("aiDeck", aiDeck);
        command.addProperty("aiDeckFile", aiDeckFile);
        // 还没有连锁/效果 UI 时，把空连锁窗口交给 host 自动放弃，否则决斗会卡在
        // 每一个优先权窗口上（用 -Dmdpromod.autoPassChain=false 可以关掉）。
        // 有链选择窗口了，默认让玩家自己决定；空连锁窗口 host 仍然自动放弃。
        command.addProperty("autoPassChain",
                Boolean.parseBoolean(System.getProperty("mdpromod.autoPassChain", "false")));
        command.addProperty("aiDialog", aiDialog == null || aiDialog.isBlank()
                ? System.getProperty("mdpromod.aiDialog", "cirno.zh-CN") : aiDialog);
        // 时点：全时点会连"没有可接的卡"的空连锁窗口都停下来问，关闭则可选的一律不问
        command.addProperty("timing", timingMode);
        visualState.reset();
        initializeDeck(0, deck0);
        initializeDeck(1, aiDeckFile);
        send(command);
    }

    synchronized void stop() throws IOException {
        if (!running.get()) return;
        JsonObject command = new JsonObject();
        command.addProperty("cmd", "stop");
        send(command);
        close();
    }

    /** 换时点模式：宿主在跑就马上生效，没跑就记着、下一局开局带上。 */
    synchronized void setTiming(String mode) throws IOException {
        timingMode = mode == null || mode.isBlank() ? "default" : mode;
        if (!running.get()) return;
        JsonObject command = new JsonObject();
        command.addProperty("cmd", "timing");
        command.addProperty("mode", timingMode);
        send(command);
    }

    /** 玩家认输：宿主会把这一局判给对面（发一条标准的 Win 消息，Java 侧照常收尾）。 */
    synchronized void surrender() throws IOException {
        if (!running.get()) throw new IllegalStateException("没有进行中的决斗");
        JsonObject command = new JsonObject();
        command.addProperty("cmd", "surrender");
        command.addProperty("player", 0);
        send(command);
    }

    synchronized void respond(int value) throws IOException {
        JsonObject command = new JsonObject();
        command.addProperty("cmd", "response");
        command.addProperty("value", value);
        send(command);
    }

    synchronized void respondBytes(String hex) throws IOException {
        JsonObject command = new JsonObject();
        command.addProperty("cmd", "responseBytes");
        command.addProperty("hex", hex);
        send(command);
    }

    synchronized String placeOptions() {
        if (promptJson == null || promptJson.isEmpty()) return "no active prompt";
        JsonObject prompt = JsonParser.parseString(promptJson).getAsJsonObject();
        if (!prompt.has("places")) {
            int function = prompt.has("function") ? prompt.get("function").getAsInt() : -1;
            return "current prompt has no places (function=" + function + ")";
        }
        JsonArray places = prompt.getAsJsonArray("places");
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < places.size(); i++) {
            JsonObject place = places.get(i).getAsJsonObject();
            if (i > 0) builder.append("; ");
            builder.append("[").append(i).append("] player=")
                    .append(place.get("player").getAsInt())
                    .append(" location=").append(place.get("location").getAsInt())
                    .append(" sequence=").append(place.get("sequence").getAsInt());
        }
        return builder.toString();
    }

    synchronized void respondPlace(int index) throws IOException {
        if (promptJson == null || promptJson.isEmpty()) {
            throw new IllegalStateException("No active prompt");
        }
        JsonObject prompt = JsonParser.parseString(promptJson).getAsJsonObject();
        if (!prompt.has("places")) {
            throw new IllegalStateException("Current prompt is not a SelectPlace prompt");
        }
        JsonArray places = prompt.getAsJsonArray("places");
        if (index < 0 || index >= places.size()) {
            throw new IllegalArgumentException("Place index out of range: " + index);
        }
        JsonObject place = places.get(index).getAsJsonObject();
        byte[] bytes = new byte[]{
                (byte) place.get("player").getAsInt(),
                (byte) place.get("location").getAsInt(),
                (byte) place.get("sequence").getAsInt()
        };
        JsonObject command = new JsonObject();
        command.addProperty("cmd", "responseBytes");
        command.addProperty("hex", bytesToHex(bytes));
        send(command);
    }

    private static String bytesToHex(byte[] bytes) {
        StringBuilder builder = new StringBuilder();
        for (byte value : bytes) {
            builder.append(String.format("%02X", value));
        }
        return builder.toString();
    }

    boolean isRunning() {
        return running.get() && process != null && process.isAlive() && !ended;
    }

    boolean isProcessAlive() {
        return process != null && process.isAlive();
    }

    boolean isReady() {
        return ready;
    }

    boolean isWaiting() {
        return waiting;
    }

    /** 当前等待回答的功能号（没在等就是 -1）。 */
    int promptFunction() {
        return waiting ? lastFunction : -1;
    }

    /**
     * 当前提示的序号（核心每问一次就 +1）。
     * 界面用它做去重：形状相同的两次提问（例如 RETRY 后重发的同一问）也能区分开，
     * 不会因为"看起来一样"就漏掉一次弹窗。
     */
    long promptSeq() {
        JsonObject prompt = currentPrompt();
        return prompt != null && prompt.has("seq") ? prompt.get("seq").getAsLong() : 0L;
    }

    boolean isEnded() {
        return ended;
    }

    Integer getWinner() {
        return winner;
    }

    String statusSummary() {
        return String.format(
                "alive=%s running=%s ready=%s waiting=%s ended=%s last=%s(%d) winner=%s prompt=%s responseKind=%s error=%s",
                isProcessAlive(), isRunning(), ready, waiting, ended,
                lastName, lastFunction, winner == null ? "-" : winner,
                promptSummary, promptResponseKind, lastError);
    }

    String promptSummary() {
        return promptSummary;
    }

    String promptResponseKind() {
        return promptResponseKind;
    }

    String promptJson() {
        return promptJson;
    }

    private void initializeDeck(int player, String name) throws IOException {
        Path path = Path.of(name);
        if (!path.isAbsolute()) path = runtimeRoot.resolve(path);
        DeckData deck = DeckData.fromYdk(path);
        initialMain[player] = deck.mainDeck().size();
        initialExtra[player] = deck.extraDeck().size();
        visualState.initializeCounts(player, initialMain[player], initialExtra[player]);
    }

    private void applyStats(String name, String hex) {
        try {
            String compact = hex == null ? "" : hex.replaceAll("\\s", "");
            // 91 Damage / 92 Recover / 100 PayLpCost 是**增量**，94 LpUpdate 是"设为某值"。
            // 以前只处理了 94（而且名字写成 LPUpdate、大小写对不上），所以血永远停在 8000：
            // 核心扣血只发 91，不会再补一条 94。
            if ("LpUpdate".equals(name) && compact.length() >= 12) {
                lifePoints[playerByte(compact)] = clampLp(leU32(compact, 4));
                logger.info("LP {}: {} / {}", name, lifePoints[0], lifePoints[1]);
            } else if ("Damage".equals(name) && compact.length() >= 12) {
                int player = playerByte(compact);
                lifePoints[player] = clampLp((long) lifePoints[player] - leU32(compact, 4));
                logger.info("LP {}: {} / {}", name, lifePoints[0], lifePoints[1]);
            } else if ("PayLpCost".equals(name) && compact.length() >= 12) {
                int player = playerByte(compact);
                lifePoints[player] = clampLp((long) lifePoints[player] - leU32(compact, 4));
                logger.info("LP {}: {} / {}", name, lifePoints[0], lifePoints[1]);
            } else if ("Recover".equals(name) && compact.length() >= 12) {
                int player = playerByte(compact);
                lifePoints[player] = clampLp((long) lifePoints[player] + leU32(compact, 4));
                logger.info("LP {}: {} / {}", name, lifePoints[0], lifePoints[1]);
            } else if ("NewPhase".equals(name) && compact.length() >= 6) {
                phaseCode = Integer.parseInt(compact.substring(4, 6) + compact.substring(2, 4), 16) & 0xFFFF;
            } else if ("Chaining".equals(name) && compact.length() >= 10) {
                // 载荷：code(4) + info_location(4) + 触发位置(3) + 描述(4)
                int code = Integer.parseInt(compact.substring(8, 10) + compact.substring(6, 8)
                        + compact.substring(4, 6) + compact.substring(2, 4), 16);
                chainLinks.add(new int[]{code, chainLinks.size() + 1});
                chainVersion++;
            } else if ("ChainSolving".equals(name)) {
                chainResolving = chainLinks.size();
                chainVersion++;
            } else if ("ChainSolved".equals(name)) {
                if (!chainLinks.isEmpty()) chainLinks.remove(chainLinks.size() - 1);
                chainResolving = Math.min(chainResolving, chainLinks.size());
                chainVersion++;
            } else if ("ChainEnd".equals(name)) {
                if (!chainLinks.isEmpty() || chainResolving != 0) {
                    chainLinks.clear();
                    chainResolving = 0;
                    chainVersion++;
                }
            }
            visualState.apply(name, hex);
            if ("NewTurn".equals(name) && compact.length() >= 4)
                turnPlayer = Integer.parseInt(compact.substring(2, 4), 16) & 1;
        } catch (RuntimeException ex) {
            logger.warn("Invalid duel visual message {}: {}", name, ex.getMessage());
        }
    }

    /** 消息里第 2 个字节（0 起算的第 1 个）是要回答/受影响的玩家。 */
    private static int playerByte(String compact) {
        return Integer.parseInt(compact.substring(2, 4), 16) & 1;
    }

    /** 小端 32 位无符号数：核心的消息载荷一律小端。 */
    private static long leU32(String compact, int offset) {
        long value = Long.parseLong(compact.substring(offset, offset + 8), 16);
        return ((value & 0xFF000000L) >>> 24)
                | ((value & 0x00FF0000L) >>> 8)
                | ((value & 0x0000FF00L) << 8)
                | ((value & 0x000000FFL) << 24);
    }

    private static int clampLp(long value) {
        return (int) Math.max(0, Math.min(Integer.MAX_VALUE, value));
    }

    int[] handOf(int player) {
        return visualState.hand(player & 1).stream().mapToInt(DuelVisualState.Card::code).toArray();
    }

    int handSize(int player) { return visualState.hand(player & 1).size(); }
    int turnPlayer() { return turnPlayer; }
    int lifePoints(int player) { return lifePoints[player & 1]; }
    int phaseCode() { return phaseCode; }

    private static boolean flag(JsonObject message, String key) {
        return message.has(key) && message.get(key).getAsBoolean();
    }

    private void clearPhaseOptions() {
        promptToBattle = false;
        promptToEnd = false;
        promptToMain2 = false;
        promptPlayer = -1;
    }

    /** 当前等待中的决策点属于哪个座位；没有决策点时返回 -1。 */
    int promptPlayer() { return waiting ? promptPlayer : -1; }
    boolean promptToBattle() { return waiting && promptToBattle; }
    boolean promptToEnd() { return waiting && promptToEnd; }
    boolean promptToMain2() { return waiting && promptToMain2; }

    /**
     * 把阶段点击翻译成核心要的应答值：
     * SelectIdleCmd → 6 进战斗阶段 / 7 进结束阶段；
     * SelectBattleCmd → 2 进主要阶段 2 / 3 结束战斗阶段。
     * 当前决策点不允许这个转移时返回 -1，绝不硬发，避免 MSG_RETRY。
     */
    int phaseResponse(String phase) {
        if (!waiting) return -1;
        String wanted = phase == null ? "" : phase.trim().toUpperCase(java.util.Locale.ROOT);
        if (lastFunction == IDLE_COMMAND) {
            if ("BP".equals(wanted) && promptToBattle) return 6;
            if ("EP".equals(wanted) && promptToEnd) return 7;
        } else if (lastFunction == BATTLE_COMMAND) {
            if ("M2".equals(wanted) && promptToMain2) return 2;
            if ("EP".equals(wanted) && promptToEnd) return 3;
        }
        return -1;
    }

    /** 当前提示是否在等玩家选区域（召唤/盖放/移动的落点）。 */
    boolean hasPlacePrompt() {
        if (!waiting || promptJson == null || promptJson.isEmpty()) return false;
        try {
            return JsonParser.parseString(promptJson).getAsJsonObject().has("places");
        } catch (RuntimeException ex) {
            return false;
        }
    }

    /** 在 places 列表里找 (controller, location, sequence) 对应的下标；找不到返回 -1。 */
    int placeIndex(int controller, int location, int sequence) {
        if (!hasPlacePrompt()) return -1;
        JsonArray places = JsonParser.parseString(promptJson).getAsJsonObject().getAsJsonArray("places");
        for (int i = 0; i < places.size(); i++) {
            JsonObject place = places.get(i).getAsJsonObject();
            if (place.get("player").getAsInt() != controller) continue;
            if (place.get("location").getAsInt() != location) continue;
            if (place.get("sequence").getAsInt() != sequence) continue;
            return i;
        }
        return -1;
    }

    /** 当前落点提示里的全部候选，打包成 controller&lt;&lt;16 | location&lt;&lt;8 | sequence。 */
    int[] placeAddresses() {
        if (!hasPlacePrompt()) return new int[0];
        JsonArray places = JsonParser.parseString(promptJson).getAsJsonObject().getAsJsonArray("places");
        int[] packed = new int[places.size()];
        for (int i = 0; i < places.size(); i++) {
            JsonObject place = places.get(i).getAsJsonObject();
            packed[i] = (place.get("player").getAsInt() << 16)
                    | (place.get("location").getAsInt() << 8)
                    | place.get("sequence").getAsInt();
        }
        return packed;
    }

    private com.google.gson.JsonObject currentPrompt() {
        if (!waiting || promptJson == null || promptJson.isEmpty()) return null;
        try {
            return JsonParser.parseString(promptJson).getAsJsonObject();
        } catch (RuntimeException ex) {
            return null;
        }
    }

    /** 当前是否在等玩家选卡（弃牌、选素材那类），以及要选几张。 */
    boolean hasSelectCardPrompt() {
        JsonObject prompt = currentPrompt();
        return prompt != null && prompt.has("cards") && prompt.has("min");
    }

    int selectMin() {
        JsonObject prompt = currentPrompt();
        return prompt != null && prompt.has("min") ? prompt.get("min").getAsInt() : 0;
    }

    int selectMax() {
        JsonObject prompt = currentPrompt();
        return prompt != null && prompt.has("max") ? prompt.get("max").getAsInt() : 0;
    }

    /** 26 = SelectUnselectCard（融合素材那类"一张一张点 + 完成"）。 */
    boolean unselectPrompt() {
        JsonObject prompt = currentPrompt();
        return lastFunction == 26 && prompt != null && prompt.has("cards");
    }

    boolean unselectFinishable() {
        JsonObject prompt = currentPrompt();
        return prompt != null && prompt.has("finish") && prompt.get("finish").getAsBoolean();
    }

    private int[] cardCodes(String key) {
        JsonObject prompt = currentPrompt();
        if (prompt == null || !prompt.has(key)) return new int[0];
        JsonArray list = prompt.getAsJsonArray(key);
        int[] codes = new int[list.size()];
        for (int i = 0; i < list.size(); i++) {
            codes[i] = visibleCode(list.get(i).getAsJsonObject());
        }
        return codes;
    }

    /**
     * 提示里的卡号：核心对"场地效果"（没有实体卡的效果）会把最高位置 1，
     * 而且数值可能超出 int 范围——按 long 读再掩掉最高位，别让它回绕成负数。
     */
    private static int promptCode(JsonObject card) {
        if (card == null || !card.has("code")) return 0;
        return (int) (card.get("code").getAsLong() & 0x7FFFFFFFL);
    }

    /**
     * 提示里的卡号，但**按可见性过滤**：对手的手牌 / 卡组 / 里侧场上的卡（以及里侧除外、里侧额外）
     * 对自己都是未知信息 —— 一律返回 0，客户端就画卡背、名字显示"里侧卡片"。
     * 自己的卡、以及公开信息（双方墓地、表侧场上的卡、表侧除外/额外）照实返回。
     * 规则依据：这个提示是核心直接给"读核心的客户端"的，核心不替我们隐藏信息，必须自己过滤。
     */
    private static int visibleCode(JsonObject card) {
        if (card == null || !card.has("code")) return 0;
        int controller = card.has("controller") ? card.get("controller").getAsInt() : 0;
        if (controller == 0) return promptCode(card);          // 自己这一侧自己看得见
        int location = card.has("location") ? card.get("location").getAsInt() : 0;
        if (location == 16) return promptCode(card);           // 墓地永远是公开信息
        int position = card.has("position") ? card.get("position").getAsInt() : 0;
        boolean faceUp = (position & 0x05) != 0;               // 表侧攻击 0x1 / 表侧守备 0x4
        return faceUp ? promptCode(card) : 0;
    }

    int[] unselectSelectCodes() {
        return cardCodes("cards");
    }

    int[] unselectChosenCodes() {
        return cardCodes("unselectCards");
    }

    /** 26 号提示里两组卡的打包地址（可选 / 已选），客户端据此做发光标记。 */
    int[] unselectSelectAddresses() {
        return addressesFrom("cards");
    }

    int[] unselectChosenAddresses() {
        return addressesFrom("unselectCards");
    }

    private int[] addressesFrom(String key) {
        JsonObject prompt = currentPrompt();
        if (prompt == null || !prompt.has(key)) return new int[0];
        JsonArray list = prompt.getAsJsonArray(key);
        int[] packed = new int[list.size()];
        for (int i = 0; i < list.size(); i++) {
            JsonObject card = list.get(i).getAsJsonObject();
            packed[i] = (card.get("controller").getAsInt() << 16)
                    | (card.get("location").getAsInt() << 8)
                    | card.get("sequence").getAsInt();
        }
        return packed;
    }

    /** 19 = SelectPosition：可选表示形式（值就是应答）。 */
    /** 25 = SortCard：把卡组里的若干张按玩家想要的顺序排好。 */
    /** 14 = SelectOption：核心给若干选项（"选一个处理方式"），要选一个。 */
    boolean optionPrompt() {
        JsonObject prompt = currentPrompt();
        return lastFunction == 14 && prompt != null && prompt.has("options")
                && prompt.getAsJsonArray("options").size() > 0;
    }

    int[] optionValues() {
        JsonObject prompt = currentPrompt();
        if (prompt == null || !prompt.has("options")) return new int[0];
        JsonArray options = prompt.getAsJsonArray("options");
        int[] values = new int[options.size()];
        for (int i = 0; i < options.size(); i++) {
            values[i] = options.get(i).getAsJsonObject().get("index").getAsInt();
        }
        return values;
    }

    String[] optionLabels() {
        JsonObject prompt = currentPrompt();
        if (prompt == null || !prompt.has("options")) return new String[0];
        JsonArray options = prompt.getAsJsonArray("options");
        String[] labels = new String[options.size()];
        for (int i = 0; i < options.size(); i++) {
            JsonObject option = options.get(i).getAsJsonObject();
            String raw = option.has("label") ? option.get("label").getAsString() : "";
            labels[i] = raw.isEmpty() ? ("选项 " + (i + 1)) : raw;
        }
        return labels;
    }

    boolean sortPrompt() {
        JsonObject prompt = currentPrompt();
        return lastFunction == 25 && prompt != null && prompt.has("cards")
                && prompt.getAsJsonArray("cards").size() > 1;
    }

    int[] sortCodes() {
        return cardCodes("cards");
    }

    boolean positionPrompt() {
        JsonObject prompt = currentPrompt();
        return lastFunction == 19 && prompt != null && prompt.has("options");
    }

    int[] positionValues() {
        JsonObject prompt = currentPrompt();
        if (prompt == null || !prompt.has("options")) return new int[0];
        JsonArray options = prompt.getAsJsonArray("options");
        int[] values = new int[options.size()];
        for (int i = 0; i < options.size(); i++) {
            values[i] = options.get(i).getAsJsonObject().get("index").getAsInt();
        }
        return values;
    }

    /** 选卡提示里所有候选的打包地址，用来给客户端做发光标记。 */
    int[] selectableAddresses() {
        JsonObject prompt = currentPrompt();
        if (prompt == null || !prompt.has("cards")) return new int[0];
        JsonArray cards = prompt.getAsJsonArray("cards");
        int[] packed = new int[cards.size()];
        for (int i = 0; i < cards.size(); i++) {
            JsonObject card = cards.get(i).getAsJsonObject();
            packed[i] = (card.get("controller").getAsInt() << 16)
                    | (card.get("location").getAsInt() << 8)
                    | card.get("sequence").getAsInt();
        }
        return packed;
    }

    /** 选卡候选的卡号（按提示里的顺序，客户端选完回传的也是这个顺序的下标）。 */
    int[] selectCodes() {
        JsonObject prompt = currentPrompt();
        if (prompt == null || !prompt.has("cards")) return new int[0];
        JsonArray cards = prompt.getAsJsonArray("cards");
        int[] codes = new int[cards.size()];
        for (int i = 0; i < cards.size(); i++) {
            JsonObject card = cards.get(i).getAsJsonObject();
            codes[i] = visibleCode(card);
        }
        return codes;
    }

    /**
     * 选卡候选各自的"在哪"（{@code controller<<16 | location<<8 | sequence}），
     * 和 {@link #selectCodes()} 一一对应；界面悬停时显示它。
     */
    int[] selectAddrs() {
        JsonObject prompt = currentPrompt();
        if (prompt == null || !prompt.has("cards")) return new int[0];
        JsonArray cards = prompt.getAsJsonArray("cards");
        int[] addrs = new int[cards.size()];
        for (int i = 0; i < cards.size(); i++) {
            JsonObject card = cards.get(i).getAsJsonObject();
            int controller = card.has("controller") ? card.get("controller").getAsInt() : 0;
            int location = card.has("location") ? card.get("location").getAsInt() : 0;
            int sequence = card.has("sequence") ? card.get("sequence").getAsInt() : 0;
            addrs[i] = ((controller & 1) << 16) | ((location & 0xFF) << 8) | (sequence & 0xFF);
        }
        return addrs;
    }

    /** 这张卡在选卡候选里的下标；不在候选里返回 -1。 */
    int selectCardIndex(int controller, int location, int sequence) {
        JsonObject prompt = currentPrompt();
        if (prompt == null || !prompt.has("cards")) return -1;
        JsonArray cards = prompt.getAsJsonArray("cards");
        for (int i = 0; i < cards.size(); i++) {
            JsonObject card = cards.get(i).getAsJsonObject();
            if (card.get("controller").getAsInt() != controller) continue;
            if (card.get("location").getAsInt() != location) continue;
            if (card.get("sequence").getAsInt() != sequence) continue;
            return i;
        }
        return -1;
    }

    /** 选卡提示的"身份"：候选或张数一变就说明换了一个新的选择。 */
    String selectSignature() {
        JsonObject prompt = currentPrompt();
        if (prompt == null || !prompt.has("cards")) return "";
        return prompt.get("cards").getAsJsonArray().size() + ":" + selectMin() + ":" + selectMax();
    }

    /**
     * 需要发光的卡堆（打包地址，controller&lt;&lt;16 | location&lt;&lt;8 | sequence）：
     * 额外卡组里有能特殊召唤的卡，或墓地/除外区里有能发动的卡。
     */
    /** 当前在等的是不是"是/否"决定：12 = 诱发效果，13 = 一般确认；没有则 -1。 */
    int decisionFunction() {
        if (!waiting) return -1;
        return lastFunction == 12 || lastFunction == 13 ? lastFunction : -1;
    }

    /** 这个决定的身份（功能号 + 卡号 + 描述），用来避免每 tick 重复弹窗。 */
    String decisionSignature() {
        JsonObject prompt = currentPrompt();
        if (prompt == null) return "";
        return lastFunction + ":"
                + (prompt.has("code") ? prompt.get("code").getAsString() : "") + ":"
                + (prompt.has("description") ? prompt.get("description").getAsString() : "");
    }

    int decisionCardCode() {
        JsonObject prompt = currentPrompt();
        if (prompt == null) return 0;
        if (prompt.has("code")) return promptCode(prompt);
        // 兜底：老版本提示只把卡号写进 summary（"SelectEffectYN: card=12345, ..."）
        if (prompt.has("summary")) {
            String summary = prompt.get("summary").getAsString();
            int at = summary.indexOf("card=");
            if (at >= 0) {
                int end = at + 5;
                while (end < summary.length() && Character.isDigit(summary.charAt(end))) end++;
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
        JsonObject prompt = currentPrompt();
        if (prompt == null || !prompt.has("description")) return "";
        return prompt.get("description").getAsString();
    }

    /** 当前是不是"要不要接连锁、接哪张"的询问（有候选卡时才算）。 */
    boolean chainChoicePrompt() {
        return lastFunction == 16 && chainChoiceCount() > 0;
    }

    /**
     * 连锁窗口的候选张数：以核心给的 count 为准，和解析出的候选条数取较大值。
     * 这样即使某条候选没解析出来，玩家仍能看到窗口并作答，不会卡在对局里。
     */
    int chainChoiceCount() {
        JsonObject prompt = currentPrompt();
        if (prompt == null) return 0;
        int count = prompt.has("count") ? prompt.get("count").getAsInt() : 0;
        if (prompt.has("cards")) {
            count = Math.max(count, prompt.getAsJsonArray("cards").size());
        }
        return Math.max(0, count);
    }

    /** 连锁候选的卡号（客户端用来显示卡名）；解析不到的候选用 0 占位（显示卡背）。 */
    int[] chainChoiceCodes() {
        int count = chainChoiceCount();
        int[] codes = new int[count];
        JsonObject prompt = currentPrompt();
        if (prompt == null || !prompt.has("cards")) return codes;
        JsonArray cards = prompt.getAsJsonArray("cards");
        for (int i = 0; i < cards.size() && i < count; i++) {
            JsonObject card = cards.get(i).getAsJsonObject();
            codes[i] = visibleCode(card);
        }
        return codes;
    }

    /** 连锁窗口里有没有解析不出卡号的候选（用来提示玩家"仍可作答"）。 */
    boolean chainChoiceHasUnknown() {
        for (int code : chainChoiceCodes()) {
            if (code <= 0) return true;
        }
        return false;
    }

    /** 连锁窗口里候选卡各自的"在哪"（和 {@link #chainChoiceCodes()} 一一对应）。 */
    int[] chainChoiceAddrs() {
        return addressesFrom("cards");
    }

    boolean chainForced() {
        JsonObject prompt = currentPrompt();
        return prompt != null && prompt.has("forced") && prompt.get("forced").getAsBoolean();
    }

    int[] glowingPileZones() {
        JsonObject prompt = currentPrompt();
        if (prompt == null) return new int[0];
        java.util.Set<Integer> zones = new java.util.LinkedHashSet<>();
        collectZones(prompt, "spsummonCards", zones, true);
        collectZones(prompt, "activateCards", zones, false);
        int[] packed = new int[zones.size()];
        int i = 0;
        for (int zone : zones) packed[i++] = zone;
        return packed;
    }

    private static void collectZones(JsonObject prompt, String key,
                                     java.util.Set<Integer> zones, boolean extraOnly) {
        if (!prompt.has(key)) return;
        JsonArray list = prompt.getAsJsonArray(key);
        for (int i = 0; i < list.size(); i++) {
            JsonObject entry = list.get(i).getAsJsonObject();
            if (!entry.has("controller") || entry.get("controller").getAsInt() != 0) continue;
            int location = entry.has("location") ? entry.get("location").getAsInt() : -1;
            if (extraOnly && location != 64) continue;
            if (!extraOnly && location != 16 && location != 32) continue;
            zones.add(location);   // 堆用 (0, location, 0) 表示
        }
    }

    /** 某个卡堆里"现在可以特召/发动"的卡：返回 {sequence, code} 列表（只看自己的）。 */
    int[][] pileActions(int location) {
        JsonObject prompt = currentPrompt();
        if (prompt == null) return new int[0][];
        String key = location == 64 ? "spsummonCards" : "activateCards";
        if (!prompt.has(key)) return new int[0][];
        JsonArray list = prompt.getAsJsonArray(key);
        java.util.List<int[]> out = new java.util.ArrayList<>();
        for (int i = 0; i < list.size(); i++) {
            JsonObject entry = list.get(i).getAsJsonObject();
            if (entry.get("controller").getAsInt() != 0) continue;
            if (entry.get("location").getAsInt() != location) continue;
            out.add(new int[]{entry.get("sequence").getAsInt(), promptCode(entry)});
        }
        return out.toArray(new int[0][]);
    }

    /**
     * 某张卡当前可以做的行动，返回核心要的应答值；没有这个行动时返回 -1。
     * kind 的先后顺序就是优先级，可选值：
     * summon / spsummon / position / mset / sset / activate（主要阶段），attack / activate（战斗阶段）。
     */
    int promptAction(int controller, int location, int sequence, String... kinds) {
        if (!waiting || promptJson == null || promptJson.isEmpty()) return -1;
        JsonObject prompt;
        try {
            prompt = JsonParser.parseString(promptJson).getAsJsonObject();
        } catch (RuntimeException ex) {
            return -1;
        }
        for (String kind : kinds) {
            String key;
            int type;
            switch (kind) {
                case "summon" -> { key = "summonCards"; type = 0; }
                case "spsummon" -> { key = "spsummonCards"; type = 1; }
                case "position" -> { key = "positionCards"; type = 2; }
                case "mset" -> { key = "monsterSetCards"; type = 3; }
                case "sset" -> { key = "spellSetCards"; type = 4; }
                case "activate" -> { key = "activateCards"; type = 5; }
                case "attack" -> { key = "attackCards"; type = 1; }
                default -> { continue; }
            }
            if (lastFunction == IDLE_COMMAND) {
                if ("attack".equals(kind)) continue;
            } else if (lastFunction == BATTLE_COMMAND) {
                // 战斗提示里只有"攻击"和"发动"：两者都是 (序号<<16)|类型，发动是类型 0
                if (!"attack".equals(kind) && !"activate".equals(kind)) continue;
                if ("activate".equals(kind)) type = 0;
            } else {
                continue;
            }
            if (!prompt.has(key)) continue;
            JsonArray list = prompt.getAsJsonArray(key);
            for (int i = 0; i < list.size(); i++) {
                JsonObject entry = list.get(i).getAsJsonObject();
                if (!entry.has("controller") || entry.get("controller").getAsInt() != controller) continue;
                if (!entry.has("location") || entry.get("location").getAsInt() != location) continue;
                if (!entry.has("sequence") || entry.get("sequence").getAsInt() != sequence) continue;
                if (entry.has("code") && promptCode(entry) == 0) continue;
                return (i << 16) | type;
            }
        }
        return -1;
    }
    String handSummary() {
        return "p1 hand=" + handSize(0) + " p2 hand=" + handSize(1) + " turn=" + turnPlayer;
    }
    java.util.List<DuelVisualState.Event> drainEvents() { return visualState.drainEvents(); }
    int count(int player, int location) { return visualState.count(player & 1, location); }
    int initialMain(int player) { return initialMain[player & 1]; }
    int initialExtra(int player) { return initialExtra[player & 1]; }
    int drawCount(int player) { return initialMain(player) - count(player, 1); }
    int graveyardCount(int player) { return count(player, 0x10); }
    int banishedCount(int player) { return count(player, 0x20); }
    int extraMovedCount(int player) { return count(player, 0x40) - initialExtra(player); }
    String statsSummary() {
        return String.format("p1[deck=%d hand=%d grave=%d banish=%d extra=%d] p2[deck=%d hand=%d grave=%d banish=%d extra=%d]",
                count(0, 1), handSize(0), graveyardCount(0), banishedCount(0), count(0, 0x40),
                count(1, 1), handSize(1), graveyardCount(1), banishedCount(1), count(1, 0x40));
    }
    private void send(JsonObject command) throws IOException {
        if (input == null) throw new IOException("Duel Host stdin is not available");
        input.write(command.toString());
        input.newLine();
        input.flush();
    }

    private void readLoop() {
        try (BufferedReader reader = new BufferedReader(new InputStreamReader(process.getInputStream(), StandardCharsets.UTF_8))) {
            String line;
            while ((line = reader.readLine()) != null) {
                try {
                    JsonObject message = JsonParser.parseString(line).getAsJsonObject();
                    String type = message.has("type") ? message.get("type").getAsString() : "unknown";
                    switch (type) {
                        case "ready" -> {
                            ready = true;
                            lastError = "";
                        }
                        case "started" -> {
                            duelActive = true;
                            lastError = "";
                        }
                        case "state" -> {
                            waiting = message.has("waiting") && message.get("waiting").getAsBoolean();
                            if (message.has("function")) lastFunction = message.get("function").getAsInt();
                            // 提示事件紧跟 waiting=true 之后到达，这里先清干净，避免一 tick 的陈旧窗口
                            clearPhaseOptions();
                        }
                        case "prompt" -> {
                            promptJson = message.toString();
                            promptSummary = message.has("summary")
                                    ? message.get("summary").getAsString() : "";
                            promptResponseKind = message.has("responseKind")
                                    ? message.get("responseKind").getAsString() : "integer";
                            if (message.has("function")) lastFunction = message.get("function").getAsInt();
                            clearPhaseOptions();
                            if (message.has("player")) promptPlayer = message.get("player").getAsInt();
                            promptToBattle = flag(message, "toBattle");
                            promptToEnd = flag(message, "toEnd");
                            promptToMain2 = flag(message, "toMain2");
                        }
                        case "message" -> {
                            if (message.has("function")) lastFunction = message.get("function").getAsInt();
                            if (message.has("name")) lastName = message.get("name").getAsString();
                            if (message.has("winner") && !message.get("winner").isJsonNull()) {
                                winner = message.get("winner").getAsInt();
                            }
                            if (message.has("hex")) {
                                applyStats(lastName, message.get("hex").getAsString());
                            }
                        }
                        case "ended" -> {
                            ended = true;
                            waiting = false;
                            clearPhaseOptions();
                        }
                        case "error" -> {
                            lastError = message.has("message")
                                    ? message.get("message").getAsString() : "unknown error";
                            notice = "对局出错：" + lastError;
                        }
                        // 应答被核心判非法（RETRY）：说一句，否则玩家只会看到"点了没反应"
                        case "retry" -> {
                            long now = System.currentTimeMillis();
                            if (now - lastRetryNoticeAt > 1500) {
                                lastRetryNoticeAt = now;
                                notice = "核心判上次应答非法，会重新问一次，请再选一遍";
                            }
                        }
                        default -> { }
                    }
                    logger.info("Duel Host {}: {}", type, line);
                } catch (RuntimeException ex) {
                    logger.warn("Duel Host emitted non-JSON output: {}", line);
                }
            }
        } catch (IOException ex) {
            if (running.get()) logger.warn("Duel Host output ended: {}", ex.getMessage());
        } finally {
            running.set(false);
        }
    }

    @Override
    public synchronized void close() {
        running.set(false);
        if (input != null) {
            try { input.close(); } catch (IOException ignored) { }
            input = null;
        }
        if (process != null) {
            try {
                if (!process.waitFor(2, java.util.concurrent.TimeUnit.SECONDS)) {
                    process.destroy();
                    if (!process.waitFor(2, java.util.concurrent.TimeUnit.SECONDS)) {
                        process.destroyForcibly();
                    }
                }
            } catch (InterruptedException ex) {
                Thread.currentThread().interrupt();
                process.destroyForcibly();
            }
            process = null;
        }
        ready = false;
        waiting = false;
        ended = false;
        duelActive = false;
        lastFunction = -1;
        lastName = "";
        winner = null;
        lastError = "";
        promptSummary = "";
        promptResponseKind = "";
        promptJson = "";
        clearPhaseOptions();
    }
}
