package net.mdpromod;

import java.util.ArrayDeque;
import java.util.Comparator;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import java.util.Map.Entry;
import net.mdpromod.spine.CutinRoster;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.EntityType;
import net.minecraft.world.phys.Vec3;
import org.slf4j.LoggerFactory;

final class DuelAnimationScene {
    private final UUID session = UUID.randomUUID();
    private final DuelSceneAudio audio = new DuelSceneAudio(this.session);
    private final Map<Long, DuelVisualState.Card> cards = new LinkedHashMap<>();
    private final Map<Long, Entity> entities = new HashMap<>();
    private final ArrayDeque<DuelVisualState.Event> queue = new ArrayDeque<>();
    private final int[][] counts = new int[2][256];
    private long nextAt;
    private long eventId;
    private int materialReason;
    private long materialReasonAt;
    private int materialCount;
    private int pendingDelayMs;
    private int holdMs;
    private boolean opening = true;
    private boolean openingDrew;
    private long openingDeadline;
    private boolean openingSound;
    private boolean bgmStarted;
    private UUID seat0;
    private UUID seat1;

    void initialize(int main0, int extra0, int main1, int extra1) {
        this.counts[0][1] = main0;
        this.counts[0][64] = extra0;
        this.counts[1][1] = main1;
        this.counts[1][64] = extra1;
        this.opening = true;
        this.openingDrew = false;
        this.openingSound = false;
        this.bgmStarted = false;
        this.openingDeadline = System.nanoTime() / 1000000L + 8000L;
    }

    void enqueue(List<DuelVisualState.Event> events) {
        this.queue.addAll(events);
    }

    int[] codesIn(int side, int location) {
        return this.cards
            .values()
            .stream()
            .filter(c -> c.address().controller() == side && c.address().location() == location)
            .sorted(Comparator.comparingInt(c -> c.address().sequence()))
            .mapToInt(DuelVisualState.Card::code)
            .toArray();
    }

    int[] publicCodesIn(int side, int location) {
        return this.cards
            .values()
            .stream()
            .filter(c -> c.address().controller() == side && c.address().location() == location && (c.publicKnown() || location == 16))
            .sorted(Comparator.comparingInt(c -> c.address().sequence()))
            .mapToInt(DuelVisualState.Card::code)
            .toArray();
    }

    boolean busy() {
        return !this.queue.isEmpty() || System.nanoTime() / 1000000L < this.nextAt;
    }

    boolean openingBusy() {
        return this.opening;
    }

    int[] hand(int side) {
        return this.cards
            .values()
            .stream()
            .filter(c -> c.address().controller() == side && c.address().location() == 2)
            .sorted(Comparator.comparingInt(c -> c.address().sequence()))
            .mapToInt(DuelVisualState.Card::code)
            .toArray();
    }

    void tick(ServerLevel level, DuelFieldEntity field, UUID owner, UUID second) {
        this.seat0 = owner;
        this.seat1 = second;
        long now = System.nanoTime() / 1000000L;
        if (!this.openingSound) {
            this.openingSound = true;
            this.audio.timeline(level, field, "prefab", "DuelDeckAppearance", field.position(), 0, 0);
            this.audio.timeline(level, field, "prefab", "DuelTextStart", field.position(), 650, 0);
        }

        if (now >= this.nextAt) {
            int budget = 64;

            while (!this.queue.isEmpty() && budget-- > 0) {
                DuelVisualState.Event event = this.queue.removeFirst();
                int duration = this.play(level, field, event);
                if (duration != 0) {
                    String group = group(event);
                    boolean adjacent = !this.queue.isEmpty() && group.equals(group(this.queue.peekFirst()));
                    this.nextAt = now + (!adjacent || !group.equals("draw") && !group.equals("material") ? duration + 60 : 120);
                    break;
                }
            }

            field.updateVisualCounts(
                new int[]{this.counts[0][1], this.counts[1][1]},
                new int[]{this.counts[0][64], this.counts[1][64]},
                new int[]{this.counts[0][16], this.counts[1][16]},
                new int[]{this.counts[0][32], this.counts[1][32]}
            );
            if (this.opening && (this.openingDrew && !this.busy() || now > this.openingDeadline)) {
                this.opening = false;
            }

            if (!this.opening && !this.bgmStarted) {
                this.bgmStarted = true;
                this.audio.bgm(level, field, 250);
            }
        }
    }

