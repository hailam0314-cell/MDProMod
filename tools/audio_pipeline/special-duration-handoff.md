# Special-win durations and pendulum trigger — source audit

## Overall Timeline durations

Re-read the six installed SpecialWin asset folders with UnityPy, resolving PPtrs and traversing every Timeline track and child track. All main TimelineAssets have `m_DurationMode=0` (BasedOnClips), `m_FixedDuration=0`, editor frame rate 60. Overall duration is the maximum `m_Start + m_Duration` across all clips, including animation/activation/event tracks, rather than the sound-trigger clip width.

Source folder: `D:/MyCardLibrary/mdpro3/MDPro3_Data/StandaloneWindows64/MasterDuel/Timeline/SpecialWin/`.

| Folder | Main TimelineAsset | Overall clip endpoint (s) | Rounded milliseconds | WinStart event (s) |
|---|---|---:|---:|---:|
| 33396948 | SummonSpecialWin4027Timeline | 11.400000 | 11400 | 9.600 |
| 94212438 | SummonSpecialWin14585Timeline | 8.916667 | 8917 | 8.600 |
| 53334641 | SummonSpecialWin11422Timeline | 5.166667 | 5167 | 5.000 |
| 96637156 | SummonSpecialWin17158Timeline | 5.566667 | 5567 | 5.400 |
| SpecialWinCommonCard01 | SpecialWinCommonCard01TL | 3.216667 | 3217 | 3.110 |
| SpecialWinCommonCard03 | SpecialWinCommonCard03TL | 3.233333 | 3233 | 3.120 |

The 1.333333s `SummonFusionShowUnitCard05TL` also present in CommonCard folders is a nested dependency, not the overall special-win duration. Sound clip trigger windows similarly do not determine overall duration or the length of the playing OGG.

Important result timing distinction: `YgomSystem.Timeline/EventPlayableBehaviour.cs:37-46` schedules `OcgCore.endingAction` at the `WinStart` event time. If the runtime needs to match when the result banner starts, use the WinStart column rather than the end-of-Timeline column. `DuelBGManager.PlaySpecialWin`/`PlayCommonSpecialWin` await director stop, but result presentation can overlap the animation tail.

The existing `runtime/audio_timeline_census_full.json` preserves the Event Track event lists and most relevant end points; the independent direct UnityPy check above included all track kinds omitted by the audio-oriented census. Unity discrete Timeline duration may differ from the continuous clip endpoint by one internal tick; millisecond-rounded values above are unaffected.

## SummonPendulumScaleSet timing and conditions

It is part of the full **pendulum-summon scale demonstration**, not a cue for placing every pendulum scale card.

1. `MDPro3.Duel/DuelMessage.cs:2349-2380`, `GameMessage_BecomeTarget` (MSG_BECOME_TARGET, 83), resolves all target cards into `OcgCore.cardsBeTarget`.
2. During Main1 or Main2, with zero cards in chain, exactly two target cards, both in pendulum zones and controlled by the same side, it sets `OcgCore.inPendulumSummon=true`.
3. `DuelPendulum` / `WatchPendulum` / `ReplayPendulum` setting for the active mode must allow the animation (default true). Only then it awaits `duelBGManager.PlaySummonPendulum()`.
4. `DuelBGManager.cs:3508-3556` creates `SummonPendulum01`, loads both scale card pictures and L/R scale values. There is no await between this start and the next step.
5. Only `OcgCore.MasterRule >= 4` creates **SummonPendulumScaleSet** (`:3557-3565`), at the start of this demonstration; it rotates 180 degrees for the opposing controller. It waits 3.5 seconds, stops `LoopTrackManager`, waits another 0.5 seconds, then destroys scaleSet and pendulum. For older rules it only waits 4 seconds and destroys pendulum, without the ScaleSet prefab.
6. The ScaleSet Timeline sound cues are `SE_SUMMON_PENDULUM_SET` at +33ms and `SE_LP_STOP` at +267ms relative to prefab start. They must not be attached to each scale-placement MSG_MOVE or each ordinary summon.

`GameMessage_UpdateData` clears `inPendulumSummon` at `DuelMessage.cs:717`; the code does not set it on every scale card movement.

## Attribute priority

Installed battle source (`DuelMessage.cs:1381` onward) checks multi-bit attributes in this order: DARK (32), EARTH (1), FIRE (4), LIGHT (16), WATER (2), WIND (8), then DIVINE fallback. A test for single and mixed bitfields was added to `DuelAudioRulesTest`; runtime agent owns the corresponding rule change.
