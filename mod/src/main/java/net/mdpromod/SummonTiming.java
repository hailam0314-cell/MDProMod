package net.mdpromod;

final class SummonTiming {
    static final int MATERIAL_MS = 1330;
    static final int LAND_MS = 600;
    private static final int[] SUMMON_MS = new int[]{3310, 3990, 3870, 6770, 3690, 4000};

    static int summonMs(int kind) {
        return kind >= 0 && kind < SUMMON_MS.length ? SUMMON_MS[kind] : 3200;
    }

    private SummonTiming() {
    }
}