    private int materialFxKind() {
        if (this.materialReason == 0) {
            return -1;
        } else if (System.nanoTime() / 1000000L - this.materialReasonAt > 4000L) {
            return -1;
        } else if ((this.materialReason & 262144) != 0) {
            return 0;
        } else if ((this.materialReason & 524288) != 0) {
            return 1;
        } else if ((this.materialReason & 2097152) != 0) {
            return 2;
        } else if ((this.materialReason & 268435456) != 0) {
            return 3;
        } else if ((this.materialReason & 1048576) != 0) {
            return 4;
        } else {
            return (this.materialReason & 4194304) != 0 ? 5 : -1;
        }
    }

    private int summonFxKind(int code) {
        if (this.materialReason == 0) {
            return -1;
        } else {
            long now = System.nanoTime() / 1000000L;
            if (now - this.materialReasonAt > 4000L) {
                return -1;
            } else {
                int type = CardRegistry.get(code).map(CardInfo::type).orElse(0);
                if ((type & 64) != 0 && (this.materialReason & 262144) != 0) {
                    return 0;
                } else if ((type & 8192) != 0 && (this.materialReason & 524288) != 0) {
                    return 1;
                } else if ((type & 8388608) != 0 && (this.materialReason & 2097152) != 0) {
                    return 2;
                } else if ((type & 67108864) != 0 && (this.materialReason & 268435456) != 0) {
                    return 3;
                } else if ((type & 128) != 0 && (this.materialReason & 1048576) != 0) {
                    return 4;
                } else {
                    return (type & 16777216) != 0 && (this.materialReason & 4194304) != 0 ? 5 : -1;
                }
            }
        }
    }

    private static String group(DuelVisualState.Event e) {
        return (e.reason() & 272367616L) != 0L && (e.reason() & 8L) != 0L ? "material" : e.kind();
    }

