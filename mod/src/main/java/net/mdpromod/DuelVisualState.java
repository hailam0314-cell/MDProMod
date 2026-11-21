package net.mdpromod;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.HashMap;
import java.util.HashSet;
import java.util.HexFormat;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

public final class DuelVisualState {
    public static final int DECK = 1;
    public static final int HAND = 2;
    public static final int MONSTER = 4;
    public static final int SPELL = 8;
    public static final int GRAVE = 16;
    public static final int BANISHED = 32;
    public static final int EXTRA = 64;
    public static final int OVERLAY = 128;
    public static final long DESTROY = 1L;
    public static final long MATERIAL = 8L;
    public static final long FUSION = 262144L;
    public static final long SYNCHRO = 524288L;
    public static final long XYZ = 2097152L;
    public static final long LINK = 268435456L;
    private static final Set<Integer> LIST_ZONES = Set.of(1, 2, 16, 32, 64);
    private final Map<Long, DuelVisualState.MutableCard> cards = new LinkedHashMap<>();
    private final List<DuelVisualState.Event> events = new ArrayList<>();
    private final Map<Integer, DuelVisualState.Card> chainCards = new HashMap<>();
    private final Set<Integer> negatedChains = new HashSet<>();
    private final Set<Integer> targetedChains = new HashSet<>();
    private final Set<Long> targetMarkIds = new LinkedHashSet<>();
    private int phase;
    private MutableCard firstScale;
    private int pendulumSide = -1;
    private final Set<Long> pendulumArrivals = new HashSet<>();
    private int latestChain;
    private DuelVisualState.Event pendingAttack;
    private long nextId = 1L;
    private long malformedMessages;
    private boolean deckReversed;
    private int turnSide;
    private int turnNumber;
    private int matchKill;
    private final int[] lifePoints = new int[]{8000, 8000};
    private int lastHintType;
    private int lastHintPlayer;
    private int lastHintValue;

    public synchronized void reset() {
        this.cards.clear();
        this.events.clear();
        this.chainCards.clear();
        this.negatedChains.clear();
        this.targetedChains.clear();
        this.targetMarkIds.clear();
        this.phase = 0;
        this.endPendulum();
        this.latestChain = 0;
        this.pendingAttack = null;
        this.nextId = 1L;
        this.malformedMessages = 0L;
        this.deckReversed = false;
        this.turnSide = 0;
        this.turnNumber = 0;
        this.matchKill = 0;
        this.lifePoints[0] = this.lifePoints[1] = 8000;
        this.lastHintType = this.lastHintPlayer = this.lastHintValue = 0;
    }

    public synchronized String hintSelectText() {
        return this.lastHintType == 3 ? SystemStrings.get(this.lastHintValue) : "";
    }

    public synchronized int hintSelectId() {
        return this.lastHintType == 3 ? this.lastHintValue : 0;
    }

    synchronized int[] chainOwnerSides(int count) {
        int[] sides = new int[count];
        for (int i = 0; i < count; i++) {
            Card card = chainCards.get(i + 1);
            sides[i] = card == null ? -1 : card.ownerSide();
        }
        return sides;
    }

    public synchronized int[] chainMarkAddresses() {
        Set<Integer> packed = new LinkedHashSet<>();

        for (DuelVisualState.Card card : this.chainCards.values()) {
            this.addFieldMark(packed, card.id());
        }

        return packMarks(packed);
    }

    public synchronized int[] targetMarkAddresses() {
        Set<Integer> packed = new LinkedHashSet<>();

        for (long id : this.targetMarkIds) {
            this.addFieldMark(packed, id);
        }

        return packMarks(packed);
    }

    private void addFieldMark(Set<Integer> packed, long id) {
        DuelVisualState.MutableCard card = this.cards.get(id);
        if (card != null && card.address.field()) {
            DuelVisualState.Address a = card.address;
            packed.add(a.controller() << 16 | a.location() << 8 | a.sequence());
        }
    }

    private static int[] packMarks(Set<Integer> packed) {
        int[] result = new int[packed.size()];
        int i = 0;

        for (int value : packed) {
            result[i++] = value;
        }

        return result;
    }

    public synchronized void initializeCounts(int side, int mainCount, int extraCount) {
        requireSide(side);
        if (mainCount >= 0 && mainCount <= 255 && extraCount >= 0 && extraCount <= 255) {
            this.cards.values().removeIf(c -> c.address.controller() == side && (c.address.location() == 1 || c.address.location() == 64));

            for (int i = 0; i < mainCount; i++) {
                this.create(0, new DuelVisualState.Address(side, 1, i, 10));
            }

            for (int i = 0; i < extraCount; i++) {
                this.create(0, new DuelVisualState.Address(side, 64, i, 8));
            }
        } else {
            throw new IllegalArgumentException("Invalid deck count");
        }
    }

    public synchronized List<DuelVisualState.Card> hand(int side) {
        return snapshots(this.zone(side, 2));
    }

    synchronized int monsterCode(int controller, int sequence) {
        MutableCard card = this.find(new Address(controller, 4, sequence, 0));
        return card == null ? 0 : card.code;
    }

    public synchronized List<DuelVisualState.Card> cards() {
        return snapshots(new ArrayList<>(this.cards.values()));
    }

    public synchronized int count(int side, int location) {
        return this.zone(side, location).size();
    }

    public synchronized long malformedMessages() {
        return this.malformedMessages;
    }

    public synchronized List<DuelVisualState.Event> drainEvents() {
        List<DuelVisualState.Event> result = List.copyOf(this.events);
        this.events.clear();
        return result;
    }

    public synchronized void apply(String name, String hex) {
        byte[] bytes;
        try {
            String compact = hex.replaceAll("\\s", "");
            if ((compact.length() & 1) != 0) {
                throw new IllegalArgumentException("Odd hex length");
            }

            bytes = HexFormat.of().parseHex(compact);
        } catch (RuntimeException ex) {
            this.malformedMessages++;
            return;
        }

        int offset = 0;

        while (offset < bytes.length) {
            int function = u8(bytes, offset);
            int size = messageSize(bytes, offset);
            if (size == -1) {
                return;
            }

            if (size == 0 || offset + size > bytes.length) {
                this.malformedMessages++;
                return;
            }

            this.applyMessage(function, bytes, offset, size);
            offset += size;
        }
    }

