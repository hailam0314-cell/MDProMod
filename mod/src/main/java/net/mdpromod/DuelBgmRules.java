package net.mdpromod;

import java.util.List;
import java.util.Locale;

final class DuelBgmRules {
    static final String CUE_KEY = "BGM_DUEL";
    static final int START_DELAY_MS = 250;
    static final int CLIMAX_LP = 2000;
    static final int STAGE_NORMAL = 0;
    static final int STAGE_KEYCARD = 1;
    static final int STAGE_CLIMAX = 2;
    static final String MOMENT_DUEL = "duel";
    static final String MOMENT_KEYCARD = "keycard";
    static final String MOMENT_CLIMAX = "climax";
    static final List<String> MOMENTS = List.of("duel", "keycard", "climax");
    static final String DEFAULT_KEY = "BGM_DUEL_NORMAL_01";

    private DuelBgmRules() {
    }

    static boolean isMoment(String moment) {
        return moment != null && MOMENTS.contains(moment);
    }

    static String momentLabel(String moment) {
        if ("keycard".equals(moment)) {
            return "王牌登场";
        } else {
            return "climax".equals(moment) ? "低 LP 高潮" : "决斗";
        }
    }

    static String defaultTrack(String moment) {
        if ("keycard".equals(moment)) {
            return "BGM_DUEL_KEYCARD_01";
        } else {
            return "climax".equals(moment) ? "BGM_DUEL_CLIMAX_01" : "BGM_DUEL_NORMAL_01";
        }
    }

    static boolean isCue(String key) {
        return key != null && "BGM_DUEL".equalsIgnoreCase(key.trim());
    }

    static int advanceBgmStage(int stage, boolean cutinStarted, boolean lowLifePoints) {
        if (stage == 0 && cutinStarted) {
            stage = 1;
        }

        if (stage != 2 && lowLifePoints) {
            stage = 2;
        }

        return stage;
    }

    static String localChoice(String value) {
        if (value == null) {
            return "BGM_DUEL_NORMAL_01";
        } else {
            String choice = value.trim();
            if (choice.isEmpty() || choice.equalsIgnoreCase("off") || choice.equalsIgnoreCase("none")) {
                return "";
            } else {
                return !choice.matches("[a-zA-Z0-9_./-]+") ? "BGM_DUEL_NORMAL_01" : choice.toUpperCase(Locale.ROOT);
            }
        }
    }
}
