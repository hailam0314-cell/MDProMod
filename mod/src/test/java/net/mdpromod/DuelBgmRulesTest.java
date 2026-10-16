package net.mdpromod;

/** Standalone policy checks; no Minecraft runtime required. */
public final class DuelBgmRulesTest {
    public static void main(String[] args) {
        check(DuelBgmRules.isCue("BGM_DUEL"), "BGM cue is recognized");
        check(DuelBgmRules.isCue(" bgm_duel "), "BGM cue matching is case insensitive");
        check(!DuelBgmRules.isCue("SE_DUEL_START"), "SE is not treated as BGM");
        check(DuelBgmRules.localChoice(null).equals("BGM_DUEL_NORMAL_01"), "default local track");
        check(DuelBgmRules.localChoice("off").isEmpty(), "off disables local BGM");
        check(DuelBgmRules.localChoice("se_m04758_f_loop_v1").equals("SE_M04758_F_LOOP_V1"),
                "local choice normalizes track spelling");
        check(DuelBgmRules.localChoice("bad key!").equals("BGM_DUEL_NORMAL_01"),
                "invalid local choice falls back safely");
        check(DuelBgmRules.START_DELAY_MS >= 200, "BGM starts after opening SE has a release window");
        moments();
        bundledTrackList();
        System.out.println("DuelBgmRulesTest passed");
    }

    /** 三个时机各自独立，默认曲目和触发阈值都要对得上。 */
    private static void moments() {
        check(DuelBgmRules.MOMENTS.size() == 3, "three selectable moments");
        check(DuelBgmRules.isMoment("duel") && DuelBgmRules.isMoment("keycard")
                && DuelBgmRules.isMoment("climax"), "moment keys are recognized");
        check(!DuelBgmRules.isMoment("nope"), "unknown moment is rejected");
        check(!DuelBgmRules.isMoment(null), "null moment is rejected");
        check(DuelBgmRules.defaultTrack("keycard").equals("BGM_DUEL_KEYCARD_01"), "keycard default");
        check(DuelBgmRules.defaultTrack("climax").equals("BGM_DUEL_CLIMAX_01"), "climax default");
        check(DuelBgmRules.defaultTrack("duel").equals(DuelBgmRules.DEFAULT_KEY), "duel default");
        check(DuelBgmRules.CLIMAX_LP == 2000, "climax threshold is 25% of 8000");
        for (String moment : DuelBgmRules.MOMENTS) {
            check(!DuelBgmTracks.label(DuelBgmRules.defaultTrack(moment)).isBlank(),
                    "default of " + moment + " resolves to a label");
        }
        stageMachine();
    }

    /**
     * 原版守卫的回归测试：王牌只切一次、第二只王牌不动音乐、高潮把王牌顶掉且不回头。
     * （照抄 MDPro3 {@code AudioManager.PlayBgmKeyCard/PlayBgmClimax} 的 if 判断。）
     */
    private static void stageMachine() {
        int normal = DuelBgmRules.STAGE_NORMAL;
        int keycard = DuelBgmRules.STAGE_KEYCARD;
        int climax = DuelBgmRules.STAGE_CLIMAX;
        check(DuelBgmRules.advanceBgmStage(normal, false, false) == normal, "idle stays normal");
        check(DuelBgmRules.advanceBgmStage(normal, true, false) == keycard, "first ace enters keycard");
        // 关键回归：第二只王牌（阶段已是 keycard）不得再触发任何切换
        int afterSecond = DuelBgmRules.advanceBgmStage(keycard, true, false);
        check(afterSecond == keycard, "second ace does not restart the keycard stage");
        check(DuelBgmRules.advanceBgmStage(keycard, true, false) == afterSecond,
                "more aces keep the stage unchanged");
        // 高潮可以顶掉王牌，且之后不再变化；王牌也没法把高潮顶回去
        check(DuelBgmRules.advanceBgmStage(keycard, false, true) == climax, "low LP overrides keycard");
        check(DuelBgmRules.advanceBgmStage(climax, false, true) == climax, "climax does not re-fire");
        check(DuelBgmRules.advanceBgmStage(climax, true, false) == climax, "ace cannot override climax");
        check(DuelBgmRules.advanceBgmStage(climax, true, true) == climax, "climax stays terminal");
        // 高潮从常规直接来也一样
        check(DuelBgmRules.advanceBgmStage(normal, false, true) == climax, "low LP from normal");
    }

    /** 随包的曲目清单必须真的能读到，否则「音乐设置」会是个空壳。 */
    private static void bundledTrackList() {
        String[] keys = DuelBgmTracks.keys();
        String[] labels = DuelBgmTracks.labels();
        check(keys.length >= 110, "bundled track list has all BGMs, got " + keys.length);
        check(labels.length == keys.length, "labels line up with keys");
        check(DuelBgmTracks.label("BGM_DUEL_NORMAL_01").contains("常规"),
                "labels come from the generated list");
        check(DuelBgmTracks.label("off").equals("不播放"), "off shows as a skip entry");
        check(DuelBgmTracks.label("BGM_DUEL_KEYCARD_08").contains("王牌"),
                "keycard tracks are labelled");
        check(!DuelBgmTracks.label(DuelBgmRules.DEFAULT_KEY).isBlank(),
                "the default track resolves to a label");
        boolean unknownPassthrough = DuelBgmTracks.label("BGM_NOT_IN_LIST").equals("BGM_NOT_IN_LIST");
        check(unknownPassthrough, "unknown keys fall back to the raw key");
    }

    private static void check(boolean value, String message) {
        if (!value) throw new AssertionError(message);
    }
}
