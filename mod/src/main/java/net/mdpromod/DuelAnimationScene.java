package net.mdpromod;

import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.phys.Vec3;
import net.mdpromod.spine.CutinRoster;

import java.util.*;

/** Plays protocol events in order, without slowing the rules engine or its reader thread. */
final class DuelAnimationScene {
    private final UUID session = UUID.randomUUID();
    private final DuelSceneAudio audio = new DuelSceneAudio(session);
    private final Map<Long, DuelVisualState.Card> cards = new LinkedHashMap<>();
    private final Map<Long, Entity> entities = new HashMap<>();
    private final ArrayDeque<DuelVisualState.Event> queue = new ArrayDeque<>();
    private final int[][] counts = new int[2][256];
    private long nextAt, eventId;
    /**
     * 最近的"素材用法"：MD 就是靠材料卡的 reason 位判断这是融合/同调/超量/连接/仪式/灵摆召唤，
     * 这里同款记下来，召唤事件到来时据此挑演出（见 ClientSummonEffects）。
     */
    private int materialReason;
    private long materialReasonAt;
    /** 本次召唤消耗的素材数量（融合按它决定法阵层数、超量按它叠素材）。 */
    private int materialCount;
    /** 本次飞行要延迟多久才开始（召唤落地要等中央演出放完）。 */
    private int pendingDelayMs;
    /** 这条事件播完还要按住多少毫秒（召唤演出放完之前不让别的事件插进来，和 MD 一样）。 */
    private int holdMs;
    /** 开局发牌演出：从建场开始到初始手牌全部落地为止。 */
    private boolean opening = true;
    private boolean openingDrew;
    private long openingDeadline;
    private boolean openingSound;
    private boolean bgmStarted;

    void initialize(int main0, int extra0, int main1, int extra1) {
        counts[0][1] = main0; counts[0][64] = extra0;
        counts[1][1] = main1; counts[1][64] = extra1;
        opening = true;
        openingDrew = false;
        openingSound = false;
        bgmStarted = false;
        openingDeadline = System.nanoTime() / 1_000_000L + 8000;
    }

    void enqueue(List<DuelVisualState.Event> events) { queue.addAll(events); }

    /** 某个公开卡堆里现有的卡号（按序号排好），给"查看全部"用。 */
    int[] codesIn(int side, int location) {
        return cards.values().stream()
                .filter(c -> c.address().controller() == side && c.address().location() == location)
                .sorted(Comparator.comparingInt(c -> c.address().sequence()))
                .mapToInt(DuelVisualState.Card::code).toArray();
    }

    /**
     * 同上，但只保留"对外公开"的卡。
     * 对手的卡堆清单要用这个：墓地/表侧场上的卡是公开信息，里侧盖卡、卡组内容不是。
     */
    int[] publicCodesIn(int side, int location) {
        return cards.values().stream()
                .filter(c -> c.address().controller() == side && c.address().location() == location
                        // 墓地在规则上就是公开信息（无论卡面朝向记录成什么）
                        && (c.publicKnown() || location == 16))
                .sorted(Comparator.comparingInt(c -> c.address().sequence()))
                .mapToInt(DuelVisualState.Card::code).toArray();
    }
    boolean busy() { return !queue.isEmpty() || System.nanoTime() / 1_000_000L < nextAt; }
    /** 开局发牌是否还在演：决斗盘据此保持「游戏中」并锁住阶段键。 */
    boolean openingBusy() { return opening; }
    int[] hand(int side) {
        return cards.values().stream().filter(c -> c.address().controller() == side
                && c.address().location() == 2)
                .sorted(Comparator.comparingInt(c -> c.address().sequence()))
                .mapToInt(DuelVisualState.Card::code).toArray();
    }

