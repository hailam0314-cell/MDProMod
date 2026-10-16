# Server event integration — audio_server_events

Restored worker has received an empty task message. I am taking the normal
server event integration boundary based on ROOT_RUNTIME_HANDOFF.md and the
currently missing calls. Please use this file if collaboration message bodies
are empty.

Owned files: DuelVisualState.java, DuelAnimationScene.java,
DuelSessionManager.java, and their focused tests. Other agents should not edit
these files concurrently. I will add semantic non-card events to the existing
visual queue so sounds align with presentation rather than the core reader.
I will not edit ClientDuelAudio, ModNetwork, ClientCutinEffects, or UI files.

Current discovery: the transport, sound registry and timeline manifest exist;
no server cue invocation exists yet. Summon/cutin timeline cues must not be
double-played by both client and server hooks.

Status: implemented initial semantic queue parsing in DuelVisualState (phase,
turn, LP, chain stack/resolve, counters, coin/dice, result), and calls from
DuelAnimationScene into new DuelSceneAudio.java. Card movement, landing,
attack attribute/guard/direct and dedicated effect keys are integrated.

I have not edited DuelSessionManager yet. ROOT MAY OWN IT if preferred;
Win is currently projected as an audio_win queue event, so avoid a duplicate
result cue in sessions. Startup cues are emitted once from scene.tick. Client
summon should not play its own timeline sounds while server audio.summon is
active (this worker currently schedules exact Timeline cues, trims material
display from the existing two-stage mod visual sequence).

## Completion, 2026-10-15

The server scene now compiles. DuelVisualState queues public turn, phase, LP,
chain, counter, coin/dice, stat/characteristic and result events in protocol
order. Attack sound starts at BATTLE, uses dedicated lethal card families and
their family-specific hit offsets, and cannot be mistaken for a release move.
The card path preserves original target/position events for audio even where
the visual animation kind changes. Chain effect dispatch uses the current
resolving card without the stale disabled-snapshot gate. Installed dedicated
effect IDs include Heavy Storm 19613556 and deferred Engage/negate callbacks.

DuelSceneAudio applies LP, turn and phase to the field when each queued cue
begins. LP holds the queue 2300 ms so its delayed counter cue precedes the next
action. Special wins choose the audited common/card-specific timeline, then
start the result sound at the installed WinStart event (not the sound clip end).
Scene clear sends both session stop and field-wide stop for local cutin cues.
Summon material display chooses CARD_01/02 by material count (including fusion
offsets); Link main track uses Link rating, synchro circle uses 5/9 thresholds,
and ritual child trails follow the source material-count switch. A spell card
moving onto the field no longer receives a monster landing sound.

Verified: standalone DuelVisualStateAudioTest 526 assertions, DuelAudioRulesTest
109 assertions, `mod/gradlew.bat compileJava --offline` successful. No in-game
playback test has run. Material count is inferred from queued movement events
at the first material animation; if the host delivers one material group across
multiple drain batches, the first cue can choose CARD_01 before the final count
arrives. Ritual child track start is currently a 1500 ms alignment estimate
because the audio manifest gives child-local cue time but no parent activation
offset. Pendulum ScaleSet cues are not attached to ordinary scale placement:
the strict two-target demonstration and master-rule gate are not represented
by this scene. VS entry sounds and active-action hints likewise need their own
presentation hooks; they are not claimed as covered here.
