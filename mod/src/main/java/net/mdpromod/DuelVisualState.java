package net.mdpromod;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.HashMap;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

/**
 * Server-side visual projection of classic ocgcore messages. This class knows no Minecraft
 * classes or card catalogue. Codes are authoritative input, never identities. A token follows
 * one observed card through hand compression, control changes and overlay detachment.
 * Raw core codes are private: the caller must redact them for every receiving player.
 */
public final class DuelVisualState {
    public static final int DECK = 1, HAND = 2, MONSTER = 4, SPELL = 8;
    public static final int GRAVE = 16, BANISHED = 32, EXTRA = 64, OVERLAY = 128;
    public static final long DESTROY = 1, MATERIAL = 8, FUSION = 0x40000;
    public static final long SYNCHRO = 0x80000, XYZ = 0x200000, LINK = 0x10000000;
    private static final Set<Integer> LIST_ZONES = Set.of(DECK, HAND, GRAVE, BANISHED, EXTRA);

    /** For an overlay, sequence is its host's slot and position is the material index. */
    public record Address(int controller, int location, int sequence, int position) {
        public boolean overlay() { return (location & OVERLAY) != 0; }
        public boolean field() { return !overlay() && (location == MONSTER || location == SPELL); }
        public boolean faceUp() { return !overlay() && (position & 5) != 0; }
    }

    /**
     * publicKnown means currently public, not merely revealed at some earlier moment.
     * disabled = 核心查询状态位里的"效果被无效"（QUERY_STATUS bit0），客户端据此把立绘变灰。
     */
    public record Card(long id, int code, Address address, boolean publicKnown, boolean disabled) { }

    /**
     * Immutable state at this step, not the state after later buffered engine messages.
     * fromCount counts the source zone before movement; toCount counts the destination after.
     * For draw the old hand size is toCount - 1. audience is a Confirm recipient, or -1 for all.
     * A summon annotation received after drain has from == to and must not repeat flight.
     */
    public record Event(String kind, Card card, Address from, Address to, long reason,
                        Card target, int fromCount, int toCount, int audience) { }

    private static final class MutableCard {
        final long id;
        int code;
        Address address;
        boolean publicKnown;
        boolean disabled;
        Integer attack, attribute, race;
        MutableCard(long id, int code, Address address, boolean publicKnown, boolean disabled) {
            this.id = id; this.code = code; this.address = address;
            this.publicKnown = publicKnown; this.disabled = disabled;
        }
        Card snapshot() { return new Card(id, code, address, publicKnown, disabled); }
    }

    private final Map<Long, MutableCard> cards = new LinkedHashMap<>();
    private final List<Event> events = new ArrayList<>();
    private final Map<Integer, Card> chainCards = new HashMap<>();
    private final Set<Integer> negatedChains = new HashSet<>();
    private final Set<Integer> targetedChains = new HashSet<>();
    /**
     * 当前连锁里"被别的卡点名"的卡（存令牌而不是地址：点名后卡还会被移动）。
     * 只在连锁进行中记账，MSG_CHAIN_END(74) 清空，所以标记不会留在场上。
     */
    private final Set<Long> targetMarkIds = new LinkedHashSet<>();
    private int latestChain;
    /** Last attack declaration awaiting its actual Damage Step/Battle message. */
    private Event pendingAttack;
    private long nextId = 1;
    private long malformedMessages;
    private boolean deckReversed;
    private int turnSide, turnNumber, matchKill;
    private final int[] lifePoints = {8000, 8000};

    public synchronized void reset() {
        cards.clear(); events.clear(); chainCards.clear(); negatedChains.clear(); targetedChains.clear();
        targetMarkIds.clear(); latestChain = 0; pendingAttack = null;
        nextId = 1; malformedMessages = 0; deckReversed = false;
        turnSide = 0; turnNumber = 0; matchKill = 0;
        lifePoints[0] = lifePoints[1] = 8000;
    }

    /**
     * 连锁里已经发动、而且现在还留在场上的卡（打包地址 {@code controller<<16|location<<8|sequence}）。
     * 位置取令牌的当前状态：发动后又被移走/送墓的卡不再标记。
     */
    public synchronized int[] chainMarkAddresses() {
        Set<Integer> packed = new LinkedHashSet<>();
        for (Card card : chainCards.values()) addFieldMark(packed, card.id());
        return packMarks(packed);
    }

    /** 当前连锁里被点名的、还在场上的卡（同上打包）。没有连锁时永远是空的。 */
    public synchronized int[] targetMarkAddresses() {
        Set<Integer> packed = new LinkedHashSet<>();
        for (long id : targetMarkIds) addFieldMark(packed, id);
        return packMarks(packed);
    }

    private void addFieldMark(Set<Integer> packed, long id) {
        MutableCard card = cards.get(id);
        if (card == null || !card.address.field()) return;
        Address a = card.address;
        packed.add(a.controller() << 16 | a.location() << 8 | a.sequence());
    }

    private static int[] packMarks(Set<Integer> packed) {
        int[] result = new int[packed.size()];
        int i = 0;
        for (int value : packed) result[i++] = value;
        return result;
    }