    private void applyMessage(int function, byte[] b, int o, int size) {
        // The shipped procedure emits two consecutive MSG_BECOME_TARGET packets.
        // Host queries and hints may arrive between them, even across scene drains.
        if (function != 83 && function != 6 && function != 7 && function != 2) {
            this.firstScale = null;
        }
        switch (function) {
            case 2:
                this.lastHintType = u8(b, o + 1);
                this.lastHintPlayer = u8(b, o + 2);
                this.lastHintValue = (int)u32(b, o + 3);
            case 3:
            case 8:
            case 9:
            case 10:
            case 11:
            case 12:
            case 13:
            case 14:
            case 15:
            case 16:
            case 17:
            case 18:
            case 19:
            case 20:
            case 21:
            case 22:
            case 23:
            case 24:
            case 25:
            case 26:
            case 27:
            case 28:
            case 29:
            case 34:
            case 43:
            case 44:
            case 45:
            case 46:
            case 47:
            case 48:
            case 49:
            case 51:
            case 52:
            case 56:
            case 57:
            case 58:
            case 59:
            case 61:
            case 65:
            case 66:
            case 67:
            case 68:
            case 69:
            case 73:
            case 77:
            case 78:
            case 79:
            case 80:
            case 82:
            case 84:
            case 85:
            case 86:
            case 87:
            case 88:
            case 89:
            case 95:
            case 98:
            case 99:
            case 103:
            case 104:
            case 105:
            case 106:
            case 107:
            case 108:
            case 109:
            case 113:
            case 114:
            case 115:
            case 116:
            case 117:
            case 118:
            case 119:
            case 120:
            case 121:
            case 122:
            case 123:
            case 124:
            case 125:
            case 126:
            case 127:
            case 128:
            case 129:
            case 132:
            case 133:
            case 134:
            case 135:
            case 136:
            case 137:
            case 138:
            case 139:
            case 140:
            case 141:
            case 142:
            case 143:
            case 144:
            case 145:
            case 146:
            case 147:
            case 148:
            case 149:
            case 150:
            case 151:
            case 152:
            case 153:
            case 154:
            case 155:
            case 156:
            case 157:
            case 158:
            case 159:
            case 160:
            case 161:
            case 162:
            case 163:
            case 164:
            case 165:
            case 166:
            case 167:
            case 168:
            case 169:
            default:
                break;
            case 63:
                this.endPendulum();
                break;
            case 4:
                this.phase = 0;
                this.endPendulum();
                this.lifePoints[0] = (int)Math.min(2147483647L, u32(b, o + 2));
                this.lifePoints[1] = (int)Math.min(2147483647L, u32(b, o + 6));
                break;
            case 5:
                this.semantic("audio_win", u8(b, o + 1), u8(b, o + 2), null, 0, this.matchKill);
                break;
            case 6:
            case 7:
                this.updateQuery(function, b, o, size);
                break;
            case 30:
            case 31:
            case 42:
                this.confirm(function, b, o, size);
                break;
            case 32:
                this.shuffleDeck(u8(b, o + 1));
                break;
            case 33:
            case 39:
                this.shuffleList(function == 33 ? 2 : 64, b, o);
                break;
            case 35:
                this.swapGraveDeck(u8(b, o + 1));
                break;
            case 36:
                this.shuffleSet(b, o);
                break;
            case 37:
                this.reverseDeck();
                break;
            case 38:
                this.deckTop(b, o);
                break;
            case 40:
                this.endPendulum();
                this.phase = 0;
                this.turnSide = u8(b, o + 1) & 1;
                this.semantic("audio_turn", this.turnSide, ++this.turnNumber, null, 0, 0);
                break;
            case 41:
                this.endPendulum();
                this.phase = u8(b, o + 1) | u8(b, o + 2) << 8;
                this.semantic("audio_phase", this.turnSide, this.phase, null, 0, 0);
                break;
            case 50:
                this.move(code(b, o + 1), address(b, o + 5), address(b, o + 9), u32(b, o + 13));
                break;
            case 53:
                this.positionChange(b, o);
                break;
            case 54:
            case 60:
            case 62:
            case 64:
                this.annotate(function, b, o);
                break;
            case 55:
                this.swap(b, o);
                break;
            case 70:
                this.endPendulum();
                this.activate(b, o);
                break;
            case 71:
                this.semantic("audio_chain_stack", this.turnSide, u8(b, o + 1), null, this.chainCards.size(), 0);
                break;
            case 72:
                this.chainResolve(u8(b, o + 1));
                break;
            case 74:
                this.semantic("audio_chain_end", this.turnSide, 0L, null, 0, 0);
                this.chainCards.clear();
                this.negatedChains.clear();
                this.targetedChains.clear();
                this.latestChain = 0;
                this.targetMarkIds.clear();
                break;
            case 75:
            case 76:
                this.negate(u8(b, o + 1));
                break;
            case 81:
                this.targetList("selected", b, o, 3);
                break;
            case 83:
                if (!this.pendulumTargets(b, o)) {
                    this.targetList("target", b, o, 2);
                }
                break;
            case 90:
                this.draw(b, o);
                break;
            case 91:
            case 92:
            case 94:
            case 100:
                this.lifePoints(function, b, o);
                break;
            case 93:
            case 96:
            case 97:
                this.relation(function, b, o);
                break;
            case 101:
            case 102:
                DuelVisualState.Address a = new DuelVisualState.Address(u8(b, o + 3), u8(b, o + 4), u8(b, o + 5), 0);
                if (valid(a)) {
                    this.semantic("audio_counter", a.controller(), 0L, this.getOrCreate(a, 0).snapshot(), 0, 0);
                }
                break;
            case 110:
                this.attackDeclare(b, o);
                break;
            case 111:
                this.battle(b, o);
                break;
            case 112:
                this.attackDisabled();
                break;
            case 130:
            case 131:
                int count = u8(b, o + 2);

                for (int i = 0; i < (function == 130 ? count : Math.min(1, count)); i++) {
                    this.semantic(function == 130 ? "audio_coin" : "audio_dice", u8(b, o + 1) & 1, u8(b, o + 3 + i), null, i, count);
                }
                break;
            case 170:
                this.matchKill = code(b, o + 1);
        }
    }

    private void semantic(String kind, int side, long value, DuelVisualState.Card subject, int before, int after) {
        DuelVisualState.Address at = new DuelVisualState.Address(side, 0, 0, 0);
        this.events.add(new DuelVisualState.Event(kind, null, at, at, value, subject, before, after, -1));
    }

    private void lifePoints(int function, byte[] b, int o) {
        int side = u8(b, o + 1);
        if (side > 1) {
            this.malformedMessages++;
        } else {
            long amount = u32(b, o + 2);
            long old = this.lifePoints[side];
            long value = function == 94 ? amount : (function == 92 ? old + amount : old - amount);
            this.lifePoints[side] = (int)Math.max(0L, Math.min(2147483647L, value));
            long delta = this.lifePoints[side] - old;
            this.semantic("audio_lp", side, delta, null, (int)old, this.lifePoints[side]);
        }
    }

