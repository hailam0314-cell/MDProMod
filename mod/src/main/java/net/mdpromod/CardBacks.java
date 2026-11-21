package net.mdpromod;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import net.minecraft.resources.ResourceLocation;

final class CardBacks {
    static final int DEFAULT = 0;
    private static final Map<Integer, Entry> ENTRIES = load();
    static final List<Entry> ALL = List.copyOf(ENTRIES.values());

    record Entry(int id, String chinese, String english, ResourceLocation texture) {
        String name(boolean zh) {
            String name = zh && !chinese.isBlank() ? chinese : english;
            return name.isBlank() ? Integer.toString(id) : name;
        }
    }

    private static Map<Integer, Entry> load() {
        Map<Integer, Entry> entries = new LinkedHashMap<>();
        entries.put(DEFAULT, new Entry(DEFAULT, "原版卡背", "Original card back",
            ResourceLocation.fromNamespaceAndPath("mdpromod", "textures/entity/card_back.png")));
        var stream = CardBacks.class.getResourceAsStream("/assets/mdpromod/textures/card_back/catalog.tsv");
        if (stream == null) throw new IllegalStateException("Missing card back catalog");
        try (var reader = new BufferedReader(new InputStreamReader(stream, StandardCharsets.UTF_8))) {
            for (String line; (line = reader.readLine()) != null;) {
                String[] fields = line.split("\t", -1);
                int id = Integer.parseInt(fields[0]);
                var texture = ResourceLocation.fromNamespaceAndPath("mdpromod", "textures/card_back/" + id + ".jpg");
                entries.put(id, new Entry(id, fields[1], fields[2], texture));
            }
        } catch (IOException e) {
            throw new IllegalStateException("Reading card back catalog", e);
        }
        return entries;
    }

    static int validId(int id) {
        return ENTRIES.containsKey(id) ? id : DEFAULT;
    }

    static Entry entry(int id) {
        return ENTRIES.get(validId(id));
    }

    static ResourceLocation texture(int id) {
        return entry(id).texture();
    }

    private CardBacks() {}
}
