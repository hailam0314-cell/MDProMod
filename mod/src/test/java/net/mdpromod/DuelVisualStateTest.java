package net.mdpromod;

import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.HexFormat;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/** Standalone Java 17 regression tests; no Forge, Gson, native core or JUnit dependency. */
public final class DuelVisualStateTest {
    private static int assertions;
    private static final Pattern NAME = Pattern.compile("\\\"name\\\":\\\"([^\\\"]+)\\\"");
    private static final Pattern HEX = Pattern.compile("\\\"hex\\\":\\\"([0-9A-Fa-f]+)\\\"");

    public static void main(String[] args) throws Exception {
        repeatedCardsAndHandCompression();
        eventSnapshotsAndSummonAnnotations();
        drawPrivacyAndTemporaryConfirm();
        overlayAttachmentAndControlChange();
        listShuffleAndHiddenSetShuffle();
        movementReasonAndEffects();
        queryRefreshAndPublicRevocation();
        effectDisabledStatusFlag();
        truncatedInput();
        for (String arg : args) replay(Path.of(arg));
        System.out.println("DuelVisualState passed " + assertions + " assertions");
    }

    private static void repeatedCardsAndHandCompression() {
        DuelVisualState state = new DuelVisualState();
        state.initializeCounts(0, 40, 15);
        state.apply("Draw", draw(0, 123, 123, 456));
        List<DuelVisualState.Card> hand = state.hand(0);
        check(hand.size() == 3, "All duplicate copies enter hand");
        long first = hand.get(0).id(), second = hand.get(1).id(), third = hand.get(2).id();
        check(first != second, "Duplicate card codes never collapse identities");
        state.drainEvents();
        state.apply("Move", move(123, a(0, 2, 1, 10), a(0, 4, 3, 1), 0x10));
        check(state.hand(0).size() == 2, "Remove by source GPS");
        check(state.hand(0).get(0).id() == first, "First duplicate preserved");
        check(state.hand(0).get(1).id() == third, "Remaining hand compacts without replacing token");
        check(state.hand(0).get(1).address().sequence() == 1, "Compressed sequence correct");
        check(at(state, 0, 4, 3, 0).id() == second, "Second duplicate placed in selected slot");
        List<DuelVisualState.Event> events = state.drainEvents();
        check(events.get(0).fromCount() == 3, "Source count captured before movement");
        check(events.stream().anyMatch(e -> e.kind().equals("reposition") && e.card().id() == third),
                "Scene receives the exact hand index update");
        state.apply("Move", move(0, a(0, 2, 0, 10), a(0, 16, 0, 5), 0));
        check(at(state, 0, 16, 0, 0).code() == 123, "Hidden Move does not erase known source identity");
        check(state.hand(0).get(0).id() == third, "Code zero departure still removes correct card");
        check(state.count(0, 1) == 37, "Three draws reduce deck count");
    }

    private static void eventSnapshotsAndSummonAnnotations() {
        DuelVisualState state = new DuelVisualState();
        state.initializeCounts(0, 40, 0);
        state.apply("Draw", draw(0, 11, 22));
        List<DuelVisualState.Event> draws = state.drainEvents();
        check(draws.get(0).card().address().sequence() == 0 && draws.get(0).toCount() == 1,
                "First draw retains its intermediate layout");
        check(draws.get(1).toCount() == 2, "Second draw advances hand count");
        long token = state.hand(0).get(0).id();
        state.apply("Move", move(11, a(0, 2, 0, 10), a(0, 4, 2, 1), 0x10));
        state.apply("Summoning", summon(60, 11, a(0, 4, 2, 1)));
        List<DuelVisualState.Event> result = state.drainEvents();
        check(result.get(0).kind().equals("summon"), "Move and Summoning merge into one flight");
        check(result.get(0).card().id() == token && result.get(0).from().location() == 2,
                "Merged summon keeps source hand GPS and token");
        state.apply("SpSummoning", summon(62, 11, a(0, 4, 2, 1)));
        DuelVisualState.Event late = state.drainEvents().get(0);
        check(late.kind().equals("special_summon") && late.from().equals(late.to()),
                "Cross-drain annotation has no repeated flight");
        state.apply("Summoned", "3D");
        state.apply("SpSummoned", "3F");
        check(state.drainEvents().isEmpty(), "Completion messages never create duplicate entities");
        state.apply("Move", move(22, a(0, 2, 0, 10), a(0, 8, 4, 10), 0));
        state.apply("Set", summon(54, 22, a(0, 8, 4, 10)));
        check(state.drainEvents().get(0).kind().equals("set"), "Set annotates original flight");
        check(state.cards().stream().filter(c -> c.address().location() == 8).count() == 1,
                "One model for Move followed by Set");
    }

