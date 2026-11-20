package net.mdpromod;

final class CardLabels {
    static final String[] ATTRIBUTE_NAMES = new String[]{"光", "暗", "地", "水", "炎", "风", "神"};
    static final int[] ATTRIBUTE_BITS = new int[]{16, 32, 1, 2, 4, 8, 64};
    static final String[] RACE_NAMES = new String[]{
        "战士",
        "魔法师",
        "天使",
        "恶魔",
        "不死",
        "机械",
        "水",
        "炎",
        "岩石",
        "鸟兽",
        "植物",
        "昆虫",
        "雷",
        "龙",
        "兽",
        "兽战士",
        "恐龙",
        "鱼",
        "海龙",
        "爬虫",
        "念动力",
        "幻神兽",
        "创造神",
        "幻龙",
        "电子界",
        "幻想魔"
    };
    static final int[] RACE_BITS = new int[]{
        1,
        2,
        4,
        8,
        16,
        32,
        64,
        128,
        256,
        512,
        1024,
        2048,
        4096,
        8192,
        16384,
        32768,
        65536,
        131072,
        262144,
        524288,
        1048576,
        2097152,
        4194304,
        8388608,
        16777216,
        33554432
    };

    private CardLabels() {
    }

    static String attributeName(int bit) {
        for (int i = 0; i < ATTRIBUTE_BITS.length; i++) {
            if (ATTRIBUTE_BITS[i] == bit) {
                return ATTRIBUTE_NAMES[i];
            }
        }

        return "属性 0x" + Integer.toHexString(bit);
    }

    static String raceName(int bit) {
        for (int i = 0; i < RACE_BITS.length; i++) {
            if (RACE_BITS[i] == bit) {
                return RACE_NAMES[i];
            }
        }

        return "种族 0x" + Integer.toHexString(bit);
    }
}
