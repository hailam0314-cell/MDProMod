package net.mdpromod;

import java.util.Locale;

/**
 * 提示里的"时机"文案：把当前的阶段 / 回合 / 连锁来源说成一句人话，
 * 让弹窗能写清楚「什么时候、在问什么」——和 MD 的提示一个意思。
 *
 * <p>例：
 * <pre>
 *   对方进入准备阶段，是否发动「青眼白龙」的效果？
 *   「真红眼暗钢龙」的效果发动了，是否连锁？
 * </pre>
 *
 * 纯函数，不依赖 Minecraft，可离线测试。
 */
final class DuelTiming {

    private DuelTiming() {
    }

    /** 2 字母阶段码 → 中文名（和场地控制条用的是同一套码）。 */
    static String phaseLabel(String code) {
        String normalized = code == null ? "" : code.trim().toUpperCase(Locale.ROOT);
        return switch (normalized) {
            case "DP" -> "抽卡阶段";
            case "SP" -> "准备阶段";
            case "M1" -> "主要阶段 1";
            case "BP" -> "战斗阶段";
            case "M2" -> "主要阶段 2";
            case "EP" -> "结束阶段";
            default -> "抽卡阶段";
        };
    }

    /** 「对方进入准备阶段」 */
    static String phaseEntry(boolean opponentTurn, String phaseCode) {
        return (opponentTurn ? "对方" : "自己") + "进入" + phaseLabel(phaseCode);
    }

    /** 「「青眼白龙」的效果发动了」；拿不到卡名时说"有卡发动了效果"。 */
    static String chainTrigger(String cardName) {
        if (cardName == null || cardName.isBlank()) return "有卡发动了效果";
        return "「" + cardName + "」的效果发动了";
    }

    /** 连锁询问：「〈卡〉的效果发动了，是否连锁？」 */
    static String chainQuestion(String cardName) {
        return chainTrigger(cardName) + "，是否连锁？";
    }

    /**
     * 还没接上连锁时的询问——核心在问"要不要发动效果"（转阶段时的诱发效果、
     * 或者自己回合的"可以发动"窗口），这时候什么都还没发动，不能说"已经发动了"。
     *
     * <p>只有一张候选而且知道卡名就直接点名，多张只说张数（候选清单就在窗口里）。
     */
    static String openQuestion(String[] candidateNames) {
        String single = firstName(candidateNames);
        if (single != null) return "「" + single + "」的效果可以发动，是否发动？";
        int count = candidateNames == null ? 0 : candidateNames.length;
        if (count > 1) return "有 " + count + " 张卡可以发动效果，是否发动？";
        return "可以发动效果，是否发动？";
    }

    private static String firstName(String[] names) {
        if (names == null) return null;
        for (String name : names) {
            if (name != null && !name.isBlank()) return name;
        }
        return null;
    }

    /** 空连锁窗口（时点=全时点时会停下来问）：没有能接的东西，只是在问"放不放优先权"。 */
    static String passQuestion() {
        return "没有可以发动或连锁的卡，是否放弃优先权？";
    }

    /** 发动询问：「〈谁〉进入〈阶段〉，是否发动「〈卡〉」的效果？」 */
    static String activateQuestion(boolean opponentTurn, String phaseCode, String cardName) {
        String head = phaseEntry(opponentTurn, phaseCode);
        if (cardName == null || cardName.isBlank()) return head + "，是否发动效果？";
        return head + "，是否发动「" + cardName + "」的效果？";
    }
}
