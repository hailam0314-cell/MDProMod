package net.mdpromod;

/** No Minecraft runtime required. Verifies the two listeners hear opposite seats. */
public final class DuelAudioPerspectiveTest {
    public static void main(String[] args) {
        String[][] pairs = {
                {"SE_PHASE_DRAW", "SE_PHASE_OPP_DRAW"},
                {"SE_PHASE_END", "SE_PHASE_OPP_END"},
                {"SE_NEXT_TURN_PLAYER", "SE_NEXT_TURN_RIVAL"},
                {"SE_LP_COUNT_PLAYER", "SE_LP_COUNT_RIVAL"},
                {"SE_LP_RECOVERY_PLAYER", "SE_LP_RECOVERY_RIVAL"},
                {"SE_DIRECT_ATTACK_PLAYER", "SE_DIRECT_ATTACK_RIVAL"},
                {"SE_DUEL_WIN", "SE_DUEL_LOSE"},
                {"SE_EV_EF21233_R", "SE_EV_EF21233_P"},
                {"SE_EV_LIGHTNINGSTORM_P", "SE_EV_LIGHTNINGSTORM_R"}
        };
        for (String[] pair : pairs) {
            check(pair[0], DuelAudioPerspective.key(pair[0], false));
            check(pair[1], DuelAudioPerspective.key(pair[0], true));
            check(pair[0], DuelAudioPerspective.key(pair[1], true));
        }
        for (String shared : new String[]{"SE_CARD_COUNTER", "SE_DUEL_DRAW", "SE_PHASE_WINDOW_OPEN"})
            check(shared, DuelAudioPerspective.key(shared, true));
        if (DuelAudioPerspective.delay("SE_NEXT_TURN_PLAYER", 483, true) != 417
                || DuelAudioPerspective.delay("SE_NEXT_TURN_RIVAL", 417, true) != 483)
            throw new AssertionError("Turn banner offset must follow the listener's perspective");
        System.out.println("DuelAudioPerspectiveTest passed");
    }

    private static void check(String expected, String actual) {
        if (!expected.equals(actual)) throw new AssertionError(expected + " != " + actual);
    }
}