    private static void drawPrivacyAndTemporaryConfirm() {
        DuelVisualState state = new DuelVisualState();
        state.initializeCounts(1, 40, 0);
        state.apply("Draw", draw(1, 0, 888, 999 | 0x80000000));
        check(state.hand(1).size() == 3 && state.hand(1).get(0).code() == 0,
                "Unknown opponent draws remain real cards with unknown code");
        check(!state.hand(1).get(1).publicKnown(), "Raw direct-core code does not imply public hand");
        check(state.hand(1).get(2).code() == 999 && state.hand(1).get(2).publicKnown(),
                "Draw's high bit means currently public hand, stripped from code");
        state.drainEvents();
        state.apply("ConfirmCards", confirm(true, 0, 888, 1, 2, 1));
        DuelVisualState.Event reveal = state.drainEvents().get(0);
        check(reveal.kind().equals("reveal") && reveal.audience() == 0 && reveal.card().code() == 888,
                "Current-core confirmation creates targeted presentation");
        check(!state.hand(1).get(1).publicKnown(), "Temporary reveal never exposes hand forever");
        state.apply("ConfirmCards", confirm(false, 1, 777, 1, 2, 0));
        check(state.hand(1).get(0).code() == 777, "Original classic confirm supported");
        state.apply("ConfirmCards", "1F0100");
        check(state.malformedMessages() == 0, "Original zero-card confirmation parses safely");
        state.apply("Draw", draw(1, 0));
        check(state.hand(1).get(3).code() == 0 && !state.hand(1).get(3).publicKnown(),
                "Hidden subsequent draw does not infer any earlier revealed card code");
    }

    private static void overlayAttachmentAndControlChange() {
        DuelVisualState state = new DuelVisualState();
        state.apply("Draw", draw(0, 11, 22, 33));
        state.apply("Move", move(11, a(0, 2, 0, 10), a(0, 4, 0, 1), 0));
        state.apply("Move", move(22, a(0, 2, 0, 10), a(0, 4, 1, 1), 0));
        long material = at(state, 0, 4, 0, 0).id();
        state.apply("Move", move(33, a(0, 2, 0, 10), a(0, 4, 2, 1), 0x800));
        long host = at(state, 0, 4, 2, 0).id();
        state.apply("Move", move(11, a(0, 4, 0, 1), a(0, 132, 2, 0), 0x200008));
        state.apply("Move", move(22, a(0, 4, 1, 1), a(0, 132, 2, 1), 0x200008));
        check(at(state, 0, 132, 2, 0).id() == material, "Overlay material preserves its token");
        check(state.count(0, 132) == 2, "Different material index shares one host slot");
        state.apply("Move", move(33, a(0, 4, 2, 1), a(1, 4, 4, 1), 0));
        check(at(state, 1, 4, 4, 0).id() == host, "Host control change preserves token");
        check(at(state, 1, 132, 4, 0).id() == material, "Materials follow host's controller and slot");
        state.apply("Move", move(11, a(1, 132, 4, 0), a(0, 16, 0, 5), 0));
        check(at(state, 0, 16, 0, 0).id() == material, "Detachment uses overlay source index");
        check(at(state, 1, 132, 4, 0).code() == 22, "Remaining overlay materials compact indices");
        check(state.cards().stream().filter(c -> c.id() == material).count() == 1,
                "No shadow duplicate left behind after detach");
    }

