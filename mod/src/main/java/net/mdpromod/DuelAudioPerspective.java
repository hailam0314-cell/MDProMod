package net.mdpromod;

/** Server sound keys use engine seat 0; each listener hears their own side. */
final class DuelAudioPerspective {
    private DuelAudioPerspective() {}

    static String key(String key, boolean farSide) {
        if (!farSide || key == null) return key;
        if (key.startsWith("SE_PHASE_OPP_")) return "SE_PHASE_" + key.substring(13);
        if (key.startsWith("SE_PHASE_") && !key.startsWith("SE_PHASE_WINDOW_"))
            return "SE_PHASE_OPP_" + key.substring(9);
        return switch (key) {
            case "SE_NEXT_TURN_PLAYER" -> "SE_NEXT_TURN_RIVAL";
            case "SE_NEXT_TURN_RIVAL" -> "SE_NEXT_TURN_PLAYER";
            case "SE_LP_COUNT_PLAYER" -> "SE_LP_COUNT_RIVAL";
            case "SE_LP_COUNT_RIVAL" -> "SE_LP_COUNT_PLAYER";
            case "SE_LP_RECOVERY_PLAYER" -> "SE_LP_RECOVERY_RIVAL";
            case "SE_LP_RECOVERY_RIVAL" -> "SE_LP_RECOVERY_PLAYER";
            case "SE_DIRECT_ATTACK_PLAYER" -> "SE_DIRECT_ATTACK_RIVAL";
            case "SE_DIRECT_ATTACK_RIVAL" -> "SE_DIRECT_ATTACK_PLAYER";
            case "SE_DUEL_WIN" -> "SE_DUEL_LOSE";
            case "SE_DUEL_LOSE" -> "SE_DUEL_WIN";
            case "SE_EV_EF21233_P" -> "SE_EV_EF21233_R";
            case "SE_EV_EF21233_R" -> "SE_EV_EF21233_P";
            case "SE_EV_LIGHTNINGSTORM_P" -> "SE_EV_LIGHTNINGSTORM_R";
            case "SE_EV_LIGHTNINGSTORM_R" -> "SE_EV_LIGHTNINGSTORM_P";
            default -> key;
        };
    }

    static int delay(String key, int delay, boolean farSide) {
        if (!farSide) return delay;
        if ("SE_NEXT_TURN_PLAYER".equals(key)) return Math.max(0, delay - 66);
        if ("SE_NEXT_TURN_RIVAL".equals(key)) return delay + 66;
        return delay;
    }
}
