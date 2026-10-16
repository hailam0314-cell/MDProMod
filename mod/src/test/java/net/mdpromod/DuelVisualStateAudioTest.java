package net.mdpromod;

import java.util.HexFormat;
import java.util.List;

/** Standalone Java 17 audio-event regressions; no Forge or native core required. */
public final class DuelVisualStateAudioTest {
    private static int assertions;
    private static final String ATTACKER = "00040201";
    private static final String TARGET = "01040301";

    public static void main(String[] args) {
        lifePointDeltasAndClamping();
        orderedSemanticEvents();
        matchKillAndWinBoundary();
        chainResolutionAndNegation();
        coinDiceAndCounters();
        battleBeginsOnlyAtBattleMessage();
        guardAndFinalAttackFlags();
        queryStatChanges();
        cancellationAndReset();
        truncatedMessages();
        System.out.println("DuelVisualStateAudio passed " + assertions + " assertions");
    }

    private static void lifePointDeltasAndClamping() {
        DuelVisualState state = new DuelVisualState();
        state.apply("Start", start(12000, 3000));
        check(state.drainEvents().isEmpty(), "Initial LP snapshot is silent");
        state.apply("Damage", lp(91, 1, 500));
        lpEvent(only(state, "audio_lp"), 1, 3000, 2500, -500);
        state.apply("PayLpCost", lp(100, 1, 9000));
        lpEvent(only(state, "audio_lp"), 1, 2500, 0, -2500);
        state.apply("Recover", lp(92, 1, 700));
        lpEvent(only(state, "audio_lp"), 1, 0, 700, 700);
        state.apply("LpUpdate", lp(94, 1, 100));
        lpEvent(only(state, "audio_lp"), 1, 700, 100, -600);
        state.apply("LpUpdate", lp(94, 1, 100));
        lpEvent(only(state, "audio_lp"), 1, 100, 100, 0);
        state.apply("Recover", lp(92, 1, -1));
        lpEvent(only(state, "audio_lp"), 1, 100, Integer.MAX_VALUE,
                (long) Integer.MAX_VALUE - 100);
        state.apply("Damage", lp(91, 1, -1));
        lpEvent(only(state, "audio_lp"), 1, Integer.MAX_VALUE, 0, -Integer.MAX_VALUE);
        state.apply("Damage", lp(91, 0, 1000));
        lpEvent(only(state, "audio_lp"), 0, 12000, 11000, -1000);
        check(state.malformedMessages() == 0, "Unsigned LP values do not overflow parsing");
        state.apply("Damage", lp(91, 2, 10));
        check(state.drainEvents().isEmpty() && state.malformedMessages() == 1,
                "Invalid LP side is rejected without a cue");
    }

    private static void orderedSemanticEvents() {
        DuelVisualState state = new DuelVisualState();
        state.apply("Batch", "2801" + "290400" + lp(91, 0, 500) + "050101");
        List<DuelVisualState.Event> events = state.drainEvents();
        check(events.stream().map(DuelVisualState.Event::kind).toList()
                        .equals(List.of("audio_turn", "audio_phase", "audio_lp", "audio_win")),
                "Semantic audio events preserve core message order");
        check(events.get(0).from().controller() == 1 && events.get(0).reason() == 1,
                "First turn retains side and turn number");
        check(events.get(1).from().controller() == 1 && events.get(1).reason() == 4,
                "Phase cue uses current turn side and 16-bit phase");
        check(events.get(3).from().controller() == 1 && events.get(3).reason() == 1,
                "Win cue retains winning side and result reason");
        state.apply("NewTurn", "2800");
        check(only(state, "audio_turn").reason() == 2, "Subsequent turns increment once");
        check(state.drainEvents().isEmpty(), "Draining snapshots cannot replay audio events");
    }

