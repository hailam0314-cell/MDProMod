package net.mdpromod;

import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import org.slf4j.LoggerFactory;

import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/** Original Unity timeline sound cues, expressed in milliseconds after the visual starts. */
public final class DuelAudioTimelines {
    public record Cue(String key, int timeMs, int durationMs, float volume, float pitch, boolean loop) {}
    private static final String RESOURCE = "/data/mdpromod/duel_audio_timelines.json";
    private static Map<String, Map<String, List<Cue>>> timelines;

    private DuelAudioTimelines() {}

    public static synchronized List<Cue> cues(String group, String id) {
        if (timelines == null) load();
        return timelines.getOrDefault(group, Map.of()).getOrDefault(id, List.of());
    }

    private static void load() {
        Map<String, Map<String, List<Cue>>> result = new HashMap<>();
        try (var input = DuelAudioTimelines.class.getResourceAsStream(RESOURCE)) {
            if (input != null) {
                JsonObject root = JsonParser.parseReader(new InputStreamReader(input, StandardCharsets.UTF_8))
                        .getAsJsonObject();
                for (var group : root.entrySet()) {
                    if (!group.getValue().isJsonObject()) continue;
                    Map<String, List<Cue>> entries = new HashMap<>();
                    for (var entry : group.getValue().getAsJsonObject().entrySet()) {
                        if (!entry.getValue().isJsonArray()) continue;
                        List<Cue> cues = new ArrayList<>();
                        for (JsonElement value : entry.getValue().getAsJsonArray()) {
                            JsonObject cue = value.getAsJsonObject();
                            String key = cue.get("key").getAsString();
                            if (key.isBlank()) continue;
                            cues.add(new Cue(key, Math.max(0, integer(cue, "timeMs", 0)),
                                    Math.max(0, integer(cue, "durationMs", 0)),
                                    decimal(cue, "volume", 1.0F), decimal(cue, "pitch", 1.0F),
                                    cue.has("loop") && cue.get("loop").getAsBoolean()));
                        }
                        cues.sort(Comparator.comparingInt(Cue::timeMs));
                        entries.put(entry.getKey(), List.copyOf(cues));
                    }
                    result.put(group.getKey(), Map.copyOf(entries));
                }
            }
        } catch (Exception exception) {
            LoggerFactory.getLogger("mdpromod").error("Cannot read bundled duel sound timelines", exception);
        }
        timelines = Map.copyOf(result);
    }

    private static int integer(JsonObject object, String name, int fallback) {
        return object.has(name) ? object.get(name).getAsInt() : fallback;
    }
    private static float decimal(JsonObject object, String name, float fallback) {
        return object.has(name) ? object.get(name).getAsFloat() : fallback;
    }
}
