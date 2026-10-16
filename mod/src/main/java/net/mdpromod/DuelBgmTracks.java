package net.mdpromod;

import org.slf4j.LoggerFactory;

import java.io.BufferedReader;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;

/**
 * 可选曲目清单：随包带 {@code data/mdpromod/bgm_tracks.txt}（键 + 中文标签，
 * 由 {@code tools/audio_pipeline/build_bgm_assets.py} 生成）。
 *
 * 服务端用它下发列表，客户端用它显示标签。纯 Java，不依赖 Minecraft，可脱离游戏测试。
 */
public final class DuelBgmTracks {

    private static final String RESOURCE = "/data/mdpromod/bgm_tracks.txt";
    private static final String OFF_LABEL = "不播放";

    private static Map<String, String> labels;
    private static List<String> keys;

    private DuelBgmTracks() {
    }

    private static synchronized void load() {
        if (labels != null) {
            return;
        }
        Map<String, String> table = new LinkedHashMap<>();
        try (InputStream in = DuelBgmTracks.class.getResourceAsStream(RESOURCE)) {
            if (in != null) {
                try (BufferedReader reader = new BufferedReader(
                        new InputStreamReader(in, StandardCharsets.UTF_8))) {
                    String line;
                    while ((line = reader.readLine()) != null) {
                        String trimmed = line.trim();
                        if (trimmed.isEmpty() || trimmed.startsWith("#")) {
                            continue;
                        }
                        String[] parts = trimmed.split("\t", 2);
                        String key = parts[0].trim().toUpperCase(Locale.ROOT);
                        if (key.isEmpty()) {
                            continue;
                        }
                        table.put(key, parts.length > 1 && !parts[1].isBlank()
                                ? parts[1].trim() : key);
                    }
                }
            } else {
                LoggerFactory.getLogger("mdpromod").warn("包里没有 {}", RESOURCE);
            }
        } catch (Exception error) {
            LoggerFactory.getLogger("mdpromod").warn("读不出 BGM 曲目清单: {}", error.toString());
        }
        labels = table;
        keys = List.copyOf(table.keySet());
    }

    /** 全部可选曲目的键（大写，和资源路径里的名字一致）。 */
    public static String[] keys() {
        load();
        return keys.toArray(new String[0]);
    }

    /** 与 {@link #keys()} 一一对应的显示名。 */
    public static String[] labels() {
        load();
        return keys.stream().map(labels::get).toArray(String[]::new);
    }

    /**
     * 显示名：空值给默认曲目，{@code off}/{@code none} 给「不播放」，
     * 清单里没有的键原样显示（方便用系统属性挂自定义曲目）。
     */
    public static String label(String key) {
        load();
        if (key == null) {
            return labels.getOrDefault(DuelBgmRules.DEFAULT_KEY, DuelBgmRules.DEFAULT_KEY);
        }
        String choice = key.trim();
        if (choice.isEmpty() || choice.equalsIgnoreCase("off") || choice.equalsIgnoreCase("none")) {
            return OFF_LABEL;
        }
        String normalized = DuelBgmRules.localChoice(choice);
        return labels.getOrDefault(normalized, normalized);
    }

    public static int size() {
        load();
        return keys.size();
    }
}