    /** Initialize only counts. Engine shuffling makes a deck's loaded YDK order unreliable. */
    public synchronized void initializeCounts(int side, int mainCount, int extraCount) {
        requireSide(side);
        if (mainCount < 0 || mainCount > 255 || extraCount < 0 || extraCount > 255)
            throw new IllegalArgumentException("Invalid deck count");
        cards.values().removeIf(c -> c.address.controller() == side
                && (c.address.location() == DECK || c.address.location() == EXTRA));
        for (int i = 0; i < mainCount; i++) create(0, new Address(side, DECK, i, 10));
        for (int i = 0; i < extraCount; i++) create(0, new Address(side, EXTRA, i, 8));
    }

    public synchronized List<Card> hand(int side) { return snapshots(zone(side, HAND)); }
    public synchronized List<Card> cards() { return snapshots(new ArrayList<>(cards.values())); }
    public synchronized int count(int side, int location) { return zone(side, location).size(); }
    public synchronized long malformedMessages() { return malformedMessages; }
    public synchronized List<Event> drainEvents() {
        List<Event> result = List.copyOf(events); events.clear(); return result;
    }

    /**
     * The host may emit multiple core messages in one hex buffer. Consume complete known
     * messages in order; stop at an unknown prompt instead of treating payload bytes as events.
     * ConfirmCards accepts both original and current-core layouts (current adds one flag byte).
     */
    public synchronized void apply(String name, String hex) {
        byte[] bytes;
        try {
            String compact = hex.replaceAll("\\s", "");
            if ((compact.length() & 1) != 0) throw new IllegalArgumentException("Odd hex length");
            bytes = java.util.HexFormat.of().parseHex(compact);
        } catch (RuntimeException ex) {
            malformedMessages++; return;
        }
        int offset = 0;
        while (offset < bytes.length) {
            int function = u8(bytes, offset);
            int size = messageSize(bytes, offset);
            if (size == -1) return; // Unknown prompts and queries are not visual events.
            if (size == 0 || offset + size > bytes.length) {
                malformedMessages++; return;
            }
            applyMessage(function, bytes, offset, size);
            offset += size;
        }
    }

    private void applyMessage(int function, byte[] b, int o, int size) {
        switch (function) {
            case 4 -> {
                lifePoints[0] = (int) Math.min(Integer.MAX_VALUE, u32(b, o + 2));
                lifePoints[1] = (int) Math.min(Integer.MAX_VALUE, u32(b, o + 6));
            }
            case 5 -> semantic("audio_win", u8(b, o + 1), u8(b, o + 2), null, 0, matchKill);
            case 6, 7 -> updateQuery(function, b, o, size);
            case 30, 31, 42 -> confirm(function, b, o, size);
            case 32 -> shuffleDeck(u8(b, o + 1));
            case 33, 39 -> shuffleList(function == 33 ? HAND : EXTRA, b, o);
            case 35 -> swapGraveDeck(u8(b, o + 1));
            case 36 -> shuffleSet(b, o);
            case 37 -> reverseDeck();
            case 38 -> deckTop(b, o);
            case 40 -> {
                turnSide = u8(b, o + 1) & 1;
                semantic("audio_turn", turnSide, ++turnNumber, null, 0, 0);
            }
            case 41 -> semantic("audio_phase", turnSide, u8(b, o + 1) | u8(b, o + 2) << 8,
                    null, 0, 0);
            case 50 -> move(code(b, o + 1), address(b, o + 5), address(b, o + 9), u32(b, o + 13));
            case 53 -> positionChange(b, o);
            case 54, 60, 62, 64 -> annotate(function, b, o);
            case 55 -> swap(b, o);
            case 70 -> activate(b, o);
            case 71 -> semantic("audio_chain_stack", turnSide, u8(b, o + 1), null, chainCards.size(), 0);
            case 72 -> chainResolve(u8(b, o + 1));
            case 74 -> {
                semantic("audio_chain_end", turnSide, 0, null, 0, 0);
                chainCards.clear(); negatedChains.clear(); targetedChains.clear(); latestChain = 0;
                targetMarkIds.clear();
            }
            case 75, 76 -> negate(u8(b, o + 1));
            case 81, 83 -> targetList(function == 81 ? "selected" : "target", b, o, function == 81 ? 3 : 2);
            case 90 -> draw(b, o);
            case 91, 92, 94, 100 -> lifePoints(function, b, o);
            case 110 -> attackDeclare(b, o);
            case 93, 96, 97 -> relation(function, b, o);
            case 111 -> battle(b, o);
            case 112 -> attackDisabled();
            case 101, 102 -> {
                Address a = new Address(u8(b, o + 3), u8(b, o + 4), u8(b, o + 5), 0);
                if (valid(a)) semantic("audio_counter", a.controller(), 0,
                        getOrCreate(a, 0).snapshot(), 0, 0);
            }
            case 130, 131 -> {
                int count = u8(b, o + 2);
                // Keep every coin result (the roll sound itself is played once).
                for (int i = 0; i < (function == 130 ? count : Math.min(1, count)); i++)
                    semantic(function == 130 ? "audio_coin" : "audio_dice", u8(b, o + 1) & 1,
                            u8(b, o + 3 + i), null, i, count);
            }
            case 170 -> matchKill = code(b, o + 1);
            default -> { } // phase/life-point/chain bookkeeping is parsed for packet boundaries.
        }
    }

    /** Non-card presentation events share the card queue, preserving core ordering. */
    private void semantic(String kind, int side, long value, Card subject, int before, int after) {
        Address at = new Address(side, 0, 0, 0);
        events.add(new Event(kind, null, at, at, value, subject, before, after, -1));
    }

