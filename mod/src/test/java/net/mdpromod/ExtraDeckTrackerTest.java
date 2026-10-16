package net.mdpromod;

import java.util.List;

/**
 * 额外卡组记账的回归测试。用例照抄实机日志：核心在卡片离开后会把序号整体前移，
 * 界面必须跟着同一套规则走，否则会出现"召唤过的怪兽还在列表里、再点它却召唤出另一张"。
 */
public final class ExtraDeckTrackerTest {
    private static int assertions;
    /** 实机那副额外卡组的前几张。 */
    private static final List<Integer> DECK = List.of(
            8491961, 8491961, 8491961, 48608796, 48608796, 72971064, 26847978);

    public static void main(String[] args) {
        mirrorsEngineShift();
        ignoringOtherZonesAndSeats();
        returnsGoToTheEnd();
        duplicatesOnlyDropOne();
        untrackedUntilReset();
        System.out.println("ExtraDeckTrackerTest passed " + assertions + " assertions");
    }

    /** 实机日志：召掉一张 8491961 后，核心给的候选是 0=8491961,1=8491961,2=48608796,... */
    private static void mirrorsEngineShift() {
        ExtraDeckTracker tracker = new ExtraDeckTracker();
        tracker.reset(DECK);
        check(tracker.tracking(), "重置之后开始记账");
        check(tracker.move(0, ExtraDeckTracker.EXTRA, 0, 4, 8491961), "召唤离开额外卡组");
        List<Integer> live = tracker.codes();
        check(live.size() == DECK.size() - 1, "张数少了一张：" + live.size());
        check(live.get(0) == 8491961 && live.get(1) == 8491961, "前面两张还是 8491961");
        check(live.get(2) == 48608796 && live.get(3) == 48608796 && live.get(4) == 72971064,
                "序号整体前移（和核心一致）：" + live);
    }

    private static void ignoringOtherZonesAndSeats() {
        ExtraDeckTracker tracker = new ExtraDeckTracker();
        tracker.reset(DECK);
        check(!tracker.move(0, 4, 0, 16, 8491961), "场上进墓地不算额外卡组的事");
        check(!tracker.move(0, 2, 0, 4, 8491961), "手牌上场不算");
        check(!tracker.move(1, ExtraDeckTracker.EXTRA, 1, 4, 48608796), "对手的额外卡组不管");
        check(tracker.codes().size() == DECK.size(), "没被动过：" + tracker.codes().size());
    }

    private static void returnsGoToTheEnd() {
        ExtraDeckTracker tracker = new ExtraDeckTracker();
        tracker.reset(List.of(111, 222));
        check(tracker.move(0, 4, 0, ExtraDeckTracker.EXTRA, 999), "表侧灵摆怪被破坏回到额外卡组");
        check(tracker.codes().equals(List.of(111, 222, 999)), "追加到末尾（核心就是 push_back）：" + tracker.codes());
    }

    private static void duplicatesOnlyDropOne() {
        ExtraDeckTracker tracker = new ExtraDeckTracker();
        tracker.reset(List.of(7, 7, 7));
        tracker.move(0, ExtraDeckTracker.EXTRA, 0, 4, 7);
        check(tracker.codes().equals(List.of(7, 7)), "三张同卡号只掉一张");
        tracker.move(0, ExtraDeckTracker.EXTRA, 0, 4, 7);
        tracker.move(0, ExtraDeckTracker.EXTRA, 0, 4, 7);
        check(tracker.codes().isEmpty(), "三张全用完之后是空的");
        check(!tracker.move(0, ExtraDeckTracker.EXTRA, 0, 4, 7), "再来一张同卡号也不会变负数");
    }

    private static void untrackedUntilReset() {
        ExtraDeckTracker tracker = new ExtraDeckTracker();
        check(!tracker.tracking(), "没开过局就不记账");
        check(!tracker.move(0, ExtraDeckTracker.EXTRA, 0, 4, 123), "没记账时移动被忽略");
        tracker.reset(DECK);
        tracker.stop();
        check(!tracker.tracking() && !tracker.move(0, ExtraDeckTracker.EXTRA, 0, 4, 8491961),
                "停掉之后也不记账");
    }

    private static void check(boolean condition, String message) {
        assertions++;
        if (!condition) throw new AssertionError(message);
    }
}