    private static void listShuffleAndHiddenSetShuffle() {
        DuelVisualState state = new DuelVisualState();
        state.initializeCounts(0, 2, 0);
        state.apply("Draw", draw(0, 101, 202, 101));
        long first = state.hand(0).get(0).id(), second = state.hand(0).get(1).id(), third = state.hand(0).get(2).id();
        state.drainEvents();
        state.apply("ShuffleHand", list(33, 0, 101, 101, 202) + "2000");
        check(state.hand(0).get(0).id() == first && state.hand(0).get(1).id() == third
                && state.hand(0).get(2).id() == second, "Shuffle matches duplicate codes as separate FIFO tokens");
        check(state.drainEvents().stream().filter(e -> e.kind().equals("shuffle")).count() == 2,
                "Concatenated ShuffleDeck tail is consumed");
        state.apply("Move", move(101, a(0, 2, 0, 10), a(0, 8, 0, 10), 0));
        state.apply("Move", move(101, a(0, 2, 0, 10), a(0, 8, 1, 10), 0));
        state.apply("ShuffleSetCard", "240802" + gps(a(0, 8, 0, 10)) + gps(a(0, 8, 1, 10)) + "0000000000000000");
        check(at(state, 0, 8, 0, 0).code() == 0 && at(state, 0, 8, 1, 0).code() == 0,
                "Opaque facedown shuffle forgets card identity");
        check(state.cards().stream().noneMatch(c -> c.address().location() == 0),
                "Opaque destination zeros never relocate cards out of duel");
    }

    private static void movementReasonAndEffects() {
        DuelVisualState state = new DuelVisualState();
        state.apply("Draw", draw(0, 10, 20));
        state.apply("Move", move(10, a(0, 2, 0, 10), a(0, 4, 2, 1), 0));
        state.apply("Move", move(20, a(0, 2, 0, 10), a(1, 4, 3, 1), 0));
        state.drainEvents();
        state.apply("Attack", "6E" + gps(a(0, 4, 2, 1)) + gps(a(1, 4, 3, 1)));
        DuelVisualState.Event declaration = state.drainEvents().get(0);
        check(declaration.kind().equals("attack_declare") && declaration.card().code() == 10
                        && declaration.target().code() == 20,
                "Attack declaration identifies exact attacker and target");
        state.apply("Battle", "6F" + gps(a(0, 4, 2, 1)) + le32(3000) + le32(2500) + "00"
                + gps(a(1, 4, 3, 1)) + le32(2000) + le32(1800) + "00");
        DuelVisualState.Event attack = state.drainEvents().get(0);
        check(attack.kind().equals("attack") && attack.card().code() == 10 && attack.target().code() == 20,
                "Battle message creates the actual collision");
        state.apply("Attack", "6E" + gps(a(0, 4, 2, 1)) + "00000000");
        check(state.drainEvents().get(0).kind().equals("attack_declare"), "Direct attack declaration is distinct");
        state.apply("Battle", "6F" + gps(a(0, 4, 2, 1)) + le32(3000) + le32(2500) + "00"
                + "00000000000000000000000000");
        DuelVisualState.Event direct = state.drainEvents().get(0);
        check(direct.kind().equals("attack") && direct.target() == null, "Direct attack has no card target");
        state.apply("Attack", "6E" + gps(a(0, 4, 2, 1)) + gps(a(1, 4, 3, 1)));
        state.drainEvents();
        state.apply("AttackDisabled", "70");
        DuelVisualState.Event cancelled = state.drainEvents().get(0);
        check(cancelled.kind().equals("attack_disabled") && cancelled.card().code() == 10,
                "Disabled attack cancels declaration without collision");
        state.apply("Chaining", "46" + le32(10) + gps(a(0, 4, 2, 1)) + "000402" + le32(1234) + "01");
        check(state.drainEvents().get(0).kind().equals("activate"), "Effect activation creates motion cue");
        state.apply("ChainNegated", "4B01");
        check(state.drainEvents().get(0).card().code() == 10, "Negation follows original chain card");
        state.apply("Move", move(20, a(1, 4, 3, 1), a(1, 32, 0, 8), 0x10000049));
        DuelVisualState.Event destroy = state.drainEvents().get(0);
        check((destroy.reason() & DuelVisualState.DESTROY) != 0 && destroy.to().location() == 32,
                "Destroy reason retained alongside facedown banish destination");
        check(!destroy.card().publicKnown(), "Facedown banish remains hidden");
        state.apply("PosChange", "35" + le32(10) + gps(a(0, 4, 2, 1)) + "04");
        check(at(state, 0, 4, 2, 0).address().position() == 4, "Position change updates current card");
        state.apply("Move", move(10, a(0, 4, 2, 4), a(0, 0, 0, 0), 0x400));
        check(state.cards().stream().noneMatch(c -> c.code() == 10), "Vanishing token leaves scene with location zero event");
    }

