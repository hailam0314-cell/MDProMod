package net.mdpromod;

final class DuelMarks {
    static final int CHAIN = 1;
    static final int TARGET = 2;
    static final int CHAIN_RGB = 16739895;
    static final int TARGET_RGB = 4633855;

    private DuelMarks() {
    }

    static boolean contains(int[] addresses, int packed) {
        if (addresses == null) {
            return false;
        }

        for (int address : addresses) {
            if (address == packed) {
                return true;
            }
        }

        return false;
    }

    static int at(int[] chainAddresses, int[] targetAddresses, int packed) {
        int mark = 0;
        if (contains(chainAddresses, packed)) {
            mark |= 1;
        }

        if (contains(targetAddresses, packed)) {
            mark |= 2;
        }

        return mark;
    }

    static String label(int mark) {
        if (mark == 3) {
            return "连锁中发动 · 被指定为对象";
        } else if (mark == 1) {
            return "连锁中发动";
        } else {
            return mark == 2 ? "被指定为对象" : "";
        }
    }
}
