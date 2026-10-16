package net.mdpromod;

/**
 * 时点开关：右下角确认键弹出的那个「时点」按钮切换的三档，
 * 和 MD / MDPro3 的「発動確認」是同一个东西。
 *
 * <ul>
 *   <li>{@link #ALL} 全时点：每个优先权窗口都停下来问，连"没有可接的卡"的空连锁窗口也问；</li>
 *   <li>{@link #DEFAULT} 默认：有卡可接 / 有可选的发动才问（原来的行为）；</li>
 *   <li>{@link #OFF} 关闭：可选的都不问——连锁窗口直接放弃、可选效果直接答"不发动"，
 *       只有必须作答的窗口（阶段推进、真正的选择）才停下来。</li>
 * </ul>
 *
 * <p>纯数据 + 纯函数，可离线测试；{@link #hostKey()} 就是发给决斗宿主的模式名。
 */
enum DuelTimingMode {
    ALL("all", "全时点", "每个优先权窗口都停下来问"),
    DEFAULT("default", "默认时点", "有可发动/可连锁的东西才问"),
    OFF("off", "关闭时点", "可选的发动和连锁都不再询问");

    /** 按钮循环的先后：全时点 → 默认 → 关闭 → 全时点。 */
    static final DuelTimingMode[] CYCLE = {ALL, DEFAULT, OFF};

    private final String hostKey;
    private final String label;
    private final String detail;

    DuelTimingMode(String hostKey, String label, String detail) {
        this.hostKey = hostKey;
        this.label = label;
        this.detail = detail;
    }

    String hostKey() {
        return hostKey;
    }

    String label() {
        return label;
    }

    String detail() {
        return detail;
    }

    /** 「时点：关闭时点」——按钮上那行字。 */
    String buttonText() {
        return "时点：" + label;
    }

    DuelTimingMode next() {
        return CYCLE[(ordinal() + 1) % CYCLE.length];
    }

    static DuelTimingMode byHostKey(String key) {
        if (key != null) {
            String trimmed = key.trim().toLowerCase(java.util.Locale.ROOT);
            for (DuelTimingMode mode : values()) {
                if (mode.hostKey.equals(trimmed)) return mode;
            }
        }
        return DEFAULT;
    }

    /** 实体数据里存的是序号；越界或者空值一律当默认。 */
    static DuelTimingMode byOrdinal(int value) {
        return value >= 0 && value < CYCLE.length ? CYCLE[value] : DEFAULT;
    }
}