    private int play(ServerLevel level, DuelFieldEntity field, DuelVisualState.Event e) {
        DuelVisualState.Card card = e.card();
        if (card == null) {
            if (e.kind().startsWith("audio_")) {
                return this.audio.semantic(level, field, e);
            } else if (e.kind().equals("shuffle") && e.from() != null) {
                DuelVisualState.Address at = e.from();
                DuelVisualState.Card dummy = new DuelVisualState.Card(-1L, 0, at, false, false);
                Vec3 p = point(field, at, this.counts[at.controller()][at.location()]);
                this.send(level, field, dummy, "shuffle", p, p, at, at, 500, -1, -1, false);
                this.audio.shuffle(level, field, p);
                return 500;
            } else {
                return 0;
            }
        } else {
            DuelVisualState.Address from = e.from();
            DuelVisualState.Address to = e.to();
            String kind = e.kind();
            this.pendingDelayMs = 0;
            this.holdMs = 0;
            long reason = e.reason();
            if ((reason & 8L) != 0L) {
                int bits = (int)(reason & 276692992L);
                if (bits != 0) {
                    this.materialReason |= bits;
                    this.materialReasonAt = System.nanoTime() / 1000000L;
                }
            }

            if (kind.equals("update")) {
                if (card.address().location() == 0) {
                    this.cards.remove(card.id());
                } else {
                    this.cards.put(card.id(), card);
                }

                if (e.to().location() != 0 && e.to().controller() < 2) {
                    this.counts[e.to().controller()][e.to().location()] = e.toCount();
                } else if (e.from() != null && e.from().controller() < 2 && e.from().location() != 0) {
                    this.counts[e.from().controller()][e.from().location()] = Math.max(0, e.fromCount() - 1);
                }

                this.reconcile(level, field);
                return 0;
            } else {
                boolean relocation = switch (kind) {
                    case "draw", "move", "summon", "special_summon", "flip_summon", "set", "position", "reposition", "shuffle", "shuffle_set" -> true;
                    default -> false;
                };
                DuelVisualState.Card old = this.cards.get(card.id());
                if (!kind.equals("reposition")) {
                    this.cards.put(card.id(), card);
                }

                Map<Long, Vec3> oldHandPositions = new HashMap<>();

                for (Entry<Long, Entity> entry : this.entities.entrySet()) {
                    if (entry.getValue() instanceof HandCardEntity) {
                        oldHandPositions.put(entry.getKey(), entry.getValue().position());
                    }
                }

                Entity previous = this.entities.get(card.id());
                Vec3 source = previous != null ? previous.position() : point(field, from, e.fromCount());
                Vec3 destination = point(field, to, e.toCount());
                if (relocation) {
                    if ((kind.equals("summon") || kind.equals("special_summon") || kind.equals("flip_summon") || kind.equals("set"))
                        && old != null
                        && old.address().equals(to)
                        && from.equals(to)) {
                        relocation = false;
                    }

                    if (to.location() == 0) {
                        this.cards.remove(card.id());
                    } else {
                        this.cards.put(card.id(), card);
                    }

                    this.updateCounts(e);

                    while (!this.queue.isEmpty() && this.queue.peekFirst().kind().equals("reposition")) {
                        DuelVisualState.Event correction = this.queue.removeFirst();
                        if (correction.card() != null) {
                            this.cards.put(correction.card().id(), correction.card());
                        }
                    }

                    this.reconcile(level, field);

                    for (Entry<Long, Vec3> entry : oldHandPositions.entrySet()) {
                        if (entry.getKey() != card.id()) {
                            Entity h = this.entities.get(entry.getKey());
                            DuelVisualState.Card snapshot = this.cards.get(entry.getKey());
                            if (h instanceof HandCardEntity && snapshot != null && !(entry.getValue().distanceToSqr(h.position()) < 1.0E-5)) {
                                this.send(
                                    level,
                                    field,
                                    snapshot,
                                    "reposition",
                                    entry.getValue(),
                                    h.position(),
                                    snapshot.address(),
                                    snapshot.address(),
                                    230,
                                    h.getId(),
                                    -1,
                                    false
                                );
                            }
                        }
                    }
                }

                Entity destinationEntity = this.entities.get(card.id());
                int hide = destinationEntity != null ? destinationEntity.getId() : (previous != null ? previous.getId() : -1);
                if (kind.equals("reposition")) {
                    if (from.location() == 2 && !(source.distanceToSqr(destination) < 1.0E-5)) {
                        this.send(level, field, card, "reposition", source, destination, from, to, 230, hide, -1, false);
                        return 0;
                    } else {
                        return 0;
                    }
                } else {
                    if (kind.equals("move")) {
                        if (from.location() == 1 && to.location() == 2) {
                            kind = "search";
                        } else if ((e.reason() & 1L) != 0L) {
                            kind = "destroy";
                        } else if (group(e).equals("material")) {
                            kind = "material";
                        } else if (to.location() == 4 && !isField(from)) {
                            kind = (to.position() & 10) != 0 ? "set" : "summon";
                        }
                    }

                    boolean absorbing = false;
                    if (kind.equals("material")) {
                        int fxKind = this.materialFxKind();
                        if (fxKind >= 0) {
                            absorbing = true;
                            this.materialCount++;
                            kind = "absorb" + fxKind;
                            if (this.materialCount == 1) {
                                Vec3 center0 = field.position().add(0.0, 0.12, 0.0);
                                int expected = 1;

                                for (DuelVisualState.Event queued : this.queue) {
                                    if (group(queued).equals("material")) {
                                        expected++;
                                    } else if (queued.kind().equals("summon") || queued.kind().equals("special_summon")) {
                                        break;
                                    }
                                }

                                this.audio.materialCard(level, field, fxKind, expected, center0);
                                ModNetwork.sendNear(
                                    level,
                                    field.position(),
                                    128.0,
                                    new ModNetwork.SummonEffect(field.getId(), fxKind, 0, 0, 0, center0.x, center0.y, center0.z, 1330)
                                );
                            }
                        }
                    }

                    if (relocation || !kind.equals("summon") && !kind.equals("special_summon") && !kind.equals("set")) {
                        int duration = switch (kind) {
                            case "draw", "search" -> 520;
                            case "summon", "special_summon", "flip_summon" -> 660;
                            case "set" -> 510;
                            case "destroy" -> 750;
                            case "material" -> 650;
                            case "attack" -> this.audio.attackDuration(e);
                            case "attack_declare" -> 1000;
                            case "activate" -> 900;
                            case "reveal" -> 1250;
                            case "position" -> 320;
                            case "shuffle" -> 500;
                            case "target", "equip", "negate", "attack_disabled" -> 350;
                            default -> 560;
                        };
                        int audience = e.audience();
                        boolean forcePublic = false;
                        int audioSummonKind = -1;
                        if (absorbing) {
                            destination = field.position().add(0.0, 0.16, 0.0);
                            duration = Math.max(280, 1330 - 120 * (this.materialCount - 1));
                        }

                        if (kind.equals("summon") || kind.equals("special_summon")) {
                            int fxKind = this.summonFxKind(card.code());
                            audioSummonKind = fxKind;
                            int cutinMs = CutinRoster.durationMs(card.code());
                            if (fxKind >= 0) {
                                Vec3 center = field.position().add(0.0, 0.12, 0.0);
                                int show = SummonTiming.summonMs(fxKind);
                                ModNetwork.sendNear(
                                    level,
                                    field.position(),
                                    128.0,
                                    new ModNetwork.SummonEffect(
                                        field.getId(),
                                        fxKind,
                                        1,
                                        card.code(),
                                        Math.max(1, this.materialCount),
                                        center.x,
                                        center.y,
                                        center.z,
                                        show
                                    )
                                );
                                this.audio.summon(level, field, fxKind, Math.max(1, this.materialCount), card.code(), center);
                                LoggerFactory.getLogger("mdpromod")
                                    .info(
                                        "Summon FX: kind={} card={} materials={} show={}ms at field center",
                                        new Object[]{fxKind, card.code(), this.materialCount, show}
                                    );
                                source = center;
                                duration = 600;
                                this.pendingDelayMs = Math.max(0, show - duration);
                                this.holdMs = show;
                                if (cutinMs > 0) {
                                    ModNetwork.sendNear(
                                        level,
                                        field.position(),
                                        128.0,
                                        new ModNetwork.SummonEffect(
                                            field.getId(), fxKind, 2, card.code(), 0, center.x, center.y, center.z, cutinMs
                                        )
                                    );
                                    this.holdMs = Math.max(this.holdMs, show + cutinMs - 600);
                                    LoggerFactory.getLogger("mdpromod")
                                        .info(
                                            "Cut-in: card={} 接在法阵之后（法阵 {}ms + 演出 {}ms - 落地 {}ms），按住 {}ms",
                                            new Object[]{card.code(), show, cutinMs, 600, this.holdMs}
                                        );
                                }

                                this.materialReason = 0;
                                this.materialCount = 0;
                            } else if (cutinMs > 0) {
                                ModNetwork.sendNear(
                                    level,
                                    field.position(),
                                    128.0,
                                    new ModNetwork.SummonEffect(
                                        field.getId(), -1, 2, card.code(), 0, source.x, source.y, source.z, cutinMs
                                    )
                                );
                                this.pendingDelayMs = cutinMs;
                                this.holdMs = Math.max(this.holdMs, cutinMs);
                                LoggerFactory.getLogger("mdpromod").info("Cut-in: card={} 先演出 {}ms 再落地", card.code(), cutinMs);
                            }
                        }

                        switch (kind) {
                            case "attack":
                                destination = e.target() == null
                                    ? point(field, new DuelVisualState.Address(1 - from.controller(), 2, 0, 1), 1)
                                    : point(field, e.target().address(), 1);
                                forcePublic = true;
                                break;
                            case "attack_declare":
                                kind = "attack_declare";
                                destination = source;
                                forcePublic = true;
                                break;
                            case "activate":
                                kind = from.field() ? "effect" : "effect_reveal";
                                destination = source;
                                forcePublic = true;
                                break;
                            case "reveal":
                                destination = source;
                                forcePublic = true;
                                if (to.location() == 2) {
                                    kind = "search_reveal";
                                }
                                break;
                            case "position":
                            case "flip_summon":
                                kind = "flip";
                                break;
                            case "target":
                            case "equip":
                                kind = "effect";
                                destination = source;
                                break;
                            case "negate":
                                kind = "negate";
                                destination = source;
                                break;
                            case "attack_disabled":
                                kind = "attack_disabled";
                                destination = source;
                                break;
                            case "shuffle":
                            case "shuffle_set":
                                kind = "shuffle";
                                destination = source;
                        }

                        this.send(level, field, card, kind, source, destination, from, to, duration, hide, audience, forcePublic);
                        this.audio.card(level, field, e, kind, source, destination, duration, this.pendingDelayMs, audioSummonKind);
                        if (this.opening && kind.equals("draw")) {
                            this.openingDrew = true;
                        }

                        return Math.max(duration, this.holdMs);
                    } else {
                        this.audio.card(level, field, e, kind, source, destination, 0, 0, this.summonFxKind(card.code()));
                        return 0;
                    }
                }
            }
        }
    }