    private void lifePoints(int function, byte[] b, int o) {
        int side = u8(b, o + 1);
        if (side > 1) { malformedMessages++; return; }
        long amount = u32(b, o + 2), old = lifePoints[side];
        long value = function == 94 ? amount : function == 92 ? old + amount : old - amount;
        lifePoints[side] = (int) Math.max(0L, Math.min(Integer.MAX_VALUE, value));
        long delta = (long) lifePoints[side] - old;
        semantic("audio_lp", side, delta, null, (int) old, lifePoints[side]);
    }

    /** Classic query refresh is authoritative state, never a card-flight event. */
    private void updateQuery(int function, byte[] b, int o, int size) {
        int side = u8(b, o + 1), location = u8(b, o + 2);
        if (side > 1) { malformedMessages++; return; }
        int cursor = o + (function == 7 ? 4 : 3), limit = o + size;
        int sequence = function == 7 ? u8(b, o + 3) : 0;
        Set<Long> observed = new HashSet<>();
        while (cursor + 4 <= limit) {
            int length = i32(b, cursor);
            if (length < 4 || cursor + length > limit) { malformedMessages++; return; }
            if (length >= 8) {
                int flags = i32(b, cursor + 4), data = cursor + 8;
                if (flags != 0) {
                    int incomingCode = 0;
                    if ((flags & 1) != 0) {
                        if (data + 4 > cursor + length) { malformedMessages++; return; }
                        incomingCode = code(b, data); data += 4;
                    }
                    Address a = new Address(side, location, sequence, 0);
                    if ((flags & 2) != 0) {
                        if (data + 4 > cursor + length) { malformedMessages++; return; }
                        a = address(b, data);
                    }
                    if (valid(a) && a.location() != 0) {
                        MutableCard card = find(a);
                        Card before = card == null ? null : card.snapshot();
                        if (card == null) card = create(incomingCode, a);
                        if ((flags & 1) != 0) card.code = incomingCode;
                        if ((flags & 2) != 0) { card.address = a; card.publicKnown = publiclyVisible(a); }
                        // QUERY_STATUS(0x80000) 的 bit0 = 效果被无效（MDPro 的 Disabled 就是这么读的）
                        int status = queryStatus(b, cursor + 8, cursor + length, flags);
                        if (status >= 0) card.disabled = (status & 1) != 0;
                        queryAudio(card, b, cursor + 8, cursor + length, flags);
                        observed.add(card.id);
                        if (before == null || !before.equals(card.snapshot()))
                            events.add(new Event("update", card.snapshot(), before == null ? a : before.address(),
                                    a, 0, null, zoneCount(a), zoneCount(a), -1));
                    }
                }
            }
            sequence++; cursor += length;
        }
        if (cursor != limit) { malformedMessages++; return; }
        if (function == 6) {
            // Full field queries include empty slots as length=4. They also clear cards that
            // disappeared without a Move (e.g. reset or opaque SwapGraveDeck redirection).
            for (MutableCard stale : new ArrayList<>(zone(side, location))) if (!observed.contains(stale.id)) {
                Address from = stale.address;
                Address nowhere = new Address(side, 0, 0, 0);
                stale.address = nowhere; stale.publicKnown = false;
                events.add(new Event("update", stale.snapshot(), from, nowhere, 0, null,
                        zoneCount(from), 0, -1));
                cards.remove(stale.id);
            }
        }
    }

    /** First observation and hidden snapshots establish a baseline, never replay a buff. */
    private void queryAudio(MutableCard card, byte[] b, int data, int limit, int flags) {
        boolean shown = card.address.field() && card.address.faceUp();
        if (!shown) { card.attack = card.attribute = card.race = null; return; }
        Integer attack = queryFixed(b, data, limit, flags, 0x100);
        Integer attribute = queryFixed(b, data, limit, flags, 0x40);
        Integer race = queryFixed(b, data, limit, flags, 0x80);
        if (attack != null) {
            if (shown && card.attack != null && !attack.equals(card.attack))
                semantic("audio_stat", card.address.controller(), (long) attack - card.attack,
                        card.snapshot(), card.attack, attack);
            card.attack = attack;
        }
        if (attribute != null) {
            if (shown && card.attribute != null && !attribute.equals(card.attribute))
                semantic("audio_characteristic", card.address.controller(), 0x40,
                        card.snapshot(), card.attribute, attribute);
            card.attribute = attribute;
        }
        if (race != null) {
            if (shown && card.race != null && !race.equals(card.race))
                semantic("audio_characteristic", card.address.controller(), 0x80,
                        card.snapshot(), card.race, race);
            card.race = race;
        }
    }

    private static Integer queryFixed(byte[] b, int data, int limit, int flags, int field) {
        if ((flags & field) == 0) return null;
        int p = data + 4 * Integer.bitCount(flags & (field - 1));
        return p + 4 <= limit ? i32(b, p) : null;
    }

