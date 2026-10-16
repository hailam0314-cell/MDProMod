package net.mdpromod;

import net.minecraft.nbt.CompoundTag;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;

public final class DeckData {
    private String name = "starter";
    private final List<Integer> mainDeck = new ArrayList<>();
    private final List<Integer> extraDeck = new ArrayList<>();
    private final List<Integer> sideDeck = new ArrayList<>();

    public String name() {
        return name;
    }

    public void setName(String name) {
        this.name = name == null || name.isBlank() ? "starter" : name;
    }

    public List<Integer> mainDeck() {
        return mainDeck;
    }

    public List<Integer> extraDeck() {
        return extraDeck;
    }

    public List<Integer> sideDeck() {
        return sideDeck;
    }

    public String summary() {
        return name + " main=" + mainDeck.size() + " extra=" + extraDeck.size()
                + " side=" + sideDeck.size();
    }

    public CompoundTag save() {
        CompoundTag tag = new CompoundTag();
        tag.putString("name", name);
        tag.putIntArray("main", toArray(mainDeck));
        tag.putIntArray("extra", toArray(extraDeck));
        tag.putIntArray("side", toArray(sideDeck));
        return tag;
    }

    public static DeckData load(CompoundTag tag) {
        DeckData data = new DeckData();
        data.setName(tag.getString("name"));
        for (int code : tag.getIntArray("main")) data.mainDeck.add(code);
        for (int code : tag.getIntArray("extra")) data.extraDeck.add(code);
        for (int code : tag.getIntArray("side")) data.sideDeck.add(code);
        return data;
    }

    public int[] mainArray() {
        return toArray(mainDeck);
    }

    public int[] extraArray() {
        return toArray(extraDeck);
    }

    public int[] sideArray() {
        return toArray(sideDeck);
    }

    public static DeckData of(String name, int[] main, int[] extra, int[] side) {
        DeckData data = new DeckData();
        data.setName(name);
        if (main != null) for (int code : main) data.mainDeck.add(code);
        if (extra != null) for (int code : extra) data.extraDeck.add(code);
        if (side != null) for (int code : side) data.sideDeck.add(code);
        return data;
    }

    public static DeckData fromYdk(Path path) throws IOException {
        DeckData data = new DeckData();
        data.setName(path.getFileName().toString().replaceFirst("\\.[^.]+$", ""));
        int section = 0;
        for (String raw : Files.readAllLines(path)) {
            String line = raw.trim();
            if (line.isEmpty() || line.startsWith("#") && !line.equalsIgnoreCase("#main")
                    && !line.equalsIgnoreCase("#extra")) {
                continue;
            }
            if (line.equalsIgnoreCase("#main")) {
                section = 0;
                continue;
            }
            if (line.equalsIgnoreCase("#extra")) {
                section = 1;
                continue;
            }
            if (line.equalsIgnoreCase("!side")) {
                section = 2;
                continue;
            }
            try {
                int code = Integer.parseInt(line);
                switch (section) {
                    case 0 -> data.mainDeck.add(code);
                    case 1 -> data.extraDeck.add(code);
                    case 2 -> data.sideDeck.add(code);
                    default -> { }
                }
            } catch (NumberFormatException ignored) {
            }
        }
        return data;
    }

    private static int[] toArray(List<Integer> values) {
        int[] array = new int[values.size()];
        for (int i = 0; i < values.size(); i++) array[i] = values.get(i);
        return array;
    }
}
