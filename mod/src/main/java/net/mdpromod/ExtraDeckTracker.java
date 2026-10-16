package net.mdpromod;

import java.util.ArrayList;
import java.util.List;

/**
 * "当前额外卡组里还有什么"的记账（纯逻辑，可离线测试）。
 *
 * <p>为什么需要它：核心的额外卡组是**有序表，卡片离开后序号会整体前移**——
 * 实测日志：额外卡组 `[8491961,8491961,8491961,48608796,48608796,72971064,…]`，
 * 召掉一张 8491961 之后，核心给的候选是 `0=8491961, 1=8491961, 2=48608796,
 * 3=48608796, 4=72971064`，也就是"卡组清单去掉那一张、后面的往前挪一格"。
 * 所以界面既不能一直用卡组原始清单（张数不会少、序号还会错位），
 * 也不能随便过滤，必须按同一套规则维护一份实时清单：**序号 = 这份清单里的下标**。
 *
 * <p>规则：
 * <ul>
 *   <li>卡片离开额外卡组（融合 / 同步 / 超量 / 连接召唤等）→ 去掉**最前面**那个相同的卡号
 *       （同卡号的副本在规则上不可区分，核心也是按顺序前移）；</li>
 *   <li>卡片回到额外卡组（表侧灵摆怪被破坏等）→ 追加到末尾（核心就是 push_back）；</li>
 *   <li>只记自己的（controller 0）那一侧。</li>
 * </ul>
 */
final class ExtraDeckTracker {
    /** 引擎给的额外卡组序号上限（0x40 = EXTRA）。 */
    static final int EXTRA = 0x40;

    private final List<Integer> codes = new ArrayList<>();
    private boolean tracking;

    /** 开局（或重新放卡组）时重置成整副额外卡组。 */
    void reset(List<Integer> extraDeck) {
        codes.clear();
        if (extraDeck != null) codes.addAll(extraDeck);
        tracking = true;
    }

    /** 没开过局 / 没记过账时返回 false，调用方就继续用卡组原始清单。 */
    boolean tracking() {
        return tracking;
    }

    void stop() {
        tracking = false;
    }

    /**
     * 喂一条移动事件：谁的卡、从哪到哪、卡号多少。
     *
     * @return 这份清单是否因此变了
     */
    boolean move(int fromController, int fromLocation, int toController, int toLocation, int code) {
        if (!tracking || code <= 0) return false;
        boolean left = fromController == 0 && fromLocation == EXTRA && toLocation != EXTRA;
        if (left) return codes.remove(Integer.valueOf(code));
        boolean back = toController == 0 && toLocation == EXTRA && fromLocation != EXTRA;
        if (back) {
            codes.add(code);
            return true;
        }
        return false;
    }

    /** 当前额外卡组（顺序就是引擎的序号顺序）。 */
    List<Integer> codes() {
        return List.copyOf(codes);
    }
}
