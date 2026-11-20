package net.mdpromod;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import com.mojang.logging.LogUtils;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import org.slf4j.Logger;

public final class DuelFieldLayout {
    private static final Logger LOGGER = LogUtils.getLogger();
    private static final List<DuelFieldLayout.Zone> ZONES = new ArrayList<>();
    private static final Map<String, DuelFieldLayout.Zone> BY_ID = new HashMap<>();
    private static final List<DuelFieldLayout.Control> CONTROLS = new ArrayList<>();
    private static final Map<String, DuelFieldLayout.Control> CONTROL_BY_ID = new HashMap<>();
    private static boolean loaded;
    private static double widthBlocks = 16.0;
    private static double depthBlocks = 14.0;

    private DuelFieldLayout() {
    }

    public static synchronized void load() {
        if (!loaded) {
            try (InputStream input = DuelFieldLayout.class.getResourceAsStream("/data/mdpromod/duel_field_layout.json")) {
                if (input != null) {
                    JsonObject root = JsonParser.parseReader(new InputStreamReader(input, StandardCharsets.UTF_8)).getAsJsonObject();
                    if (root.has("size_blocks")) {
                        JsonArray size = root.getAsJsonArray("size_blocks");
                        widthBlocks = size.get(0).getAsDouble();
                        depthBlocks = size.get(2).getAsDouble();
                    }

                    for (JsonElement element : root.getAsJsonArray("zones")) {
                        JsonObject zone = element.getAsJsonObject();
                        JsonArray center = zone.getAsJsonArray("center");
                        DuelFieldLayout.Zone value = new DuelFieldLayout.Zone(
                            zone.get("id").getAsString(),
                            zone.get("owner").getAsString(),
                            zone.get("kind").getAsString(),
                            center.get(0).getAsDouble(),
                            center.get(1).getAsDouble(),
                            center.get(2).getAsDouble(),
                            zone.has("rotation_y") ? zone.get("rotation_y").getAsInt() : 0
                        );
                        ZONES.add(value);
                        BY_ID.put(value.id(), value);
                    }

                    for (JsonElement element : root.has("controls") ? root.getAsJsonArray("controls") : new JsonArray()) {
                        JsonObject control = element.getAsJsonObject();
                        JsonArray center = control.getAsJsonArray("center");
                        JsonArray size = control.getAsJsonArray("size");
                        String phase = control.has("phase") ? control.get("phase").getAsString() : "";
                        DuelFieldLayout.Control value = new DuelFieldLayout.Control(
                            control.get("id").getAsString(),
                            control.get("owner").getAsString(),
                            control.get("kind").getAsString(),
                            phase,
                            control.has("label") ? control.get("label").getAsString() : "",
                            center.get(0).getAsDouble(),
                            center.get(1).getAsDouble(),
                            center.get(2).getAsDouble(),
                            size.get(0).getAsDouble(),
                            size.get(1).getAsDouble(),
                            control.has("rotation_y") ? control.get("rotation_y").getAsInt() : 0
                        );
                        CONTROLS.add(value);
                        CONTROL_BY_ID.put(value.id(), value);
                    }

                    loaded = true;
                    LOGGER.info("Loaded {} duel field zones", ZONES.size());
                } else {
                    LOGGER.error("Duel field layout resource is missing");
                }
            } catch (Exception ex) {
                LOGGER.error("Failed to load duel field layout", ex);
            }
        }
    }

    public static List<DuelFieldLayout.Zone> zones() {
        load();
        return Collections.unmodifiableList(ZONES);
    }

    public static int zoneCount() {
        load();
        return ZONES.size();
    }

    public static double widthBlocks() {
        load();
        return widthBlocks;
    }

    public static double depthBlocks() {
        load();
        return depthBlocks;
    }

    public static Optional<DuelFieldLayout.Zone> byId(String id) {
        load();
        return Optional.ofNullable(BY_ID.get(id));
    }

    public static List<DuelFieldLayout.Zone> ofKind(String kind) {
        load();
        List<DuelFieldLayout.Zone> result = new ArrayList<>();

        for (DuelFieldLayout.Zone zone : ZONES) {
            if (zone.kind().equals(kind)) {
                result.add(zone);
            }
        }

        return result;
    }

    public static List<DuelFieldLayout.Control> controls() {
        load();
        return Collections.unmodifiableList(CONTROLS);
    }

    public static Optional<DuelFieldLayout.Control> controlById(String id) {
        load();
        return Optional.ofNullable(CONTROL_BY_ID.get(id));
    }

    public static int[] engineAddress(String zoneId, int side) {
        if (zoneId != null && zoneId.length() >= 4) {
            int controller = zoneId.startsWith("p2_") ? 1 : 0;
            String rest = zoneId.substring(3);
            if (rest.startsWith("mzone_")) {
                try {
                    return new int[]{controller, 4, Integer.parseInt(rest.substring(6))};
                } catch (NumberFormatException ex) {
                    return null;
                }
            } else if (rest.startsWith("szone_")) {
                try {
                    return new int[]{controller, 8, Integer.parseInt(rest.substring(6))};
                } catch (NumberFormatException ex) {
                    return null;
                }
            } else if (rest.equals("field")) {
                return new int[]{controller, 8, 5};
            } else if (rest.equals("extra")) {
                return new int[]{controller, 64, 0};
            } else if (rest.equals("deck")) {
                return new int[]{controller, 1, 0};
            } else if (rest.equals("graveyard")) {
                return new int[]{controller, 16, 0};
            } else if (rest.equals("banished")) {
                return new int[]{controller, 32, 0};
            } else if (zoneId.startsWith("shared_emz_")) {
                boolean left = zoneId.endsWith("left");
                return side == 0 ? new int[]{0, 4, left ? 5 : 6} : new int[]{1, 4, left ? 6 : 5};
            } else {
                return null;
            }
        } else {
            return null;
        }
    }

    public static String zoneIdFor(int controller, int location, int sequence) {
        String prefix = "p" + (controller + 1) + "_";
        if (location == 16) {
            return prefix + "graveyard";
        }

        if (location == 32) {
            return prefix + "banished";
        }

        if (location == 64) {
            return prefix + "extra";
        }

        if (location == 4) {
            if (sequence <= 4) {
                return prefix + "mzone_" + sequence;
            }

            boolean left = controller == 0 ? sequence == 5 : sequence == 6;
            return left ? "shared_emz_left" : "shared_emz_right";
        } else {
            if (location == 8) {
                if (sequence <= 4) {
                    return prefix + "szone_" + sequence;
                }

                if (sequence == 5) {
                    return prefix + "field";
                }
            }

            return null;
        }
    }

    public record Control(
        String id, String owner, String kind, String phase, String label, double x, double y, double z, double width, double depth, int rotationY
    ) {
    }

    public record Zone(String id, String owner, String kind, double x, double y, double z, int rotationY) {
    }
}
