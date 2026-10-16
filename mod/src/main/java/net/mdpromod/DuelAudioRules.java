package net.mdpromod;

import java.util.Set;
import java.util.List;

/** Pure decisions copied from the installed MDPro duel handlers. No Minecraft state. */
final class DuelAudioRules {
    private DuelAudioRules() {}

    static String phase(int phase, int side) {
        String suffix = switch (phase) {
            case 1 -> "DRAW"; case 2 -> "STANDBY"; case 4 -> "MAIN1";
            case 8 -> "BATTLE"; case 0x100 -> "MAIN2"; case 0x200 -> "END";
            default -> "";
        };
        return suffix.isEmpty() ? "" : "SE_PHASE" + (side == 1 ? "_OPP" : "") + "_" + suffix;
    }

    static String phaseName(int phase) {
        return switch (phase) {
            case 1 -> "DP"; case 2 -> "SP"; case 4 -> "M1";
            case 8 -> "BP"; case 0x100 -> "M2"; case 0x200 -> "EP";
            default -> "DP";
        };
    }

    static String materialCard(int kind, int count) {
        if (kind < 0 || kind > 4 || count <= 0) return "";
        return "SE_SMN_CMN_CARD_0" + (kind == 3 || count <= 4 ? 1 : 2);
    }

    static String specialFusionMaterialCard(int actualCode, int originalCode) {
        if (originalCode == 48130397) return "SE_SMN_CMN_CARD_SUPER";
        if (originalCode == 74063034)
            return actualCode == 74063035 ? "SE_EV_045_SPECIAL" : "SE_EV_045_NORMAL";
        return "";
    }

    static int materialCardDelay(int kind, int count) {
        if (kind != 0 || count > 4) return 0;
        return switch (count) {
            case 1 -> 100; case 2 -> 67; case 3, 4 -> 50; default -> 0;
        };
    }

    static int materialCardDelay(int kind, int count, String key) {
        if (kind == 0 && (key.equals("SE_EV_045_NORMAL") || key.equals("SE_EV_045_SPECIAL")))
            return 17;
        return materialCardDelay(kind, count);
    }

    static List<Integer> ritualTrails(int count) {
        return switch (count) {
            case 1 -> List.of(1); case 2 -> List.of(2); case 3 -> List.of(3);
            case 4 -> List.of(1, 3); case 5 -> List.of(2, 3);
            default -> count >= 6 ? List.of(1, 2, 3) : List.of();
        };
    }

    static boolean high(int type, int level) {
        int stars = level & 255;
        return (type & 0x4000000) != 0 && stars > 2
                || (type & 0x800000) != 0 && stars > 3 || stars > 6;
    }

    static String land(boolean special, int kind, int type, int level) {
        if ((type & 0x4000) != 0) return "";
        if (!special) return (level & 255) > 6 ? "SE_LAND_ADVANCE_HIGH"
                : (level & 255) > 4 ? "SE_LAND_ADVANCE_MIDDLE" : "SE_LAND_NORMAL";
        String category = switch (kind) {
            case 0 -> "FUSION"; case 1 -> "SYNCHRO"; case 2 -> "XYZ";
            case 3 -> "LINK"; case 4 -> "RITUAL"; case 5 -> "PENDULUM"; default -> "NORMAL";
        };
        return "SE_LAND_" + category + (high(type, level) ? "_HIGH" : "_MIDDLE");
    }

    static String attribute(int attribute) {
        // Attribute is a bitfield; use the same ordered tests as the source.
        if ((attribute & 32) != 0) return "DARK";
        if ((attribute & 1) != 0) return "EARTH";
        if ((attribute & 4) != 0) return "FIRE";
        if ((attribute & 16) != 0) return "LIGHT";
        if ((attribute & 2) != 0) return "WATER";
        if ((attribute & 8) != 0) return "WIND";
        return "DIVINE";
    }