    private void updateQuery(int function, byte[] b, int o, int size) {
        int side = u8(b, o + 1);
        int location = u8(b, o + 2);
        if (side > 1) {
            this.malformedMessages++;
        } else {
            int cursor = o + (function == 7 ? 4 : 3);
            int limit = o + size;
            int sequence = function == 7 ? u8(b, o + 3) : 0;
            Set<Long> observed = new HashSet<>();

            while (cursor + 4 <= limit) {
                int length = i32(b, cursor);
                if (length < 4 || cursor + length > limit) {
                    this.malformedMessages++;
                    return;
                }

                if (length >= 8) {
                    int flags = i32(b, cursor + 4);
                    int data = cursor + 8;
                    if (flags != 0) {
                        int incomingCode = 0;
                        if ((flags & 1) != 0) {
                            if (data + 4 > cursor + length) {
                                this.malformedMessages++;
                                return;
                            }

                            incomingCode = code(b, data);
                            data += 4;
                        }

                        DuelVisualState.Address a = new DuelVisualState.Address(side, location, sequence, 0);
                        if ((flags & 2) != 0) {
                            if (data + 4 > cursor + length) {
                                this.malformedMessages++;
                                return;
                            }

                            a = address(b, data);
                        }

                        if (valid(a) && a.location() != 0) {
                            DuelVisualState.MutableCard card = this.find(a);
                            DuelVisualState.Card before = card == null ? null : card.snapshot();
                            if (card == null) {
                                card = this.create(incomingCode, a);
                            }

                            if ((flags & 1) != 0) {
                                card.code = incomingCode;
                            }

                            if ((flags & 2) != 0) {
                                card.address = a;
                                card.publicKnown = publiclyVisible(a);
                            }

                            if (card.address.scaleSlot() && (flags & 8) != 0) {
                                // QUERY_TYPE follows code, position and alias. A scale is a Pendulum Spell.
                                int typeAt = cursor + 8 + 4 * Integer.bitCount(flags & 7);
                                if (typeAt + 4 <= cursor + length) {
                                    card.inPendulumZone = (i32(b, typeAt) & 0x1000002) == 0x1000002;
                                }
                            }
                            int tail = queryTail(b, cursor + 8, cursor + length, flags);
                            int status = queryTailValue(b, tail, cursor + length, flags, 0x80000);
                            int left = queryTailValue(b, tail, cursor + length, flags, 0x200000);
                            int right = queryTailValue(b, tail, cursor + length, flags, 0x400000);
                            if (left >= 0) card.leftScale = left;
                            if (right >= 0) card.rightScale = right;
                            if (status >= 0) {
                                card.disabled = (status & 1) != 0;
                            }

                            this.queryAudio(card, b, cursor + 8, cursor + length, flags);
                            observed.add(card.id);
                            if (before == null || !before.equals(card.snapshot())) {
                                this.events
                                    .add(
                                        new DuelVisualState.Event(
                                            "update",
                                            card.snapshot(),
                                            before == null ? a : before.address(),
                                            a,
                                            0L,
                                            null,
                                            this.zoneCount(a),
                                            this.zoneCount(a),
                                            -1
                                        )
                                    );
                            }
                        }
                    }
                }

                sequence++;
                cursor += length;
            }

            if (cursor != limit) {
                this.malformedMessages++;
            } else {
                if (function == 6) {
                    for (DuelVisualState.MutableCard stale : new ArrayList<>(this.zone(side, location))) {
                        if (!observed.contains(stale.id)) {
                            DuelVisualState.Address from = stale.address;
                            DuelVisualState.Address nowhere = new DuelVisualState.Address(side, 0, 0, 0);
                            stale.address = nowhere;
                            stale.publicKnown = false;
                            this.events.add(new DuelVisualState.Event("update", stale.snapshot(), from, nowhere, 0L, null, this.zoneCount(from), 0, -1));
                            this.cards.remove(stale.id);
                        }
                    }
                }
            }
        }
    }

    private void queryAudio(DuelVisualState.MutableCard card, byte[] b, int data, int limit, int flags) {
        boolean shown = card.address.field() && card.address.faceUp();
        if (!shown) {
            card.attack = card.attribute = card.race = null;
        } else {
            Integer attack = queryFixed(b, data, limit, flags, 256);
            Integer attribute = queryFixed(b, data, limit, flags, 64);
            Integer race = queryFixed(b, data, limit, flags, 128);
            if (attack != null) {
                if (shown && card.attack != null && !attack.equals(card.attack)) {
                    this.semantic(
                        "audio_stat", card.address.controller(), (long)attack.intValue() - card.attack.intValue(), card.snapshot(), card.attack, attack
                    );
                }

                card.attack = attack;
            }

            if (attribute != null) {
                if (shown && card.attribute != null && !attribute.equals(card.attribute)) {
                    this.semantic("audio_characteristic", card.address.controller(), 64L, card.snapshot(), card.attribute, attribute);
                }

                card.attribute = attribute;
            }

            if (race != null) {
                if (shown && card.race != null && !race.equals(card.race)) {
                    this.semantic("audio_characteristic", card.address.controller(), 128L, card.snapshot(), card.race, race);
                }

                card.race = race;
            }
        }
    }

    private static Integer queryFixed(byte[] b, int data, int limit, int flags, int field) {
        if ((flags & field) == 0) {
            return null;
        }

        int p = data + 4 * Integer.bitCount(flags & field - 1);
        return p + 4 <= limit ? i32(b, p) : null;
    }

    private static int queryTailValue(byte[] b, int tail, int limit, int flags, int field) {
        int p = tail + 4 * Integer.bitCount(flags & (field - 1) & (0x80000 | 0x200000 | 0x400000));
        return tail >= 0 && (flags & field) != 0 && p + 4 <= limit ? i32(b, p) : -1;
    }

    private static int queryTail(byte[] b, int data, int limit, int flags) {
        int p = data;

        for (int bit = 1; bit <= 16384; bit <<= 1) {
            if ((flags & bit) != 0) {
                p += 4;
            }
        }

        for (int bit : new int[]{32768, 65536, 131072}) {
            if ((flags & bit) != 0) {
                if (p + 4 > limit) {
                    return -1;
                }

                int count = i32(b, p);
                if (count < 0 || count > (limit - p - 4) / 4) return -1;
                p += 4 + 4 * count;
            }
        }

        if ((flags & 262144) != 0) {
            p += 4;
        }

        return p;
    }

    private void draw(byte[] b, int o) {
        int side = u8(b, o + 1);
        int amount = u8(b, o + 2);
        if (side > 1) {
            this.malformedMessages++;
        } else {
            for (int i = 0; i < amount; i++) {
                int rawCode = i32(b, o + 3 + 4 * i);
                List<DuelVisualState.MutableCard> deck = this.zone(side, 1);
                int deckCount = deck.size();
                DuelVisualState.Address from = new DuelVisualState.Address(side, 1, Math.max(0, deckCount - 1), 10);
                DuelVisualState.MutableCard card = deck.isEmpty() ? this.create(0, from) : deck.get(deck.size() - 1);
                from = card.address;
                int newHandCount = this.count(side, 2) + 1;
                card.code = rawCode & 2147483647;
                card.address = new DuelVisualState.Address(side, 2, newHandCount - 1, rawCode < 0 ? 5 : 10);
                card.publicKnown = rawCode < 0;
                this.events.add(new DuelVisualState.Event("draw", card.snapshot(), from, card.address, 131072L, null, deckCount, newHandCount, -1));
            }
        }
    }

