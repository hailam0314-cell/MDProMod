package net.mdpromod;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.attribute.BasicFileAttributes;
import java.util.List;
import java.util.Set;

/**
 * 禁卡表的两张表：
 * <ul>
 *   <li>{@code lflist.conf} —— 卡表数据（ygopro 格式，只读）：放在 {@code config/mdpromod/}
 *       里就用它，否则用运行时根目录里那份；</li>
 *   <li>{@code banlist.conf} —— 开关配置，放在 {@code config/mdpromod/}，
 *       没有就按最新的卡表生成一份（旧位置里那份会自动搬过来）。</li>
 * </ul>
 *
 * <p>改文件不用重启：每 {@value #RELOAD_MS} 毫秒查一次两个文件的修改时间/大小，
 * 变了才重新解析（不然 676KB 的 lflist 每次渲染都解析一遍会卡）。
 */
final class BanlistFile {
    static final String DATA_FILE = "lflist.conf";
    static final String CONFIG_FILE = "banlist.conf";
    private static final long RELOAD_MS = 2000;

    private static Banlists.Active cached = new Banlists.Active(List.of());
    private static String cachedSignature = "";
    private static long checkedAt;

    private BanlistFile() {
    }

    /** 当前生效的禁卡表；配置改过（最多 2 秒后）会自动重读。 */
    static synchronized Banlists.Active active() {
        long now = System.currentTimeMillis();
        if (now - checkedAt < RELOAD_MS) return cached;
        checkedAt = now;
        cached = load(ModPaths.runtimeRoot());
        return cached;
    }

    /** 下一次 {@link #active()} 一定重新读（给测试和"改完立刻生效"用）。 */
    static synchronized void invalidate() {
        checkedAt = 0;
        cachedSignature = "";
    }

    private static Banlists.Active load(Path root) {
        Path configFolder = ModPaths.configDir();
        Path data = resolveData(root, configFolder);
        Path config = resolveConfig(root, configFolder);
        String signature = signature(data) + '|' + signature(config);
        if (signature.equals(cachedSignature)) return cached;   // 文件没动：沿用上次的结果

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
            enabled = names.isEmpty() ? Set.of() : Set.of(names.get(0));   // 默认打开最新那张
            try {
                if (config.getParent() != null) Files.createDirectories(config.getParent());
                Files.writeString(config, Banlists.template(names, enabled), StandardCharsets.UTF_8);
                ModLog.info("已生成禁卡表开关 {}（默认打开「{}」，其余都是 off）", config,
                        names.isEmpty() ? "无" : names.get(0));
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
        // 只在文件真的变了（或第一次读）时说一次，顺带把两个路径带上，方便找文件
        ModLog.info("生效的禁卡表：{}（开关 {}，数据 {}{}）", result.summary(), config, data,
                enabled.isEmpty() ? "，没有打开任何卡表" : "");
        return result;
    }

    /** 卡表数据：Config 目录里自带一份就用它，否则用运行时根目录的。 */
    private static Path resolveData(Path root, Path configFolder) {
        Path custom = configFolder.resolve(DATA_FILE);
        return Files.isRegularFile(custom) ? custom : root.resolve(DATA_FILE);
    }

    /**
     * 开关文件：新位置在 Config 目录。旧位置（运行时根目录）有一份而新位置还没有时，
     * 搬过去而不是丢在那儿假装还生效；搬不动（文件被占用之类）就继续就地读。
     */
    private static Path resolveConfig(Path root, Path configFolder) {
        Path target = configFolder.resolve(CONFIG_FILE);
        if (Files.isRegularFile(target)) return target;
        Path legacy = root.resolve(CONFIG_FILE);
        if (!Files.isRegularFile(legacy)) return target;
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

    /** 文件签名：路径 + 修改时间 + 大小；读不到就记成 missing。 */
    private static String signature(Path path) {
        try {
            BasicFileAttributes attributes = Files.readAttributes(path, BasicFileAttributes.class);
            return path.getFileName() + ":" + attributes.lastModifiedTime().toMillis()
                    + ":" + attributes.size();
        } catch (IOException ex) {
            return path.getFileName() + ":missing";
        }
    }
}