    private static void chainResolutionAndNegation() {
        DuelVisualState state = new DuelVisualState();
        state.apply("Chaining", chaining(101, ATTACKER, 1)
                + chaining(202, TARGET, 2) + "4702");
        List<DuelVisualState.Event> events = state.drainEvents();
        check(events.size() == 3 && events.get(0).kind().equals("activate")
                        && events.get(1).kind().equals("activate")
                        && events.get(2).kind().equals("audio_chain_stack"),
                "Activation view and chain stack remain separate ordered cues");
        check(events.get(2).reason() == 2 && events.get(2).fromCount() == 2,
                "Stack cue retains chain index and total stack count");
        state.apply("ChainSolving", "4802");
        DuelVisualState.Event resolve = only(state, "audio_chain_resolve");
        check(resolve.target() != null && resolve.target().code() == 202
                        && resolve.reason() == 2 && resolve.fromCount() == 2,
                "Resolution retains original chain card and total count");
        state.apply("ChainNegated", "4B01");
        check(only(state, "negate").card().code() == 101,
                "Negation keeps the card required by its visual effect");
        state.apply("ChainSolving", "4801");
        resolve = only(state, "audio_chain_resolve");
        check(resolve.target() == null && resolve.reason() == 1 && resolve.fromCount() == 2,
                "Negated resolve omits card-specific effects but keeps chain identity");
        state.apply("ChainEnd", "4A");
        check(only(state, "audio_chain_end").target() == null,
                "ChainEnd queues session cleanup without a stale card effect");
        state.apply("ChainSolving", "4802");
        resolve = only(state, "audio_chain_resolve");
        check(resolve.target() == null && resolve.fromCount() == 0,
                "ChainEnd clears cards before later chain messages are parsed");
        state.apply("Chaining", chaining(303, ATTACKER, 1) + "4801");
        events = state.drainEvents();
        check(events.size() == 2 && events.get(1).target() != null
                        && events.get(1).target().code() == 303
                        && events.get(1).fromCount() == 1,
                "New chain clears both previous cards and previous negations");
        check(state.malformedMessages() == 0, "Chain packet boundaries parse cleanly");
    }

    private static void coinDiceAndCounters() {
        DuelVisualState state = new DuelVisualState();
        state.apply("TossCoin", "820103000100");
        List<DuelVisualState.Event> coins = state.drainEvents();
        check(coins.size() == 3 && coins.stream().allMatch(e -> e.kind().equals("audio_coin")),
                "Every coin result survives a multi-coin packet");
        for (int i = 0; i < coins.size(); i++) {
            DuelVisualState.Event coin = coins.get(i);
            check(coin.from().controller() == 1 && coin.fromCount() == i && coin.toCount() == 3,
                    "Coin cue retains side, ordinal and total count");
            check(coin.reason() == (i == 1 ? 1 : 0), "Coin face is not collapsed to first result");
        }
        state.apply("TossDice", "830003060102");
        DuelVisualState.Event dice = only(state, "audio_dice");
        check(dice.reason() == 6 && dice.toCount() == 3,
                "Multi-dice roll is one roll/decide cue with its result context");
        state.apply("TossCoin", "820000" + "830100");
        check(state.drainEvents().isEmpty(), "Zero-count tosses create no phantom sound");
        state.apply("AddCounter", "65" + "0100" + "000402" + "0200");
        DuelVisualState.Event counter = only(state, "audio_counter");
        check(counter.target() != null && counter.target().address().controller() == 0
                        && counter.target().address().location() == DuelVisualState.MONSTER
                        && counter.target().address().sequence() == 2,
                "Counter cue identifies its exact card slot");
        state.apply("RemoveCounter", "66" + "0100" + "000402" + "0100");
        check(only(state, "audio_counter").target().id() == counter.target().id(),
                "Adding and removing counters share the same stable card identity");
        check(state.malformedMessages() == 0, "Toss and counter messages remain aligned");
    }