    private void updateCounts(DuelVisualState.Event e) {
        DuelVisualState.Address from = e.from();
        DuelVisualState.Address to = e.to();
        if (from.controller() < 2 && to.controller() < 2 && (from.location() != to.location() || from.controller() != to.controller())) {
            if (from.location() != 0) {
                this.counts[from.controller()][from.location()] = Math.max(0, e.fromCount() - 1);
            }

            if (to.location() != 0) {
                this.counts[to.controller()][to.location()] = Math.max(0, e.toCount());
            }
        } else if (to.controller() < 2 && e.toCount() > 0) {
            this.counts[to.controller()][to.location()] = e.toCount();
        }
    }

    private UUID seatOf(int side) {
        return side == 1 ? this.seat1 : this.seat0;
    }

    private void reconcile(ServerLevel level, DuelFieldEntity field) {
        this.entities.entrySet().removeIf(entry -> {
            if (this.cards.containsKey(entry.getKey())) {
                return false;
            }

            entry.getValue().discard();
            return true;
        });

        for (DuelVisualState.Card card : this.cards.values()) {
            DuelVisualState.Address at = card.address();
            Entity entity = this.entities.get(card.id());
            boolean hand = at.location() == 2;
            boolean visible = hand || isField(at) || at.location() == 16 || at.location() == 32 || at.location() == 64 && card.publicKnown();
            if (!visible) {
                if (entity != null) {
                    entity.discard();
                    this.entities.remove(card.id());
                }
            } else {
                if (entity != null && (entity instanceof HandCardEntity != hand || entity.isRemoved())) {
                    entity.discard();
                    this.entities.remove(card.id());
                    entity = null;
                }

                Vec3 position = point(field, at, this.counts[at.controller()][at.location()]);
                float yaw = yaw(field, at);
                UUID sidePlayer = this.seatOf(at.controller());
                String ownerText = sidePlayer == null ? "" : sidePlayer.toString();
                Entity var15;
                if (hand) {
                    HandCardEntity h = entity instanceof HandCardEntity existing
                        ? existing
                        : (HandCardEntity)((EntityType)ModEntities.HAND_CARD.get()).create(level);
                    if (h == null) {
                        continue;
                    }

                    h.configure(field.getUUID(), ownerText, at.controller(), at.sequence());
                    h.setPrivateCode(card.code());
                    h.setRevealed(card.publicKnown());
                    h.setCardCode(card.publicKnown() ? card.code() : 0);
                    h.moveTo(position.x, position.y, position.z, yaw, 0.0F);
                    if (entity == null) {
                        level.addFreshEntity(h);
                    }

                    var15 = h;
                } else {
                    DuelCardEntity d = entity instanceof DuelCardEntity existing
                        ? existing
                        : (DuelCardEntity)((EntityType)ModEntities.DUEL_CARD.get()).create(level);
                    if (d == null) {
                        continue;
                    }

                    boolean face = publicFace(card);
                    d.configureDuel(field.getUUID(), ownerText, card.code());
                    d.setCardCode(face ? card.code() : 0);
                    d.setFaceUp(face);
                    d.setDefense(defense(at));
                    d.setZoneId(zoneId(at));
                    d.setDisabled(card.disabled());
                    d.moveTo(position.x, position.y, position.z, yaw, 0.0F);
                    if (entity == null) {
                        level.addFreshEntity(d);
                    }

                    var15 = d;
                }

                this.entities.put(card.id(), (Entity)var15);
                if (sidePlayer != null) {
                    ServerPlayer player = level.getServer().getPlayerList().getPlayer(sidePlayer);
                    if (player != null) {
                        ModNetwork.sendTo(player, new ModNetwork.CardKnowledge(var15.getId(), card.code()));
                    }
                }
            }
        }
    }

