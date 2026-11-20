package net.mdpromod;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Collection;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Iterator;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Set;
import java.util.Map.Entry;
import java.util.stream.Stream;

public final class RelatedCards {
    private static final Set<String> GENERIC_SET_NAMES = Set.of("融合", "同调", "超量", "灵摆", "连接", "反击", "升阶魔法", "降阶魔法");
    private static volatile Map<String, Integer> cachedSetNames;

    private RelatedCards() {
    }

    public static List<CardInfo> find(int code) {
        CardInfo source = CardRegistry.get(code).orElse(null);
        return source == null ? List.of() : find(source, CardRegistry.all());
    }

    public static List<CardInfo> find(CardInfo source, Collection<CardInfo> cards) {
        return find(source, cards, setNames());
    }

    static List<CardInfo> find(CardInfo source, Collection<CardInfo> cards, Map<String, Integer> setNames) {
        if (source != null && !source.isToken() && cards != null && !cards.isEmpty()) {
            RelatedCards.RelationTerms terms = extractTerms(source);
            Set<Integer> sourceGroups = relationGroups(source, terms, setNames);
            List<CardInfo> related = new ArrayList<>();

            for (CardInfo candidate : cards) {
                if (candidate != null
                    && candidate.code() != source.code()
                    && !candidate.isToken()
                    && (mentions(candidate, terms) || sharesGroup(sourceGroups, candidate.setcode()))) {
                    related.add(candidate);
                }
            }

            related.sort((left, right) -> Integer.compare(left.code(), right.code()));
            return related;
        } else {
            return List.of();
        }
    }

    static List<String> relationTerms(CardInfo card) {
        return extractTerms(card).all();
    }

    private static RelatedCards.RelationTerms extractTerms(CardInfo card) {
        LinkedHashSet<String> quoted = new LinkedHashSet<>();
        String desc = card.desc();
        if (desc != null && !desc.isEmpty()) {
            int cursor = 0;

            while (cursor < desc.length()) {
                int open = desc.indexOf(12300, cursor);
                if (open < 0) {
                    break;
                }

                int close = desc.indexOf(12301, open + 1);
                if (close < 0) {
                    break;
                }

                addTerm(quoted, desc.substring(open + 1, close));
                cursor = close + 1;
            }
        }

        LinkedHashSet<String> all = new LinkedHashSet<>();
        addTerm(all, card.name());
        all.addAll(quoted);
        String name = card.name() == null ? "" : card.name().trim();
        return new RelatedCards.RelationTerms(name, List.copyOf(all), List.copyOf(quoted));
    }

    private static void addTerm(Set<String> terms, String raw) {
        if (raw != null) {
            String value = raw.trim();
            if (!value.isEmpty()) {
                terms.add(value);
            }
        }
    }

    private static boolean mentions(CardInfo card, RelatedCards.RelationTerms terms) {
        String name = card.name() == null ? "" : card.name().toLowerCase(Locale.ROOT);
        String desc = card.desc() == null ? "" : card.desc().toLowerCase(Locale.ROOT);

        for (String raw : terms.all()) {
            String term = raw.toLowerCase(Locale.ROOT);
            if (name.contains(term) || desc.contains("「" + term + "」")) {
                return true;
            }
        }

        return false;
    }

    private static Set<Integer> relationGroups(CardInfo source, RelatedCards.RelationTerms terms, Map<String, Integer> setNames) {
        Set<Integer> groups = new HashSet<>();
        if (setNames != null && !setNames.isEmpty()) {
            for (String quoted : terms.quoted()) {
                Integer code = setNames.get(quoted);
                if (code != null && code != 0) {
                    groups.add(code);
                }
            }

            Set<Integer> rawGroups = groupCodes(source.setcode());
            if (rawGroups.isEmpty()) {
                return groups;
            }

            String sourceName = terms.name().toLowerCase(Locale.ROOT);

            for (Entry<String, Integer> entry : setNames.entrySet()) {
                int code = entry.getValue();
                if (code != 0 && rawGroups.contains(code)) {
                    String alias = entry.getKey();
                    if ((!GENERIC_SET_NAMES.contains(alias) || sourceName.equals(alias.toLowerCase(Locale.ROOT)))
                        && sourceName.contains(alias.toLowerCase(Locale.ROOT))) {
                        groups.add(code);
                    }
                }
            }

            return groups;
        } else {
            return groups;
        }
    }

    private static boolean sharesGroup(Set<Integer> sourceGroups, int setcode) {
        if (sourceGroups.isEmpty()) {
            return false;
        }

        for (int group : groupCodes(setcode)) {
            if (sourceGroups.contains(group)) {
                return true;
            }
        }

        return false;
    }

    static Set<Integer> groupCodes(int setcode) {
        Set<Integer> groups = new HashSet<>();

        for (int shift = 0; shift < 32; shift += 16) {
            int group = setcode >>> shift & 65535;
            if (group != 0) {
                groups.add(group);
            }
        }

        return groups;
    }

    private static Map<String, Integer> setNames() {
        Map<String, Integer> cached = cachedSetNames;
        if (cached != null) {
            return cached;
        }

        synchronized (RelatedCards.class) {
            cached = cachedSetNames;
            if (cached == null) {
                Map<String, Integer> loaded = new HashMap<>();
                Path root = ModPaths.runtimeRoot();
                loadSetNames(root.resolve("Data").resolve("locales").resolve("zh-CN").resolve("strings.conf"), loaded);
                Path expansions = root.resolve("Expansions");
                if (Files.isDirectory(expansions)) {
                    try (Stream<Path> files = Files.list(expansions)) {
                        for (Path path : files.filter(file -> file.getFileName().toString().toLowerCase(Locale.ROOT).endsWith(".conf")).toList()) {
                            loadSetNames(path, loaded);
                        }
                    } catch (IOException error) {
                        ModLog.warn("读取扩展 setname 失败：{}", error.toString());
                    }
                }

                if (loaded.isEmpty()) {
                    return Map.of();
                }

                cached = Map.copyOf(loaded);
                cachedSetNames = cached;
            }

            return cached;
        }
    }

    static void loadSetNames(Path path, Map<String, Integer> target) {
        if (path != null && target != null && Files.isRegularFile(path)) {
            try {
                Iterator error = Files.readAllLines(path, StandardCharsets.UTF_8).iterator();

                while (true) {
                    String rest;
                    int code;
                    while (true) {
                        if (!error.hasNext()) {
                            return;
                        }

                        String raw = (String)error.next();
                        String line = raw.startsWith("\ufeff") ? raw.substring(1) : raw;
                        if (line.startsWith("!setname ")) {
                            int firstSpace = line.indexOf(32, 9);
                            if (firstSpace >= 0) {
                                String codeText = line.substring(9, firstSpace);
                                rest = line.substring(firstSpace + 1);
                                int tab = rest.indexOf(9);
                                if (tab >= 0) {
                                    rest = rest.substring(0, tab);
                                }

                                try {
                                    code = Integer.decode(codeText);
                                    break;
                                } catch (NumberFormatException ignored) {
                                }
                            }
                        }
                    }

                    for (String alias : rest.split("\\|")) {
                        addTerm(target, alias, code);
                    }
                }
            } catch (IOException error) {
                ModLog.warn("读取 setname 失败 {}：{}", path, error.toString());
            }
        }
    }

    private static void addTerm(Map<String, Integer> target, String raw, int code) {
        if (raw != null) {
            String value = raw.trim();
            if (!value.isEmpty()) {
                target.putIfAbsent(value, code);
            }
        }
    }

    private record RelationTerms(String name, List<String> all, List<String> quoted) {
    }
}
