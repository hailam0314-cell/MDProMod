package net.mdpromod;

/**
 * 把"卡在哪"说成人话：自己/对方的怪兽区 3、墓地、除外区、手牌……
 *
 * <p>打包格式和落点提示一致：{@code controller << 16 | location << 8 | sequence}。
 * 纯函数，可离线测试。
 */
final class DuelLocation {

    private DuelLocation() {
    }

    /** {@code 0x000408} → 「自己墓地」这类。 */
    static String label(int packed) {
        int controller = (packed >> 16) & 1;
        int location = (packed >> 8) & 0xFF;
        int sequence = packed & 0xFF;
        String who = controller == 0 ? "自己" : "对方";
        return switch (location) {
            case 1 -> who + "卡组";
            case 2 -> who + "手牌";
            case 4 -> sequence < 5 ? who + "怪兽区" + (sequence + 1) : "额外怪兽区";
            case 8 -> sequence == 5 ? who + "场地魔法区" : who + "魔陷区" + (sequence + 1);
            case 16 -> who + "墓地";
            case 32 -> who + "除外区";
            case 64 -> who + "额外卡组";
            default -> who + "区域 " + location + "-" + sequence;
        };
    }

    /** 选卡列表悬停时显示的两行：卡名 + 位置。 */
    static String tooltipLine(int packed) {
        return "位置：" + label(packed);
    }
}
