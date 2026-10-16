# Duel audio audit complete — duel_audio_audit

Owned and completed:

- `tools/audit_duel_audio.py`: bundle Timeline, AnimationClip event, prefab sound field/context, Cross Duel CSV, C# call/literal and resource-catalog audit. `python -m py_compile` passes.
- `analysis/duel_audio_audit.json`: 11,573 original Timeline/cutin scan files, zero bundle errors. MonsterCutin/MonsterCutin2 themselves have no SoundPlayable tracks.
- `analysis/duel_audio_extended.json`: final extended census. 2,225 files; 183 bundles with records; 228 Timeline events / 78 labels; 768 animation events / 756 labels; 1,725 prefab fields / 100 labels; 1,265 Cross Duel CSV events / 629 labels. Zero bundle/object errors, zero direct AudioSource refs. 243 C# call sites and 407 literal lines.
- `analysis/duel_audio_rules.json`: structured trigger-family rules and source boundaries, including exact timing, local perspective, chain skip offsets and optional channels.
- `docs/duel-audio-audit-2026-10-15.md`: comprehensive Chinese trigger report, including card-specific effect map, field/mate/UI/voice/BGM boundaries; final statistics and missing-resource caveat included.

Critical conclusions for runtime review:

- Cutin audio is the attribute background `SE_MONSTER_CUTIN_<attribute>` at t0, not a per-card cutin SoundPlayable track.
- `SE_EV_ENGAGE` for 63166095/63166096 is a Card Timeline cue at +383ms after the deferred nextMoveAction effect begins.
- 58297729 transmission-gear Win/Draw/Lose tracks exist, but no enabled current ShowCardEffectAnimation branch was found. Do not claim unconditional runtime playback.
- Actual launch/impact belongs to MSG_BATTLE; MSG_ATTACK has only direct declaration `SE_DA_TEXT`. Impact +400ms below 2000 ATK, +650ms otherwise; guard/direct hit override ordinary impact.
- CHAINING card-view and CHAINED ChainSpot are separate. Single chain skips large stack/resolve. Later resolves with index>1 start source timeline at 830ms: skip earlier cues and rebase later cues.
- LP damage/cost and LP number rolling are distinct. Count/recovery sound starts at numeric roll (+1100ms in installed OcgCore), not at message arrival.
- `SE_DISP_ATTACK/DEFENS` belongs to AnimationPosition battle position, not generic numeric ATK/DEF display. Only ATK numeric changes use BUFF_ATTACK/DEBUFF_ATTACK. Race/attribute and counters have separate keys.
- Shared events require per-client PLAYER/RIVAL/OPP perspective. Private UI sounds must stay local. Snapshot refresh/reconnect must not replay events.
- Same-label 100ms suppression and missing `_oneshot` fallback are original AudioManager behavior.

Catalog limitation: all 78 Timeline labels and 69 expanded core-family labels exist. Of 1,485 referenced labels, 39 lack exact resources; eight resolve by `_oneshot` fallback; 31 remain absent in installed package and old source (optional mate/Cross Duel/unused fields). They are catalog limitations, not extraction failures. Full missing list/source records are in the extended JSON.

I did not modify or verify runtime Java, compression output, sounds.json or final JAR. Those remain owned by the coordinator/runtime agents. BGM and optional character voice are audited separately and must not be described as included in the SE conversion without separate work.
