package net.mdpromod;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;
import java.util.stream.Stream;

/**
 * 模组用到的两个目录：
 * <ul>
 *   <li><b>运行时根目录</b>：宿主的工作目录，cards.cdb / lflist.conf / 卡组都在这儿；</li>
 *   <li><b>配置目录</b>：游戏里的 {@code config/mdpromod}（由 {@code MDProMod} 启动时登记），
 *       玩家要改的东西放这里。</li>
 * </ul>
 * 单独一个类是为了让不依赖 Minecraft 的部分（禁卡表、人机卡组目录）也能取到路径——
 * 所以这里只存路径，不引用任何 FML 类型。
 */
final class ModPaths {
    static final String ROOT_PROPERTY = "mdpromod.runtimeRoot";
    static final String CONFIG_PROPERTY = "mdpromod.configDir";
    static final String DATA_PROPERTY = "mdpromod.dataDir";
    static final String DEFAULT_ROOT = "D:\\MDProMod\\tools\\ygo_tcp_probe\\staging";
    /** 配置目录下的子目录名：{@code config/mdpromod/}。 */
    static final String FOLDER = "mdpromod";
    /** 解包出来的自包含宿主（自带 .NET 运行时，所以不用另外装 dotnet）。 */
    static final String HOST_EXE = "duel_host.exe";
    /** 没有解包目录时（开发环境）卡图/立绘/特效的旧位置，可用 -D 覆盖。 */
    static final String DEFAULT_ART = "D:\\MDProMod\\runtime\\card_textures_176x256_q80";
    static final String DEFAULT_CLOSEUP = "D:\\MDProMod\\runtime\\closeup_128";
    static final String DEFAULT_SUMMON_FX = "D:\\MDProMod\\runtime\\summon_fx";
    static final String DEFAULT_SUMMON_MESH = "D:\\MDProMod\\runtime\\summon_mesh";
    static final String DEFAULT_DECKS = "D:\\MDProMod\\runtime\\decks";

    private static Path registeredConfigDir;
    private static Path registeredGameDir;

    private ModPaths() {
    }

    /**
     * 宿主的工作目录（cards.cdb / script.zip / lflist.conf / 人机卡组都在里面）。
     * 优先级：{@code -Dmdpromod.runtimeRoot} &gt; 包内解包目录 &gt; 开发用外置目录。
     */
    static Path runtimeRoot() {
        String property = System.getProperty(ROOT_PROPERTY);
        if (property != null && !property.isBlank()) return Path.of(property);
        if (ModBundle.bundled()) {
            Path extracted = bundleDir("runtime");
            if (extracted != null) return extracted;
        }
        return Path.of(DEFAULT_ROOT);
    }

    /**
     * 包内资源解包到的根目录（{@code <游戏目录>/mdpromod}）。
     * 离线测试（没登记游戏目录、也没设 {@code -Dmdpromod.dataDir}）时返回 null——
     * 那时所有路径都退回外置目录，测试照旧能跑。
     */
    static Path dataRoot() {
        String property = System.getProperty(DATA_PROPERTY);
        if (property != null && !property.isBlank()) return Path.of(property);
        return registeredGameDir == null ? null : registeredGameDir.resolve(FOLDER);
    }

    /** 解包目录下的一级子目录（art / closeup / summonfx / summonmesh / runtime / duelhost）。 */
    static Path bundleDir(String name) {
        Path root = dataRoot();
        return root == null ? null : root.resolve(name);
    }

    /**
     * 单个资源文件：解包目录优先，旧的外置目录兜底。
     * 两边都还没有时返回"这个构建该用的那个"（解包跑完之后再查一次就能读到）。
     */
    static Path resolveFile(String subdir, String legacyProperty, String legacyDefault, String name) {
        Path bundled = null;
        Path root = dataRoot();
        if (root != null) bundled = root.resolve(subdir).resolve(name);
        Path external = Path.of(System.getProperty(legacyProperty, legacyDefault), name);
        if (bundled != null && Files.isRegularFile(bundled)) return bundled;
        if (Files.isRegularFile(external)) return external;
        return bundled != null && ModBundle.bundled() ? bundled : external;
    }

