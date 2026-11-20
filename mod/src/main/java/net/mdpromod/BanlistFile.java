package net.mdpromod;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.attribute.BasicFileAttributes;
import java.util.List;
import java.util.Set;

final class BanlistFile {
    static final String DATA_FILE = "lflist.conf";
    static final String CONFIG_FILE = "banlist.conf";
    private static final long RELOAD_MS = 2000L;
    private static Banlists.Active cached = new Banlists.Active(List.of());
    private static String cachedSignature = "";
    private static long checkedAt;

    private BanlistFile() {
    }

    static synchronized Banlists.Active active() {
        long now = System.currentTimeMillis();
        if (now - checkedAt < 2000L) {
            return cached;
        }

        checkedAt = now;
        cached = load(ModPaths.runtimeRoot());
        return cached;
    }

    static synchronized void invalidate() {
        checkedAt = 0L;
        cachedSignature = "";
    }

    private static Banlists.Active load(Path root) {
        Path configFolder = ModPaths.configDir();
        Path data = resolveData(root, configFolder);
        Path config = resolveConfig(root, configFolder);
        String signature = signature(data) + "|" + signature(config);
        if (signature.equals(cachedSignature)) {
            return cached;
        }

        List<Banlists.Table> tables;
        try {
            tables = Banlists.parse(Files.readString(data, StandardCharsets.UTF_8));
        } catch (IOException ex) {
            ModLog.warn("读不到禁卡表数据 {}（{}）：卡组将不限制张数", data, ex.toString());
            cachedSignature = signature;
            return new Banlists.Active(List.of());
        }

        List<String> names = tables.stream().map(Banlists.Table::name).toList();
        Set<String> enabled;
        if (!Files.isRegularFile(config)) {
            enabled = names.isEmpty() ? Set.of() : Set.of(names.get(0));

            try {
                if (config.getParent() != null) {
                    Files.createDirectories(config.getParent());
                }

                Files.writeString(config, Banlists.template(names, enabled), StandardCharsets.UTF_8);
                ModLog.info("已生成禁卡表开关 {}（默认打开「{}」，其余都是 off）", config, names.isEmpty() ? "无" : names.get(0));
            } catch (IOException ex) {
                ModLog.warn("写不了禁卡表开关 {}：{}", config, ex.toString());
            }
        } else {
            try {
                enabled = Banlists.parseConfig(Files.readString(config, StandardCharsets.UTF_8));
            } catch (IOException ex) {
                ModLog.warn("读不到禁卡表开关 {}：卡组将不限制张数", config);
                enabled = Set.of();
            }
        }

        Banlists.Active result = Banlists.merge(tables, enabled);
        cachedSignature = signature;
        ModLog.info("生效的禁卡表：{}（开关 {}，数据 {}{}）", result.summary(), config, data, enabled.isEmpty() ? "，没有打开任何卡表" : "");
        return result;
    }

    private static Path resolveData(Path root, Path configFolder) {
        Path custom = configFolder.resolve("lflist.conf");
        return Files.isRegularFile(custom) ? custom : root.resolve("lflist.conf");
    }

    private static Path resolveConfig(Path root, Path configFolder) {
        Path target = configFolder.resolve("banlist.conf");
        if (Files.isRegularFile(target)) {
            return target;
        }

        Path legacy = root.resolve("banlist.conf");
        if (!Files.isRegularFile(legacy)) {
            return target;
        }

        try {
            Files.createDirectories(configFolder);
            Files.move(legacy, target);
            ModLog.info("禁卡表开关已搬到 {}", target);
            return target;
        } catch (IOException ex) {
            ModLog.warn("搬不动 {}（{}）：先继续用旧位置", legacy, ex.toString());
            return legacy;
        }
    }

    private static String signature(Path path) {
        try {
            BasicFileAttributes attributes = Files.readAttributes(path, BasicFileAttributes.class);
            return path.getFileName() + ":" + attributes.lastModifiedTime().toMillis() + ":" + attributes.size();
        } catch (IOException ex) {
            return path.getFileName() + ":missing";
        }
    }
}
