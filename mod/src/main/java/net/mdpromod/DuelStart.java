package net.mdpromod;

import java.util.UUID;

final class DuelStart {
    static final int ROCK = 1;
    static final int SCISSORS = 2;
    static final int PAPER = 3;

    private DuelStart() {
    }

    static int winner(int seat0, int seat1) {
        if (seat0 == seat1) {
            return -1;
        }

        boolean seat0Wins = seat0 == 1 && seat1 == 2 || seat0 == 2 && seat1 == 3 || seat0 == 3 && seat1 == 1;
        return seat0Wins ? 0 : 1;
    }

    static String handName(int hand) {
        return switch (hand) {
            case 1 -> "石头";
            case 2 -> "剪刀";
            case 3 -> "布";
            default -> "？";
        };
    }

    static int firstSeat(int winnerSeat, boolean chooseFirst) {
        return chooseFirst ? winnerSeat : 1 - winnerSeat;
    }

    static boolean needsFlip(int firstSeat) {
        return firstSeat == 1;
    }

    static int humanSeat(int aiSeat) {
        return aiSeat == 0 ? 1 : 0;
    }

    static boolean requiresPlayerDeck(int seat, int aiSeat) {
        return seat != aiSeat;
    }

    static boolean shouldSetOwnerSeat(UUID linkedId, UUID seat1Id) {
        return linkedId != null && !linkedId.equals(seat1Id);
    }

    static int viewerSeat(UUID playerId, UUID seat0Id, UUID seat1Id) {
        if (playerId == null) {
            return -1;
        } else if (playerId.equals(seat0Id)) {
            return 0;
        } else {
            return playerId.equals(seat1Id) ? 1 : -1;
        }
    }
}