    private static void matchKillAndWinBoundary() {
        DuelVisualState state = new DuelVisualState();
        state.apply("Batch", "AA" + le32(89631139) + "050001");
        DuelVisualState.Event result = only(state, "audio_win");
        check(result.from().controller() == 0 && result.reason() == 1
                        && result.toCount() == 89631139,
                "Five-byte MatchKill does not swallow following Win and retains its card");
        state.reset();
        state.apply("Win", "050101");
        check(only(state, "audio_win").toCount() == 0,
                "Reset prevents a previous match-kill card leaking into the next result");
        state.apply("Batch", "4701" + "4A" + "4801" + "050200");
        List<DuelVisualState.Event> events = state.drainEvents();
        check(events.stream().map(DuelVisualState.Event::kind).toList().equals(
                        List.of("audio_chain_stack", "audio_chain_end", "audio_chain_resolve", "audio_win")),
                "ChainEnd cleanup remains ordered between chain and result events");
        check(events.get(2).target() == null && events.get(2).fromCount() == 0,
                "Post-ChainEnd resolve has no previous chain state");
        check(state.malformedMessages() == 0, "MatchKill and Win packet boundaries parse cleanly");
    }

    private static void battleBeginsOnlyAtBattleMessage() {
        DuelVisualState state = combatState(TARGET, 8000);
        state.apply("Attack", "6E" + ATTACKER + TARGET);
        DuelVisualState.Event declaration = only(state, "attack_declare");
        check(declaration.card().code() == 101 && declaration.target().code() == 202,
                "ATTACK only declares the known attacker and target");
        state.apply("Battle", battle(TARGET, 3000, 2500, 2000, 1800));
        DuelVisualState.Event attack = only(state, "attack");
        check(attack.fromCount() == 3000 && attack.toCount() == 2000 && attack.reason() == 0,
                "BATTLE emits one launch/collision event with actual ATK values");
        check(state.drainEvents().isEmpty(), "Subsequent queue reads do not replay a collision");
        state.apply("Attack", "6E" + ATTACKER + "00000000");
        check(only(state, "attack_declare").target() == null,
                "Direct declaration has no invented target card");
        state.apply("Battle", battle("00000000", 1800, 1000, 0, 0));
        attack = only(state, "attack");
        check(attack.target() == null && attack.toCount() == 0 && attack.reason() == 0,
                "Direct impact retains zero target power without false final flag");
    }

    private static void guardAndFinalAttackFlags() {
        String defenseTarget = "01040304";
        DuelVisualState state = combatState(defenseTarget, 1500);
        state.apply("Battle", battle(defenseTarget, 2000, 100, 500, 2500));
        DuelVisualState.Event attack = only(state, "attack");
        check(attack.reason() == 1 && attack.fromCount() == 2000 && attack.toCount() == 2500,
                "Guard bit0 uses defending DEF, not its lower ATK");
        state = combatState(TARGET, 1500);
        state.apply("Battle", battle(TARGET, 2000, 100, 2000, 0));
        check(only(state, "attack").reason() == 1, "Equal attack power also selects guard");
        state.apply("Battle", battle(TARGET, 3000, 100, 1000, 9999));
        attack = only(state, "attack");
        check(attack.reason() == 2 && attack.toCount() == 1000,
                "Final bit1 uses lethal ATK difference against attack position");
        state = combatState(defenseTarget, 1500);
        state.apply("Battle", battle(defenseTarget, 5000, 100, 1, 100));
        check(only(state, "attack").reason() == 0,
                "Ordinary defense-position battle does not invent piercing lethal damage");
        state.apply("Battle", battle("00000000", 1500, 0, 0, 0));
        attack = only(state, "attack");
        check(attack.reason() == 2 && attack.target() == null,
                "Direct damage equal to remaining LP sets final bit1");
    }

    private static void cancellationAndReset() {
        DuelVisualState state = combatState(TARGET, 1000);
        state.apply("Attack", "6E" + ATTACKER + TARGET);
        state.drainEvents();
        state.apply("AttackDisabled", "70");
        DuelVisualState.Event cancelled = only(state, "attack_disabled");
        check(cancelled.card().code() == 101 && cancelled.target().code() == 202,
                "Cancellation preserves pending declaration across drains");
        state.apply("Chaining", chaining(101, ATTACKER, 1) + "2801");
        state.reset();
        check(state.cards().isEmpty() && state.drainEvents().isEmpty(),
                "Reset discards all old presentation state and queued sounds");
        state.apply("Damage", lp(91, 1, 500));
        lpEvent(only(state, "audio_lp"), 1, 8000, 7500, -500);
        state.apply("NewTurn", "2800");
        check(only(state, "audio_turn").reason() == 1, "Reset restarts turn numbering");
        state.apply("ChainSolving", "4801");
        DuelVisualState.Event resolve = only(state, "audio_chain_resolve");
        check(resolve.target() == null && resolve.fromCount() == 0,
                "Reset prevents stale chain-card effects");
    }