    /**
     * 从一条查询记录里取出 QUERY_STATUS(0x80000) 的 4 字节状态位；取不到返回 -1。
     *
     * 字段顺序和尺寸照抄 MDPro 的 {@code BinaryExtensions.ReadCard}（按 flag 位从低到高：
     * code、position、alias、type、level、rank、attribute、race、attack、defense、
     * baseAttack、baseDefense、reason、reasonCard、equip 各 4 字节；
     * target、overlay、counters 是"数量 + 4×N"；owner 4 字节；status 4 字节）。
     * 这套尺寸用实机日志里 8,831 条查询记录验过：按它走完正好等于记录长度。
     * 走不齐就返回 -1（当作没读到），绝不猜一个值出来。
     */
    private static int queryStatus(byte[] b, int data, int limit, int flags) {
        int p = data;
        for (int bit = 0x1; bit <= 0x4000; bit <<= 1) {
            if ((flags & bit) != 0) p += 4;
        }
        for (int bit : new int[]{0x8000, 0x10000, 0x20000}) {
            if ((flags & bit) != 0) {
                if (p + 4 > limit) return -1;
                p += 4 + 4 * i32(b, p);
            }
        }
        if ((flags & 0x40000) != 0) p += 4;
        if ((flags & 0x80000) == 0 || p + 4 > limit) return -1;
        return i32(b, p);
    }

    private void draw(byte[] b, int o) {
        int side = u8(b, o + 1), amount = u8(b, o + 2);
        if (side > 1) { malformedMessages++; return; }
        for (int i = 0; i < amount; i++) {
            int rawCode = i32(b, o + 3 + 4 * i);
            List<MutableCard> deck = zone(side, DECK);
            int deckCount = deck.size();
            Address from = new Address(side, DECK, Math.max(0, deckCount - 1), 10);
            MutableCard card = deck.isEmpty() ? create(0, from) : deck.get(deck.size() - 1);
            from = card.address;
            int newHandCount = count(side, HAND) + 1;
            card.code = rawCode & 0x7fffffff;
            card.address = new Address(side, HAND, newHandCount - 1, rawCode < 0 ? 5 : 10);
            card.publicKnown = rawCode < 0;
            events.add(new Event("draw", card.snapshot(), from, card.address, 0x20000,
                    null, deckCount, newHandCount, -1));
        }
    }

    private void move(int incomingCode, Address from, Address to, long reason) {
        if (!valid(from) || !valid(to)) { malformedMessages++; return; }
        int sourceCount = zoneCount(from);
        Map<Long, Address> before = addresses();
        MutableCard card = from.location() == 0 ? create(incomingCode, from) : find(from);
        if (card == null) card = create(incomingCode, from);
        if (incomingCode != 0) card.code = incomingCode;
        boolean sameSlot = sameAddress(from, to);
        if (!sameSlot) {
            compactRemoval(from, card.id);
            expandInsertion(to, card.id);
        }
        card.address = to;
        card.publicKnown = publiclyVisible(to);
        if (!to.field() || !to.faceUp()) card.attack = card.attribute = card.race = null;
        if (from.field() && to.field()) relocateOverlays(from, to);
        int targetCount = to.location() == 0 ? 0 : zoneCount(to);
        events.add(new Event("move", card.snapshot(), from, to, reason, null,
                sourceCount, targetCount, -1));
        if (to.location() == 0) cards.remove(card.id);
        emitRepositions(before, card.id);
    }

    private void compactRemoval(Address from, long movingId) {
        if (from.overlay()) {
            for (MutableCard other : cards.values()) {
                Address a = other.address;
                if (other.id != movingId && sameZone(a, from) && a.sequence() == from.sequence()
                        && a.position() > from.position())
                    other.address = new Address(a.controller(), a.location(), a.sequence(), a.position() - 1);
            }
        } else if (LIST_ZONES.contains(from.location())) {
            for (MutableCard other : cards.values()) {
                Address a = other.address;
                if (other.id != movingId && sameZone(a, from) && a.sequence() > from.sequence())
                    other.address = new Address(a.controller(), a.location(), a.sequence() - 1, a.position());
            }
        }
    }

    private void expandInsertion(Address to, long movingId) {
        if (to.overlay()) {
            for (MutableCard other : cards.values()) {
                Address a = other.address;
                if (other.id != movingId && sameZone(a, to) && a.sequence() == to.sequence()
                        && a.position() >= to.position())
                    other.address = new Address(a.controller(), a.location(), a.sequence(), a.position() + 1);
            }
        } else if (LIST_ZONES.contains(to.location())) {
            for (MutableCard other : cards.values()) {
                Address a = other.address;
                if (other.id != movingId && sameZone(a, to) && a.sequence() >= to.sequence())
                    other.address = new Address(a.controller(), a.location(), a.sequence() + 1, a.position());
            }
        }
    }

    private void relocateOverlays(Address from, Address to) {
        for (MutableCard other : cards.values()) {
            Address a = other.address;
            if (a.overlay() && a.controller() == from.controller()
                    && (a.location() & ~OVERLAY) == from.location() && a.sequence() == from.sequence())
                other.address = new Address(to.controller(), to.location() | OVERLAY, to.sequence(), a.position());
        }
    }

    private void annotate(int function, byte[] b, int o) {
        int incomingCode = code(b, o + 1);
        Address to = address(b, o + 5);
        if (!valid(to) || to.location() == 0) { malformedMessages++; return; }
        MutableCard card = find(to);
        if (card == null) card = create(incomingCode, to);
        if (incomingCode != 0) card.code = incomingCode;
        card.address = to; card.publicKnown = publiclyVisible(to);
        String kind = function == 54 ? "set" : function == 62 ? "special_summon"
                : function == 64 ? "flip_summon" : "summon";
        for (int i = events.size() - 1; i >= 0; i--) {
            Event pending = events.get(i);
            if (pending.card() != null && pending.card().id() == card.id) {
                if (pending.kind().equals("move") && sameAddress(pending.to(), to)) {
                    events.set(i, new Event(kind, card.snapshot(), pending.from(), to,
                            pending.reason(), pending.target(), pending.fromCount(), pending.toCount(), -1));
                    return;
                }
                break;
            }
        }
        // Cross-drain annotation: preserve identity and show only the landing cue.
        events.add(new Event(kind, card.snapshot(), to, to, 0, null, zoneCount(to), zoneCount(to), -1));
    }

