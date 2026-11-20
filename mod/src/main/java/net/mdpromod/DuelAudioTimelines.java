package net.mdpromod;

import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Map.Entry;
import org.slf4j.LoggerFactory;

public final class DuelAudioTimelines {
    private static final String RESOURCE = "/data/mdpromod/duel_audio_timelines.json";
    private static Map<String, Map<String, List<DuelAudioTimelines.Cue>>> timelines;

    private DuelAudioTimelines() {
    }

    public static synchronized List<DuelAudioTimelines.Cue> cues(String group, String id) {
        if (timelines == null) {
            load();
        }

        return timelines.getOrDefault(group, Map.of()).getOrDefault(id, List.of());
    }

    private static void load() {
        Map<String, Map<String, List<DuelAudioTimelines.Cue>>> result = new HashMap<>();

        try (InputStream input = DuelAudioTimelines.class.getResourceAsStream("/data/mdpromod/duel_audio_timelines.json")) {
            if (input != null) {
                JsonObject root = JsonParser.parseReader(new InputStreamReader(input, StandardCharsets.UTF_8)).getAsJsonObject();

                for (Entry<String, JsonElement> group : root.entrySet()) {
                    if (group.getValue().isJsonObject()) {
                        Map<String, List<DuelAudioTimelines.Cue>> entries = new HashMap<>();

                        for (Entry<String, JsonElement> entry : group.getValue().getAsJsonObject().entrySet()) {
                            if (entry.getValue().isJsonArray()) {
                                List<DuelAudioTimelines.Cue> cues = new ArrayList<>();

                                for (JsonElement value : entry.getValue().getAsJsonArray()) {
                                    JsonObject cue = value.getAsJsonObject();
                                    String key = cue.get("key").getAsString();
                                    if (!key.isBlank()) {
                                        cues.add(
                                            new DuelAudioTimelines.Cue(
                                                key,
                                                Math.max(0, integer(cue, "timeMs", 0)),
                                                Math.max(0, integer(cue, "durationMs", 0)),
                                                decimal(cue, "volume", 1.0F),
                                                decimal(cue, "pitch", 1.0F),
                                                cue.has("loop") && cue.get("loop").getAsBoolean()
                                            )
                                        );
                                    }
                                }

                                cues.sort(Comparator.comparingInt(DuelAudioTimelines.Cue::timeMs));
                                entries.put(entry.getKey(), List.copyOf(cues));
                            }
                        }

                        result.put(group.getKey(), Map.copyOf(entries));
                    }
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

    public record Cue(String key, int timeMs, int durationMs, float volume, float pitch, boolean loop) {
    }
}
