package net.mdpromod;

import java.util.Collections;
import java.util.LinkedHashMap;
import java.util.Map;

public final class DuelMoment {
    public static final long TIMING_NORMAL_SUMMON = 64L;
    public static final long TIMING_SPECIAL_SUMMON = 128L;
    public static final long TIMING_FLIP_SUMMON = 256L;
    public static final long TIMING_END_MAIN = 4L;
    public static final long TIMING_END_BATTLE_STEP = 67108864L;
    public static final long TIMING_BATTLE_PHASE_END = 16L;
    public static final long TIMING_END_PHASE = 32L;
    public static final long TIMING_DRAW_PHASE = 1L;
    public static final long TIMING_STANDBY_PHASE = 2L;
    public static final long TIMING_DAMAGE_STEP_START = 8192L;
    public static final long TIMING_DAMAGE_CALCULATION = 16384L;
    public static final long TIMING_DESTROY = 524288L;
    public static final long TIMING_SEND_GRAVE = 8388608L;
    public static final long TIMING_ATTACK = 4096L;
    public static final long TIMING_CHAIN = 32768L;
    public static final long TIMING_BATTLED = 134217728L;
    public static final long TIMING_BATTLE_START = 8L;
    public static final long TIMING_MONSTER_SET = 512L;
    public static final long TIMING_SPELL_SET = 1024L;
    public static final long TIMING_POSITION_CHANGE = 2048L;
    public static final long TIMING_DRAW = 65536L;
    public static final long TIMING_DAMAGE = 131072L;
    public static final long TIMING_RECOVER = 262144L;
    public static final long TIMING_REMOVE = 1048576L;
    public static final long TIMING_TO_HAND = 2097152L;
    public static final long TIMING_TO_DECK = 4194304L;
    public static final long TIMING_BATTLE_PHASE = 16777216L;
    public static final long TIMING_EQUIP = 33554432L;
    public static final Map<Long, String> TIMING_TEXT;
    private static final long[] PRIORITY;

    private DuelMoment() {
    }

    public static String describe(int hintA, int hintB, String cardName, String action) {
        return describe(Integer.toUnsignedLong(hintA) | Integer.toUnsignedLong(hintB), cardName, action);
    }

    public static String describe(long hintA, long hintB, String cardName, String action) {
        return describe(hintA | hintB, cardName, action);
    }

    public static String describe(long hints, String cardName, String action) {
        if (hints == 0L) {
            return "";
        }

        for (long bit : PRIORITY) {
            if ((hints & bit) != 0L) {
                String text = TIMING_TEXT.get(bit);
                if (hasCardContext(bit) && cardName != null && !cardName.isBlank()) {
                    return "「" + cardName.trim() + "」" + text;
                }

                return text;
            }
        }

        return "";
    }

    public static String describe(int hintA, int hintB, String cardName) {
        return describe(hintA, hintB, cardName, null);
    }

    public static String describe(long hintA, long hintB, String cardName) {
        return describe(hintA, hintB, cardName, null);
    }

    public static String timingText(long hints) {
        for (long bit : PRIORITY) {
            if ((hints & bit) != 0L) {
                return TIMING_TEXT.get(bit);
            }
        }

        return "";
    }

    private static boolean hasCardContext(long bit) {
        return bit == 64L || bit == 128L || bit == 256L || bit == 4096L || bit == 524288L || bit == 8388608L;
    }

    static {
        LinkedHashMap<Long, String> values = new LinkedHashMap<>();
        values.put(64L, "通常召唤成功时");
        values.put(128L, "特殊召唤成功时");
        values.put(256L, "反转召唤成功时");
        values.put(16384L, "伤害计算时");
        values.put(134217728L, "伤害计算后");
        values.put(8192L, "伤害步骤中");
        values.put(4096L, "攻击宣言时");
        values.put(524288L, "被破坏时");
        values.put(8388608L, "送去墓地时");
        values.put(1048576L, "除外时");
        values.put(2097152L, "加入手牌时");
        values.put(4194304L, "返回卡组时");
        values.put(65536L, "抽卡时");
        values.put(131072L, "受到伤害时");
        values.put(262144L, "回复生命值时");
        values.put(33554432L, "装备时");
        values.put(512L, "怪兽盖放时");
        values.put(1024L, "魔法·陷阱盖放时");
        values.put(2048L, "表示形式变更时");
        values.put(4L, "即将结束主要阶段");
        values.put(67108864L, "战斗步骤结束时");
        values.put(16L, "战斗阶段结束时");
        values.put(8L, "战斗阶段开始时");
        values.put(32L, "结束阶段中");
        values.put(1L, "抽卡阶段中");
        values.put(2L, "准备阶段中");
        values.put(16777216L, "战斗阶段中");
        values.put(32768L, "连锁处理结束时");
        TIMING_TEXT = Collections.unmodifiableMap(values);
        PRIORITY = new long[values.size()];
        int i = 0;

        for (long bit : values.keySet()) {
            PRIORITY[i++] = bit;
        }
    }
}