    private void positionChange(byte[] b, int o) {
        int incomingCode = code(b, o + 1);
        Address from = address(b, o + 5);
        Address to = new Address(from.controller(), from.location(), from.sequence(), u8(b, o + 9));
        if (!valid(from)) { malformedMessages++; return; }
        MutableCard card = find(from);
        if (card == null) card = create(incomingCode, from);
        if (incomingCode != 0) card.code = incomingCode;
        card.address = to; card.publicKnown = publiclyVisible(to);
        if (!to.field() || !to.faceUp()) card.attack = card.attribute = card.race = null;
        events.add(new Event("position", card.snapshot(), from, to, 0, null, zoneCount(from), zoneCount(to), -1));
    }

    private void swap(byte[] b, int o) {
        Address a = address(b, o + 5), c = address(b, o + 13);
        if (!valid(a) || !valid(c)) { malformedMessages++; return; }
        MutableCard first = getOrCreate(a, code(b, o + 1));
        MutableCard second = getOrCreate(c, code(b, o + 9));
        Map<Long, Address> before = addresses();
        Address firstTo = new Address(c.controller(), c.location(), c.sequence(), a.position());
        Address secondTo = new Address(a.controller(), a.location(), a.sequence(), c.position());
        first.address = firstTo; second.address = secondTo;
        first.publicKnown = publiclyVisible(firstTo); second.publicKnown = publiclyVisible(secondTo);
        // Move both hosts' overlays atomically so one set cannot be relocated twice.
        for (MutableCard other : cards.values()) {
            Address old = before.get(other.id);
            if (old == null || !old.overlay()) continue;
            Address dest = hostMatches(old, a) ? firstTo : hostMatches(old, c) ? secondTo : null;
            if (dest != null) other.address = new Address(dest.controller(), dest.location() | OVERLAY,
                    dest.sequence(), old.position());
        }
        events.add(new Event("move", first.snapshot(), a, firstTo, 0, second.snapshot(), zoneCount(a), zoneCount(firstTo), -1));
        events.add(new Event("move", second.snapshot(), c, secondTo, 0, first.snapshot(), zoneCount(c), zoneCount(secondTo), -1));
        emitRepositions(before, first.id, second.id);
    }

    private void activate(byte[] b, int o) {
        Address source = address(b, o + 5);
        if (!valid(source)) { malformedMessages++; return; }
        MutableCard card = getOrCreate(source, code(b, o + 1));
        latestChain = u8(b, o + 16);
        chainCards.put(latestChain, card.snapshot());
        // The trailing short GPS is the triggering chain state, not necessarily current card location.
        events.add(new Event("activate", card.snapshot(), source, source, u32(b, o + 12),
                null, zoneCount(source), zoneCount(source), -1));
    }

    private void chainResolve(int index) {
        Card original = chainCards.get(index);
        MutableCard current = original == null ? null : cards.get(original.id());
        Card resolving = current == null ? original : current.snapshot();
        if (negatedChains.contains(index) || (resolving != null && original != null
                && resolving.code() != original.code())) resolving = null;
        Card previous = chainCards.get(index - 1);
        MutableCard previousNow = previous == null ? null : cards.get(previous.id());
        boolean previousField = previousNow != null && previousNow.address.field();
        semantic("audio_chain_resolve", turnSide, index, resolving, chainCards.size(),
                (targetedChains.contains(index) ? 1 : 0) | (previousField ? 2 : 0));
    }

    private void negate(int chainIndex) {
        negatedChains.add(chainIndex);
        Card original = chainCards.get(chainIndex);
        MutableCard current = original == null ? null : cards.get(original.id());
        Card card = current == null ? original : current.snapshot();
        Address a = card == null ? null : card.address();
        events.add(new Event("negate", card, a, a, chainIndex, null,
                a == null ? 0 : zoneCount(a), a == null ? 0 : zoneCount(a), -1));
    }

    private void relation(int function, byte[] b, int o) {
        Address a = address(b, o + 1), dest = address(b, o + 5);
        if (!valid(a) || !valid(dest)) { malformedMessages++; return; }
        MutableCard card = a.location() == 0 ? null : getOrCreate(a, 0);
        MutableCard target = dest.location() == 0 ? null : getOrCreate(dest, 0);
        String kind = function == 110 ? "attack_declare" : function == 93 ? "equip"
                : function == 97 ? "cancel_target" : "target";
        events.add(new Event(kind, card == null ? null : card.snapshot(), a, dest, function,
                target == null ? null : target.snapshot(), zoneCount(a), zoneCount(dest), -1));
    }

    /** MSG_ATTACK (110): attacker GPS followed by target GPS, or zero for a direct attack. */
    private void attackDeclare(byte[] b, int o) {
        Address attacker = address(b, o + 1);
        Address target = address(b, o + 5);
        if (!valid(attacker) || (target.location() != 0 && !valid(target))) {
            malformedMessages++; return;
        }
        MutableCard a = getOrCreate(attacker, 0);
        MutableCard t = target.location() == 0 ? null : getOrCreate(target, 0);
        pendingAttack = new Event("attack_declare", a.snapshot(), attacker, target, 0,
                t == null ? null : t.snapshot(), zoneCount(attacker), zoneCount(target), -1);
        events.add(pendingAttack);
    }

