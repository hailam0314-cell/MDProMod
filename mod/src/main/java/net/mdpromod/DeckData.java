package net.mdpromod;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;
import net.minecraft.nbt.CompoundTag;

public final class DeckData {
    private String name = "starter";
    private final List<Integer> mainDeck = new ArrayList<>();
    private final List<Integer> extraDeck = new ArrayList<>();
    private final List<Integer> sideDeck = new ArrayList<>();

    public String name() {
        return this.name;
    }

    public void setName(String name) {
        this.name = name != null && !name.isBlank() ? name : "starter";
    }

    public List<Integer> mainDeck() {
        return this.mainDeck;
    }

    public List<Integer> extraDeck() {
        return this.extraDeck;
    }

    public List<Integer> sideDeck() {
        return this.sideDeck;
    }

    public String summary() {
        return this.name + " main=" + this.mainDeck.size() + " extra=" + this.extraDeck.size() + " side=" + this.sideDeck.size();
    }

    public CompoundTag save() {
        CompoundTag tag = new CompoundTag();
        tag.putString("name", this.name);
        tag.putIntArray("main", toArray(this.mainDeck));
        tag.putIntArray("extra", toArray(this.extraDeck));
        tag.putIntArray("side", toArray(this.sideDeck));
        return tag;
    }

    public static DeckData load(CompoundTag tag) {
        DeckData data = new DeckData();
        data.setName(tag.getString("name"));

        for (int code : tag.getIntArray("main")) {
            data.mainDeck.add(code);
        }

        for (int code : tag.getIntArray("extra")) {
            data.extraDeck.add(code);
        }

        for (int code : tag.getIntArray("side")) {
            data.sideDeck.add(code);
        }

        return data;
    }

    public int[] mainArray() {
        return toArray(this.mainDeck);
    }

    public int[] extraArray() {
        return toArray(this.extraDeck);
    }

    public int[] sideArray() {
        return toArray(this.sideDeck);
    }

    public static DeckData of(String name, int[] main, int[] extra, int[] side) {
        DeckData data = new DeckData();
        data.setName(name);
        if (main != null) {
            for (int code : main) {
                data.mainDeck.add(code);
            }
        }

        if (extra != null) {
            for (int code : extra) {
                data.extraDeck.add(code);
            }
        }

        if (side != null) {
            for (int code : side) {
                data.sideDeck.add(code);
            }
        }

        return data;
    }

    public static DeckData fromYdk(Path path) throws IOException {
        return fromYdkText(Files.readString(path), path.getFileName().toString().replaceFirst("\\.[^.]+$", ""));
    }

    public static DeckData fromYdkText(String text, String name) {
        DeckData data = new DeckData();
        data.setName(name);
        int section = 0;

        for (String raw : (text == null ? "" : text).split("\\R")) {
            String line = raw.trim();
            if (!line.isEmpty()
                && (!line.startsWith("#") || line.equalsIgnoreCase("#main") || line.equalsIgnoreCase("#extra") || line.equalsIgnoreCase("#side"))) {
                if (line.equalsIgnoreCase("#main")) {
                    section = 0;
                } else if (line.equalsIgnoreCase("#extra")) {
                    section = 1;
                } else if (!line.equalsIgnoreCase("!side") && !line.equalsIgnoreCase("#side")) {
                    try {
                        int code = Integer.parseInt(line);
                        switch (section) {
                            case 0:
                                data.mainDeck.add(code);
                                break;
                            case 1:
                                data.extraDeck.add(code);
                                break;
                            case 2:
                                data.sideDeck.add(code);
                        }
                    } catch (NumberFormatException var10) {
                    }
                } else {
                    section = 2;
                }
            }
        }

        return data;
    }

    private static int[] toArray(List<Integer> values) {
        int[] array = new int[values.size()];

        for (int i = 0; i < values.size(); i++) {
            array[i] = values.get(i);
        }

        return array;
    }
}
