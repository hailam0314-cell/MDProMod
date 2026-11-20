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
import java.util.Map.Entry;

public final class CardRegistry {
    private static final String CHINESE_RESOURCE = "/data/mdpromod/cards.json";
    private static final String ENGLISH_RESOURCE = "/data/mdpromod/cards_en.json";
    private static final Map<Integer, CardInfo> CHINESE = new HashMap<>();
    private static Map<Integer, CardInfo> english;
    private static volatile boolean useEnglish;
    private static boolean englishFailed;
    private static boolean loaded;
    private static String loadError = "";

    private CardRegistry() {
    }

    public static synchronized void load() {
        if (!loaded) {
            loaded = true;
            loadError = read(CHINESE, "/data/mdpromod/cards.json");
        }
    }

    public static synchronized void setEnglish(boolean value) {
        if (value) {
            loadEnglish();
        }

        boolean next = value && english != null;
        if (useEnglish != next) {
            useEnglish = next;
            CardNameSearch.clearIndex();
            ModLog.info("卡牌文字：{}", next ? "英文（缺卡回落中文）" : "中文");
        }
    }

    public static boolean english() {
        return useEnglish;
    }

    private static void loadEnglish() {
        if (english == null && !englishFailed) {
            Map<Integer, CardInfo> parsed = new HashMap<>();
            String error = read(parsed, "/data/mdpromod/cards_en.json");
            if (error.isEmpty()) {
                english = Map.copyOf(parsed);
            } else {
                englishFailed = true;
                ModLog.warn("英文卡库不可用，继续使用中文卡名/卡效：{}", error);
            }
        }
    }

    private static Map<Integer, CardInfo> active() {
        Map<Integer, CardInfo> current = english;
        return useEnglish && current != null ? current : CHINESE;
    }

    private static String read(Map<Integer, CardInfo> target, String resource) {
        try (InputStream input = CardRegistry.class.getResourceAsStream(resource)) {
            if (input == null) {
                ModLog.warn("卡库资源缺失：{}", resource);
                return "找不到资源 " + resource;
            }

            JsonObject root = JsonParser.parseReader(new InputStreamReader(input, StandardCharsets.UTF_8)).getAsJsonObject();

            for (Entry<String, JsonElement> entry : root.entrySet()) {
                try {
                    int code = Integer.parseInt(entry.getKey());
                    JsonObject value = entry.getValue().getAsJsonObject();
                    target.put(
                        code,
                        new CardInfo(
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
                            value.get("attribute").getAsInt(),
                            value.has("setcode") ? value.get("setcode").getAsInt() : 0
                        )
                    );
                } catch (RuntimeException ex) {
                    ModLog.warn("跳过损坏的卡数据 {}：{}", entry.getKey(), ex.toString());
                }
            }

            ModLog.info("载入 {} 张卡的数据（{}）", target.size(), resource);
            return "";
        } catch (Throwable ex) {
            ModLog.warn("卡库载入失败（{}）：{}", resource, ex.toString());
            return ex.toString();
        }
    }

    static String loadError() {
        load();
        return loadError;
    }

    public static Optional<CardInfo> get(int code) {
        load();
        CardInfo card = active().get(code);
        if (card == null && useEnglish) {
            card = CHINESE.get(code);
        }

        return Optional.ofNullable(card);
    }

    public static boolean contains(int code) {
        load();
        return active().containsKey(code) || CHINESE.containsKey(code);
    }

    public static int size() {
        load();
        return active().size();
    }

    public static Collection<CardInfo> all() {
        load();
        return Collections.unmodifiableCollection(active().values());
    }
}
