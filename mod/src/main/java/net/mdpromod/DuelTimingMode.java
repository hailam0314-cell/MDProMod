package net.mdpromod;

import java.util.Locale;

enum DuelTimingMode {
    ALL("all", "全时点", "每个优先权窗口都停下来问"),
    DEFAULT("default", "默认时点", "有可发动/可连锁的东西才问"),
    OFF("off", "关闭时点", "可选的发动和连锁都不再询问");

    static final DuelTimingMode[] CYCLE = new DuelTimingMode[]{ALL, DEFAULT, OFF};
    private final String hostKey;
    private final String label;
    private final String detail;

    DuelTimingMode(String hostKey, String label, String detail) {
        this.hostKey = hostKey;
        this.label = label;
        this.detail = detail;
    }

    String hostKey() {
        return this.hostKey;
    }

    String label() {
        return this.label;
    }

    String detail() {
        return this.detail;
    }

    String buttonText() {
        return "时点：" + this.label;
    }

    DuelTimingMode next() {
        return CYCLE[(this.ordinal() + 1) % CYCLE.length];
    }

    static DuelTimingMode byHostKey(String key) {
        if (key != null) {
            String trimmed = key.trim().toLowerCase(Locale.ROOT);

            for (DuelTimingMode mode : values()) {
                if (mode.hostKey.equals(trimmed)) {
                    return mode;
                }
            }
        }

        return DEFAULT;
    }

    static DuelTimingMode byOrdinal(int value) {
        return value >= 0 && value < CYCLE.length ? CYCLE[value] : DEFAULT;
    }
}