    private void move(int incomingCode, DuelVisualState.Address from, DuelVisualState.Address to, long reason) {
        if (valid(from) && valid(to)) {
            int sourceCount = this.zoneCount(from);
            Map<Long, DuelVisualState.Address> before = this.addresses();
            DuelVisualState.MutableCard card = from.location() == 0 ? this.create(incomingCode, from) : this.find(from);
            if (card == null) {
                card = this.create(incomingCode, from);
            }

            if (incomingCode != 0) {
                card.code = incomingCode;
            }

            boolean sameSlot = sameAddress(from, to);
            if (!sameSlot) {
                card.leftScale = card.rightScale = null;
                card.inPendulumZone = to.scaleSlot() && to.faceUp() && (to.sequence() >= 6 ||
                    (from.location() == HAND && (reason & 0x2000000L) != 0 &&
                     CardRegistry.get(card.code).map(info -> (info.type() & 0x1000000) != 0).orElse(false)));
                this.compactRemoval(from, card.id);
                this.expandInsertion(to, card.id);
            }

            card.address = to;
            card.publicKnown = publiclyVisible(to);
            if (!to.field() || !to.faceUp()) {
                card.attack = card.attribute = card.race = null;
            }

            if (from.field() && to.field()) {
                this.relocateOverlays(from, to);
            }

            int targetCount = to.location() == 0 ? 0 : this.zoneCount(to);
            boolean arrival = to.controller() == this.pendulumSide && to.location() == MONSTER && to.faceUp()
                && (reason & 0x800L) != 0 && (from.location() == HAND || from.location() == EXTRA);
            if (arrival) {
                this.pendulumArrivals.add(card.id);
            }
            this.events.add(new DuelVisualState.Event(arrival ? "pendulum_summon" : "move",
                card.snapshot(), from, to, reason, null, sourceCount, targetCount, -1));
            if (to.location() == 0) {
                this.cards.remove(card.id);
            }

            this.emitRepositions(before, card.id);
        } else {
            this.malformedMessages++;
        }
    }

    private void compactRemoval(DuelVisualState.Address from, long movingId) {
        if (from.overlay()) {
            for (DuelVisualState.MutableCard other : this.cards.values()) {
                DuelVisualState.Address a = other.address;
                if (other.id != movingId && sameZone(a, from) && a.sequence() == from.sequence() && a.position() > from.position()) {
                    other.address = new DuelVisualState.Address(a.controller(), a.location(), a.sequence(), a.position() - 1);
                }
            }
        } else if (LIST_ZONES.contains(from.location())) {
            for (DuelVisualState.MutableCard other : this.cards.values()) {
                DuelVisualState.Address a = other.address;
                if (other.id != movingId && sameZone(a, from) && a.sequence() > from.sequence()) {
                    other.address = new DuelVisualState.Address(a.controller(), a.location(), a.sequence() - 1, a.position());
                }
            }
        }
    }

    private void expandInsertion(DuelVisualState.Address to, long movingId) {
        if (to.overlay()) {
            for (DuelVisualState.MutableCard other : this.cards.values()) {
                DuelVisualState.Address a = other.address;
                if (other.id != movingId && sameZone(a, to) && a.sequence() == to.sequence() && a.position() >= to.position()) {
                    other.address = new DuelVisualState.Address(a.controller(), a.location(), a.sequence(), a.position() + 1);
                }
            }
        } else if (LIST_ZONES.contains(to.location())) {
            for (DuelVisualState.MutableCard other : this.cards.values()) {
                DuelVisualState.Address a = other.address;
                if (other.id != movingId && sameZone(a, to) && a.sequence() >= to.sequence()) {
                    other.address = new DuelVisualState.Address(a.controller(), a.location(), a.sequence() + 1, a.position());
                }
            }
        }
    }

    private void relocateOverlays(DuelVisualState.Address from, DuelVisualState.Address to) {
        for (DuelVisualState.MutableCard other : this.cards.values()) {
            DuelVisualState.Address a = other.address;
            if (a.overlay() && a.controller() == from.controller() && (a.location() & -129) == from.location() && a.sequence() == from.sequence()) {
                other.address = new DuelVisualState.Address(to.controller(), to.location() | 128, to.sequence(), a.position());
            }
        }
    }

    private void annotate(int function, byte[] b, int o) {
        int incomingCode = code(b, o + 1);
        DuelVisualState.Address to = address(b, o + 5);
        if (valid(to) && to.location() != 0) {
            DuelVisualState.MutableCard card = this.find(to);
            if (card == null) {
                card = this.create(incomingCode, to);
            }

            if (incomingCode != 0) {
                card.code = incomingCode;
            }

            card.address = to;
            card.publicKnown = publiclyVisible(to);
            String kind = function == 54 ? "set" : (function == 62 ? "special_summon" : (function == 64 ? "flip_summon" : "summon"));

            if (function == 62 && to.controller() == this.pendulumSide && to.location() == MONSTER) {
                // MOVE can already have reached the client before SPSUMMONING arrives.
                if (!this.pendulumArrivals.add(card.id)) {
                    return;
                }
                kind = "pendulum_summon";
            }
            for (int i = this.events.size() - 1; i >= 0; i--) {
                DuelVisualState.Event pending = this.events.get(i);
                if (pending.card() != null && pending.card().id() == card.id) {
                    if (pending.kind().equals("move") && sameAddress(pending.to(), to)) {
                        this.events
                            .set(
                                i,
                                new DuelVisualState.Event(
                                    kind, card.snapshot(), pending.from(), to, pending.reason(), pending.target(), pending.fromCount(), pending.toCount(), -1
                                )
                            );
                        return;
                    }
                    break;
                }
            }

            this.events.add(new DuelVisualState.Event(kind, card.snapshot(), to, to, 0L, null, this.zoneCount(to), this.zoneCount(to), -1));
        } else {
            this.malformedMessages++;
        }
    }

    private void positionChange(byte[] b, int o) {
        int incomingCode = code(b, o + 1);
        DuelVisualState.Address from = address(b, o + 5);
        DuelVisualState.Address to = new DuelVisualState.Address(from.controller(), from.location(), from.sequence(), u8(b, o + 9));
        if (!valid(from)) {
            this.malformedMessages++;
        } else {
            DuelVisualState.MutableCard card = this.find(from);
            if (card == null) {
                card = this.create(incomingCode, from);
            }

            if (incomingCode != 0) {
                card.code = incomingCode;
            }

            card.address = to;
            card.publicKnown = publiclyVisible(to);
            if (!to.field() || !to.faceUp()) {
                card.attack = card.attribute = card.race = null;
            }

            this.events.add(new DuelVisualState.Event("position", card.snapshot(), from, to, 0L, null, this.zoneCount(from), this.zoneCount(to), -1));
        }
    }

