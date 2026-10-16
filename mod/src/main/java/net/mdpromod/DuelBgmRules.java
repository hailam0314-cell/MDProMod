package net.mdpromod;

import java.util.List;
import java.util.Locale;

/**
 * 服务端与客户端共用的纯规则：**音乐时机**、各时机的默认曲目、本地选择的归一化。
 *
 * 时机（moment）就是"什么时候该放哪首"：
 *
 * <ul>
 *   <li>{@link #MOMENT_DUEL} —— 对局常驻 BGM，开局演出结束后起播。</li>
 *   <li>{@link #MOMENT_KEYCARD} —— 王牌怪兽登场（cut-in）期间临时接管，放完回到常驻。</li>
 *   <li>{@link #MOMENT_CLIMAX} —— 本地玩家 LP 低于 {@link #CLIMAX_LP} 时切高潮曲
 *       （对应原版 {@code AudioManager.PlayBgmClimax()}）。</li>
 * </ul>
 *
 * 优先级：王牌登场 &gt; 低 LP 高潮 &gt; 决斗。
 */
final class DuelBgmRules {

    /** 服务端发的"该起播 BGM 了"这一条 cue 的键。 */
    static final String CUE_KEY = "BGM_DUEL";
    /** 开局演出结束后留一点释放时间再起播。 */
    static final int START_DELAY_MS = 250;
    /** LP 降到这个值以下切「高潮」曲目（8000 的 25%）。 */
    static final int CLIMAX_LP = 2000;

    /**
     * 和 MD 原版 {@code AudioManager.bgmState} 完全一致的**只前进不后退**状态机：
     * 0 常规 → 1 王牌 → 2 高潮。
     *
     * <p>原版的两条守卫（`MonsterCutin.Play` / `OcgCore` 里的调用）：
     * <pre>
     * PlayBgmKeyCard(): if (bgmState &gt; 0) return;   // 已经进过王牌或高潮就不再重放
     * PlayBgmClimax():  if (bgmState == 2) return;  // 已经高潮就不再重放
     * </pre>
     * 所以一场决斗里：王牌只切一次（第二只王牌不再动音乐），
     * 高潮可以把王牌顶掉，而且一旦高潮就再也回不去。状态在开局演出结束时复位。
     */
    static final int STAGE_NORMAL = 0;
    static final int STAGE_KEYCARD = 1;
    static final int STAGE_CLIMAX = 2;

    static final String MOMENT_DUEL = "duel";
    static final String MOMENT_KEYCARD = "keycard";
    static final String MOMENT_CLIMAX = "climax";
    static final List<String> MOMENTS = List.of(MOMENT_DUEL, MOMENT_KEYCARD, MOMENT_CLIMAX);

    /** 默认曲目：各取该家族的第一首。 */
    static final String DEFAULT_KEY = "BGM_DUEL_NORMAL_01";

    private DuelBgmRules() {
    }

    static boolean isMoment(String moment) {
        return moment != null && MOMENTS.contains(moment);
    }

    static String momentLabel(String moment) {
        if (MOMENT_KEYCARD.equals(moment)) return "王牌登场";
        if (MOMENT_CLIMAX.equals(moment)) return "低 LP 高潮";
        return "决斗";
    }

    static String defaultTrack(String moment) {
        if (MOMENT_KEYCARD.equals(moment)) return "BGM_DUEL_KEYCARD_01";
        if (MOMENT_CLIMAX.equals(moment)) return "BGM_DUEL_CLIMAX_01";
        return DEFAULT_KEY;
    }

    static boolean isCue(String key) {
        return key != null && CUE_KEY.equalsIgnoreCase(key.trim());
    }

    /**
     * 推进状态机。{@code cutinStarted} 只在"王牌登场演出**刚开始**的那一下"为真（上升沿）。
     * 返回新的阶段值。
     */
    static int advanceBgmStage(int stage, boolean cutinStarted, boolean lowLifePoints) {
        if (stage == STAGE_NORMAL && cutinStarted) stage = STAGE_KEYCARD;
        if (stage != STAGE_CLIMAX && lowLifePoints) stage = STAGE_CLIMAX;
        return stage;
    }

    /** 空、off、none 表示"这个时机不播放"；非法值回落到默认曲目。 */
    static String localChoice(String value) {
        if (value == null) return DEFAULT_KEY;
        String choice = value.trim();
        if (choice.isEmpty() || choice.equalsIgnoreCase("off") || choice.equalsIgnoreCase("none")) return "";
        if (!choice.matches("[a-zA-Z0-9_./-]+")) return DEFAULT_KEY;
        return choice.toUpperCase(Locale.ROOT);
    }
}
