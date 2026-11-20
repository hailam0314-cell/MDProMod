package net.mdpromod;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.Map.Entry;

public final class DeckRules {
    public static final int MAIN_MIN = 40;
    public static final int MAIN_MAX = 60;
    public static final int EXTRA_MAX = 15;
    public static final int SIDE_MAX = 15;

    private DeckRules() {
    }

    static Banlists.Active banlists() {
        return BanlistFile.active();
    }

    public static String banlistStatus() {
        return "禁卡表：" + banlists().summary();
    }

    public static int limitOf(int code) {
        return banlists().limitOf(key(code));
    }

    public static String limitText(int code) {
        int limit = limitOf(code);
        String reason = banlists().reason(key(code));
        String head = limit == 0 ? "禁止" : (limit >= 3 ? "限制 3" : "限制 " + limit);
        return reason.isEmpty() ? head : head + "（" + reason + "）";
    }

    public static String limitSuffix(int code) {
        String reason = banlists().reason(key(code));
        return reason.isEmpty() ? "" : "（" + reason + "）";
    }

    public static String displayName(int code) {
        return CardRegistry.get(code).map(CardInfo::name).orElse("#" + code);
    }

    public static int copies(DeckData deck, int code) {
        int wanted = key(code);
        int total = 0;

        for (int value : deck.mainDeck()) {
            if (key(value) == wanted) {
                total++;
            }
        }

        for (int value : deck.extraDeck()) {
            if (key(value) == wanted) {
                total++;
            }
        }

        for (int value : deck.sideDeck()) {
            if (key(value) == wanted) {
                total++;
            }
        }

        return total;
    }

    public static List<Integer> groupMates(DeckData deck, int code) {
        int wanted = key(code);
        Set<Integer> mates = new LinkedHashSet<>();

        for (int value : cards(deck)) {
            if (value != code && key(value) == wanted) {
                mates.add(value);
            }
        }

        return new ArrayList<>(mates);
    }

    public static String groupMateText(DeckData deck, int code) {
        List<Integer> mates = groupMates(deck, code);
        if (mates.isEmpty()) {
            return "";
        }

        StringBuilder text = new StringBuilder("，和");

        for (int i = 0; i < mates.size() && i < 2; i++) {
            if (i > 0) {
                text.append("、");
            }

            text.append("「").append(displayName(mates.get(i))).append("」");
        }

        if (mates.size() > 2) {
            text.append(" 等 ").append(mates.size()).append(" 种");
        }

        return text.append("算同一张卡").toString();
    }

    public static List<String> problems(DeckData deck) {
        Set<String> issues = new LinkedHashSet<>();
        int main = deck.mainDeck().size();
        int extra = deck.extraDeck().size();
        int side = deck.sideDeck().size();
        if (main < 40) {
            issues.add("主卡组还差 " + (40 - main) + " 张（当前 " + main + "/60）");
        }

        if (main > 60) {
            issues.add("主卡组超过 60 张（当前 " + main + " 张）");
        }

        if (extra > 15) {
            issues.add("额外卡组超过 15 张（当前 " + extra + " 张）");
        }

        if (side > 15) {
            issues.add("副卡组超过 15 张（当前 " + side + " 张）");
        }

        Map<Integer, int[]> groups = groupCounts(deck);

        for (Entry<Integer, int[]> entry : groups.entrySet()) {
            int group = entry.getKey();
            int total = entry.getValue()[0];
            int sample = entry.getValue()[1];
            int limit = banlists().limitOf(group);
            if (total > limit) {
                String name = displayName(sample);
                if (limit == 0) {
                    issues.add(name + " 是禁止卡" + limitSuffix(sample) + "，当前 " + total + " 张");
                } else {
                    issues.add(name + " 最多 " + limit + " 张" + limitSuffix(sample) + "，当前 " + total + " 张");
                }
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

    private static Map<Integer, int[]> groupCounts(DeckData deck) {
        Map<Integer, int[]> groups = new HashMap<>();

        for (int value : cards(deck)) {
            int group = key(value);
            int[] counts = groups.get(group);
            if (counts == null) {
                groups.put(group, new int[]{1, value});
            } else {
                counts[0]++;
            }
        }

        return groups;
    }

    private static List<Integer> cards(DeckData deck) {
        List<Integer> all = new ArrayList<>(deck.mainDeck().size() + deck.extraDeck().size() + deck.sideDeck().size());
        all.addAll(deck.mainDeck());
        all.addAll(deck.extraDeck());
        all.addAll(deck.sideDeck());
        return all;
    }

    private static int key(int code) {
        CardInfo info = CardRegistry.get(code).orElse(null);
        return info != null && info.alias() != 0 ? info.alias() : code;
    }
}
