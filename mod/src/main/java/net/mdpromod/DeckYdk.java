package net.mdpromod;

import java.io.BufferedWriter;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;

/** YDK 读写：不依赖 Minecraft 类，方便命令行与测试直接调用。 */
public final class DeckYdk {
    private DeckYdk() {
    }

    public static Path write(DeckData deck, Path directory) throws IOException {
        Files.createDirectories(directory);
        String fileName = (deck.name() == null || deck.name().isBlank() ? "deck" : deck.name())
                .replaceAll("[\\\\/:*?\"<>|]", "_") + ".ydk";
        Path path = directory.resolve(fileName);
        try (BufferedWriter writer = Files.newBufferedWriter(path, StandardCharsets.UTF_8)) {
            writer.write("#created by MDProMod");
            writer.newLine();
            writer.write("#main");
            writer.newLine();
            for (int code : deck.mainDeck()) {
                writer.write(Integer.toString(code));
                writer.newLine();
            }
            writer.write("#extra");
            writer.newLine();
            for (int code : deck.extraDeck()) {
                writer.write(Integer.toString(code));
                writer.newLine();
            }
            writer.write("!side");
            writer.newLine();
            for (int code : deck.sideDeck()) {
                writer.write(Integer.toString(code));
                writer.newLine();
            }
        }
        return path;
    }
}
