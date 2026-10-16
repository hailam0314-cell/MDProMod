package net.mdpromod;

/**
 * 卡面标记的判定与文案（纯函数，可离线测试）：世界里的边框和选卡界面上的边框
 * 用的是同一套地址和同一套颜色，别在两处各写一份。
 *
 * <p>地址打包格式和落点提示一致：{@code controller << 16 | location << 8 | sequence}。
 */
final class DuelMarks {
    /** 连锁里发动。 */
    static final int CHAIN = 1;
    /** 被连锁中其他卡指定为对象。 */
    static final int TARGET = 2;
    /** 橙红 / 青蓝，和世界里的边框同一个色。 */
    static final int CHAIN_RGB = 0xFF6E37;
    static final int TARGET_RGB = 0x46B4FF;

    private DuelMarks() {
    }

    static boolean contains(int[] addresses, int packed) {
        if (addresses == null) return false;
        for (int address : addresses) {
            if (address == packed) return true;
        }
        return false;
    }

    /** 某个地址上的标记位；0 表示这张卡没有标记。 */
    static int at(int[] chainAddresses, int[] targetAddresses, int packed) {
        int mark = 0;
        if (contains(chainAddresses, packed)) mark |= CHAIN;
        if (contains(targetAddresses, packed)) mark |= TARGET;
        return mark;
    }

    /** 悬停提示里的那句话；没有标记时返回空串。 */
    static String label(int mark) {
        if (mark == (CHAIN | TARGET)) return "连锁中发动 · 被指定为对象";
        if (mark == CHAIN) return "连锁中发动";
        if (mark == TARGET) return "被指定为对象";
        return "";
    }
}
