package net.mdpromod;

import java.util.HashSet;
import java.util.Set;
import java.util.UUID;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.world.phys.Vec3;

final class DuelSceneAudio {
    private final UUID session;
    private final Set<Long> landed = new HashSet<>();
    private boolean engageOnMove;
    private boolean ghostAlt;
    private String negateEffect = "";
    private String additionalNegateEffect = "";
    private String fusionMaterialEffect = "";

    DuelSceneAudio(UUID session) {
        this.session = session;
    }

    void clear() {
        this.landed.clear();
        this.clearCallbacks();
    }

    void bgm(ServerLevel level, DuelFieldEntity field, int delayMs) {
        ModNetwork.sendNear(level, field.position(), 128.0, ModNetwork.DuelSound.bgm(this.session, field.getId(), delayMs));
    }

    private void clearCallbacks() {
        this.engageOnMove = this.ghostAlt = false;
        this.negateEffect = this.additionalNegateEffect = this.fusionMaterialEffect = "";
    }

    void once(ServerLevel level, DuelFieldEntity field, String key, Vec3 at, int delay) {
        this.once(level, field, key, at, delay, 1.0F);
    }

    private void once(ServerLevel level, DuelFieldEntity field, String key, Vec3 at, int delay, float volume) {
        if (key != null && !key.isBlank()) {
            ModNetwork.sendNear(
                level,
                field.position(),
                128.0,
                new ModNetwork.DuelSound(this.session, field.getId(), key, at, Math.max(0, delay), 0, volume, 1.0F, false, false)
            );
        }
    }

    void timeline(ServerLevel level, DuelFieldEntity field, String group, String id, Vec3 at, int delay, int trim) {
        for (DuelAudioTimelines.Cue cue : DuelAudioTimelines.cues(group, id)) {
            if (cue.timeMs() >= trim) {
                ModNetwork.sendNear(
                    level,
                    field.position(),
                    128.0,
                    new ModNetwork.DuelSound(
                        this.session,
                        field.getId(),
                        cue.key(),
                        at,
                        delay + Math.max(0, cue.timeMs() - trim),
                        cue.durationMs(),
                        cue.volume(),
                        cue.pitch(),
                        cue.loop(),
                        false
                    )
                );
            }
        }
    }

