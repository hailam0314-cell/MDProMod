package net.mdpromod;

import java.io.BufferedReader;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import org.slf4j.LoggerFactory;

public final class DuelBgmTracks {
    private static final String RESOURCE = "/data/mdpromod/bgm_tracks.txt";
    private static final String OFF_LABEL = "不播放";
    private static Map<String, String> labels;
    private static List<String> keys;

    private DuelBgmTracks() {
    }

    private static synchronized void load() {
        if (labels == null) {
            Map<String, String> table = new LinkedHashMap<>();

            try (InputStream in = DuelBgmTracks.class.getResourceAsStream("/data/mdpromod/bgm_tracks.txt")) {
                if (in == null) {
                    LoggerFactory.getLogger("mdpromod").warn("包里没有 {}", "/data/mdpromod/bgm_tracks.txt");
                } else {
                    String line;
                    try (BufferedReader reader = new BufferedReader(new InputStreamReader(in, StandardCharsets.UTF_8))) {
                        while ((line = reader.readLine()) != null) {
                            String trimmed = line.trim();
                            if (!trimmed.isEmpty() && !trimmed.startsWith("#")) {
                                String[] parts = trimmed.split("\t", 2);
                                String key = parts[0].trim().toUpperCase(Locale.ROOT);
                                if (!key.isEmpty()) {
                                    table.put(key, parts.length > 1 && !parts[1].isBlank() ? parts[1].trim() : key);
                                }
                            }
                        }
                    }
                }
            } catch (Exception error) {
                LoggerFactory.getLogger("mdpromod").warn("读不出 BGM 曲目清单: {}", error.toString());
            }

            labels = table;
            keys = List.copyOf(table.keySet());
        }
    }

    public static String[] keys() {
        load();
        return keys.toArray(new String[0]);
    }

    public static String[] labels() {
        load();
        return keys.stream().map(labels::get).toArray(String[]::new);
    }

    public static String label(String key) {
        load();
        if (key == null) {
            return labels.getOrDefault("BGM_DUEL_NORMAL_01", "BGM_DUEL_NORMAL_01");
        } else {
            String choice = key.trim();
            if (!choice.isEmpty() && !choice.equalsIgnoreCase("off") && !choice.equalsIgnoreCase("none")) {
                String normalized = DuelBgmRules.localChoice(choice);
                return labels.getOrDefault(normalized, normalized);
            } else {
                return "不播放";
            }
        }
    }

    public static int size() {
        load();
        return keys.size();
    }
}