    private static void queryStatChanges() {
        DuelVisualState state = new DuelVisualState();
        state.apply("UpdateCard", queryStats(ATTACKER, 2000, 1500, 0x10, 1));
        check(state.drainEvents().stream().noneMatch(e -> e.kind().startsWith("audio_")),
                "Initial public query establishes stats without replaying buffs");
        state.apply("UpdateCard", queryStats(ATTACKER, 2000, 1500, 0x10, 1));
        check(state.drainEvents().isEmpty(), "Repeated unchanged stat snapshot is silent");
        state.apply("UpdateCard", queryStats(ATTACKER, 2500, 1500, 0x10, 1));
        DuelVisualState.Event change = only(state, "audio_stat");
        check(change.reason() == 500 && change.fromCount() == 2000 && change.toCount() == 2500
                        && change.target() != null && change.target().code() == 101,
                "ATK increase carries signed delta, old/new value and subject");
        state.apply("UpdateCard", queryStats(ATTACKER, 1200, 1500, 0x10, 1));
        change = only(state, "audio_stat");
        check(change.reason() == -1300 && change.fromCount() == 2500 && change.toCount() == 1200,
                "ATK decrease remains negative instead of unsigned overflow");
        state.apply("UpdateCard", queryStats(ATTACKER, 1200, 3000, 0x10, 1));
        check(state.drainEvents().isEmpty(), "DEF-only numeric changes have no ATK buff cue");
        state.apply("UpdateCard", queryStats(ATTACKER, 1200, 3000, 0x20, 2));
        List<DuelVisualState.Event> characteristics = state.drainEvents();
        check(characteristics.size() == 2
                        && characteristics.stream().allMatch(e -> e.kind().equals("audio_characteristic")),
                "Attribute and race changes each retain their own characteristic cue");
        check(characteristics.get(0).reason() == 0x40
                        && characteristics.get(0).fromCount() == 0x10
                        && characteristics.get(0).toCount() == 0x20,
                "Attribute cue identifies its query field and old/new values");
        check(characteristics.get(1).reason() == 0x80
                        && characteristics.get(1).fromCount() == 1
                        && characteristics.get(1).toCount() == 2,
                "Race cue identifies its query field and old/new values");
        state.apply("UpdateCard", queryStats(ATTACKER, 1200, 3000, 0x20, 2));
        check(state.drainEvents().isEmpty(), "Repeated characteristic snapshot cannot spam audio");

        String facedown = "00040208";
        state.apply("PosChange", "35" + le32(101) + ATTACKER + "08");
        state.drainEvents();
        state.apply("UpdateCard", queryStats(facedown, 900, 500, 0x40, 4));
        check(state.drainEvents().isEmpty(), "Hidden face-down stat changes are silent");
        state.apply("PosChange", "35" + le32(101) + facedown + "01");
        state.drainEvents();
        state.apply("UpdateCard", queryStats(ATTACKER, 1800, 1500, 0x10, 1));
        check(state.drainEvents().isEmpty(), "First query after face-up reveal is a fresh baseline");
        state.apply("UpdateCard", queryStats(ATTACKER, 1900, 1500, 0x10, 1));
        check(only(state, "audio_stat").reason() == 100,
                "After reveal baseline, a later public change still sounds");
        state.apply("PosChange", "35" + le32(101) + ATTACKER + "08");
        state.drainEvents();
        state.apply("Move", "32" + le32(101) + facedown + "0002000A" + le32(0));
        state.drainEvents();
        state.apply("UpdateCard", queryStats("0002000A", 500, 500, 0x80, 8));
        check(state.drainEvents().isEmpty(), "Private hand stats never create shared buff sounds");
        state.apply("Move", "32" + le32(101) + "0002000A" + ATTACKER + le32(0));
        state.drainEvents();
        state.apply("UpdateCard", queryStats(ATTACKER, 1800, 500, 0x10, 1));
        check(state.drainEvents().stream().noneMatch(e -> e.kind().startsWith("audio_")),
                "First public query after re-entry establishes a fresh baseline");
        check(state.malformedMessages() == 0, "Stat query layouts parse completely");
    }