    /** 解包出来的自包含宿主路径（可能还没解出来，所以不检查存在性）。 */
    static Path bundledHostExe() {
        Path dir = bundleDir("duelhost");
        return dir == null ? null : dir.resolve(HOST_EXE);
    }

    /**
     * 把自包含宿主登记进系统属性：{@code DuelSessionManager} 读
     * {@code mdpromod.duelHost} / {@code mdpromod.dotnet} 起进程。
     * 玩家自己用 -D 设过就不覆盖。
     *
     * <p>注：自包含的 exe 不需要 dotnet，这里把两个属性都指向它——宿主对多余的位置参数
     * 不敏感（{@code GetOption} 只认 {@code --root}），所以命令行里多一个路径无副作用。
     */
    static void applyBundledHost() {
        if (!ModBundle.bundled()) return;
        Path exe = bundledHostExe();
        if (exe == null) return;
        if (System.getProperty("mdpromod.duelHost") == null) {
            System.setProperty("mdpromod.duelHost", exe.toString());
        }
        if (System.getProperty("mdpromod.dotnet") == null) {
            System.setProperty("mdpromod.dotnet", exe.toString());
        }
    }

    /** 玩家自己导出的卡组：放 config 目录，别混在资源里（可改、可备份、删了不影响运行）。 */
    static Path deckDir() {
        return configDir().resolve("decks");
    }

    /**
     * 第一次跑时把旧卡组目录里的 {@code *.ydk} 拷一份到 {@link #deckDir()}，
     * 免得玩家觉得"卡组不见了"。目标目录里已经有卡组就不动。
     */
    static void seedDeckDir() {
        Path target = deckDir();
        try {
            if (Files.isDirectory(target)) {
                try (Stream<Path> existing = Files.list(target)) {
                    if (existing.findAny().isPresent()) return;
                }
            }
            Files.createDirectories(target);
            int copied = 0;
            for (Path legacy : List.of(Path.of(DEFAULT_DECKS), Path.of(DEFAULT_ROOT))) {
                if (!Files.isDirectory(legacy)) continue;
                try (Stream<Path> files = Files.list(legacy)) {
                    for (Path file : files.filter(p -> p.getFileName().toString()
                            .toLowerCase(java.util.Locale.ROOT).endsWith(".ydk")).toList()) {
                        Path destination = target.resolve(file.getFileName().toString());
                        if (Files.exists(destination)) continue;
                        Files.copy(file, destination);
                        copied++;
                    }
                }
            }
            if (copied > 0) ModLog.info("把 {} 个旧卡组复制到 {}", copied, target);
        } catch (IOException error) {
            ModLog.warn("迁移旧卡组失败：{}", error.toString());
        }
    }

    /**
     * 模组自己的配置目录（{@code <游戏config目录>/mdpromod}）。
     * 优先取系统属性 {@code mdpromod.configDir}（测试用），其次用启动时登记的目录，
     * 都没有就退回"运行时根目录下的 config"（离线跑测试时就是这个）。
     */
    static Path configDir() {
        String property = System.getProperty(CONFIG_PROPERTY);
        if (property != null && !property.isBlank()) return Path.of(property).resolve(FOLDER);
        if (registeredConfigDir != null) return registeredConfigDir.resolve(FOLDER);
        return runtimeRoot().resolve("config").resolve(FOLDER);
    }

    /** 游戏启动时把 {@code FMLPaths.CONFIGDIR} 登记进来（MDProMod 构造函数里调）。 */
    static void useGameConfigDir(Path gameConfigDir) {
        registeredConfigDir = gameConfigDir;
    }

    /** 游戏启动时把 {@code FMLPaths.GAMEDIR} 登记进来：包内资源解到它下面的 {@code mdpromod/}。 */
    static void useGameDir(Path gameDir) {
        registeredGameDir = gameDir;
    }
}