    private void battle(byte[] b, int o) {
        Address attacker = address(b, o + 1);
        Address target = address(b, o + 14);
        if (!valid(attacker) || attacker.location() == 0
                || (target.location() != 0 && !valid(target))) { malformedMessages++; return; }
        MutableCard a = getOrCreate(attacker, 0);
        MutableCard t = target.location() == 0 ? null : getOrCreate(target, 0);
        int attackPower = Math.max(0, i32(b, o + 5));
        int targetPower = t == null ? 0 : Math.max(0,
                i32(b, o + ((target.position() & 0xC) != 0 ? 22 : 18)));
        boolean guard = t != null && targetPower >= attackPower;
        int attackedSide = t == null ? 1 - attacker.controller() : target.controller();
        boolean finalBlow = attackPower - targetPower >= lifePoints[attackedSide]
                && (t == null || (target.position() & 0xC) == 0);
        Event attack = new Event("attack", a.snapshot(), attacker, target,
                (guard ? 1L : 0L) | (finalBlow ? 2L : 0L)
                        | (a.attribute == null ? 0 : (long) a.attribute << 8),
                t == null ? null : t.snapshot(), attackPower, targetPower, -1);
        events.add(attack);
        pendingAttack = null;
    }

    private void attackDisabled() {
        if (pendingAttack != null) {
            Event event = pendingAttack;
            events.add(new Event("attack_disabled", event.card(), event.from(), event.to(),
                    0, event.target(), event.fromCount(), event.toCount(), -1));
            pendingAttack = null;
            return;
        }
        for (int i = events.size() - 1; i >= 0; i--) {
            Event event = events.get(i);
            if (event.kind().equals("attack_declare")) {
                events.add(new Event("attack_disabled", event.card(), event.from(), event.to(),
                        0, event.target(), event.fromCount(), event.toCount(), -1));
                pendingAttack = null;
                return;
            }
        }
        emitSimple("attack_disabled", null, null, 0, -1);
    }

    private void targetList(String kind, byte[] b, int o, int header) {
        int count = u8(b, o + header - 1);
        for (int i = 0; i < count; i++) {
            Address a = address(b, o + header + i * 4);
            if (valid(a) && a.location() != 0) {
                MutableCard card = getOrCreate(a, 0);
                if (kind.equals("target") && latestChain > 0) {
                    targetedChains.add(latestChain);
                    targetMarkIds.add(card.id);
                }
                emitSimple(kind, card, null, 0, -1);
            }
        }
    }

    private void confirm(int function, byte[] b, int o, int size) {
        int audience = u8(b, o + 1);
        int header = function == 31 && size >= 4 && size == 4 + 7 * u8(b, o + 3) ? 4 : 3;
        int count = u8(b, o + header - 1);
        for (int i = 0; i < count; i++) {
            int p = o + header + i * 7;
            Address a = new Address(u8(b, p + 4), u8(b, p + 5), u8(b, p + 6), 0);
            if (!valid(a) || a.location() == 0) continue;
            MutableCard card = find(a);
            if (card == null) card = create(code(b, p), a);
            else if (code(b, p) != 0) card.code = code(b, p);
            // Confirm is a temporary presentation, never an EFFECT_PUBLIC flag.
            emitSimple("reveal", card, null, 0, audience);
        }
    }

    private void shuffleList(int location, byte[] b, int o) {
        int side = u8(b, o + 1), count = u8(b, o + 2);
        if (side > 1) { malformedMessages++; return; }
        List<MutableCard> old = zone(side, location);
        List<MutableCard> unused = new ArrayList<>(old);
        Map<Long, Address> before = addresses();
        Map<Long, Card> snapshots = new HashMap<>();
        for (MutableCard card : old) snapshots.put(card.id, card.snapshot());
        for (int i = 0; i < count; i++) {
            int incoming = code(b, o + 3 + i * 4);
            MutableCard selected = null;
            for (MutableCard candidate : unused) {
                if (incoming != 0 && candidate.code == incoming) { selected = candidate; break; }
            }
            if (selected == null) for (MutableCard candidate : unused) {
                if (candidate.code == 0) { selected = candidate; break; }
            }
            if (selected == null && !unused.isEmpty()) selected = unused.get(0);
            if (selected == null) selected = create(0, new Address(side, location, i, location == EXTRA ? 8 : 10));
            unused.remove(selected);
            if (incoming != 0) selected.code = incoming;
            selected.address = new Address(side, location, i, selected.address.position());
            selected.publicKnown = publiclyVisible(selected.address);
        }
        for (MutableCard removed : unused) cards.remove(removed.id);
        emitRepositions(before);
        for (MutableCard card : zone(side, location)) {
            Card oldCard = snapshots.get(card.id);
            if (oldCard == null || oldCard.address().equals(card.address)
                    && (oldCard.code() != card.code || oldCard.publicKnown() != card.publicKnown))
                events.add(new Event("reposition", card.snapshot(), card.address, card.address, 0,
                        null, count, count, -1));
        }
        emitSimple("shuffle", null, null, location, -1, new Address(side, location, 0, 10));
    }

