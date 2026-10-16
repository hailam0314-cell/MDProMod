package net.mdpromod;

import java.util.List;

/**
 * 卡堆界面排序的回归测试：能召唤/发动的排最前面、一张都不漏。
 * （"第 4 张之后点不到"就是漏了，所以这里专门盯住"每张都出现且有且只有一次"。）
 */
public final class PileOrderTest {
    private static int assertions;

    public static void main(String[] args) {
        actionsGoFirst();
        missingSequenceStillShown();
        deduplicatesAndKeepsOrder();
        emptyInputs();
        System.out.println("PileOrderTest passed " + assertions + " assertions");
    }

    private static void actionsGoFirst() {
        // 卡堆：0..5；能召唤的是 4、1、3
        int[] all = {101, 102, 103, 104, 105, 106};
        List<PileOrder.Entry> entries = PileOrder.sorted(new int[]{4, 1, 3},
                new int[]{105, 102, 104}, all);
        check(entries.size() == all.length, "一张不多一张不少：" + entries.size());
        check(entries.get(0).sequence() == 1 && entries.get(1).sequence() == 3
                        && entries.get(2).sequence() == 4,
                "能召唤的排最前面，并且按卡堆位置从前到后：" + show(entries));
        check(entries.get(0).code() == 102 && entries.get(1).code() == 104
                        && entries.get(2).code() == 105, "卡号和序号对得上");
        check(entries.get(3).sequence() == 0 && entries.get(4).sequence() == 2
                        && entries.get(5).sequence() == 5, "其余按原顺序跟在后面：" + show(entries));
    }

    /** 核心给的序号如果跑到清单外面（卡堆顺序变了），那张卡也得留在界面上。 */
    private static void missingSequenceStillShown() {
        int[] all = {201, 202};
        List<PileOrder.Entry> entries = PileOrder.sorted(new int[]{7}, new int[]{999}, all);
        check(entries.size() == 3, "对不上的那张也留一行：" + entries.size());
        check(entries.get(0).sequence() == 7 && entries.get(0).code() == 999,
                "用动作里的卡号兜底：" + show(entries));
        check(entries.get(1).sequence() == 0 && entries.get(2).sequence() == 1, "其余照旧");
    }

    private static void deduplicatesAndKeepsOrder() {
        int[] all = {301, 302, 303};
        List<PileOrder.Entry> entries = PileOrder.sorted(new int[]{2, 2, 0},
                new int[]{303, 303, 301}, all);
        check(entries.size() == 3, "重复的动作只留一次：" + show(entries));
        check(entries.get(0).sequence() == 0 && entries.get(1).sequence() == 2
                        && entries.get(2).sequence() == 1,
                "顺序：先动作（0、2），再剩下的（1）：" + show(entries));
    }

    private static void emptyInputs() {
        check(PileOrder.sorted(null, null, null).isEmpty(), "全空不炸");
        check(PileOrder.sorted(new int[0], new int[0], new int[]{401, 402}).size() == 2,
                "没有动作时就是原样全部卡");
        check(PileOrder.sorted(new int[0], new int[0], new int[0]).isEmpty(), "空卡堆");
    }

    private static String show(List<PileOrder.Entry> entries) {
        StringBuilder text = new StringBuilder();
        for (PileOrder.Entry entry : entries) {
            if (text.length() > 0) text.append(' ');
            text.append(entry.sequence()).append('/').append(entry.code());
        }
        return text.toString();
    }

    private static void check(boolean condition, String message) {
        assertions++;
        if (!condition) throw new AssertionError(message);
    }
}
