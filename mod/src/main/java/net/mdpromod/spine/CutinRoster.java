package net.mdpromod.spine;

import java.io.BufferedReader;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.HashMap;
import java.util.Map;

/**
 * "哪些卡有王牌登场演出"的清单，随模组打包（{@code data/mdpromod/cutins.txt}，
 * 由离线管线生成）。服务端据此决定要不要发 cut-in 包，客户端据此知道演出多长。
 *
 * 清单与低清素材一起随模组打包，客户端按需读取（见 {@link CutinAssets}）。
 */
public final class CutinRoster {

    private static final String RESOURCE = "/data/mdpromod/cutins.txt";
    private static Map<Integer, Float> durations;

    private CutinRoster() {
    }

    private static synchronized Map<Integer, Float> table() {
        if (durations != null) {
            return durations;
        }
        Map<Integer, Float> loaded = new HashMap<>();
        try (InputStream in = CutinRoster.class.getResourceAsStream(RESOURCE)) {
            if (in != null) {
                try (BufferedReader reader = new BufferedReader(
                        new InputStreamReader(in, StandardCharsets.UTF_8))) {
                    String line;
                    while ((line = reader.readLine()) != null) {
                        String trimmed = line.trim();
                        if (trimmed.isEmpty() || trimmed.startsWith("#")) {
                            continue;
                        }
                        // 格式：卡号 毫秒时长   （时长取整数毫秒，留给节奏用）
                        String[] parts = trimmed.split("\\s+");
                        if (parts.length < 2) {
                            continue;
                        }
                        try {
                            loaded.put(Integer.parseInt(parts[0]), Float.parseFloat(parts[1]));
                        } catch (NumberFormatException ignored) {
                            // 坏行跳过
                        }
                    }
                }
            }
        } catch (Exception error) {
            org.slf4j.LoggerFactory.getLogger("mdpromod").warn(
                    "读不出 cut-in 清单: {}", error.toString());
        }
        durations = loaded;
        return durations;
    }

    /** 这张卡有没有王牌登场演出。 */
    public static boolean has(int code) {
        return table().containsKey(code);
    }

    /** 演出时长（毫秒）；没有就是 0。 */
    public static int durationMs(int code) {
        Float value = table().get(code);
        return value == null ? 0 : Math.round(value);
    }

    public static int size() {
        return table().size();
    }
}
