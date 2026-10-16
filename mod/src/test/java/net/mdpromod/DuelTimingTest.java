package net.mdpromod;

/** 提示里"时机"文案的回归测试：阶段名、谁的回合、连锁来源。 */
public final class DuelTimingTest {
    private static int assertions;

    private static void check(boolean value, String message) {
        assertions++;
        if (!value) throw new AssertionError(message);
    }

    private static void equals(Object actual, Object expected, String message) {
        assertions++;
        if (expected == null ? actual != null : !expected.equals(actual)) {
            throw new AssertionError(message + "：期望 " + expected + "，实际 " + actual);
        }
    }

    public static void main(String[] args) {
        equals(DuelTiming.phaseLabel("DP"), "抽卡阶段", "抽卡阶段");
        equals(DuelTiming.phaseLabel("SP"), "准备阶段", "准备阶段");
        equals(DuelTiming.phaseLabel("M1"), "主要阶段 1", "主要阶段 1");
        equals(DuelTiming.phaseLabel("BP"), "战斗阶段", "战斗阶段");
        equals(DuelTiming.phaseLabel("M2"), "主要阶段 2", "主要阶段 2");
        equals(DuelTiming.phaseLabel("EP"), "结束阶段", "结束阶段");
        equals(DuelTiming.phaseLabel("sp"), "准备阶段", "阶段码大小写不敏感");
        equals(DuelTiming.phaseLabel(null), "抽卡阶段", "空阶段码有兜底");

        equals(DuelTiming.phaseEntry(true, "SP"), "对方进入准备阶段", "对方进入准备阶段");
        equals(DuelTiming.phaseEntry(false, "BP"), "自己进入战斗阶段", "自己进入战斗阶段");

        equals(DuelTiming.chainTrigger("青眼白龙"), "「青眼白龙」的效果发动了", "连锁来源有卡名");
        equals(DuelTiming.chainTrigger(""), "有卡发动了效果", "连锁来源没卡名");
        equals(DuelTiming.chainQuestion("真红眼暗钢龙"),
                "「真红眼暗钢龙」的效果发动了，是否连锁？", "连锁问句");

        // 还没接上连锁时问的是"要不要发动"，不能说成"已经发动了"（转阶段那类窗口）
        equals(DuelTiming.openQuestion(new String[]{"青眼白龙"}),
                "「青眼白龙」的效果可以发动，是否发动？", "发动问句（一张候选，知道卡名）");
        equals(DuelTiming.openQuestion(new String[]{"", "", ""}),
                "有 3 张卡可以发动效果，是否发动？", "发动问句（多张候选只说张数）");
        equals(DuelTiming.openQuestion(new String[]{"青眼白龙", "真红眼暗钢龙"}),
                "「青眼白龙」的效果可以发动，是否发动？", "发动问句（多张里第一张有名字时就点名）");
        equals(DuelTiming.openQuestion(new String[]{""}),
                "可以发动效果，是否发动？", "发动问句（一张候选但没名字）");
        equals(DuelTiming.openQuestion(null), "可以发动效果，是否发动？", "候选为空也有兜底");
        equals(DuelTiming.passQuestion(), "没有可以发动或连锁的卡，是否放弃优先权？",
                "全时点下的空连锁窗口问的是放不放优先权");

        equals(DuelTiming.activateQuestion(true, "SP", "青眼白龙"),
                "对方进入准备阶段，是否发动「青眼白龙」的效果？", "发动问句（对方回合）");
        equals(DuelTiming.activateQuestion(false, "M1", null),
                "自己进入主要阶段 1，是否发动效果？", "发动问句（没卡名）");

        System.out.println("DuelTimingTest passed " + assertions + " assertions");
    }
}