    private void swap(byte[] b, int o) {
        DuelVisualState.Address a = address(b, o + 5);
        DuelVisualState.Address c = address(b, o + 13);
        if (valid(a) && valid(c)) {
            DuelVisualState.MutableCard first = this.getOrCreate(a, code(b, o + 1));
            DuelVisualState.MutableCard second = this.getOrCreate(c, code(b, o + 9));
            Map<Long, DuelVisualState.Address> before = this.addresses();
            DuelVisualState.Address firstTo = new DuelVisualState.Address(c.controller(), c.location(), c.sequence(), a.position());
            DuelVisualState.Address secondTo = new DuelVisualState.Address(a.controller(), a.location(), a.sequence(), c.position());
            first.address = firstTo;
            second.address = secondTo;
            first.publicKnown = publiclyVisible(firstTo);
            second.publicKnown = publiclyVisible(secondTo);

            for (DuelVisualState.MutableCard other : this.cards.values()) {
                DuelVisualState.Address old = before.get(other.id);
                if (old != null && old.overlay()) {
                    DuelVisualState.Address dest = hostMatches(old, a) ? firstTo : (hostMatches(old, c) ? secondTo : null);
                    if (dest != null) {
                        other.address = new DuelVisualState.Address(dest.controller(), dest.location() | 128, dest.sequence(), old.position());
                    }
                }
            }

            this.events
                .add(new DuelVisualState.Event("move", first.snapshot(), a, firstTo, 0L, second.snapshot(), this.zoneCount(a), this.zoneCount(firstTo), -1));
            this.events
                .add(new DuelVisualState.Event("move", second.snapshot(), c, secondTo, 0L, first.snapshot(), this.zoneCount(c), this.zoneCount(secondTo), -1));
            this.emitRepositions(before, first.id, second.id);
        } else {
            this.malformedMessages++;
        }
    }

    private void activate(byte[] b, int o) {
        DuelVisualState.Address source = address(b, o + 5);
        if (!valid(source)) {
            this.malformedMessages++;
        } else {
            DuelVisualState.MutableCard card = this.getOrCreate(source, code(b, o + 1));
            this.latestChain = u8(b, o + 16);
            this.chainCards.put(this.latestChain, card.snapshot());
            this.events
                .add(
                    new DuelVisualState.Event(
                        "activate", card.snapshot(), source, source, u32(b, o + 12), null, this.zoneCount(source), this.zoneCount(source), -1
                    )
                );
        }
    }

    private void chainResolve(int index) {
        DuelVisualState.Card original = this.chainCards.get(index);
        DuelVisualState.MutableCard current = original == null ? null : this.cards.get(original.id());
        DuelVisualState.Card resolving = current == null ? original : current.snapshot();
        if (this.negatedChains.contains(index) || resolving != null && original != null && resolving.code() != original.code()) {
            resolving = null;
        }

        DuelVisualState.Card previous = this.chainCards.get(index - 1);
        DuelVisualState.MutableCard previousNow = previous == null ? null : this.cards.get(previous.id());
        boolean previousField = previousNow != null && previousNow.address.field();
        this.semantic(
            "audio_chain_resolve",
            this.turnSide,
            index,
            resolving,
            this.chainCards.size(),
            (this.targetedChains.contains(index) ? 1 : 0) | (previousField ? 2 : 0)
        );
    }

    private void negate(int chainIndex) {
        this.negatedChains.add(chainIndex);
        DuelVisualState.Card original = this.chainCards.get(chainIndex);
        DuelVisualState.MutableCard current = original == null ? null : this.cards.get(original.id());
        DuelVisualState.Card card = current == null ? original : current.snapshot();
        DuelVisualState.Address a = card == null ? null : card.address();
        this.events
            .add(new DuelVisualState.Event("negate", card, a, a, chainIndex, null, a == null ? 0 : this.zoneCount(a), a == null ? 0 : this.zoneCount(a), -1));
    }

    private void relation(int function, byte[] b, int o) {
        DuelVisualState.Address a = address(b, o + 1);
        DuelVisualState.Address dest = address(b, o + 5);
        if (valid(a) && valid(dest)) {
            DuelVisualState.MutableCard card = a.location() == 0 ? null : this.getOrCreate(a, 0);
            DuelVisualState.MutableCard target = dest.location() == 0 ? null : this.getOrCreate(dest, 0);
            String kind = function == 110 ? "attack_declare" : (function == 93 ? "equip" : (function == 97 ? "cancel_target" : "target"));
            this.events
                .add(
                    new DuelVisualState.Event(
                        kind,
                        card == null ? null : card.snapshot(),
                        a,
                        dest,
                        function,
                        target == null ? null : target.snapshot(),
                        this.zoneCount(a),
                        this.zoneCount(dest),
                        -1
                    )
                );
        } else {
            this.malformedMessages++;
        }
    }

    private void attackDeclare(byte[] b, int o) {
        DuelVisualState.Address attacker = address(b, o + 1);
        DuelVisualState.Address target = address(b, o + 5);
        if (valid(attacker) && (target.location() == 0 || valid(target))) {
            DuelVisualState.MutableCard a = this.getOrCreate(attacker, 0);
            DuelVisualState.MutableCard t = target.location() == 0 ? null : this.getOrCreate(target, 0);
            this.pendingAttack = new DuelVisualState.Event(
                "attack_declare", a.snapshot(), attacker, target, 0L, t == null ? null : t.snapshot(), this.zoneCount(attacker), this.zoneCount(target), -1
            );
            this.events.add(this.pendingAttack);
        } else {
            this.malformedMessages++;
        }
    }

    private void battle(byte[] b, int o) {
        DuelVisualState.Address attacker = address(b, o + 1);
        DuelVisualState.Address target = address(b, o + 14);
        if (valid(attacker) && attacker.location() != 0 && (target.location() == 0 || valid(target))) {
            DuelVisualState.MutableCard a = this.getOrCreate(attacker, 0);
            DuelVisualState.MutableCard t = target.location() == 0 ? null : this.getOrCreate(target, 0);
            int attackPower = Math.max(0, i32(b, o + 5));
            int targetPower = t == null ? 0 : Math.max(0, i32(b, o + ((target.position() & 12) != 0 ? 22 : 18)));
            boolean guard = t != null && targetPower >= attackPower;
            int attackedSide = t == null ? 1 - attacker.controller() : target.controller();
            boolean finalBlow = attackPower - targetPower >= this.lifePoints[attackedSide] && (t == null || (target.position() & 12) == 0);
            DuelVisualState.Event attack = new DuelVisualState.Event(
                "attack",
                a.snapshot(),
                attacker,
                target,
                (guard ? 1L : 0L) | (finalBlow ? 2L : 0L) | (a.attribute == null ? 0L : (long)a.attribute.intValue() << 8),
                t == null ? null : t.snapshot(),
                attackPower,
                targetPower,
                -1
            );
            this.events.add(attack);
            this.pendingAttack = null;
        } else {
            this.malformedMessages++;
        }
    }