    int semantic(ServerLevel level, DuelFieldEntity field, DuelVisualState.Event e) {
        Vec3 at = field.position().add(0.0, 1.0, 0.0);
        int side = e.from().controller();
        int n = (int)e.reason();
        switch (e.kind()) {
            case "audio_turn":
                field.setTurnSide(side);
                if (n < 2) {
                    return 0;
                }

                this.timeline(level, field, "prefab", "DuelTurnChange0" + side, at, 0, 0);
                return 650;
            case "audio_phase":
                String phase = DuelAudioRules.phase(n, side);
                if (phase.isEmpty()) {
                    return 0;
                }

                field.setCurrentPhase(side == 0 ? "p1" : "p2", DuelAudioRules.phaseName(n));
                this.once(level, field, phase, at, 0);
                return 280;
            case "audio_lp":
                field.setLifePoints(side == 0 ? "p1" : "p2", e.toCount());
                if (n == 0) {
                    return 0;
                }

                if (n < 0) {
                    this.once(level, field, "SE_COST_DAMAGE", at, 0);
                }

                this.once(level, field, "SE_LP_" + (n > 0 ? "RECOVERY" : "COUNT") + (side == 0 ? "_PLAYER" : "_RIVAL"), at, 1100);
                return 2300;
            case "audio_coin":
                if (e.fromCount() == 0) {
                    this.once(level, field, "SE_COIN_THROW", at, 0);
                }

                this.once(level, field, n == 0 ? "SE_COIN_DECIDE_02" : "SE_COIN_DECIDE", at, 2000);
                return e.fromCount() + 1 == e.toCount() ? 3000 : 0;
            case "audio_dice":
                this.once(level, field, "SE_DICE_ROLL", at, 0);
                this.once(level, field, "SE_DICE_DECIDE", at, 600);
                return 2000;
            case "audio_counter":
                this.once(level, field, "SE_CARD_COUNTER", at, 0);
                return 0;
            case "audio_stat":
                this.once(level, field, n > 0 ? "SE_BUFF_ATTACK" : "SE_DEBUFF_ATTACK", at, 0);
                return 0;
            case "audio_characteristic":
                CardInfo info = e.target() == null ? null : CardRegistry.get(e.target().code()).orElse(null);
                int original = info == null ? e.fromCount() : (n == 64 ? info.attribute() : info.race());
                if (e.toCount() != original) {
                    this.once(level, field, "SE_BUFF_CHANGE", at, 0);
                }

                return 0;
            case "audio_chain_stack":
                this.timeline(level, field, "prefab", "ChainSpot", at, 0, 0);
                if (n < 2) {
                    return 0;
                }

                if (n == 2) {
                    this.timeline(level, field, "prefab", "DuelChainStack01", at, 0, 0);
                } else {
                    this.once(level, field, "SE_DUELCHAIN_STACK02", at, 17);
                    this.once(level, field, "SE_DUEL_CHAIN_NUMEFF_0" + (n == 3 ? 1 : (n == 4 ? 2 : 3)), at, 767);
                }

                return 950;
            case "audio_chain_resolve":
                int delay = 0;
                if (e.fromCount() > 1) {
                    int skip = n < e.fromCount() && n > 1 ? 830 : 0;
                    this.timeline(level, field, "prefab", "DuelChainResolve0" + Math.min(3, n), at, 0, skip);
                    delay = skip == 0 ? 1400 : 570;
                }

                this.fusionMaterialEffect = "";
                if (e.target() != null) {
                    this.effect(level, field, e.target(), at, delay, n, e.toCount());
                }

                return delay;
            case "audio_chain_end":
                this.clearCallbacks();
                return 0;
            case "audio_win":
                String special = e.toCount() > 0 ? "SpecialWinCommonCard01" : DuelAudioRules.specialWin(n);
                int wait = special.isEmpty() ? 0 : specialWinDuration(special);
                if (!special.isEmpty()) {
                    this.timeline(level, field, "specialwin", special, at, 0, 0);
                }

                this.once(level, field, side == 2 ? "SE_DUEL_DRAW" : (side != 0 && n != 4 ? "SE_DUEL_LOSE" : "SE_DUEL_WIN"), at, wait);
                return wait + 500;
            default:
                return 0;
        }
    }

    private static int specialWinDuration(String timeline) {
        return switch (timeline) {
            case "33396948" -> 9600;
            case "94212438" -> 8600;
            case "53334641" -> 5000;
            case "96637156" -> 5400;
            case "SpecialWinCommonCard03" -> 3120;
            default -> 3110;
        };
    }

    void shuffle(ServerLevel level, DuelFieldEntity field, Vec3 at) {
        int[] starts = new int[]{0, 44, 89, 141};

        for (int i = 1; i <= 4; i++) {
            this.once(level, field, "SE_CARD_MOVE_0" + i, at, starts[i - 1]);
        }
    }

    void summon(ServerLevel level, DuelFieldEntity field, int kind, int count, int code, Vec3 at) {
        int stars = CardRegistry.get(code).map(CardInfo::level).orElse(8) & 0xFF;

        String timeline = switch (kind) {
            case 0 -> count <= 5 ? "SummonFusion0" + count + "_01" : "FusionNum";
            case 1 -> count > 0 ? "SummonSynchro01" : "SummonSynchro02";
            case 2 -> "SummonXYZ0" + Math.min(3, count) + "_01";
            case 3 -> "SummonLink0" + Math.min(3, Math.max(1, stars)) + "_01";
            case 4 -> count > 0 ? "SummonRitual01" : "SummonRitual02";
            case 5 -> "SummonPendulum01";
            default -> "";
        };
        int trim = kind != 5 && count != 0 ? 1330 : 0;
        this.timeline(level, field, "prefab", timeline, at, 0, trim);
        if (kind == 1) {
            String circle = stars < 5 ? "SummonSynchroCircle01_01" : (stars < 9 ? "SummonSynchroCircle02_01" : "SummonSynchroCircle03_01");
            this.timeline(level, field, "prefab", circle, at, 0, trim);
        }

        if (kind == 4 && count > 0) {
            for (int trail : DuelAudioRules.ritualTrails(count)) {
                this.timeline(level, field, "prefab", "RitualTrailIn0" + trail + "_01", at, 1500, 0);
            }
        }
    }

