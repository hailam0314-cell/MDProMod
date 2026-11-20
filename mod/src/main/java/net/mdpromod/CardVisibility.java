package net.mdpromod;

final class CardVisibility {
    static final int GRAVE = 16;

    private CardVisibility() {
    }

    static boolean visible(int controller, int location, int position, int viewer) {
        if (viewer >= 0 && controller == (viewer & 1)) {
            return true;
        } else {
            return location == 16 ? true : (position & 5) != 0;
        }
    }
}