    void tick(ServerLevel level, DuelFieldEntity field, UUID owner) {
        long now = System.nanoTime() / 1_000_000L;
        if (!openingSound) {
            openingSound = true;
            audio.timeline(level, field, "prefab", "DuelDeckAppearance", field.position(), 0, 0);
            audio.timeline(level, field, "prefab", "DuelTextStart", field.position(), 650, 0);
        }
        if (now < nextAt) return;
        int budget = 64;
        while (!queue.isEmpty() && budget-- > 0) {
            var event = queue.removeFirst();
            int duration = play(level, field, owner, event);
            // Sequence corrections are metadata; adjacent draws/materials may overlap naturally.
            if (duration == 0) continue;
            String group = group(event);
            boolean adjacent = !queue.isEmpty() && group.equals(group(queue.peekFirst()));
            nextAt = now + (adjacent && (group.equals("draw") || group.equals("material"))
                    ? 120 : duration + 60);
            break;
        }
        field.updateVisualCounts(new int[]{counts[0][1], counts[1][1]},
                new int[]{counts[0][64], counts[1][64]},
                new int[]{counts[0][16], counts[1][16]},
                new int[]{counts[0][32], counts[1][32]});
        // 初始手牌全部落位（或兜底超时）之后，开局演出才算结束
        if (opening && ((openingDrew && !busy()) || now > openingDeadline)) opening = false;
        if (!opening && !bgmStarted) {
            bgmStarted = true;
            // Leave a short release window after the final opening SE/card land.
            audio.bgm(level, field, DuelBgmRules.START_DELAY_MS);
        }
    }

    /** 只按素材的 reason 位判断这次是哪类召唤（素材阶段怪兽还没出场，用不到卡的类型）。 */
    private int materialFxKind() {
        if (materialReason == 0) return -1;
        if (System.nanoTime() / 1_000_000L - materialReasonAt > 4000) return -1;   // 超过 4 秒就不认了
        if ((materialReason & 0x40000) != 0) return 0;       // 融合
        if ((materialReason & 0x80000) != 0) return 1;       // 同调
        if ((materialReason & 0x200000) != 0) return 2;      // 超量
        if ((materialReason & 0x10000000) != 0) return 3;    // 连接
        if ((materialReason & 0x100000) != 0) return 4;      // 仪式
        if ((materialReason & 0x400000) != 0) return 5;      // 灵摆
        return -1;
    }

    /**
     * 这次召唤该放哪种演出：0 融合 / 1 同调 / 2 超量 / 3 连接 / 4 仪式 / 5 灵摆 / -1 不放。
     * 判据和 MD 的 {@code TypeMatchReason} 一致：卡的类型位与材料卡的 reason 位必须成对出现，
     * 且素材是在最近 4 秒内被消耗的（素材 Move 先于 MSG_SPSUMMONING 到达）。
     */
    private int summonFxKind(int code) {
        if (materialReason == 0) return -1;
        long now = System.nanoTime() / 1_000_000L;
        if (now - materialReasonAt > 4000) return -1;
        int type = CardRegistry.get(code).map(CardInfo::type).orElse(0);
        if ((type & 0x40) != 0 && (materialReason & 0x40000) != 0) return 0;          // 融合
        if ((type & 0x2000) != 0 && (materialReason & 0x80000) != 0) return 1;        // 同调
        if ((type & 0x800000) != 0 && (materialReason & 0x200000) != 0) return 2;     // 超量
        if ((type & 0x4000000) != 0 && (materialReason & 0x10000000) != 0) return 3;  // 连接
        if ((type & 0x80) != 0 && (materialReason & 0x100000) != 0) return 4;         // 仪式
        if ((type & 0x1000000) != 0 && (materialReason & 0x400000) != 0) return 5;    // 灵摆
        return -1;
    }

    private static String group(DuelVisualState.Event e) {
        if ((e.reason() & (0x40000L | 0x80000L | 0x100000L | 0x200000L | 0x10000000L)) != 0
                && (e.reason() & 8) != 0) return "material";
        return e.kind();
    }

