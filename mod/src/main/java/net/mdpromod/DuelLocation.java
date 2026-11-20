package net.mdpromod;

final class DuelLocation {
    private DuelLocation() {
    }

    static String label(int packed, int viewerSeat) {
        int controller = packed >> 16 & 1;
        int location = packed >> 8 & 0xFF;
        int sequence = packed & 0xFF;
        String who = viewerSeat != 0 && viewerSeat != 1 ? (controller == 0 ? "1号玩家" : "2号玩家") : (controller == viewerSeat ? "自己" : "对方");

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

    static String tooltipLine(int packed, int viewerSeat) {
        return "位置：" + label(packed, viewerSeat);
    }
}
