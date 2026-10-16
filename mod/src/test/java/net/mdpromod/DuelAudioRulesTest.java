package net.mdpromod;

/** Source-derived decision boundaries; standalone Java 17, without Forge or native core. */
public final class DuelAudioRulesTest {
    private static int assertions;

    public static void main(String[] args) {
        summonBoundaries();
        materialTimelines();
        phasePerspectiveAndSubsteps();
        attributePriority();
        finalAttackFamiliesAndAliases();
        cardEffectGates();
        effectAliasesAndDeferredCues();
        System.out.println("DuelAudioRules passed " + assertions + " assertions");
    }

    private static void materialTimelines() {
        equals(DuelAudioRules.materialCard(0, 1), "SE_SMN_CMN_CARD_01", "Fusion one material");
        equals(DuelAudioRules.materialCard(0, 5), "SE_SMN_CMN_CARD_02", "Fusion five materials");
        equals(DuelAudioRules.materialCard(1, 5), "SE_SMN_CMN_CARD_02", "Synchro five materials");
        equals(DuelAudioRules.materialCard(4, 6), "SE_SMN_CMN_CARD_02", "Ritual six materials");
        equals(DuelAudioRules.materialCard(3, 6), "SE_SMN_CMN_CARD_01", "Link main track keeps card 01");
        equals(DuelAudioRules.materialCard(5, 2), "", "Pendulum has no generic material display");
        check(DuelAudioRules.materialCardDelay(0, 1) == 100
                        && DuelAudioRules.materialCardDelay(0, 2) == 67
                        && DuelAudioRules.materialCardDelay(0, 3) == 50
                        && DuelAudioRules.materialCardDelay(4, 3) == 0,
                "Fusion show-card timings follow the installed tracks");
        equals(DuelAudioRules.specialFusionMaterialCard(48130397, 48130397),
                "SE_SMN_CMN_CARD_SUPER", "Super Polymerization material track");
        equals(DuelAudioRules.specialFusionMaterialCard(48130398, 48130397),
                "SE_SMN_CMN_CARD_SUPER", "Super Polymerization alternate-art material track");
        equals(DuelAudioRules.specialFusionMaterialCard(74063034, 74063034),
                "SE_EV_045_NORMAL", "Invocation original-art material track");
        equals(DuelAudioRules.specialFusionMaterialCard(74063035, 74063034),
                "SE_EV_045_SPECIAL", "Invocation alternate-art material track");
        equals(DuelAudioRules.specialFusionMaterialCard(74063035, 74063035), "",
                "Actual ID alone cannot select an Invocation track without its original ID");
        equals(DuelAudioRules.specialFusionMaterialCard(24094653, 24094653), "",
                "Ordinary fusion spell keeps the generic material track");
        check(DuelAudioRules.materialCardDelay(0, 1, "SE_SMN_CMN_CARD_SUPER") == 100
                        && DuelAudioRules.materialCardDelay(0, 2, "SE_SMN_CMN_CARD_SUPER") == 67
                        && DuelAudioRules.materialCardDelay(0, 5, "SE_SMN_CMN_CARD_SUPER") == 0,
                "Super Polymerization retains per-count fusion timings");
        check(DuelAudioRules.materialCardDelay(0, 1, "SE_EV_045_NORMAL") == 17
                        && DuelAudioRules.materialCardDelay(0, 8, "SE_EV_045_SPECIAL") == 17,
                "Invocation material tracks start at 17 ms for every count");
        check(DuelAudioRules.ritualTrails(1).equals(java.util.List.of(1))
                        && DuelAudioRules.ritualTrails(2).equals(java.util.List.of(2))
                        && DuelAudioRules.ritualTrails(3).equals(java.util.List.of(3))
                        && DuelAudioRules.ritualTrails(4).equals(java.util.List.of(1, 3))
                        && DuelAudioRules.ritualTrails(5).equals(java.util.List.of(2, 3))
                        && DuelAudioRules.ritualTrails(6).equals(java.util.List.of(1, 2, 3)),
                "Ritual trail activation follows material-count switch");
    }

