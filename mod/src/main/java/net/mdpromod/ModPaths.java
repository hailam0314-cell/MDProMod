package net.mdpromod;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;
import java.util.Locale;
import java.util.stream.Stream;

final class ModPaths {
    static final String ROOT_PROPERTY = "mdpromod.runtimeRoot";
    static final String CONFIG_PROPERTY = "mdpromod.configDir";
    static final String DATA_PROPERTY = "mdpromod.dataDir";
    static final String DEFAULT_ROOT = "D:\\MDProMod\\tools\\ygo_tcp_probe\\staging";
    static final String FOLDER = "mdpromod";
    static final String HOST_EXE = "duel_host.exe";
    static final String DEFAULT_ART = "D:\\MDProMod\\runtime\\card_textures_176x256_q80";
    static final String DEFAULT_CLOSEUP = "D:\\MDProMod\\runtime\\closeup_128";
    static final String DEFAULT_SUMMON_FX = "D:\\MDProMod\\runtime\\summon_fx";
    static final String DEFAULT_SUMMON_MESH = "D:\\MDProMod\\runtime\\summon_mesh";
    static final String DEFAULT_DECKS = "D:\\MDProMod\\runtime\\decks";
    private static Path registeredConfigDir;
    private static Path registeredGameDir;

    private ModPaths() {
    }

    static Path runtimeRoot() {
        String property = System.getProperty("mdpromod.runtimeRoot");
        if (property != null && !property.isBlank()) {
            return Path.of(property);
        }

        if (ModBundle.bundled()) {
            Path extracted = bundleDir("runtime");
            if (extracted != null) {
                return extracted;
            }
        }

        return Path.of("D:\\MDProMod\\tools\\ygo_tcp_probe\\staging");
    }

    static Path dataRoot() {
        String property = System.getProperty("mdpromod.dataDir");
        if (property != null && !property.isBlank()) {
            return Path.of(property);
        } else {
            return registeredGameDir == null ? null : registeredGameDir.resolve("mdpromod");
        }
    }

    static Path bundleDir(String name) {
        Path root = dataRoot();
        return root == null ? null : root.resolve(name);
    }

    static Path resolveFile(String subdir, String legacyProperty, String legacyDefault, String name) {
        Path bundled = null;
        Path root = dataRoot();
        if (root != null) {
            bundled = root.resolve(subdir).resolve(name);
        }

        Path external = Path.of(System.getProperty(legacyProperty, legacyDefault), name);
        if (bundled != null && Files.isRegularFile(bundled)) {
            return bundled;
        } else if (Files.isRegularFile(external)) {
            return external;
        } else {
            return bundled != null && ModBundle.bundled() ? bundled : external;
        }
    }

    static Path bundledHostExe() {
        Path dir = bundleDir("duelhost");
        return dir == null ? null : dir.resolve("duel_host.exe");
    }

    static void applyBundledHost() {
        if (ModBundle.bundled()) {
            Path exe = bundledHostExe();
            if (exe != null) {
                if (System.getProperty("mdpromod.duelHost") == null) {
                    System.setProperty("mdpromod.duelHost", exe.toString());
                }

                if (System.getProperty("mdpromod.dotnet") == null) {
                    System.setProperty("mdpromod.dotnet", exe.toString());
                }
            }
        }
    }

    static Path deckDir() {
        return configDir().resolve("decks");
    }

    static void seedDeckDir() {
        Path target = deckDir();

        try {
            if (Files.isDirectory(target)) {
                try (Stream<Path> existing = Files.list(target)) {
                    if (existing.findAny().isPresent()) {
                        return;
                    }
                }
            }

            Files.createDirectories(target);
            int copied = 0;

            for (Path legacy : List.of(Path.of("D:\\MDProMod\\runtime\\decks"), Path.of("D:\\MDProMod\\tools\\ygo_tcp_probe\\staging"))) {
                if (Files.isDirectory(legacy)) {
                    try (Stream<Path> files = Files.list(legacy)) {
                        for (Path file : files.filter(p -> p.getFileName().toString().toLowerCase(Locale.ROOT).endsWith(".ydk")).toList()) {
                            Path destination = target.resolve(file.getFileName().toString());
                            if (!Files.exists(destination)) {
                                Files.copy(file, destination);
                                copied++;
                            }
                        }
                    }
                }
            }

            if (copied > 0) {
                ModLog.info("把 {} 个旧卡组复制到 {}", copied, target);
            }
        } catch (IOException error) {
            ModLog.warn("迁移旧卡组失败：{}", error.toString());
        }
    }

    static Path configDir() {
        String property = System.getProperty("mdpromod.configDir");
        if (property != null && !property.isBlank()) {
            return Path.of(property).resolve("mdpromod");
        } else {
            return registeredConfigDir != null ? registeredConfigDir.resolve("mdpromod") : runtimeRoot().resolve("config").resolve("mdpromod");
        }
    }

    static void useGameConfigDir(Path gameConfigDir) {
        registeredConfigDir = gameConfigDir;
    }

    static void useGameDir(Path gameDir) {
        registeredGameDir = gameDir;
    }
}