    void materialCard(ServerLevel level, DuelFieldEntity field, int kind, int count, Vec3 at) {
        String key = DuelAudioRules.materialCard(kind, count);
        if (kind == 0 && !this.fusionMaterialEffect.isEmpty()) {
            key = this.fusionMaterialEffect;
            this.fusionMaterialEffect = "";
        }

        this.once(level, field, key, at, DuelAudioRules.materialCardDelay(kind, count, key));
    }

    void card(ServerLevel level, DuelFieldEntity field, DuelVisualState.Event e, String kind, Vec3 from, Vec3 to, int duration, int delay, int summonKind) {
        if (e.audience() < 0) {
            int code = e.card().code();
            CardInfo info = CardRegistry.get(code).orElse(null);
            int type = info == null ? 0 : info.type();

            boolean movement = switch (e.kind()) {
                case "move", "draw", "summon", "special_summon", "set" -> !e.from().equals(e.to());
                default -> false;
            };
            if (e.kind().equals("activate")) {
                this.once(level, field, "SE_CARDVIEW_01", from, 0);
            }

            if (e.kind().equals("selected") || e.kind().equals("target") && e.reason() != 96L) {
                this.once(level, field, "SE_CEMETERY_CARD", from, 0);
            }

            if (e.kind().equals("position") || e.kind().equals("flip_summon")) {
                this.once(level, field, (e.to().position() & 3) != 0 ? "SE_DISP_ATTACK" : "SE_DISP_DEFENS", from, 0, 0.6F);
            }

            if (movement && this.engageOnMove) {
                this.engageOnMove = false;
                this.once(level, field, "SE_EV_ENGAGE", from, 383);
            }

            if (movement && !e.to().field()) {
                this.landed.remove(e.card().id());
            }

            if (kind.equals("draw") || kind.equals("search")) {
                this.once(level, field, "SE_CARD_MOVE_0" + (1 + Math.floorMod(e.card().id(), 4)), from, 0);
            }

            if (movement && kind.equals("destroy") && (e.from().field() || e.from().location() == 2)) {
                this.once(level, field, "SE_CARDBREAK_01", from, 0);
            }

            if (movement && (e.reason() & 2L) != 0L) {
                this.once(level, field, "SE_SUMMON_ADVANCE", from, 0);
            }

            if (movement && e.from().location() == 0) {
                this.once(level, field, (type & 16384) != 0 ? "SE_CARD_TOKEN_SUMMON" : "SE_CARDCHEAT_APPEAR", from, 0);
            }

            if (movement && e.to().location() == 0) {
                this.once(level, field, "SE_CARD_TOKEN_BREAK", from, 0);
            }

            if (movement && e.from().overlay() && !e.to().overlay() && (e.reason() & 1024L) == 0L) {
                this.once(level, field, "SE_CARD_XYZ_OUT", from, 0);
            } else if (movement && !e.from().overlay() && e.to().overlay()) {
                this.once(level, field, "SE_CARD_XYZ_IN", to, duration);
            }

            if (kind.startsWith("absorb")) {
                int k = Integer.parseInt(kind.substring(6));

                String name = switch (k) {
                    case 0 -> "FUS";
                    case 1 -> "SYNC";
                    case 2 -> "XYZ";
                    case 3 -> "LINK";
                    case 4 -> "RITUAL";
                    default -> "";
                };
                this.once(level, field, name.isEmpty() ? "" : "SE_SUMMON_" + name + "_MATERIAL", from, 0);
            }

            if (movement && (e.from().location() == 16 || e.from().location() == 32)) {
                this.once(level, field, e.from().location() == 16 ? "SE_CEMETARY_GOOUT" : "SE_EXCLUSION_GOOUT", from, 0);
            }

            if (movement && (e.to().location() == 16 || e.to().location() == 32)) {
                this.once(level, field, e.to().location() == 16 ? "SE_CEMETARY_ABSORB" : "SE_EXCLUSION_ABSORB", to, delay + duration);
            }

            switch (kind) {
                case "summon":
                case "special_summon":
                    if (this.landed.add(e.card().id())) {
                        this.once(
                            level,
                            field,
                            DuelAudioRules.land(kind.equals("special_summon") || summonKind >= 0, summonKind, type, info == null ? 0 : info.level()),
                            to,
                            delay + duration
                        );
                    }
                    break;
                case "set":
                    if (this.landed.add(e.card().id())) {
                        this.once(level, field, "SE_LAND_MT_SET", to, delay + duration);
                    }
                    break;
                case "search_reveal":
                case "reveal":
                    this.once(level, field, "SE_CARDVIEW_02", from, 0);
                    break;
                case "negate":
                    this.once(level, field, this.negateEffect, from, 0);
                    if (this.ghostAlt) {
                        this.once(level, field, "SE_EV_037_SPECIAL", from, 0);
                    }

                    this.once(level, field, this.additionalNegateEffect, from, 0);
                    this.negateEffect = this.additionalNegateEffect = "";
                    this.ghostAlt = false;
                    this.once(level, field, "SE_EFFECT_INVALID", from, 0);
                    break;
                case "shuffle":
                    this.shuffle(level, field, from);
                    break;
                case "attack_declare":
                    if (e.target() == null) {
                        this.once(level, field, "SE_DA_TEXT", from, 0);
                    }
                    break;
                case "attack":
                    this.attack(level, field, e, info, from, to);
            }
        }
    }

