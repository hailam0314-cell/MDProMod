package net.mdpromod;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

/**
 * 卡堆界面的排列顺序（纯函数，可离线测试）：
 * **能特殊召唤 / 能发动的卡排在最前面**（按它们在卡堆里的位置从前到后），
 * 其余按原顺序跟在后面；每一张只出现一次。
 *
 * <p>动作里的卡号单独带着：万一核心给的序号和这份清单对不上（比如卡堆顺序变了），
 * 也不至于把那一张从界面上漏掉——那正是"第 4 张之后点不到"要避免的事。
 */
final class PileOrder {

    /** sequence = 这张卡在卡堆里的位置（发给核心的应答就是它），code = 卡号。 */
    record Entry(int sequence, int code) {
    }

    private PileOrder() {
    }

    static List<Entry> sorted(int[] actionSeqs, int[] actionCodes, int[] allCodes) {
        List<Entry> entries = new ArrayList<>();
        Set<Integer> used = new LinkedHashSet<>();
        int[] sequences = actionSeqs == null ? new int[0] : actionSeqs;
        int[] codes = actionCodes == null ? new int[0] : actionCodes;
        int[] all = allCodes == null ? new int[0] : allCodes;

        Integer[] order = new Integer[sequences.length];
        for (int i = 0; i < order.length; i++) order[i] = i;
        Arrays.sort(order, (left, right) -> Integer.compare(sequences[left], sequences[right]));
        for (int index : order) {
            int sequence = sequences[index];
            if (!used.add(sequence)) continue;
            int code = sequence >= 0 && sequence < all.length ? all[sequence] : codes[index];
            entries.add(new Entry(sequence, code));
        }
        for (int i = 0; i < all.length; i++) {
            if (used.add(i)) entries.add(new Entry(i, all[i]));
        }
        return entries;
    }
}
