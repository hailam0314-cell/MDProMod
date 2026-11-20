package net.mdpromod;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

final class PileOrder {
    private PileOrder() {
    }

    static List<PileOrder.Entry> sorted(int[] actionSeqs, int[] actionCodes, int[] allCodes) {
        List<PileOrder.Entry> entries = new ArrayList<>();
        Set<Integer> used = new LinkedHashSet<>();
        int[] sequences = actionSeqs == null ? new int[0] : actionSeqs;
        int[] codes = actionCodes == null ? new int[0] : actionCodes;
        int[] all = allCodes == null ? new int[0] : allCodes;
        Integer[] order = new Integer[sequences.length];

        for (int i = 0; i < order.length; i++) {
            order[i] = i;
        }

        Arrays.sort(order, (left, right) -> Integer.compare(sequences[left], sequences[right]));
        Integer[] var15 = order;
        int var10 = var15.length;

        for (int var11 = 0; var11 < var10; var11++) {
            int index = var15[var11];
            int sequence = sequences[index];
            if (used.add(sequence)) {
                int code = sequence >= 0 && sequence < all.length ? all[sequence] : codes[index];
                entries.add(new PileOrder.Entry(sequence, code));
            }
        }

        for (int i = 0; i < all.length; i++) {
            if (used.add(i)) {
                entries.add(new PileOrder.Entry(i, all[i]));
            }
        }

        return entries;
    }

    record Entry(int sequence, int code) {
    }
}