    private static void summonBoundaries() {
        // Installed Card.IsHighLevel: Link>2, Xyz rank>3, or level>6.
        check(!DuelAudioRules.high(0x4000000, 2), "Link-2 is middle");
        check(DuelAudioRules.high(0x4000000, 3), "Link-3 is high");
        check(!DuelAudioRules.high(0x800000, 3), "Rank-3 Xyz is middle");
        check(DuelAudioRules.high(0x800000, 4), "Rank-4 Xyz is high");
        check(!DuelAudioRules.high(1, 6), "Level-6 ordinary monster is middle");
        check(DuelAudioRules.high(1, 7), "Level-7 ordinary monster is high");
        check(!DuelAudioRules.high(1, 0x08080004), "Packed pendulum scales do not inflate level");
        equals(DuelAudioRules.land(false, -1, 1, 4), "SE_LAND_NORMAL",
                "Normal level-4 landing");
        equals(DuelAudioRules.land(false, -1, 1, 5), "SE_LAND_ADVANCE_MIDDLE",
                "Normal level-5 advance landing");
        equals(DuelAudioRules.land(false, -1, 1, 6), "SE_LAND_ADVANCE_MIDDLE",
                "Normal level-6 advance landing");
        equals(DuelAudioRules.land(false, -1, 1, 7), "SE_LAND_ADVANCE_HIGH",
                "Normal level-7 advance landing");
        equals(DuelAudioRules.land(true, 2, 0x800001, 4), "SE_LAND_XYZ_HIGH",
                "Xyz special landing uses rank threshold");
        equals(DuelAudioRules.land(true, 3, 0x4000001, 2), "SE_LAND_LINK_MIDDLE",
                "Link special landing uses Link threshold");
        equals(DuelAudioRules.land(true, 5, 1, 7), "SE_LAND_PENDULUM_HIGH",
                "Pendulum special landing category");
        equals(DuelAudioRules.land(true, -1, 1, 4), "SE_LAND_NORMAL_MIDDLE",
                "Special summon without material category uses normal special landing");
        equals(DuelAudioRules.land(true, -1, 0x4001, 10), "",
                "Token does not receive ordinary special-summon landing");
    }

    private static void phasePerspectiveAndSubsteps() {
        equals(DuelAudioRules.phase(1, 0), "SE_PHASE_DRAW", "Local draw phase");
        equals(DuelAudioRules.phase(1, 1), "SE_PHASE_OPP_DRAW", "Opponent draw phase");
        equals(DuelAudioRules.phase(8, 1), "SE_PHASE_OPP_BATTLE", "Battle Start has a banner");
        equals(DuelAudioRules.phase(0x100, 0), "SE_PHASE_MAIN2", "Main2 phase");
        equals(DuelAudioRules.phase(0x200, 1), "SE_PHASE_OPP_END", "Opponent end phase");
        for (int phase : new int[]{0x10, 0x20, 0x40, 0x80}) {
            equals(DuelAudioRules.phase(phase, 0), "", "Local battle substep is silent");
            equals(DuelAudioRules.phase(phase, 1), "", "Opponent battle substep is silent");
        }
        equals(DuelAudioRules.phase(0, 0), "", "Unknown phase does not invent a banner cue");
    }

    private static void finalAttackFamiliesAndAliases() {
        int[] codes = {89631139, 46986414, 74677422, 10000000, 10000010, 10000020};
        String[] families = {"BE", "BM", "RE", "OBELISK", "RA", "SLIFER"};
        for (int i = 0; i < codes.length; i++) {
            equals(DuelAudioRules.finalAttack(codes[i], 0, true), families[i],
                    "Recognized final attack family");
            equals(DuelAudioRules.finalAttack(123, codes[i], true), families[i],
                    "Alias inherits recognized final attack family");
            equals(DuelAudioRules.finalAttack(codes[i], codes[i], false), "",
                    "Recognized card without lethal condition has no final cue");
        }
        equals(DuelAudioRules.finalAttack(53347303, 0, true), "BE",
                "Configured Blue-Eyes family variant is recognized");
        equals(DuelAudioRules.finalAttack(10000080, 0, true), "RA", "Ra Sphere family");
        equals(DuelAudioRules.finalAttack(10000090, 0, true), "RA", "Ra Phoenix family");
        equals(DuelAudioRules.finalAttack(123, 456, true), "", "Unrecognized lethal card stays ordinary");
        check(DuelAudioRules.finalImpactMs("BE") == 1050
                        && DuelAudioRules.finalImpactMs("OBELISK") == 2000,
                "Distinct special-final animations retain distinct impact timings");
        check(DuelAudioRules.finalImpactMs("") == 0, "Ordinary impact has no special-final delay");
    }

