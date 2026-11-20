package net.mdpromod;

import java.io.BufferedReader;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

public final class CardNameSearch {
    private static final Map<Integer, CardNameSearch.IndexedName> INDEX = new ConcurrentHashMap<>();
    private static volatile Map<String, List<String>> readings;

    private CardNameSearch() {
    }

    static void clearIndex() {
        INDEX.clear();
    }

    public static boolean matches(CardInfo card, String rawQuery) {
        if (card == null) {
            return false;
        }

        String query = rawQuery == null ? "" : rawQuery.trim().toLowerCase(Locale.ROOT);
        if (query.isEmpty()) {
            return true;
        }

        if (Integer.toString(card.code()).startsWith(query)) {
            return true;
        }

        String lowerName = card.name() == null ? "" : card.name().toLowerCase(Locale.ROOT);
        if (lowerName.contains(query)) {
            return true;
        }

        String compact = compact(query);
        if (compact.isEmpty()) {
            return false;
        }

        CardNameSearch.IndexedName indexed = INDEX.computeIfAbsent(card.code(), ignored -> index(card));
        int[] queryCodePoints = compact.codePoints().toArray();

        for (int start = 0; start < indexed.units().size(); start++) {
            if (matchesFull(indexed.units(), start, queryCodePoints, 0)) {
                return true;
            }

            if (indexed.units().get(start).boundary() && matchesInitials(indexed.units(), start, queryCodePoints, 0)) {
                return true;
            }
        }

        return false;
    }

    private static boolean matchesFull(List<CardNameSearch.NameUnit> units, int unitIndex, int[] query, int queryIndex) {
        if (queryIndex >= query.length) {
            return true;
        }

        if (unitIndex >= units.size()) {
            return false;
        }

        String remaining = new String(query, queryIndex, query.length - queryIndex);

        for (String reading : units.get(unitIndex).readings()) {
            if (remaining.length() <= reading.length()) {
                if (reading.startsWith(remaining)) {
                    return true;
                }
            } else if (remaining.startsWith(reading) && matchesFull(units, unitIndex + 1, query, queryIndex + reading.length())) {
                return true;
            }
        }

        return false;
    }

    private static boolean matchesInitials(List<CardNameSearch.NameUnit> units, int unitIndex, int[] query, int queryIndex) {
        if (queryIndex >= query.length) {
            return true;
        }

        if (unitIndex >= units.size()) {
            return false;
        }

        int wanted = query[queryIndex];

        for (String reading : units.get(unitIndex).readings()) {
            if (reading.codePointAt(0) == wanted && matchesInitials(units, unitIndex + 1, query, queryIndex + 1)) {
                return true;
            }
        }

        return false;
    }

    private static CardNameSearch.IndexedName index(CardInfo card) {
        String name = card.name() == null ? "" : card.name();
        List<CardNameSearch.NameUnit> units = new ArrayList<>();
        Map<String, List<String>> table = pinyin();
        boolean boundary = true;
        int cursor = 0;

        while (cursor < name.length()) {
            int codePoint = name.codePointAt(cursor);
            String text = new String(Character.toChars(codePoint));
            cursor += Character.charCount(codePoint);
            List<String> mapped = table.get(text);
            if (mapped != null) {
                units.add(new CardNameSearch.NameUnit(mapped, boundary));
                boundary = false;
            } else if (!Character.isLetterOrDigit(codePoint)) {
                boundary = true;
            } else {
                StringBuilder run = new StringBuilder();
                run.appendCodePoint(codePoint);

                while (true) {
                    if (cursor < name.length()) {
                        int next = name.codePointAt(cursor);
                        if (!table.containsKey(new String(Character.toChars(next))) && Character.isLetterOrDigit(next)) {
                            run.appendCodePoint(next);
                            cursor += Character.charCount(next);
                            continue;
                        }
                    }

                    units.add(new CardNameSearch.NameUnit(List.of(run.toString().toLowerCase(Locale.ROOT)), boundary));
                    boundary = false;
                    break;
                }
            }
        }

        return new CardNameSearch.IndexedName(List.copyOf(units));
    }

    private static String compact(String value) {
        StringBuilder compact = new StringBuilder();
        value.codePoints().forEach(codePoint -> {
            if (codePoint < 128 && Character.isLetterOrDigit(codePoint)) {
                compact.appendCodePoint(Character.toLowerCase(codePoint));
            }
        });
        return compact.toString();
    }

    private static Map<String, List<String>> pinyin() {
        Map<String, List<String>> cached = readings;
        if (cached != null) {
            return cached;
        }

        synchronized (CardNameSearch.class) {
            cached = readings;
            if (cached != null) {
                return cached;
            }

            Map<String, List<String>> loaded = new HashMap<>();

            try (InputStream input = CardNameSearch.class.getResourceAsStream("/data/mdpromod/pinyin.tsv")) {
                if (input == null) {
                    ModLog.warn("拼音索引缺失：/data/mdpromod/pinyin.tsv");
                } else {
                    String line;
                    try (BufferedReader reader = new BufferedReader(new InputStreamReader(input, StandardCharsets.UTF_8))) {
                        while ((line = reader.readLine()) != null) {
                            if (line.startsWith("\ufeff")) {
                                line = line.substring(1);
                            }

                            int tab = line.indexOf(9);
                            if (tab > 0 && tab + 1 < line.length()) {
                                LinkedHashSet<String> values = new LinkedHashSet<>();

                                for (String value : line.substring(tab + 1).split(",")) {
                                    String normalized = value.trim().toLowerCase(Locale.ROOT);
                                    if (!normalized.isEmpty()) {
                                        values.add(normalized);
                                    }
                                }

                                if (!values.isEmpty()) {
                                    loaded.put(line.substring(0, tab), List.copyOf(values));
                                }
                            }
                        }
                    }
                }
            } catch (Exception error) {
                ModLog.warn("拼音索引载入失败：{}", error.toString());
            }

            readings = Map.copyOf(loaded);
            return readings;
        }
    }

    private record IndexedName(List<CardNameSearch.NameUnit> units) {
    }

    private record NameUnit(List<String> readings, boolean boundary) {
    }
}