    private static final Set<Integer> BE = Set.of(89631139,53347303,22804410,38517737,
            30576089,9433350,53183600,23995346,43228023,56532353,2129638,11443677);
    private static final Set<Integer> BM = Set.of(46986414,92377303,342673,21296502,
            29436665,35191415,38033121,90960358,50237654);
    private static final Set<Integer> RE = Set.of(74677422,96561011,64335804,18491580,55343236,6556909);
    static String finalAttack(int code, int alias, boolean finalBlow) {
        if (!finalBlow) return "";
        if (BE.contains(code) || BE.contains(alias)) return "BE";
        if (BM.contains(code) || BM.contains(alias)) return "BM";
        if (RE.contains(code) || RE.contains(alias)) return "RE";
        if (code == 10000000 || alias == 10000000) return "OBELISK";
        if (code == 10000010 || code == 10000080 || code == 10000090
                || alias == 10000010 || alias == 10000080 || alias == 10000090) return "RA";
        if (code == 10000020 || alias == 10000020) return "SLIFER";
        return "";
    }
    static int finalImpactMs(String family) {
        return switch (family) {
            case "BE" -> 1050; case "BM" -> 1250; case "RE", "SLIFER" -> 1200;
            case "RA" -> 1300; case "OBELISK" -> 2000; default -> 0;
        };
    }

    static int effectCode(int code, int alias) {
        return switch (code) {
            case 83764719,63166096,32807848,49238329,24224831,6498707,35269905 -> code;
            default -> alias > 0 ? alias : code;
        };
    }

    static String effect(int code, int side, int chain, boolean target, boolean previousOnField) {
        return switch (code) {
            case 5318639 -> target ? "SE_EV_CYCLONE" : "";
            case 2263869 -> target ? "SE_EV_ULTIMATE_SLAYER" : "";
            case 12580477 -> "SE_EV_RAIGEKI";
            case 14558127 -> chain > 1 ? "SE_EV_ASH_BLOSSOM_v2" : "";
            case 18144506 -> "SE_EV_HARPIESFEATHER_DUSTER_3D";
            case 23002292 -> "SE_EV_REDREBOOT";
            case 24224830 -> "SE_EV_CALLED_GRAVE";
            case 24224831 -> side == 0 ? "SE_EV_EF21233_R" : "SE_EV_EF21233_P";
            case 24299458 -> "SE_EV_FORBIDDEN_DROPLET";
            case 25311006 -> "SE_EV_TRIPLETACTICS_TALENT";
            case 41420027 -> "SE_EV_SOLEMNJUDGMENT";
            case 44095762 -> "SE_EV_MIRRORFORCE";
            case 53129443 -> "SE_EV_BLACKHOLE";
            case 54693926 -> "SE_EV_DARKRULER_NOMORE";
            case 61740673 -> "SE_EV_IMPERIAL_ORDER";
            case 62279055 -> "SE_EV_MAGIC_CYLINDER";
            case 63391643 -> target ? "SE_EV_THOUSANDKNIVES" : "";
            case 65681983 -> "SE_EV_CROSSOUT_DESIGNATOR";
            case 72302403 -> "SE_EV_GOFUKEN";
            case 75500286 -> "SE_EV_GOLD_SARCOPHAGUS";
            case 83764718,83764719 -> "SE_EV_MONSTER_REBORN";
            case 19613556 -> "SE_EV_HEAVY_STORM";
            case 90448279 -> "SE_EV_AZEUS";
            case 32807846 -> "SE_EV_039_NORMAL";
            case 32807848 -> "SE_EV_039_SPECIAL";
            case 59438930 -> chain > 1 && previousOnField ? "SE_EV_038_NORMAL" : "";
            case 84192580 -> "SE_EV_040_NORMAL";
            case 42141493 -> "SE_EV_041_NORMAL";
            case 87126721 -> "SE_EV_042_NORMAL";
            case 94145021 -> "SE_EV_EF09279_v1";
            case 49238329 -> "SE_EV_EF14144_v2";
            case 6498707 -> "SE_EV_EF22188";
            case 35269905 -> "SE_EV_EF22189";
            case 82732705 -> "SE_EV_SKILLDRAIN";
            case 10045474 -> target ? "SE_EV_INFINITE_IMPERMANENCE" : "";
            case 14532163 -> side == 0 ? "SE_EV_LIGHTNINGSTORM_P" : "SE_EV_LIGHTNINGSTORM_R";
            case 97268402 -> target ? "SE_EV_EFFECT_VEILER" : "";
            default -> ""; // Engage / negate effects are callbacks, not immediate cues.
        };
    }

    static String specialWin(int reason) {
        return switch (reason) {
            case 16 -> "33396948"; case 21 -> "94212438";
            case 27 -> "53334641"; case 34 -> "96637156";
            case 17,18,19,20,22,23,24,26,28,29,30,31,32,33,35 -> "SpecialWinCommonCard01";
            case 25 -> "SpecialWinCommonCard03";
            default -> "";
        };
    }
}