    private static void queryRefreshAndPublicRevocation() {
        DuelVisualState state = new DuelVisualState();
        state.apply("Draw", draw(0, 101, 202));
        long first = state.hand(0).get(0).id(), second = state.hand(0).get(1).id();
        state.drainEvents();
        state.apply("UpdateData", "060002" + query(101, a(0, 2, 0, 5)) + query(202, a(0, 2, 1, 10)));
        check(state.hand(0).get(0).id() == first && state.hand(0).get(0).publicKnown(),
                "Full query marks EFFECT_PUBLIC without replacing token");
        check(state.drainEvents().stream().allMatch(e -> e.kind().equals("update")),
                "Query refresh creates no flight animations");
        state.apply("UpdateData", "060002" + query(101, a(0, 2, 0, 10)) + query(202, a(0, 2, 1, 10)));
        check(!state.hand(0).get(0).publicKnown(), "Revoked public effect immediately conceals hand");
        check(state.drainEvents().size() == 1, "Unchanged query records produce no queue spam");
        state.apply("UpdateCard", "07000201" + query(222, a(0, 2, 1, 10)));
        check(state.hand(0).get(1).id() == second && state.hand(0).get(1).code() == 222,
                "UpdateCard refreshes exact GPS");
        state.drainEvents();
        state.apply("UpdateData", "060002" + query(101, a(0, 2, 0, 10)));
        check(state.hand(0).size() == 1, "Full query removes stale cards absent from updated zone");
        DuelVisualState.Event disappear = state.drainEvents().get(0);
        check(disappear.card().id() == second && disappear.to().location() == 0,
                "Stale scene card removed via explicit location-zero update");
        state.apply("UpdateData", "060004" + le32(4) + le32(4) + query(333, a(0, 4, 2, 1)) + le32(4));
        check(at(state, 0, 4, 2, 0).code() == 333, "Query respects empty fixed field slots");
        state.apply("UpdateData", "060004" + le32(4) + le32(4) + le32(4) + le32(4));
        check(state.count(0, 4) == 0, "Empty field query removes models");
        state.apply("UpdateData", "060002");
        check(state.hand(0).isEmpty(), "Empty hand query clears scene");
        check(state.malformedMessages() == 0, "Known classic query shapes parse completely");
    }