    private static void truncatedMessages() {
        String[] packets = {
                start(8000, 8000), "050001", "AA" + le32(89631139), "2801", "290400", lp(91, 0, 300),
                chaining(101, ATTACKER, 1), "4701", "4801", "4B01",
                "6501000004020100", "820103000100", "830003010203",
                "6E" + ATTACKER + TARGET, battle(TARGET, 3000, 2500, 2000, 1800),
                queryStats(ATTACKER, 2000, 1500, 0x10, 1)
        };
        for (String packet : packets) {
            for (int bytes = 1; bytes < packet.length() / 2; bytes++) {
                DuelVisualState state = new DuelVisualState();
                state.apply("Truncated", packet.substring(0, bytes * 2));
                check(state.drainEvents().isEmpty(), "Truncated packet creates no partial audio cue");
                check(state.cards().isEmpty(), "Truncated packet creates no phantom card");
                boolean emptyQuery = packet.startsWith("07") && bytes == 4;
                check(state.malformedMessages() == (emptyQuery ? 0 : 1),
                        "Truncated packet is counted once; an empty query header is valid");
            }
        }
        DuelVisualState state = new DuelVisualState();
        state.apply("Odd", "5");
        state.apply("InvalidHex", "ZZ");
        check(state.malformedMessages() == 2 && state.drainEvents().isEmpty(),
                "Malformed hex is rejected safely");
    }

    private static DuelVisualState combatState(String target, int rivalLp) {
        DuelVisualState state = new DuelVisualState();
        state.apply("Start", start(8000, rivalLp));
        state.apply("Move", "32" + le32(101) + "00000000" + ATTACKER + le32(0));
        state.apply("Move", "32" + le32(202) + "01000000" + target + le32(0));
        state.drainEvents();
        return state;
    }

    private static void lpEvent(DuelVisualState.Event event, int side, int before, int after, long delta) {
        check(event.card() == null && event.from().controller() == side,
                "LP cue has side context without a fake card");
        check(event.fromCount() == before && event.toCount() == after && event.reason() == delta,
                "LP cue preserves clamped before/after and signed delta");
    }

    private static DuelVisualState.Event only(DuelVisualState state, String kind) {
        List<DuelVisualState.Event> events = state.drainEvents();
        check(events.size() == 1 && events.get(0).kind().equals(kind),
                "Expected exactly one " + kind + ", got " + events);
        return events.get(0);
    }

    private static void check(boolean condition, String message) {
        assertions++;
        if (!condition) throw new AssertionError(message);
    }

    private static String start(int firstLp, int secondLp) {
        return "0400" + le32(firstLp) + le32(secondLp) + "0000000000000000";
    }

    private static String lp(int function, int side, int amount) {
        return String.format("%02X%02X", function, side) + le32(amount);
    }

    private static String chaining(int code, String address, int index) {
        return "46" + le32(code) + address + "000402" + le32(1234)
                + String.format("%02X", index);
    }

    private static String battle(String target, int attack, int defense, int targetAttack, int targetDefense) {
        return "6F" + ATTACKER + le32(attack) + le32(defense) + "00"
                + target + le32(targetAttack) + le32(targetDefense) + "00";
    }

    private static String queryStats(String address, int attack, int defense, int attribute, int race) {
        return "07" + address.substring(0, 6) + le32(32) + le32(0x3C3)
                + le32(101) + address + le32(attribute) + le32(race) + le32(attack) + le32(defense);
    }

    private static String le32(int value) {
        return HexFormat.of().formatHex(new byte[]{(byte) value, (byte) (value >> 8),
                (byte) (value >> 16), (byte) (value >> 24)});
    }
}