    private void send(
        ServerLevel level,
        DuelFieldEntity field,
        DuelVisualState.Card card,
        String kind,
        Vec3 from,
        Vec3 to,
        DuelVisualState.Address a,
        DuelVisualState.Address b,
        int duration,
        int hide,
        int audience,
        boolean forcePublic
    ) {
        long id = ++this.eventId;

        for (ServerPlayer viewer : level.players()) {
            if (!(viewer.distanceToSqr(field) > 9216.0)) {
                UUID viewerId = viewer.getUUID();
                int viewerSide = this.seat0 != null && this.seat0.equals(viewerId) ? 0 : (this.seat1 != null && this.seat1.equals(viewerId) ? 1 : -1);
                if (audience < 0 || viewerSide == audience) {
                    boolean face = forcePublic || publicFace(card) || viewerSide >= 0 && (a.controller() == viewerSide || b.controller() == viewerSide);
                    if (kind.equals("set") && viewerSide != b.controller()) {
                        face = false;
                    }

                    if (!forcePublic && !kind.equals("set") && isField(a) && (a.position() & 5) != 0) {
                        face = true;
                    }

                    float toPitch = pitch(b);
                    if (kind.equals("set")) {
                        toPitch = -90.0F;
                    }

                    ModNetwork.sendTo(
                        viewer,
                        new ModNetwork.DuelAnimation(
                            this.session,
                            field.getId(),
                            id,
                            kind,
                            face ? card.code() : 0,
                            face,
                            from,
                            to,
                            yaw(field, a),
                            yaw(field, b),
                            pitch(a),
                            toPitch,
                            defense(a),
                            defense(b),
                            duration,
                            this.pendingDelayMs,
                            hide,
                            field.getYRot()
                        )
                    );
                }
            }
        }
    }