    private void attackDisabled() {
        if (this.pendingAttack != null) {
            DuelVisualState.Event event = this.pendingAttack;
            this.events
                .add(
                    new DuelVisualState.Event(
                        "attack_disabled", event.card(), event.from(), event.to(), 0L, event.target(), event.fromCount(), event.toCount(), -1
                    )
                );
            this.pendingAttack = null;
        } else {
            for (int i = this.events.size() - 1; i >= 0; i--) {
                DuelVisualState.Event event = this.events.get(i);
                if (event.kind().equals("attack_declare")) {
                    this.events
                        .add(
                            new DuelVisualState.Event(
                                "attack_disabled", event.card(), event.from(), event.to(), 0L, event.target(), event.fromCount(), event.toCount(), -1
                            )
                        );
                    this.pendingAttack = null;
                    return;
                }
            }

            this.emitSimple("attack_disabled", null, null, 0L, -1);
        }
    }

    private void endPendulum() {
        this.firstScale = null;
        this.pendulumSide = -1;
        this.pendulumArrivals.clear();
    }

    private boolean pendulumTargets(byte[] b, int o) {
        int count = u8(b, o + 1);
        if ((this.phase != 4 && this.phase != 0x100) || this.latestChain != 0 || count < 1 || count > 2) {
            this.firstScale = null;
            return false;
        }
        MutableCard first = count == 2 ? this.scaleAt(address(b, o + 2)) : this.firstScale;
        MutableCard last = this.scaleAt(address(b, o + 2 + (count - 1) * 4));
        this.firstScale = null;
        if (last == null || (count == 2 && first == null)) {
            return false;
        }
        if (first == null || first.address.controller() != last.address.controller()
            || first.address.sequence() == last.address.sequence()) {
            this.firstScale = count == 1 ? last : null;
            return count == 1;
        }
        MutableCard left = first.address.sequence() < last.address.sequence() ? first : last;
        MutableCard right = left == first ? last : first;
        int leftSlot = left.address.sequence();
        int rightSlot = right.address.sequence();
        if (!(leftSlot == 0 && rightSlot == 4 || leftSlot == 6 && rightSlot == 7)) {
            return false;
        }
        if (this.pendulumSide == -1) {
            this.pendulumSide = left.address.controller();
            this.events.add(new Event("pendulum_scales", left.snapshot(), left.address, right.address, 0,
                right.snapshot(), left.scale(true), right.scale(false), -1));
        }
        return true;
    }

    private MutableCard scaleAt(Address a) {
        if (a.location() != SPELL || a.controller() > 1 ||
            !(a.sequence() == 0 || a.sequence() == 4 || a.sequence() == 6 || a.sequence() == 7)) {
            return null;
        }
        MutableCard c = this.find(a);
        return c != null && c.address.faceUp() && CardRegistry.get(c.code)
            .map(info -> (info.type() & 0x1000000) != 0).orElse(false) ? c : null;
    }

    private void targetList(String kind, byte[] b, int o, int header) {
        int count = u8(b, o + header - 1);

        for (int i = 0; i < count; i++) {
            DuelVisualState.Address a = address(b, o + header + i * 4);
            if (valid(a) && a.location() != 0) {
                DuelVisualState.MutableCard card = this.getOrCreate(a, 0);
                if (kind.equals("target") && this.latestChain > 0) {
                    this.targetedChains.add(this.latestChain);
                    this.targetMarkIds.add(card.id);
                }

                this.emitSimple(kind, card, null, 0L, -1);
            }
        }
    }

    private void confirm(int function, byte[] b, int o, int size) {
        int audience = u8(b, o + 1);
        int header = function == 31 && size >= 4 && size == 4 + 7 * u8(b, o + 3) ? 4 : 3;
        int count = u8(b, o + header - 1);

        for (int i = 0; i < count; i++) {
            int p = o + header + i * 7;
            DuelVisualState.Address a = new DuelVisualState.Address(u8(b, p + 4), u8(b, p + 5), u8(b, p + 6), 0);
            if (valid(a) && a.location() != 0) {
                DuelVisualState.MutableCard card = this.find(a);
                if (card == null) {
                    card = this.create(code(b, p), a);
                } else if (code(b, p) != 0) {
                    card.code = code(b, p);
                }

                this.emitSimple("reveal", card, null, 0L, audience);
            }
        }
    }

    private void shuffleList(int location, byte[] b, int o) {
        int side = u8(b, o + 1);
        int count = u8(b, o + 2);
        if (side > 1) {
            this.malformedMessages++;
        } else {
            List<DuelVisualState.MutableCard> old = this.zone(side, location);
            List<DuelVisualState.MutableCard> unused = new ArrayList<>(old);
            Map<Long, DuelVisualState.Address> before = this.addresses();
            Map<Long, DuelVisualState.Card> snapshots = new HashMap<>();

            for (DuelVisualState.MutableCard card : old) {
                snapshots.put(card.id, card.snapshot());
            }

            for (int i = 0; i < count; i++) {
                int incoming = code(b, o + 3 + i * 4);
                DuelVisualState.MutableCard selected = null;

                for (DuelVisualState.MutableCard candidate : unused) {
                    if (incoming != 0 && candidate.code == incoming) {
                        selected = candidate;
                        break;
                    }
                }

                if (selected == null) {
                    for (DuelVisualState.MutableCard candidate : unused) {
                        if (candidate.code == 0) {
                            selected = candidate;
                            break;
                        }
                    }
                }

                if (selected == null && !unused.isEmpty()) {
                    selected = unused.get(0);
                }

                if (selected == null) {
                    selected = this.create(0, new DuelVisualState.Address(side, location, i, location == 64 ? 8 : 10));
                }

                unused.remove(selected);
                if (incoming != 0) {
                    selected.code = incoming;
                }

                selected.address = new DuelVisualState.Address(side, location, i, selected.address.position());
                selected.publicKnown = publiclyVisible(selected.address);
            }

            for (DuelVisualState.MutableCard removed : unused) {
                this.cards.remove(removed.id);
            }

            this.emitRepositions(before);

            for (DuelVisualState.MutableCard card : this.zone(side, location)) {
                DuelVisualState.Card oldCard = snapshots.get(card.id);
                if (oldCard == null || oldCard.address().equals(card.address) && (oldCard.code() != card.code || oldCard.publicKnown() != card.publicKnown)) {
                    this.events.add(new DuelVisualState.Event("reposition", card.snapshot(), card.address, card.address, 0L, null, count, count, -1));
                }
            }

            this.emitSimple("shuffle", null, null, location, -1, new DuelVisualState.Address(side, location, 0, 10));
        }
    }