    int attackDuration(DuelVisualState.Event e) {
        CardInfo info = CardRegistry.get(e.card().code()).orElse(null);
        String family = attackFamily(e, info);
        int impact = family.isEmpty() ? (e.fromCount() < 2000 ? 400 : 650) : DuelAudioRules.finalImpactMs(family);
        return Math.round(impact / 0.55F);
    }

    private static String attackFamily(DuelVisualState.Event e, CardInfo info) {
        return DuelAudioRules.finalAttack(e.card().code(), info == null ? 0 : info.alias(), (e.reason() & 2L) != 0L);
    }

    private void attack(ServerLevel level, DuelFieldEntity field, DuelVisualState.Event e, CardInfo info, Vec3 from, Vec3 to) {
        String family = attackFamily(e, info);
        if (!family.isEmpty()) {
            this.once(level, field, "SE_MONSTERATTACK_" + family + "_01", from, 0);
            this.once(level, field, "SE_MONSTERATTACK_" + family + "_02", to, DuelAudioRules.finalImpactMs(family));
        } else {
            int actualAttribute = (int)(e.reason() >>> 8);
            String attribute = DuelAudioRules.attribute(actualAttribute != 0 ? actualAttribute : (info == null ? 0 : info.attribute()));
            String base = "SE_ATTACK_A_" + attribute + (e.fromCount() < 2000 ? "_DEFAULT_0" : "_SPECIAL_0");
            this.once(level, field, base + "1", from, 0);
            String hit = e.target() == null
                ? (e.from().controller() == 0 ? "SE_DIRECT_ATTACK_RIVAL" : "SE_DIRECT_ATTACK_PLAYER")
                : ((e.reason() & 1L) != 0L ? "SE_ATTACK_GUARD" : base + "2");
            this.once(level, field, hit, to, e.fromCount() < 2000 ? 400 : 650);
        }
    }

    private void effect(ServerLevel level, DuelFieldEntity field, DuelVisualState.Card card, Vec3 at, int delay, int chain, int flags) {
        int alias = CardRegistry.get(card.code()).map(CardInfo::alias).orElse(0);
        int code = DuelAudioRules.effectCode(card.code(), alias);
        int originalCode = alias > 0 ? alias : card.code();
        this.fusionMaterialEffect = DuelAudioRules.specialFusionMaterialCard(card.code(), originalCode);
        if (code != 63166095 && code != 63166096) {
            if (chain <= 1 || code != 40366667 && code != 97045737) {
                if (chain > 1 && (code == 73642296 || card.code() == 73642297)) {
                    this.negateEffect = "SE_EV_GHOSTBELLE";
                    this.ghostAlt = card.code() == 73642297;
                } else {
                    this.once(level, field, DuelAudioRules.effect(code, card.address().controller(), chain, (flags & 1) != 0, (flags & 2) != 0), at, delay);
                }
            } else {
                this.additionalNegateEffect = code == 40366667 ? "SE_EV_044_NORMAL" : "SE_EV_043_NORMAL";
            }
        } else {
            this.engageOnMove = true;
        }
    }
}