    private int play(ServerLevel level, DuelFieldEntity field, UUID owner, DuelVisualState.Event e) {
        var card = e.card();
        if (card == null) {
            if (e.kind().startsWith("audio_")) return audio.semantic(level, field, e);
            if (!e.kind().equals("shuffle") || e.from() == null) return 0;
            var at = e.from();
            var dummy = new DuelVisualState.Card(-1, 0, at, false, false);
            Vec3 p = point(field, at, counts[at.controller()][at.location()]);
            send(level, field, owner, dummy, "shuffle", p, p, at, at, 500, -1, -1, false);
            audio.shuffle(level, field, p);
            return 500;
        }
        var from = e.from(); var to = e.to();
        String kind = e.kind();
        pendingDelayMs = 0;
        holdMs = 0;
        // 素材被消耗（reason 带 MATERIAL）时记下这次用的是哪种召唤方式
        long reason = e.reason();
        if ((reason & 0x8L) != 0) {
            int bits = (int) (reason & 0x107E0000L);
            if (bits != 0) {
                materialReason |= bits;
                materialReasonAt = System.nanoTime() / 1_000_000L;
            }
        }
        if (kind.equals("update")) {
            if (card.address().location() == 0) cards.remove(card.id());
            else cards.put(card.id(), card);
            if (e.to().location() != 0 && e.to().controller() < 2)
                counts[e.to().controller()][e.to().location()] = e.toCount();
            else if (e.from() != null && e.from().controller() < 2 && e.from().location() != 0)
                counts[e.from().controller()][e.from().location()] = Math.max(0, e.fromCount() - 1);
            reconcile(level, field, owner);
            return 0;
        }
        boolean relocation = switch (kind) {
            case "draw", "move", "summon", "special_summon", "flip_summon", "set", "position", "reposition", "shuffle", "shuffle_set" -> true;
            default -> false;
        };
        var old = cards.get(card.id());
        if (!kind.equals("reposition")) cards.put(card.id(), card);
        Map<Long, Vec3> oldHandPositions = new HashMap<>();
        for (var entry : entities.entrySet()) if (entry.getValue() instanceof HandCardEntity)
            oldHandPositions.put(entry.getKey(), entry.getValue().position());
        Entity previous = entities.get(card.id());
        Vec3 source = previous != null ? previous.position() : point(field, from, e.fromCount());
        Vec3 destination = point(field, to, e.toCount());
        if (relocation) {
            if (kind.equals("summon") || kind.equals("special_summon") || kind.equals("flip_summon") || kind.equals("set")) {
                if (old != null && old.address().equals(to) && from.equals(to)) relocation = false;
            }
            if (to.location() == 0) cards.remove(card.id());
            else cards.put(card.id(), card);
            updateCounts(e);
            // Index corrections belong to the same movement, before the hand is laid out again.
            while (!queue.isEmpty() && queue.peekFirst().kind().equals("reposition")) {
                var correction = queue.removeFirst();
                if (correction.card() != null) cards.put(correction.card().id(), correction.card());
            }
            reconcile(level, field, owner);
            for (var entry : oldHandPositions.entrySet()) {
                if (entry.getKey() == card.id()) continue;
                Entity h = entities.get(entry.getKey());
                var snapshot = cards.get(entry.getKey());
                if (!(h instanceof HandCardEntity) || snapshot == null
                        || entry.getValue().distanceToSqr(h.position()) < 1.0E-5) continue;
                send(level, field, owner, snapshot, "reposition", entry.getValue(), h.position(),
                        snapshot.address(), snapshot.address(), 230, h.getId(), -1, false);
            }
        }
        Entity destinationEntity = entities.get(card.id());
        int hide = destinationEntity != null ? destinationEntity.getId()
                : previous != null ? previous.getId() : -1;
        if (kind.equals("reposition")) {
            if (from.location() != 2 || source.distanceToSqr(destination) < 1.0E-5) return 0;
            send(level, field, owner, card, "reposition", source, destination,
                    from, to, 230, hide, -1, false);
            return 0;
        }
        if (kind.equals("move")) {
            if (from.location() == 1 && to.location() == 2) kind = "search";
            else if ((e.reason() & 1) != 0) kind = "destroy";
            else if (group(e).equals("material")) kind = "material";
            else if (to.location() == DuelVisualState.MONSTER && !isField(from)) {
                kind = (to.position() & 0xA) != 0 ? "set" : "summon";
            }
        }
        // 素材被消耗：不飞向墓地/额外，而是**被吸向场地中央**（MD 的"素材展示段"）
        boolean absorbing = false;
        if (kind.equals("material")) {
            int fxKind = materialFxKind();
            if (fxKind >= 0) {
                absorbing = true;
                materialCount++;
                kind = "absorb" + fxKind;                 // 客户端据此上色 + 撒粒子
                if (materialCount == 1) {
                    Vec3 center0 = field.position().add(0.0, 0.12, 0.0);
                    int expected = 1;
                    for (var queued : queue) {
                        if (group(queued).equals("material")) expected++;
                        else if (queued.kind().equals("summon") || queued.kind().equals("special_summon")) break;
                    }
                    audio.materialCard(level, field, fxKind, expected, center0);
                    ModNetwork.sendNear(level, field.position(), 128,
                            new ModNetwork.SummonEffect(field.getId(), fxKind, 0, 0, 0,
                                    center0.x, center0.y, center0.z, SummonTiming.MATERIAL_MS));
                }
            }
        }
        if (!relocation && (kind.equals("summon") || kind.equals("special_summon") || kind.equals("set"))) {
            audio.card(level, field, e, kind, source, destination, 0, 0, summonFxKind(card.code()));
            return 0; // A completion/annotation never makes the same card fly twice.
        }
        int duration = switch (kind) {
            case "draw", "search" -> 520;
            case "summon", "special_summon", "flip_summon" -> 660;
            case "set" -> 510;
            case "destroy" -> 750;
            case "material" -> 650;
            case "attack" -> audio.attackDuration(e);
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
        // 素材吸入：把素材卡的目标改到场地中央，并压短时长（视觉上"被吸走"）
        if (absorbing) {
            destination = field.position().add(0.0, 0.16, 0.0);
            // MD 的材料段固定 1.33 秒（TimeLineManager 的 WaitForSeconds(1.33f)）：
            // 素材几张就把它摊成几段，相邻的叠 120 ms。
            duration = Math.max(280, SummonTiming.MATERIAL_MS - 120 * (materialCount - 1));
        }
        // 正规出场（融合/同调/超量/连接/仪式/灵摆）→ 场地中央的大演出 + 类型细节，
        // 怪兽本身改成**从场地中央落下来**（代替原来"从卡组/额外飞过来"）
        if (kind.equals("summon") || kind.equals("special_summon")) {
            int fxKind = summonFxKind(card.code());
            audioSummonKind = fxKind;
            int cutinMs = CutinRoster.durationMs(card.code());
            if (fxKind >= 0) {
                Vec3 center = field.position().add(0.0, 0.12, 0.0);
                // 召唤段长度 = MD 那套召唤音效的长度（融合 3.31 s … 连接 6.77 s）
                int show = SummonTiming.summonMs(fxKind);
                ModNetwork.sendNear(level, field.position(), 128,
                        new ModNetwork.SummonEffect(field.getId(), fxKind, 1, card.code(),
                                Math.max(1, materialCount), center.x, center.y, center.z, show));
                audio.summon(level, field, fxKind, Math.max(1, materialCount), card.code(), center);
                org.slf4j.LoggerFactory.getLogger("mdpromod")
                        .info("Summon FX: kind={} card={} materials={} show={}ms at field center",
                                fxKind, card.code(), materialCount, show);
                source = center;                 // 从中央落下
                duration = SummonTiming.LAND_MS;  // MD：落地位移 0.2 s + 翻转 0.4 s
                // 演出放完再落地（客户端会连实体隐藏一起延迟，所以落地前怪兽不出现）
                pendingDelayMs = Math.max(0, show - duration);
                // 整段演出放完才继续播后面的事件
                holdMs = show;
                // 王牌登场演出：正规出场时它接在法阵之后、和落地同时开始。
                // 起播时刻交给客户端排（它知道法阵从哪一刻开始），服务端只负责**按住**整段，
                // 别让后面的消息在演出放完之前推进。
                if (cutinMs > 0) {
                    ModNetwork.sendNear(level, field.position(), 128,
                            new ModNetwork.SummonEffect(field.getId(), fxKind, 2, card.code(),
                                    0, center.x, center.y, center.z, cutinMs));
                    holdMs = Math.max(holdMs, show + cutinMs - SummonTiming.LAND_MS);
                    org.slf4j.LoggerFactory.getLogger("mdpromod")
                            .info("Cut-in: card={} 接在法阵之后（法阵 {}ms + 演出 {}ms - 落地 {}ms），"
                                            + "按住 {}ms", card.code(), show, cutinMs,
                                    SummonTiming.LAND_MS, holdMs);
                }
                materialReason = 0;
                materialCount = 0;
            } else if (cutinMs > 0) {
                // 不是"材料型"正规召唤（通常/上级召唤，或材料类型不匹配的特召）：
                // MD 是先放王牌登场演出，落地被压后演出那么长（MonsterCutin 里的 cutin ? 1.6f : 0）
                ModNetwork.sendNear(level, field.position(), 128,
                        new ModNetwork.SummonEffect(field.getId(), -1, 2, card.code(),
                                0, source.x, source.y, source.z, cutinMs));
                pendingDelayMs = cutinMs;
                holdMs = Math.max(holdMs, cutinMs);
                org.slf4j.LoggerFactory.getLogger("mdpromod")
                        .info("Cut-in: card={} 先演出 {}ms 再落地", card.code(), cutinMs);
            }
        }
        switch (kind) {
            case "attack" -> {
                destination = e.target() == null
                        ? point(field, new DuelVisualState.Address(1 - from.controller(), 2, 0, 1), 1)
                        : point(field, e.target().address(), 1);
                forcePublic = true;
            }
            case "attack_declare" -> {
                kind = "attack_declare";
                destination = source;
                forcePublic = true;
            }
            case "activate" -> {
                kind = from.field() ? "effect" : "effect_reveal";
                destination = source; forcePublic = true;
            }
            case "reveal" -> {
                destination = source; forcePublic = true;
                if (to.location() == 2) kind = "search_reveal";
            }
            case "position", "flip_summon" -> kind = "flip";
            case "target", "equip" -> { kind = "effect"; destination = source; }
            case "negate" -> { kind = "negate"; destination = source; }
            case "attack_disabled" -> { kind = "attack_disabled"; destination = source; }
            case "shuffle", "shuffle_set" -> { kind = "shuffle"; destination = source; }
            default -> { }
        }
        send(level, field, owner, card, kind, source, destination, from, to,
                duration, hide, audience, forcePublic);
        audio.card(level, field, e, kind, source, destination, duration, pendingDelayMs, audioSummonKind);
        if (opening && kind.equals("draw")) openingDrew = true;
        return Math.max(duration, holdMs);
    }

    private void updateCounts(DuelVisualState.Event e) {
        var from = e.from(); var to = e.to();
        if (from.controller() < 2 && to.controller() < 2
                && (from.location() != to.location() || from.controller() != to.controller())) {
            if (from.location() != 0) counts[from.controller()][from.location()] = Math.max(0, e.fromCount() - 1);
            if (to.location() != 0) counts[to.controller()][to.location()] = Math.max(0, e.toCount());
        } else if (to.controller() < 2 && e.toCount() > 0) {
            counts[to.controller()][to.location()] = e.toCount();
        }
    }

    private void reconcile(ServerLevel level, DuelFieldEntity field, UUID owner) {
        entities.entrySet().removeIf(entry -> {
            if (cards.containsKey(entry.getKey())) return false;
            entry.getValue().discard(); return true;
        });
        for (var card : cards.values()) {
            var at = card.address();
            Entity entity = entities.get(card.id());
            boolean hand = at.location() == 2;
            boolean visible = hand || isField(at) || at.location() == 16 || at.location() == 32
                    || (at.location() == 64 && card.publicKnown());
            if (!visible) {
                if (entity != null) { entity.discard(); entities.remove(card.id()); }
                continue;
            }
            if (entity != null && ((entity instanceof HandCardEntity) != hand || entity.isRemoved())) {
                entity.discard(); entities.remove(card.id()); entity = null;
            }
            Vec3 position = point(field, at, counts[at.controller()][at.location()]);
            float yaw = yaw(field, at);
            String ownerText = at.controller() == 0 && owner != null ? owner.toString() : "";
            if (hand) {
                HandCardEntity h = entity instanceof HandCardEntity existing ? existing
                        : ModEntities.HAND_CARD.get().create(level);
                if (h == null) continue;
                h.configure(field.getUUID(), ownerText, at.controller(), at.sequence());
                h.setPrivateCode(card.code());
                h.setRevealed(card.publicKnown());
                h.setCardCode(card.publicKnown() ? card.code() : 0);
                h.moveTo(position.x, position.y, position.z, yaw, 0);
                if (entity == null) level.addFreshEntity(h);
                entity = h;
            } else {
                DuelCardEntity d = entity instanceof DuelCardEntity existing ? existing
                        : ModEntities.DUEL_CARD.get().create(level);
                if (d == null) continue;
                boolean face = publicFace(card);
                d.configureDuel(field.getUUID(), ownerText, card.code());
                d.setCardCode(face ? card.code() : 0);
                d.setFaceUp(face); d.setDefense(defense(at)); d.setZoneId(zoneId(at));
                d.setDisabled(card.disabled());
                d.moveTo(position.x, position.y, position.z, yaw, 0);
                if (entity == null) level.addFreshEntity(d);
                entity = d;
            }
            entities.put(card.id(), entity);
            if (at.controller() == 0 && owner != null) {
                var player = level.getServer().getPlayerList().getPlayer(owner);
                if (player != null) ModNetwork.sendTo(player, new ModNetwork.CardKnowledge(entity.getId(), card.code()));
            }
        }
    }

    private void send(ServerLevel level, DuelFieldEntity field, UUID owner, DuelVisualState.Card card,
                      String kind, Vec3 from, Vec3 to, DuelVisualState.Address a, DuelVisualState.Address b,
                      int duration, int hide, int audience, boolean forcePublic) {
        long id = ++eventId;
        for (ServerPlayer viewer : level.players()) {
            if (viewer.distanceToSqr(field) > 96 * 96) continue;
            int viewerSide = owner != null && owner.equals(viewer.getUUID()) ? 0 : -1;
            if (audience >= 0 && viewerSide != audience) continue;
            boolean face = forcePublic || publicFace(card)
                    || viewerSide >= 0 && (a.controller() == viewerSide || b.controller() == viewerSide);
            // Public flights into a hidden pile may keep the already-public face, but never uncover a set card.
            if (kind.equals("set") && viewerSide != b.controller()) face = false;
            if (!forcePublic && !kind.equals("set") && isField(a) && (a.position() & 5) != 0) face = true;
            float toPitch = pitch(b);
            if (kind.equals("set")) toPitch = -90;
            ModNetwork.sendTo(viewer, new ModNetwork.DuelAnimation(session, field.getId(), id, kind,
                    face ? card.code() : 0, face, from, to, yaw(field, a), yaw(field, b),
                    pitch(a), toPitch, defense(a), defense(b), duration, pendingDelayMs, hide,
                    field.getYRot()));
        }
    }

    static boolean isField(DuelVisualState.Address a) { return a.field(); }
    private static boolean publicFace(DuelVisualState.Card c) {
        var a = c.address();
        return c.publicKnown() || a.location() == 16
                || (a.location() != 1 && a.location() != 2 && (a.position() & 5) != 0);
    }
    private static boolean defense(DuelVisualState.Address a) {
        return (a.location() & 4) != 0 && (a.location() & 0x80) == 0 && (a.position() & 0xC) != 0;
    }
    private static float pitch(DuelVisualState.Address a) {
        if (a.location() == 2) return 0;
        return a.overlay() || (a.position() & 5) != 0 ? 90 : -90;
    }
    private static float yaw(DuelFieldEntity field, DuelVisualState.Address a) {
        return field.getYRot() + (a.controller() == 1 ? 180 : 0);
    }

    static String zoneId(DuelVisualState.Address at) {
        int loc = at.location() & 0x7F;
        String p = "p" + (at.controller() + 1);
        return switch (loc) {
            case 1 -> p + "_deck";
            case 4 -> at.sequence() < 5 ? p + "_mzone_" + at.sequence()
                    : (at.sequence() == (at.controller() == 0 ? 5 : 6) ? "shared_emz_left" : "shared_emz_right");
            case 8 -> at.sequence() == 5 ? p + "_field"
                    : p + "_szone_" + (at.sequence() == 6 ? 0 : at.sequence() == 7 ? 4 : at.sequence());
            case 16 -> p + "_graveyard";
            case 32 -> p + "_banished";
            case 64 -> p + "_extra";
            default -> "";
        };
    }

    static Vec3 handPoint(DuelFieldEntity field, int side, int index, int count) {
        double spacing = Math.min(0.85, 11.0 / Math.max(1, count - 1));
        double x = (index - Math.max(0, count - 1) / 2.0) * spacing;
        if (side == 1) x = -x;
        Vec3 rotated = new Vec3(x, 0, side == 0 ? 7.75 : -7.75)
                .yRot(-(float) Math.toRadians(field.getYRot()));
        return field.position().add(rotated).add(0, 1.375, 0);
    }

    static Vec3 point(DuelFieldEntity field, DuelVisualState.Address at, int count) {
        if (at.location() == 2) return handPoint(field, at.controller(), at.sequence(), count);
        Vec3 base = DuelFieldLayout.byId(zoneId(at)).map(field::zoneWorldPosition)
                .orElse(field.position().add(0, 0.2, 0));
        double height = switch (at.location()) {
            case 1, 64 -> 0.12 + Math.min(0.35, Math.max(0, count) * 0.008);
            case 16, 32 -> 0.13 + Math.max(0, at.sequence()) * 0.008;
            default -> (at.location() & 0x80) != 0 ? 0.09 + 0.005 * Math.max(0, at.position()) : 0.12;
        };
        return base.add(0, height, 0);
    }

    void clear(ServerLevel level, DuelFieldEntity field) {
        audio.clear();
        ModNetwork.sendNear(level, field.position(), 128, ModNetwork.DuelSound.stop(session, field.getId(), ""));
        ModNetwork.sendNear(level, field.position(), 96, new ModNetwork.DuelAnimationClear(session));
        for (Entity entity : entities.values()) entity.discard();
        entities.clear(); cards.clear(); queue.clear(); nextAt = 0;
        opening = false;
        bgmStarted = false;
    }
}