    private void shuffleDeck(int side) {
        if (side > 1) {
            this.malformedMessages++;
        } else {
            for (DuelVisualState.MutableCard card : this.zone(side, 1)) {
                card.code = 0;
                card.publicKnown = false;
                card.address = new DuelVisualState.Address(side, 1, card.address.sequence(), 10);
            }

            this.emitSimple("shuffle", null, null, 1L, -1, new DuelVisualState.Address(side, 1, 0, 10));
        }
    }

    private void shuffleSet(byte[] b, int o) {
        int count = u8(b, o + 2);
        List<DuelVisualState.MutableCard> chosen = new ArrayList<>();
        List<DuelVisualState.Address> originals = new ArrayList<>();
        List<DuelVisualState.Address> destinations = new ArrayList<>();
        Map<Long, DuelVisualState.Address> before = this.addresses();

        for (int i = 0; i < count; i++) {
            DuelVisualState.Address from = address(b, o + 3 + i * 4);
            DuelVisualState.Address dest = address(b, o + 3 + count * 4 + i * 4);
            if (!valid(from)) {
                this.malformedMessages++;
                return;
            }

            DuelVisualState.MutableCard card = this.getOrCreate(from, 0);
            chosen.add(card);
            originals.add(from);
            destinations.add(dest);
        }

        for (int i = 0; i < count; i++) {
            DuelVisualState.MutableCard card = chosen.get(i);
            DuelVisualState.Address original = originals.get(i);
            DuelVisualState.Address sentDest = destinations.get(i);
            DuelVisualState.Address dest = sentDest.location() == 0 ? originals.get(i) : sentDest;
            if (!valid(dest)) {
                this.malformedMessages++;
            } else {
                card.address = new DuelVisualState.Address(dest.controller(), dest.location(), dest.sequence(), original.position());
                card.code = 0;
                card.publicKnown = false;
                this.events
                    .add(
                        new DuelVisualState.Event(
                            "shuffle_set", card.snapshot(), original, card.address, 0L, null, this.zoneCount(original), this.zoneCount(card.address), -1
                        )
                    );
            }
        }

        for (DuelVisualState.MutableCard other : this.cards.values()) {
            DuelVisualState.Address a = before.get(other.id);
            if (a != null && a.overlay()) {
                for (int i = 0; i < count; i++) {
                    if (hostMatches(a, originals.get(i))) {
                        DuelVisualState.Address dest = chosen.get(i).address;
                        other.address = new DuelVisualState.Address(dest.controller(), dest.location() | 128, dest.sequence(), a.position());
                        break;
                    }
                }
            }
        }

        long[] ids = chosen.stream().mapToLong(c -> c.id).toArray();
        this.emitRepositions(before, ids);
    }

    private void swapGraveDeck(int side) {
        if (side > 1) {
            this.malformedMessages++;
        } else {
            List<DuelVisualState.MutableCard> deck = this.zone(side, 1);
            List<DuelVisualState.MutableCard> grave = this.zone(side, 16);

            for (int i = 0; i < grave.size(); i++) {
                DuelVisualState.MutableCard card = grave.get(i);
                DuelVisualState.Address from = card.address;
                card.address = new DuelVisualState.Address(side, 1, i, 10);
                card.publicKnown = false;
                this.events.add(new DuelVisualState.Event("move", card.snapshot(), from, card.address, 64L, null, grave.size(), grave.size(), -1));
            }

            for (int i = 0; i < deck.size(); i++) {
                DuelVisualState.MutableCard card = deck.get(i);
                DuelVisualState.Address from = card.address;
                card.address = new DuelVisualState.Address(side, 16, i, 5);
                card.publicKnown = true;
                this.events.add(new DuelVisualState.Event("move", card.snapshot(), from, card.address, 64L, null, deck.size(), deck.size(), -1));
            }
        }
    }

    private void reverseDeck() {
        this.deckReversed = !this.deckReversed;
        Map<Long, DuelVisualState.Address> before = this.addresses();

        for (int side = 0; side < 2; side++) {
            List<DuelVisualState.MutableCard> deck = this.zone(side, 1);

            for (DuelVisualState.MutableCard card : deck) {
                card.address = new DuelVisualState.Address(side, 1, deck.size() - 1 - card.address.sequence(), card.address.position());
            }
        }

        this.emitRepositions(before);
    }

    private void deckTop(byte[] b, int o) {
        int side = u8(b, o + 1);
        int offset = u8(b, o + 2);
        int incomingCode = code(b, o + 3);
        if (side > 1) {
            this.malformedMessages++;
        } else {
            int sequence = Math.max(0, this.count(side, 1) - offset - 1);
            DuelVisualState.MutableCard card = this.getOrCreate(new DuelVisualState.Address(side, 1, sequence, this.deckReversed ? 5 : 10), incomingCode);
            card.publicKnown = this.deckReversed || i32(b, o + 3) < 0;
            this.emitSimple("reveal", card, null, 0L, -1);
        }
    }

    private void emitRepositions(Map<Long, DuelVisualState.Address> before, long... excluded) {
        for (DuelVisualState.MutableCard card : this.cards.values()) {
            boolean skip = false;

            for (long id : excluded) {
                if (card.id == id) {
                    skip = true;
                    break;
                }
            }

            DuelVisualState.Address original = before.get(card.id);
            if (!skip && original != null && !original.equals(card.address)) {
                this.events
                    .add(
                        new DuelVisualState.Event(
                            "reposition", card.snapshot(), original, card.address, 0L, null, this.zoneCount(original), this.zoneCount(card.address), -1
                        )
                    );
            }
        }
    }

    private void emitSimple(String kind, DuelVisualState.MutableCard card, DuelVisualState.Card target, long reason, int audience) {
        this.emitSimple(kind, card, target, reason, audience, card == null ? null : card.address);
    }

    private void emitSimple(String kind, DuelVisualState.MutableCard card, DuelVisualState.Card target, long reason, int audience, DuelVisualState.Address a) {
        this.events
            .add(
                new DuelVisualState.Event(
                    kind,
                    card == null ? null : card.snapshot(),
                    a,
                    a,
                    reason,
                    target,
                    a == null ? 0 : this.zoneCount(a),
                    a == null ? 0 : this.zoneCount(a),
                    audience
                )
            );
    }

    private DuelVisualState.MutableCard getOrCreate(DuelVisualState.Address a, int incomingCode) {
        DuelVisualState.MutableCard card = this.find(a);
        if (card == null) {
            return this.create(incomingCode, a);
        }

        if (incomingCode != 0) {
            card.code = incomingCode;
        }

        return card;
    }

    private DuelVisualState.MutableCard create(int code, DuelVisualState.Address address) {
        DuelVisualState.MutableCard card = new DuelVisualState.MutableCard(this.nextId++, code, address, publiclyVisible(address), false);
        this.cards.put(card.id, card);
        return card;
    }

    private DuelVisualState.MutableCard find(DuelVisualState.Address a) {
        for (DuelVisualState.MutableCard card : this.cards.values()) {
            if (sameAddress(card.address, a)) {
                return card;
            }
        }

        return null;
    }