    static boolean isField(DuelVisualState.Address a) {
        return a.field();
    }

    private static boolean publicFace(DuelVisualState.Card c) {
        DuelVisualState.Address a = c.address();
        return c.publicKnown() || a.location() == 16 || a.location() != 1 && a.location() != 2 && (a.position() & 5) != 0;
    }

    private static boolean defense(DuelVisualState.Address a) {
        return (a.location() & 4) != 0 && (a.location() & 128) == 0 && (a.position() & 12) != 0;
    }

    private static float pitch(DuelVisualState.Address a) {
        if (a.location() == 2) {
            return 0.0F;
        } else {
            return !a.overlay() && (a.position() & 5) == 0 ? -90.0F : 90.0F;
        }
    }

    private static float yaw(DuelFieldEntity field, DuelVisualState.Address a) {
        return field.getYRot() + (a.controller() == 1 ? 180 : 0);
    }

    static String zoneId(DuelVisualState.Address at) {
        int loc = at.location() & 127;
        String p = "p" + (at.controller() + 1);

        return switch (loc) {
            case 1 -> p + "_deck";
            case 4 -> at.sequence() < 5
                ? p + "_mzone_" + at.sequence()
                : (at.sequence() == (at.controller() == 0 ? 5 : 6) ? "shared_emz_left" : "shared_emz_right");
            case 8 -> at.sequence() == 5 ? p + "_field" : p + "_szone_" + (at.sequence() == 6 ? 0 : (at.sequence() == 7 ? 4 : at.sequence()));
            case 16 -> p + "_graveyard";
            case 32 -> p + "_banished";
            case 64 -> p + "_extra";
            default -> "";
        };
    }

    static Vec3 handPoint(DuelFieldEntity field, int side, int index, int count) {
        double spacing = Math.min(0.85, 11.0 / Math.max(1, count - 1));
        double x = (index - Math.max(0, count - 1) / 2.0) * spacing;
        if (side == 1) {
            x = -x;
        }

        Vec3 rotated = new Vec3(x, 0.0, side == 0 ? 7.75 : -7.75).yRot(-((float)Math.toRadians(field.getYRot())));
        return field.position().add(rotated).add(0.0, 1.375, 0.0);
    }

    static Vec3 point(DuelFieldEntity field, DuelVisualState.Address at, int count) {
        if (at.location() == 2) {
            return handPoint(field, at.controller(), at.sequence(), count);
        }

        Vec3 base = DuelFieldLayout.byId(zoneId(at)).map(field::zoneWorldPosition).orElse(field.position().add(0.0, 0.2, 0.0));

        double height = switch (at.location()) {
            case 1, 64 -> 0.12 + Math.min(0.35, Math.max(0, count) * 0.008);
            case 16, 32 -> 0.13 + Math.max(0, at.sequence()) * 0.008;
            default -> (at.location() & 128) != 0 ? 0.09 + 0.005 * Math.max(0, at.position()) : 0.12;
        };
        return base.add(0.0, height, 0.0);
    }

    void clear(ServerLevel level, DuelFieldEntity field) {
        this.audio.clear();
        ModNetwork.sendNear(level, field.position(), 128.0, ModNetwork.DuelSound.stop(this.session, field.getId(), ""));
        ModNetwork.sendNear(level, field.position(), 96.0, new ModNetwork.DuelAnimationClear(this.session));

        for (Entity entity : this.entities.values()) {
            entity.discard();
        }

        this.entities.clear();
        this.cards.clear();
        this.queue.clear();
        this.nextAt = 0L;
        this.opening = false;
        this.bgmStarted = false;
    }
}