    private static void attributePriority() {
        // Installed DuelMessage.GameMessage_Battle tests these bits in this order.
        equals(DuelAudioRules.attribute(32), "DARK", "Dark attribute");
        equals(DuelAudioRules.attribute(1), "EARTH", "Earth attribute");
        equals(DuelAudioRules.attribute(4), "FIRE", "Fire attribute");
        equals(DuelAudioRules.attribute(16), "LIGHT", "Light attribute");
        equals(DuelAudioRules.attribute(2), "WATER", "Water attribute");
        equals(DuelAudioRules.attribute(8), "WIND", "Wind attribute");
        equals(DuelAudioRules.attribute(64), "DIVINE", "Divine fallback");
        equals(DuelAudioRules.attribute(32 | 1 | 4 | 16 | 2 | 8), "DARK",
                "Dark wins a multi-attribute attack");
        equals(DuelAudioRules.attribute(1 | 4 | 16 | 2 | 8), "EARTH",
                "Earth precedes Fire and Light");
        equals(DuelAudioRules.attribute(4 | 16 | 2 | 8), "FIRE",
                "Fire precedes Light");
        equals(DuelAudioRules.attribute(16 | 2 | 8), "LIGHT",
                "Light precedes Water and Wind");
        equals(DuelAudioRules.attribute(2 | 8), "WATER", "Water precedes Wind");
    }

    private static void cardEffectGates() {
        int[] targetRequired = {5318639, 2263869, 63391643, 10045474, 97268402};
        for (int code : targetRequired) {
            equals(DuelAudioRules.effect(code, 0, 2, false, true), "",
                    "Missing target prevents target-dependent effect sound");
            check(!DuelAudioRules.effect(code, 0, 2, true, true).isEmpty(),
                    "Valid target permits target-dependent effect sound");
        }
        equals(DuelAudioRules.effect(14558127, 0, 1, true, true), "",
                "Ash Blossom requires chain index greater than one");
        equals(DuelAudioRules.effect(14558127, 0, 2, true, true), "SE_EV_ASH_BLOSSOM_v2",
                "Ash Blossom second-chain effect");
        equals(DuelAudioRules.effect(59438930, 0, 1, true, true), "",
                "Ghost Ogre first chain is gated");
        equals(DuelAudioRules.effect(59438930, 0, 2, true, false), "",
                "Ghost Ogre requires previous chain card on field");
        equals(DuelAudioRules.effect(59438930, 0, 2, false, true), "SE_EV_038_NORMAL",
                "Ghost Ogre gate uses previous field state rather than arbitrary effect-target flag");
        equals(DuelAudioRules.effect(24224831, 0, 2, true, true), "SE_EV_EF21233_R",
                "Called by alternate art local side uses source R suffix");
        equals(DuelAudioRules.effect(24224831, 1, 2, true, true), "SE_EV_EF21233_P",
                "Called by alternate art opponent side uses source P suffix");
        equals(DuelAudioRules.effect(14532163, 0, 1, false, false), "SE_EV_LIGHTNINGSTORM_P",
                "Lightning Storm local perspective");
        equals(DuelAudioRules.effect(14532163, 1, 1, false, false), "SE_EV_LIGHTNINGSTORM_R",
                "Lightning Storm opponent perspective");
        equals(DuelAudioRules.effect(19613556, 0, 1, false, false), "SE_EV_HEAVY_STORM",
                "Heavy Storm uses installed source card ID 19613556");
        equals(DuelAudioRules.effect(19613557, 0, 1, false, false), "",
                "Adjacent unrecognized card ID cannot play Heavy Storm sound");
    }

    private static void effectAliasesAndDeferredCues() {
        check(DuelAudioRules.effectCode(12345, 12580477) == 12580477,
                "Ordinary alternate alias selects original effect");
        check(DuelAudioRules.effectCode(12580477, 0) == 12580477,
                "Zero alias retains card code");
        int[] distinctArt = {83764719, 63166096, 32807848, 49238329, 24224831, 6498707, 35269905};
        for (int code : distinctArt)
            check(DuelAudioRules.effectCode(code, 1) == code,
                    "Source alternate-art exception retains its own resource branch");
        equals(DuelAudioRules.effect(DuelAudioRules.effectCode(32807848, 32807846),
                        0, 2, true, true), "SE_EV_039_SPECIAL",
                "Alternate art retains special effect instead of original normal effect");
        equals(DuelAudioRules.effect(63166095, 0, 1, true, true), "",
                "Engage audio waits for deferred movement Timeline");
        equals(DuelAudioRules.effect(40366667, 0, 2, true, true), "",
                "Negation-specific effect waits for negate callback");
        equals(DuelAudioRules.effect(73642296, 0, 2, true, true), "",
                "Ghost Belle audio waits for negate callback");
        equals(DuelAudioRules.effect(58297729, 0, 2, true, true), "",
                "Resource-only transmission gear track has no enabled immediate source branch");
    }

    private static void equals(String actual, String expected, String message) {
        check(expected.equals(actual), message + ": expected " + expected + ", got " + actual);
    }

    private static void check(boolean condition, String message) {
        assertions++;
        if (!condition) throw new AssertionError(message);
    }
}