    private List<DuelVisualState.MutableCard> zone(int side, int location) {
        List<DuelVisualState.MutableCard> result = new ArrayList<>();

        for (DuelVisualState.MutableCard card : this.cards.values()) {
            if (card.address.controller() == side && card.address.location() == location) {
                result.add(card);
            }
        }

        result.sort(
            Comparator.<DuelVisualState.MutableCard>comparingInt(c -> c.address.sequence())
                .thenComparingInt(c -> c.address.overlay() ? c.address.position() : 0)
        );
        return result;
    }

    private int zoneCount(DuelVisualState.Address a) {
        return a.location() == 0 ? 0 : this.count(a.controller(), a.location());
    }

    private Map<Long, DuelVisualState.Address> addresses() {
        Map<Long, DuelVisualState.Address> result = new HashMap<>();

        for (DuelVisualState.MutableCard card : this.cards.values()) {
            result.put(card.id, card.address);
        }

        return result;
    }

    private static List<DuelVisualState.Card> snapshots(List<DuelVisualState.MutableCard> values) {
        List<DuelVisualState.Card> result = new ArrayList<>(values.size());

        for (DuelVisualState.MutableCard card : values) {
            result.add(card.snapshot());
        }

        return List.copyOf(result);
    }

    private static boolean sameZone(DuelVisualState.Address a, DuelVisualState.Address b) {
        return a.controller() == b.controller() && a.location() == b.location();
    }

    private static boolean sameAddress(DuelVisualState.Address a, DuelVisualState.Address b) {
        return a != null && b != null && sameZone(a, b) && a.sequence() == b.sequence() && (!a.overlay() || a.position() == b.position());
    }

    private static boolean hostMatches(DuelVisualState.Address overlay, DuelVisualState.Address host) {
        return overlay.controller() == host.controller() && (overlay.location() & -129) == host.location() && overlay.sequence() == host.sequence();
    }

    private static boolean publiclyVisible(DuelVisualState.Address a) {
        return a.overlay() ? true : a.faceUp() && a.location() != 1 && a.location() != 0;
    }

    private static boolean valid(DuelVisualState.Address a) {
        return a.location() == 0 || a.controller() >= 0 && a.controller() < 2 && (LIST_ZONES.contains(a.location()) || a.field() || a.overlay());
    }

    private static void requireSide(int side) {
        if (side < 0 || side > 1) {
            throw new IllegalArgumentException("Invalid controller");
        }
    }

    private static DuelVisualState.Address address(byte[] b, int o) {
        return new DuelVisualState.Address(u8(b, o), u8(b, o + 1), u8(b, o + 2), u8(b, o + 3));
    }

    private static int u8(byte[] b, int o) {
        return b[o] & 0xFF;
    }

    private static int i32(byte[] b, int o) {
        return u8(b, o) | u8(b, o + 1) << 8 | u8(b, o + 2) << 16 | u8(b, o + 3) << 24;
    }

    private static long u32(byte[] b, int o) {
        return Integer.toUnsignedLong(i32(b, o));
    }

    private static int code(byte[] b, int o) {
        return i32(b, o) & 2147483647;
    }

    private static int messageSize(byte[] b, int o) {
        int available = b.length - o;
        int fn = u8(b, o);

        return switch (fn) {
            case 1, 3, 37, 61, 63, 65, 74, 112, 113, 114 -> 1;
            case 2, 38 -> 7;
            case 4 -> 18;
            case 5 -> 3;
            case 6 -> querySize(b, o, 3);
            case 7 -> querySize(b, o, 4);
            default -> -1;
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
        };
    }

    private static int confirmSize(byte[] b, int o) {
        int available = b.length - o;
        if (available < 3) {
            return 0;
        }

        int original = 3 + 7 * u8(b, o + 2);
        int current = available >= 4 ? 4 + 7 * u8(b, o + 3) : 0;
        if (current <= 0 || current > available || u8(b, o + 2) != 0 && (current != available || original == available)) {
            if (original <= available) {
                return original;
            } else {
                return current > 0 ? current : 0;
            }
        } else {
            return current;
        }
    }

    private static int querySize(byte[] b, int o, int header) {
        if (b.length - o < header) {
            return 0;
        }

        int cursor = o + header;

        while (cursor < b.length) {
            if (cursor + 4 > b.length) {
                return cursor == o + header ? 0 : cursor - o;
            }

            int length = i32(b, cursor);
            if (length < 4 || cursor + length > b.length) {
                return cursor == o + header ? 0 : cursor - o;
            }

            cursor += length;
        }

        return cursor - o;
    }

    public record Address(int controller, int location, int sequence, int position) {
        boolean scaleSlot() {
            return location == SPELL && (sequence == 0 || sequence == 4 || sequence == 6 || sequence == 7);
        }

        boolean leftScaleSlot() {
            return sequence == 0 || sequence == 6;
        }

        public boolean overlay() {
            return (this.location & 128) != 0;
        }

        public boolean field() {
            return !this.overlay() && (this.location == 4 || this.location == 8);
        }

        public boolean faceUp() {
            return !this.overlay() && (this.position & 5) != 0;
        }
    }

    public record Card(long id, int code, DuelVisualState.Address address, boolean publicKnown, boolean disabled, int pendulumScale, int ownerSide) {
        public Card(long id, int code, Address address, boolean publicKnown, boolean disabled, int pendulumScale) {
            this(id, code, address, publicKnown, disabled, pendulumScale, address.controller());
        }

        public Card(long id, int code, Address address, boolean publicKnown, boolean disabled) {
            this(id, code, address, publicKnown, disabled, -1);
        }
    }

    public record Event(
        String kind,
        DuelVisualState.Card card,
        DuelVisualState.Address from,
        DuelVisualState.Address to,
        long reason,
        DuelVisualState.Card target,
        int fromCount,
        int toCount,
        int audience
    ) {
    }

    private static final class MutableCard {
        final long id;
        final int ownerSide;
        int code;
        DuelVisualState.Address address;
        boolean publicKnown;
        boolean disabled;
        Integer attack;
        Integer attribute;
        Integer race;
        Integer leftScale, rightScale;
        boolean inPendulumZone;

        int scale(boolean left) {
            Integer current = left ? leftScale : rightScale;
            if (current != null) return current;
            int packed = CardRegistry.get(code).map(CardInfo::level).orElse(0);
            return (packed >>> (left ? 24 : 16)) & 255;
        }

        MutableCard(long id, int code, DuelVisualState.Address address, boolean publicKnown, boolean disabled) {
            this.id = id;
            this.ownerSide = address.controller();
            this.code = code;
            this.address = address;
            this.publicKnown = publicKnown;
            this.disabled = disabled;
        }

        DuelVisualState.Card snapshot() {
            int value = inPendulumZone && address.scaleSlot() && address.faceUp() ? scale(address.leftScaleSlot()) : -1;
            return new DuelVisualState.Card(id, code, address, publicKnown, disabled, value, ownerSide);
        }
    }
}