    private void shuffleDeck(int side) {
        if (side > 1) { malformedMessages++; return; }
        // No permutation is supplied. The tokens now represent unknown deck slots; forgetting
        // private identities prevents tracking a revealed card through an opaque shuffle.
        for (MutableCard card : zone(side, DECK)) {
            card.code = 0; card.publicKnown = false;
            card.address = new Address(side, DECK, card.address.sequence(), 10);
        }
        emitSimple("shuffle", null, null, DECK, -1, new Address(side, DECK, 0, 10));
    }

    private void shuffleSet(byte[] b, int o) {
        int count = u8(b, o + 2);
        List<MutableCard> chosen = new ArrayList<>();
        List<Address> originals = new ArrayList<>();
        List<Address> destinations = new ArrayList<>();
        Map<Long, Address> before = addresses();
        for (int i = 0; i < count; i++) {
            Address from = address(b, o + 3 + i * 4);
            Address dest = address(b, o + 3 + count * 4 + i * 4);
            if (!valid(from)) { malformedMessages++; return; }
            MutableCard card = getOrCreate(from, 0);
            chosen.add(card); originals.add(from); destinations.add(dest);
        }
        for (int i = 0; i < count; i++) {
            MutableCard card = chosen.get(i);
            Address original = originals.get(i), sentDest = destinations.get(i);
            // A zero new-GPS intentionally hides the actual permutation. Preserve the valid
            // occupied slots, but clear codes/public identity until a later Move/PosChange.
            Address dest = sentDest.location() == 0 ? originals.get(i) : sentDest;
            if (!valid(dest)) { malformedMessages++; continue; }
            card.address = new Address(dest.controller(), dest.location(), dest.sequence(), original.position());
            card.code = 0; card.publicKnown = false;
            events.add(new Event("shuffle_set", card.snapshot(), original, card.address, 0, null,
                    zoneCount(original), zoneCount(card.address), -1));
        }
        for (MutableCard other : cards.values()) {
            Address a = before.get(other.id);
            if (a == null || !a.overlay()) continue;
            for (int i = 0; i < count; i++) if (hostMatches(a, originals.get(i))) {
                Address dest = chosen.get(i).address;
                other.address = new Address(dest.controller(), dest.location() | OVERLAY, dest.sequence(), a.position());
                break;
            }
        }
        long[] ids = chosen.stream().mapToLong(c -> c.id).toArray();
        emitRepositions(before, ids);
    }

    private void swapGraveDeck(int side) {
        if (side > 1) { malformedMessages++; return; }
        List<MutableCard> deck = zone(side, DECK), grave = zone(side, GRAVE);
        for (int i = 0; i < grave.size(); i++) {
            MutableCard card = grave.get(i);
            Address from = card.address;
            card.address = new Address(side, DECK, i, 10); card.publicKnown = false;
            events.add(new Event("move", card.snapshot(), from, card.address, 0x40, null,
                    grave.size(), grave.size(), -1));
        }
        for (int i = 0; i < deck.size(); i++) {
            MutableCard card = deck.get(i);
            Address from = card.address;
            card.address = new Address(side, GRAVE, i, 5); card.publicKnown = true;
            events.add(new Event("move", card.snapshot(), from, card.address, 0x40, null,
                    deck.size(), deck.size(), -1));
        }
        // The protocol carries no codes or card types. Host query refresh is needed to identify
        // unknown old deck cards and extra-deck monsters redirected out of the old graveyard.
    }

    private void reverseDeck() {
        deckReversed = !deckReversed;
        Map<Long, Address> before = addresses();
        for (int side = 0; side < 2; side++) {
            List<MutableCard> deck = zone(side, DECK);
            for (MutableCard card : deck) card.address = new Address(side, DECK,
                    deck.size() - 1 - card.address.sequence(), card.address.position());
        }
        emitRepositions(before);
    }

    private void deckTop(byte[] b, int o) {
        int side = u8(b, o + 1), offset = u8(b, o + 2), incomingCode = code(b, o + 3);
        if (side > 1) { malformedMessages++; return; }
        int sequence = Math.max(0, count(side, DECK) - offset - 1);
        MutableCard card = getOrCreate(new Address(side, DECK, sequence, deckReversed ? 5 : 10), incomingCode);
        card.publicKnown = deckReversed || i32(b, o + 3) < 0;
        emitSimple("reveal", card, null, 0, -1);
    }

    private void emitRepositions(Map<Long, Address> before, long... excluded) {
        for (MutableCard card : cards.values()) {
            boolean skip = false;
            for (long id : excluded) if (card.id == id) { skip = true; break; }
            Address original = before.get(card.id);
            if (!skip && original != null && !original.equals(card.address))
                events.add(new Event("reposition", card.snapshot(), original, card.address, 0, null,
                        zoneCount(original), zoneCount(card.address), -1));
        }
    }

    private void emitSimple(String kind, MutableCard card, Card target, long reason, int audience) {
        emitSimple(kind, card, target, reason, audience, card == null ? null : card.address);
    }
    private void emitSimple(String kind, MutableCard card, Card target, long reason, int audience, Address a) {
        events.add(new Event(kind, card == null ? null : card.snapshot(), a, a, reason, target,
                a == null ? 0 : zoneCount(a), a == null ? 0 : zoneCount(a), audience));
    }

