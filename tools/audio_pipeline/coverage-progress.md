# Behavioral review handoff

Independent review is in `D:/MDProMod/tmp/audio_behavior_review.md`.

Highest priority for the server semantic worker:

1. Lethal BATTLE reason bit 2 accidentally triggers generic RELEASE (`SE_SUMMON_ADVANCE`). Restrict move-reason handling to actual moves.
2. `target` is rewritten to `effect`, so target audio becomes ChainSpot instead of `SE_CEMETERY_CARD`; `position` rewritten to `flip` becomes set-land instead of `SE_DISP_ATTACK/DEFENS`.
3. Heavy Storm ID must be 19613556, not 88119384.
4. LP holds 500ms but count begins +1100ms, so win/next action can precede LP count.
5. Missing current installed EV branches and special final-attack groups; see review for IDs.
6. BUFF/DEBUFF/BUFF_CHANGE query deltas are not projected at all.

No Java changed by reviewer. Root owns client cleanup/UI/result publication; server worker owns scene/event files.
