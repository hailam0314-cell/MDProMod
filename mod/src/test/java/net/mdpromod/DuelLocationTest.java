package net.mdpromod;

/** 「卡在哪」文案的回归测试。 */
public final class DuelLocationTest {
    private static int assertions;

    private static int pack(int controller, int location, int sequence) {
        return (controller << 16) | (location << 8) | sequence;
    }

    private static void equals(Object actual, Object expected, String message) {
        assertions++;
        if (expected == null ? actual != null : !expected.equals(actual)) {
            throw new AssertionError(message + "：期望 " + expected + "，实际 " + actual);
        }
    }

    public static void main(String[] args) {
        equals(DuelLocation.label(pack(0, 4, 0)), "自己怪兽区1", "自己的怪兽区");
        equals(DuelLocation.label(pack(1, 4, 3)), "对方怪兽区4", "对方的怪兽区");
        equals(DuelLocation.label(pack(0, 4, 5)), "额外怪兽区", "额外怪兽区");
        equals(DuelLocation.label(pack(1, 8, 2)), "对方魔陷区3", "魔陷区");
        equals(DuelLocation.label(pack(0, 8, 5)), "自己场地魔法区", "场地魔法区");
        equals(DuelLocation.label(pack(0, 1, 0)), "自己卡组", "卡组");
        equals(DuelLocation.label(pack(1, 2, 1)), "对方手牌", "手牌");
        equals(DuelLocation.label(pack(0, 16, 4)), "自己墓地", "墓地");
        equals(DuelLocation.label(pack(1, 32, 0)), "对方除外区", "除外区");
        equals(DuelLocation.label(pack(0, 64, 0)), "自己额外卡组", "额外卡组");
        equals(DuelLocation.label(0), "自己区域 0-0", "未知区域有兜底");
        equals(DuelLocation.tooltipLine(pack(0, 16, 0)), "位置：自己墓地", "悬停那一行");

        System.out.println("DuelLocationTest passed " + assertions + " assertions");
    }
}