    private MutableCard getOrCreate(Address a, int incomingCode) {
        MutableCard card = find(a);
        if (card == null) return create(incomingCode, a);
        if (incomingCode != 0) card.code = incomingCode;
        return card;
    }
    private MutableCard create(int code, Address address) {
        MutableCard card = new MutableCard(nextId++, code, address, publiclyVisible(address), false);
        cards.put(card.id, card); return card;
    }
    private MutableCard find(Address a) {
        for (MutableCard card : cards.values()) if (sameAddress(card.address, a)) return card;
        return null;
    }
    private List<MutableCard> zone(int side, int location) {
        List<MutableCard> result = new ArrayList<>();
        for (MutableCard card : cards.values()) if (card.address.controller() == side && card.address.location() == location)
            result.add(card);
        result.sort(Comparator.comparingInt((MutableCard c) -> c.address.sequence())
                .thenComparingInt(c -> c.address.overlay() ? c.address.position() : 0));
        return result;
    }
    private int zoneCount(Address a) { return a.location() == 0 ? 0 : count(a.controller(), a.location()); }
    private Map<Long, Address> addresses() {
        Map<Long, Address> result = new HashMap<>();
        for (MutableCard card : cards.values()) result.put(card.id, card.address);
        return result;
    }
    private static List<Card> snapshots(List<MutableCard> values) {
        List<Card> result = new ArrayList<>(values.size());
        for (MutableCard card : values) result.add(card.snapshot());
        return List.copyOf(result);
    }
    private static boolean sameZone(Address a, Address b) {
        return a.controller() == b.controller() && a.location() == b.location();
    }
    private static boolean sameAddress(Address a, Address b) {
        return a != null && b != null && sameZone(a, b) && a.sequence() == b.sequence()
                && (!a.overlay() || a.position() == b.position());
    }
    private static boolean hostMatches(Address overlay, Address host) {
        return overlay.controller() == host.controller() && (overlay.location() & ~OVERLAY) == host.location()
                && overlay.sequence() == host.sequence();
    }
    private static boolean publiclyVisible(Address a) {
        if (a.overlay()) return true;
        return a.faceUp() && a.location() != DECK && a.location() != 0;
    }
    private static boolean valid(Address a) {
        return a.location() == 0 || (a.controller() >= 0 && a.controller() < 2
                && (LIST_ZONES.contains(a.location()) || a.field() || a.overlay()));
    }
    private static void requireSide(int side) {
        if (side < 0 || side > 1) throw new IllegalArgumentException("Invalid controller");
    }
    private static Address address(byte[] b, int o) {
        return new Address(u8(b, o), u8(b, o + 1), u8(b, o + 2), u8(b, o + 3));
    }
    private static int u8(byte[] b, int o) { return b[o] & 255; }
    private static int i32(byte[] b, int o) {
        return u8(b, o) | u8(b, o + 1) << 8 | u8(b, o + 2) << 16 | u8(b, o + 3) << 24;
    }
    private static long u32(byte[] b, int o) { return Integer.toUnsignedLong(i32(b, o)); }
    private static int code(byte[] b, int o) { return i32(b, o) & 0x7fffffff; }

    private static int messageSize(byte[] b, int o) {
        int available = b.length - o, fn = u8(b, o);
        return switch (fn) {
            case 1, 3, 37, 61, 63, 65, 74, 112, 113, 114 -> 1;
            case 2, 38 -> 7;
            case 4 -> 18;
            case 5 -> 3;
            case 6 -> querySize(b, o, 3);
            case 7 -> querySize(b, o, 4);
            case 30, 42 -> available < 3 ? 0 : 3 + 7 * u8(b, o + 2);
            case 31 -> confirmSize(b, o);
            case 32, 34, 35, 40, 71, 72, 73, 75, 76, 133 -> 2;
            case 33, 39, 90 -> available < 3 ? 0 : 3 + 4 * u8(b, o + 2);
            case 36 -> available < 3 ? 0 : 3 + 8 * u8(b, o + 2);
            case 41 -> 3;
            case 50, 55, 70 -> 17;
            case 53, 160 -> 10;
            case 54, 60, 62, 64, 93, 96, 97, 110, 120 -> 9;
            case 56, 170 -> 5;
            case 81 -> available < 3 ? 0 : 3 + 4 * u8(b, o + 2);
            case 83 -> available < 2 ? 0 : 2 + 4 * u8(b, o + 1);
            case 91, 92, 94, 100 -> 6;
            case 95 -> 5;
            case 101, 102 -> 8;
            case 111 -> 27;
            case 130, 131 -> available < 3 ? 0 : 3 + u8(b, o + 2);
            case 165 -> 7;
            default -> -1;
        };
    }

    private static int confirmSize(byte[] b, int o) {
        int available = b.length - o;
        if (available < 3) return 0;
        int original = 3 + 7 * u8(b, o + 2);
        int current = available >= 4 ? 4 + 7 * u8(b, o + 3) : 0;
        // Current core's additional flag is normally zero. Validate the complete boundary;
        // the original count=1 cannot be mistaken for a current flag=1 plus card-code byte.
        if (current > 0 && current <= available && (u8(b, o + 2) == 0
                || current == available && original != available)) return current;
        if (original <= available) return original;
        return current > 0 ? current : 0;
    }

    private static int querySize(byte[] b, int o, int header) {
        // Native query data has self-sized records but no outer byte length. Our host emits
        // this synthetic message by itself; a non-record tail may be another core message.
        if (b.length - o < header) return 0;
        int cursor = o + header;
        while (cursor < b.length) {
            if (cursor + 4 > b.length) return cursor == o + header ? 0 : cursor - o;
            int length = i32(b, cursor);
            if (length < 4 || cursor + length > b.length) return cursor == o + header ? 0 : cursor - o;
            cursor += length;
        }
        return cursor - o;
    }
}