    /**
     * 查询里的 QUERY_STATUS(0x80000) 是"效果被无效"的来源（MDPro 的 Disabled 就是它的 bit0）。
     * 这里检查：带状态位的记录能解析、状态变化会作为 update 事件发出去、清掉后能恢复。
     */
    private static void effectDisabledStatusFlag() {
        DuelVisualState state = new DuelVisualState();
        state.initializeCounts(0, 40, 15);
        state.apply("Draw", draw(0, 89631139));
        state.apply("Move", move(89631139, a(0, 2, 0, 10), a(0, 4, 2, 1), 0x20000));
        check(at(state, 0, 4, 2, 0).code() == 89631139, "Monster lands in the monster zone");
        check(!at(state, 0, 4, 2, 0).disabled(), "Fresh card is not disabled");
        state.drainEvents();
        state.apply("UpdateData", "060004" + queryWithStatus(89631139, a(0, 4, 2, 1), 1));
        check(at(state, 0, 4, 2, 0).disabled(), "QUERY_STATUS bit0 marks the effect as disabled");
        List<DuelVisualState.Event> events = state.drainEvents();
        check(events.size() == 1 && events.get(0).card().disabled(),
                "Disabled change is delivered as one update event");
        state.apply("UpdateData", "060004" + queryWithStatus(89631139, a(0, 4, 2, 1), 0));
        check(!at(state, 0, 4, 2, 0).disabled(), "Clearing the status restores the card");
        check(state.drainEvents().size() == 1, "Restoring also delivers one event");
        check(state.malformedMessages() == 0, "Status query parses cleanly");
    }

    /** 带 QUERY_STATUS(0x80000) 的查询记录：长度 20 = 头 8 + code 4 + GPS 4 + status 4。 */
    private static String queryWithStatus(int code, DuelVisualState.Address a, int status) {
        return le32(20) + le32(0x80003) + le32(code) + gps(a) + le32(status);
    }

    private static void truncatedInput() {
        DuelVisualState state = new DuelVisualState();
        state.apply("Draw", "5A00030A000000");
        check(state.cards().isEmpty(), "Truncated Draw never partially mutates state");
        state.apply("Move", "320A00");
        state.apply("Draw", "5AZZ");
        state.apply("Move", "A");
        check(state.malformedMessages() == 4, "Malformed messages counted");
        state.apply("NewPhase", "290400" + draw(0, 888));
        check(state.hand(0).size() == 1, "Known nonvisual prefix followed by Draw is consumed");
    }

    private static void replay(Path file) throws Exception {
        DuelVisualState state = new DuelVisualState();
        state.initializeCounts(0, 40, 15); state.initializeCounts(1, 40, 15);
        Map<Long, DuelVisualState.Card> scene = new HashMap<>();
        Map<String, Integer> eventCounts = new HashMap<>();
        int messages = 0;
        for (String line : Files.readAllLines(file)) {
            if (line.contains("\"type\":\"ai\"") && line.contains("\"state\":\"deck=")) {
                Matcher ai = Pattern.compile("\\\"seat\\\":(\\d).*?deck=(\\d+) hand=(\\d+).*?ids=\\[([^\\]]*)\\]").matcher(line);
                if (ai.find()) {
                    int side = Integer.parseInt(ai.group(1));
                    List<DuelVisualState.Card> hand = state.hand(side);
                    check(hand.size() == Integer.parseInt(ai.group(3)), "Actual AI and visual hand counts agree");
                    check(state.count(side, 1) == Integer.parseInt(ai.group(2)), "Actual AI and visual deck counts agree");
                    if (!ai.group(4).isEmpty()) {
                        String[] ids = ai.group(4).split(",");
                        for (int i = 0; i < ids.length; i++)
                            check(hand.get(i).code() == Integer.parseInt(ids[i].split(":")[0]), "Actual AI and visual hand codes agree");
                    }
                }
            }
            if (!line.contains("\"type\":\"message\"")) continue;
            Matcher hex = HEX.matcher(line), name = NAME.matcher(line);
            if (!hex.find() || !name.find()) continue;
            state.apply(name.group(1), hex.group(1)); messages++;
            for (DuelVisualState.Event event : state.drainEvents()) {
                eventCounts.merge(event.kind(), 1, Integer::sum);
                if (event.card() != null) {
                    if (event.card().address().location() == 0) scene.remove(event.card().id());
                    else scene.put(event.card().id(), event.card());
                }
            }
            assertUniqueAddresses(state);
            for (int side = 0; side < 2; side++) {
                List<DuelVisualState.Card> hand = state.hand(side);
                for (int i = 0; i < hand.size(); i++)
                    check(hand.get(i).address().sequence() == i, "Log replay hand compression at message " + messages);
            }
        }
        check(messages > 20, "Actual core log parsed: " + file.getFileName());
        check(state.malformedMessages() == 0, "No malformed known messages in actual core log");
        check(eventCounts.getOrDefault("draw", 0) > 10, "Actual draw events consumed");
        check(eventCounts.getOrDefault("summon", 0) + eventCounts.getOrDefault("special_summon", 0) > 0,
                "Actual summon motion observed");
        // Scene snapshots evolve only through emitted steps and match all observed non-deck cards.
        for (DuelVisualState.Card card : state.cards()) if (card.address().location() != 1 && card.address().location() != 64) {
            check(scene.containsKey(card.id()), "All live observed cards represented in event scene");
            check(scene.get(card.id()).equals(card), "Final event scene matches authoritative snapshot");
        }
        System.out.println("Replay " + file.getFileName() + ": " + messages + " messages, " + eventCounts);
    }

