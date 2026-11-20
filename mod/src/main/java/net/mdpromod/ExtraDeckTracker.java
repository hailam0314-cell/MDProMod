package net.mdpromod;

import java.util.ArrayList;
import java.util.List;

final class ExtraDeckTracker {
    static final int EXTRA = 64;
    private final int side;
    private final List<Integer> codes = new ArrayList<>();
    private boolean tracking;

    ExtraDeckTracker() {
        this(0);
    }

    ExtraDeckTracker(int side) {
        this.side = side & 1;
    }

    void reset(List<Integer> extraDeck) {
        this.codes.clear();
        if (extraDeck != null) {
            this.codes.addAll(extraDeck);
        }

        this.tracking = true;
    }

    boolean tracking() {
        return this.tracking;
    }

    void stop() {
        this.tracking = false;
    }

    boolean move(int fromController, int fromLocation, int toController, int toLocation, int code) {
        if (this.tracking && code > 0) {
            boolean left = fromController == this.side && fromLocation == 64 && toLocation != 64;
            if (left) {
                return this.codes.remove(Integer.valueOf(code));
            } else {
                boolean back = toController == this.side && toLocation == 64 && fromLocation != 64;
                if (back) {
                    this.codes.add(code);
                    return true;
                } else {
                    return false;
                }
            }
        } else {
            return false;
        }
    }

    List<Integer> codes() {
        return List.copyOf(this.codes);
    }
}
