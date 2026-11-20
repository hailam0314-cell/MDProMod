package net.mdpromod;

import java.util.Locale;

final class DuelTiming {
    private DuelTiming() {
    }

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

    static String phaseEntry(boolean opponentTurn, String phaseCode) {
        return (opponentTurn ? "对方" : "自己") + "进入" + phaseLabel(phaseCode);
    }

    static String chainTrigger(String cardName) {
        return cardName != null && !cardName.isBlank() ? "「" + cardName + "」的效果发动了" : "有卡发动了效果";
    }

    static String chainQuestion(String cardName) {
        return chainTrigger(cardName) + "，是否连锁？";
    }

    static String openQuestion(String[] candidateNames) {
        String single = candidateNames != null && candidateNames.length == 1 ? firstName(candidateNames) : null;
        if (single != null) {
            return "「" + single + "」的效果可以发动，是否发动？";
        }

        int count = candidateNames == null ? 0 : candidateNames.length;
        return count > 1 ? "有 " + count + " 个可发动的效果，请选择。" : "可以发动效果，是否发动？";
    }

    private static String firstName(String[] names) {
        if (names == null) {
            return null;
        }

        for (String name : names) {
            if (name != null && !name.isBlank()) {
                return name;
            }
        }

        return null;
    }

    static String passQuestion() {
        return "没有可以发动或连锁的卡，是否放弃优先权？";
    }

    static String activateQuestion(boolean opponentTurn, String phaseCode, String cardName) {
        String head = phaseEntry(opponentTurn, phaseCode);
        return cardName != null && !cardName.isBlank() ? head + "，是否发动「" + cardName + "」的效果？" : head + "，是否发动效果？";
    }
}
