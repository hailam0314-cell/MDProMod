package net.mdpromod;

/** 时点开关三档的回归测试：发宿主的模式名、按钮上的字、循环顺序、序号兜底。 */
public final class DuelTimingModeTest {
    private static int assertions;

    public static void main(String[] args) {
        // 这几个字符串就是宿主的模式名（那边 --timing-selftest 断言 same），别随手改
        equals(DuelTimingMode.ALL.hostKey(), "all", "全时点的模式名");
        equals(DuelTimingMode.DEFAULT.hostKey(), "default", "默认的模式名");
        equals(DuelTimingMode.OFF.hostKey(), "off", "关闭的模式名");

        equals(DuelTimingMode.ALL.label(), "全时点", "全时点的标题");
        equals(DuelTimingMode.DEFAULT.buttonText(), "时点：默认时点", "按钮上的字");
        check(!DuelTimingMode.OFF.detail().isEmpty(), "每一档都要有一句说明");

        equals(DuelTimingMode.byHostKey("all"), DuelTimingMode.ALL, "按模式名反查");
        equals(DuelTimingMode.byHostKey(" OFF "), DuelTimingMode.OFF, "大小写和空格都容错");
        equals(DuelTimingMode.byHostKey("乱写"), DuelTimingMode.DEFAULT, "认不出来当默认");
        equals(DuelTimingMode.byHostKey(null), DuelTimingMode.DEFAULT, "null 当默认");

        equals(DuelTimingMode.byOrdinal(0), DuelTimingMode.ALL, "序号 → 全时点");
        equals(DuelTimingMode.byOrdinal(1), DuelTimingMode.DEFAULT, "序号 → 默认");
        equals(DuelTimingMode.byOrdinal(2), DuelTimingMode.OFF, "序号 → 关闭");
        equals(DuelTimingMode.byOrdinal(-1), DuelTimingMode.DEFAULT, "越界当默认");
        equals(DuelTimingMode.byOrdinal(99), DuelTimingMode.DEFAULT, "越界当默认");

        equals(DuelTimingMode.ALL.next(), DuelTimingMode.DEFAULT, "循环：全时点 → 默认");
        equals(DuelTimingMode.DEFAULT.next(), DuelTimingMode.OFF, "循环：默认 → 关闭");
        equals(DuelTimingMode.OFF.next(), DuelTimingMode.ALL, "循环：关闭 → 全时点（转一圈回来）");
        for (DuelTimingMode mode : DuelTimingMode.values()) {
            equals(DuelTimingMode.byHostKey(mode.hostKey()), mode, "模式名自洽：" + mode);
            equals(DuelTimingMode.byOrdinal(mode.ordinal()), mode, "序号自洽：" + mode);
        }

        System.out.println("DuelTimingModeTest passed " + assertions + " assertions");
    }

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
}
