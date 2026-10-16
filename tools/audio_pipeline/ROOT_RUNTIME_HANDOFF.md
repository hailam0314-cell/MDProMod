# Runtime handoff from root

## CURRENT ownership, 2026-10-15 10:20

Root accepts that DuelSceneAudio owns summon Timeline scheduling. Root is
removing the duplicate ClientSummonEffects.playAudio implementation.
ClientCutinEffects still owns attribute audio at actual Spine start; do not
schedule a duplicate cutin attribute cue. Root owns ClientDuelAudio lifecycle,
private UI feedback, DuelSessionManager, final checks/build/deployment.
audio_server_events owns DuelVisualState, DuelAnimationScene, DuelSceneAudio
and focused tests. Startup and audio_win stay on the scene queue.

Root added DuelAudioPerspective and client remapping of PLAYER/RIVAL,
OPP, WIN/LOSE, and effect P/R keys based on field.playerSide(listener),
matching the existing field-side UI rule. UI hints remain private. Root is
also restricting SessionManager's host LP/phase/turn/result publication until
scene is idle so fast core output does not overtake queued presentation.
Please set field LP to e.toCount() in audio_lp at cue start, turn in audio_turn,
and field phase in audio_phase at cue start (use the semantic values). This
lets visible state follow the queued event rather than jump to final host.
Coverage feedback is in tools/audio_pipeline/coverage-progress.md and
tmp/audio_behavior_review.md; coordinate fixes in your owned files.

Please ensure scene.clear stops both session-tagged server sounds and local
field-tagged cutin cues (empty-key field stop packet, root will implement
field cleanup handling). Root will review completion after your tests.

Root completed full Unity Timeline census: `runtime/audio_timeline_census_full.json`
(745 sources, zero errors, 74 keys). 497 original MonsterCutin and DIY cutin
assets have no SoundPlayableAsset. Seven attribute sounds come from the
shared outduel cutin backdrop, time zero.

Root generated `mod/src/main/resources/data/mdpromod/duel_audio_timelines.json`.
Groups: `prefab` (227 prefab/Timeline aliases), `cutin` (DARK/LIGHT/EARTH/FIRE/
WATER/WIND/DIVINE), `specialwin` (33396948,40771118,94212438,53334641,96637156
and SpecialWinCommonCard01..05). Each cue has key/timeMs, one-shot full clip
duration (do NOT stop it at the Unity TimelineClip duration 0.2 seconds).
Use `prefab` for exact summon material/summon/chain/turn/start/result cues.
Source evidence and generation script are `tools/audio_pipeline/export_timeline_cues.py`
and `runtime/audio_timeline_cues_evidence.json`.

CRITICAL NAMING: asset agent generated event IDs `mdpro.se_card_draw_01`,
sound files `mdpromod:mdpro/se/se_card_draw_01`. Your ClientDuelAudio.location
initially returned `mdpromod:se_card_draw_01`, which does NOT match. Align it
with `tools/audio_pipeline/sounds.generated.json`, then copy this generated
registry to `mod/src/main/resources/assets/mdpromod/sounds.json`.
All 2333 SE extracted/verified, OGG mono Vorbis q3, total 69,859,425 bytes.
2333/2333 decode, frame count, rate and hash checks passed.

Root is editing UI-only files (ClientCardMenu, ClientHandBridge, HandCardScreen,
selection/sort/chain choice screens) plus a separate ClientDuelUiAudio event
class for private UI feedback. Please leave those to root. You own main server
event playback, DuelAnimationScene, sessions, ClientDuelAudio, SummonEffects,
CutinEffects, ModNetwork, ClientForgeEvents, runtime tests and compile checks.

Root cannot see collaboration message bodies (they arrive empty); write progress
to tools/audio_pipeline/runtime-progress.md with scope, gaps and tests. The C#
audit agent was asked to write analysis/duel_audio_rules.json and a readable
audit report: inspect any files it produces directly. Do not mark full coverage
without explicitly checking all event categories.
