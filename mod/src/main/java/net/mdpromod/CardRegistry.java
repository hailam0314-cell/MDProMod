package net.mdpromod;

import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.Collection;
import java.util.Collections;
import java.util.HashMap;
import java.util.Map;
import java.util.Optional;

public final class CardRegistry {
    private static final Map<Integer, CardInfo> CARDS = new HashMap<>();
    private static boolean loaded;
    /** 载入失败的原因（空串 = 没失败）；日志和离线测试都靠它说清楚。 */
    private static String loadError = "";

    private CardRegistry() {
    }

    public static synchronized void load() {
        if (loaded) return;
        loaded = true;                     // 失败也只试一次，别每次查询都重来一遍
        try (InputStream input = CardRegistry.class.getResourceAsStream("/data/mdpromod/cards.json")) {
            if (input == null) {
                ModLog.warn("卡库资源缺失：/data/mdpromod/cards.json");
                loadError = "找不到资源 /data/mdpromod/cards.json";
                return;
            }
            JsonObject root = JsonParser.parseReader(
                    new InputStreamReader(input, StandardCharsets.UTF_8)).getAsJsonObject();
            for (Map.Entry<String, JsonElement> entry : root.entrySet()) {
                try {
                    int code = Integer.parseInt(entry.getKey());
                    JsonObject value = entry.getValue().getAsJsonObject();
                    CARDS.put(code, new CardInfo(
                            code,
                            value.get("name").getAsString(),
                            value.has("desc") ? value.get("desc").getAsString() : "",
                            value.has("ot") ? value.get("ot").getAsInt() : 0,
                            value.get("alias").getAsInt(),
                            value.get("type").getAsInt(),
                            value.get("atk").getAsInt(),
                            value.get("def").getAsInt(),
                            value.get("level").getAsInt(),
                            value.get("race").getAsInt(),
                            value.get("attribute").getAsInt()));
                } catch (RuntimeException ex) {
                    ModLog.warn("跳过损坏的卡数据 {}：{}", entry.getKey(), ex.toString());
                }
            }
            ModLog.info("载入 {} 张卡的数据", CARDS.size());
        } catch (Throwable ex) {
            // 离线测试的 classpath 上没有 gson，这里会走到：卡库为空，但功能照常（禁卡表按卡号算）
            loadError = ex.toString();
            ModLog.warn("卡库载入失败：{}", ex.toString());
        }
    }

    static String loadError() {
        load();
        return loadError;
    }

    public static Optional<CardInfo> get(int code) {
        load();
        return Optional.ofNullable(CARDS.get(code));
    }

    public static boolean contains(int code) {
        load();
        return CARDS.containsKey(code);
    }

    public static int size() {
        load();
        return CARDS.size();
    }

    public static Collection<CardInfo> all() {
        load();
        return Collections.unmodifiableCollection(CARDS.values());
    }
}