    private static void assertUniqueAddresses(DuelVisualState state) {
        Set<String> slots = new HashSet<>(); Set<Long> tokens = new HashSet<>();
        for (DuelVisualState.Card card : state.cards()) {
            check(tokens.add(card.id()), "Tokens unique");
            DuelVisualState.Address a = card.address();
            String slot = a.controller() + ":" + a.location() + ":" + a.sequence()
                    + (a.overlay() ? ":" + a.position() : "");
            check(slots.add(slot), "Addresses unique at " + slot);
        }
    }
    private static DuelVisualState.Card at(DuelVisualState state, int side, int location, int sequence, int materialIndex) {
        return state.cards().stream().filter(c -> c.address().controller() == side && c.address().location() == location
                && c.address().sequence() == sequence && ((location & 128) == 0 || c.address().position() == materialIndex))
                .findFirst().orElseThrow();
    }
    private static void check(boolean condition, String message) {
        assertions++;
        if (!condition) throw new AssertionError(message);
    }
    private static DuelVisualState.Address a(int side, int location, int sequence, int position) {
        return new DuelVisualState.Address(side, location, sequence, position);
    }
    private static String gps(DuelVisualState.Address a) {
        return String.format("%02X%02X%02X%02X", a.controller(), a.location(), a.sequence(), a.position());
    }
    private static String le32(int value) {
        return HexFormat.of().formatHex(new byte[]{(byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24)});
    }
    private static String query(int code, DuelVisualState.Address a) {
        return le32(16) + le32(3) + le32(code) + gps(a);
    }
    private static String move(int code, DuelVisualState.Address from, DuelVisualState.Address to, long reason) {
        return "32" + le32(code) + gps(from) + gps(to) + le32((int) reason);
    }
    private static String summon(int function, int code, DuelVisualState.Address address) {
        return String.format("%02X", function) + le32(code) + gps(address);
    }
    private static String draw(int side, int... codes) { return list(90, side, codes); }
    private static String list(int function, int side, int... codes) {
        StringBuilder result = new StringBuilder(String.format("%02X%02X%02X", function, side, codes.length));
        for (int code : codes) result.append(le32(code));
        return result.toString();
    }
    private static String confirm(boolean current, int audience, int code, int side, int location, int sequence) {
        return "1F" + String.format("%02X", audience) + (current ? "00" : "") + "01" + le32(code)
                + String.format("%02X%02X%02X", side, location, sequence);
    }
}
