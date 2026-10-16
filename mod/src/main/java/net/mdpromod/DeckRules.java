package net.mdpromod;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

/**
 * 卡组合法性校验：张数（主 40..60 / 额外 15 / 副 15）＋ 当前生效的禁卡表。
 *
 * <p>禁卡表来自运行时根目录的 {@code lflist.conf} + {@code banlist.conf}
 * （见 {@link Banlists} 和 {@link BanlistFile}）：配置里打开哪几张就按哪几张算，
 * 同一张卡取最严的那条；一张都没打开 = 只校验张数。
 * 卡号相同的不同版本（{@code alias}）算同一张卡，额度一起数——和 MD 一样。
 */
public final class DeckRules {
    public static final int MAIN_MIN = 40;
    public static final int MAIN_MAX = 60;
    public static final int EXTRA_MAX = 15;
    public static final int SIDE_MAX = 15;

    private DeckRules() {
    }

    /** 生效中的禁卡表。 */
    static Banlists.Active banlists() {
        return BanlistFile.active();
    }

    /** 「禁卡表：2026.10」／「禁卡表：未启用」，给卡盒标题用。 */
    public static String banlistStatus() {
        return "禁卡表：" + banlists().summary();
    }

    /** 这张卡实际能放几张（0..3）。 */
    public static int limitOf(int code) {
        return banlists().limitOf(key(code));
    }

    /** 「限制 1（2026.10 限制）」「禁止（2026.10 禁止）」「限制 3」这样的一句话。 */
    public static String limitText(int code) {
        int limit = limitOf(code);
        String reason = banlists().reason(key(code));
        String head = limit == 0 ? "禁止" : limit >= 3 ? "限制 3" : "限制 " + limit;
        return reason.isEmpty() ? head : head + "（" + reason + "）";
    }

    /** 「（2026.10 限制）」这半句；没被限就返回空串。 */
    public static String limitSuffix(int code) {
        String reason = banlists().reason(key(code));
        return reason.isEmpty() ? "" : "（" + reason + "）";
    }

    public static String displayName(int code) {
        return CardRegistry.get(code).map(CardInfo::name).orElse("#" + code);
    }

    /**
     * 卡组里这张卡占了几张——按 alias 分组数：同一张卡的不同版本共用额度，
     * 所以拿别的版本凑张数也算超。
     */
    public static int copies(DeckData deck, int code) {
        int wanted = key(code);
        int total = 0;
        for (int value : deck.mainDeck()) if (key(value) == wanted) total++;
        for (int value : deck.extraDeck()) if (key(value) == wanted) total++;
        for (int value : deck.sideDeck()) if (key(value) == wanted) total++;
        return total;
    }

    /** 卡组里和这张卡同组（不同卡号但共用额度）的卡号；没有就返回空表。 */
    public static List<Integer> groupMates(DeckData deck, int code) {
        int wanted = key(code);
        Set<Integer> mates = new LinkedHashSet<>();
        for (int value : cards(deck)) {
            if (value != code && key(value) == wanted) mates.add(value);
        }
        return new ArrayList<>(mates);
    }

    /** 「，和「青眼白龙」算同一张卡」这样的解释；没有同组卡就返回空串。 */
    public static String groupMateText(DeckData deck, int code) {
        List<Integer> mates = groupMates(deck, code);
        if (mates.isEmpty()) return "";
        StringBuilder text = new StringBuilder("，和");
        for (int i = 0; i < mates.size() && i < 2; i++) {
            if (i > 0) text.append("、");
            text.append("「").append(displayName(mates.get(i))).append("」");
        }
        if (mates.size() > 2) text.append(" 等 ").append(mates.size()).append(" 种");
        return text.append("算同一张卡").toString();
    }

    public static List<String> problems(DeckData deck) {
        Set<String> issues = new LinkedHashSet<>();
        int main = deck.mainDeck().size();
        int extra = deck.extraDeck().size();
        int side = deck.sideDeck().size();
        if (main < MAIN_MIN) issues.add("主卡组还差 " + (MAIN_MIN - main) + " 张（当前 " + main + "/60）");
        if (main > MAIN_MAX) issues.add("主卡组超过 60 张（当前 " + main + " 张）");
        if (extra > EXTRA_MAX) issues.add("额外卡组超过 15 张（当前 " + extra + " 张）");
        if (side > SIDE_MAX) issues.add("副卡组超过 15 张（当前 " + side + " 张）");

        Map<Integer, int[]> groups = groupCounts(deck);
        for (Map.Entry<Integer, int[]> entry : groups.entrySet()) {
            int group = entry.getKey(), total = entry.getValue()[0], sample = entry.getValue()[1];
            int limit = banlists().limitOf(group);
            if (total <= limit) continue;
            String name = displayName(sample);
            if (limit == 0) {
                issues.add(name + " 是禁止卡" + limitSuffix(sample) + "，当前 " + total + " 张");
            } else {
                issues.add(name + " 最多 " + limit + " 张" + limitSuffix(sample) + "，当前 " + total + " 张");
            }
        }

        for (int code : deck.mainDeck()) {
            if (CardRegistry.get(code).map(CardInfo::isExtraDeck).orElse(false)) {
                issues.add(displayName(code) + " 是额外卡组专用，不能放进主卡组");
            }
        }
        for (int code : deck.extraDeck()) {
            CardInfo info = CardRegistry.get(code).orElse(null);
            if (info != null && !info.isExtraDeck()) {
                issues.add(displayName(code) + " 不能放进额外卡组");
            }
        }
        return new ArrayList<>(issues);
    }

    /** 分组统计：别名组 → {合计张数, 组里的第一个卡号}。 */
    private static Map<Integer, int[]> groupCounts(DeckData deck) {
        Map<Integer, int[]> groups = new HashMap<>();
        for (int value : cards(deck)) {
            int group = key(value);
            int[] counts = groups.get(group);
            if (counts == null) groups.put(group, new int[]{1, value});
            else counts[0]++;
        }
        return groups;
    }

    private static List<Integer> cards(DeckData deck) {
        List<Integer> all = new ArrayList<>(deck.mainDeck().size() + deck.extraDeck().size()
                + deck.sideDeck().size());
        all.addAll(deck.mainDeck());
        all.addAll(deck.extraDeck());
        all.addAll(deck.sideDeck());
        return all;
    }

    /** 禁卡表按 alias 记账：有 alias 的卡用 alias，没有就用自己。 */
    private static int key(int code) {
        CardInfo info = CardRegistry.get(code).orElse(null);
        return info == null || info.alias() == 0 ? code : info.alias();
    }
}
