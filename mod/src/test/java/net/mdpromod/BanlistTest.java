package net.mdpromod;

import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;
import java.util.Set;

/**
 * 禁卡表（lflist 解析 + banlist.conf 开关 + 卡组校验）的回归测试。
 *
 * <p>用临时目录当运行时根，把两张配置写进去跑真实的 {@link DeckRules} 路径——
 * 也就是"配置文件打开哪张卡表，组建卡组就受哪张管"这条链路。
 */
public final class BanlistTest {
    private static int assertions;

    public static void main(String[] args) throws Exception {
        parsingLflist();
        parsingConfig();
        templateListsEveryTable();
        mergingTakesStrictest();
        whitelistTables();
        deckBuildingFollowsConfig();
        reloadAfterConfigEdit();
        configMovesOutOfRuntimeRoot();
        configFolderCanCarryItsOwnLflist();
        aliasGrouping();
        realLflistIfPresent();
        System.out.println("BanlistTest passed " + assertions + " assertions");
    }

    /**
     * 顺带验一遍运行时根目录里那份真 lflist.conf（机器上没有就跳过）：
     * 解析得动、第一张表能生效、能说清是哪张表管的。
     */
    private static void realLflistIfPresent() {
        Path data = ModPaths.runtimeRoot().resolve(BanlistFile.DATA_FILE);
        if (!Files.isRegularFile(data)) {
            System.out.println("（跳过真 lflist 检查：" + data + " 不存在）");
            return;
        }
        long started = System.currentTimeMillis();
        List<Banlists.Table> tables;
        try {
            tables = Banlists.parse(readString(data));
        } catch (Exception ex) {
            throw new AssertionError("解析真 lflist 失败：" + ex);
        }
        long elapsed = System.currentTimeMillis() - started;
        check(tables.size() > 10, "真 lflist 里有多张表：" + tables.size());
        Banlists.Table newest = tables.get(0);
        check(newest.name().equals("2026.10"), "第一张是最新的表：" + newest.name());
        check(!newest.whitelistOnly(), "最新的表不是白名单模式");
        long banned = newest.limits().values().stream().filter(value -> value == 0).count();
        long limited = newest.limits().values().stream().filter(value -> value == 1).count();
        check(banned > 10 && limited > 10, "最新的表有禁止/限制卡：" + banned + "/" + limited);
        Banlists.Active active = Banlists.merge(tables, Set.of(newest.name()));
        int bannedCode = newest.limits().entrySet().stream()
                .filter(entry -> entry.getValue() == 0).findFirst().orElseThrow().getKey();
        check(active.limitOf(bannedCode) == 0, "启用后这张卡就是禁止卡");
        check(active.reason(bannedCode).equals(newest.name() + " 禁止"),
                "说明是哪张表管的：" + active.reason(bannedCode));
        check(active.limitOf(70000000) == 3, "表里没提的卡不限制");
        System.out.println("  真 lflist：" + tables.size() + " 张表，最新「" + newest.name() + "」禁止 "
                + banned + " / 限制 " + limited + "，解析 " + elapsed + " ms");
    }

    private static void parsingLflist() {
        List<Banlists.Table> tables = Banlists.parse(SAMPLE);
        check(tables.size() == 3, "三张卡表");
        check(tables.get(0).name().equals("2026.10") && tables.get(1).name().equals("旧表"),
                "表名去掉 ! 并保持顺序：" + tables.get(0).name());
        check(tables.get(0).limits().get(53582587) == 0, "0 = 禁止");
        check(tables.get(0).limits().get(53129443) == 1, "1 = 限制");
        check(tables.get(0).limits().get(27243130) == 2, "2 = 准限制");
        check(tables.get(0).limits().size() == 3, "注释行/表头不算条目：" + tables.get(0).limits().size());
        check(!tables.get(0).whitelistOnly() && tables.get(1).whitelistOnly()
                && !tables.get(2).whitelistOnly(), "$whitelist 只影响自己那段");
        check(Banlists.parse("").isEmpty() && Banlists.parse(null).isEmpty(), "空数据不炸");
        check(Banlists.parse("!只有名字\n").size() == 1, "只有表头也算一张空表");
        check(Banlists.limitName(0).equals("禁止") && Banlists.limitName(2).equals("准限制"),
                "上限的说法");
    }

    private static void parsingConfig() {
        Set<String> enabled = Banlists.parseConfig(CONFIG);
        check(enabled.size() == 2, "只认打开的行：" + enabled);
        check(enabled.contains("2026.10") && enabled.contains("旧表"), "两种写法都认（on / 1）");
        check(!enabled.contains("2026.7"), "off 的不算");
        check(Banlists.parseConfig("2026.10\n").contains("2026.10"), "没写开关的整行当卡表名");
        check(Banlists.parseConfig("启用 \"2026.10\"\n").contains("2026.10"), "带引号也认");
        check(Banlists.parseConfig("on 打开表\n").contains("打开表"), "名字里有空格也能读全");
        check(Banlists.parseConfig("# on 2026.10\n; off 旧表\n").isEmpty(), "注释行不算");
        check(Banlists.parseConfig("onn 2026.10\n").contains("onn 2026.10"),
                "开关拼错时整行当表名（匹配不上就等于没开，不会误开别的表）");
        check(Banlists.parseConfig("on 2026.10\non 2026.10\n").size() == 1, "重复的只留一次");
    }

    private static void templateListsEveryTable() {
        String text = Banlists.template(List.of("2026.10", "2026.9 TCG", "旧表"), Set.of("2026.10"));
        check(text.contains("on  2026.10"), "启用那张写成 on");
        check(text.contains("off 2026.9 TCG") && text.contains("off 旧表"), "其余都是 off");
        check(text.contains("一共 3 个卡表"), "文件里说明有几张表");
        check(Banlists.parseConfig(text).equals(Set.of("2026.10")), "生成的文件再读回来还是同一张表");
    }

    private static void mergingTakesStrictest() {
        Banlists.Active active = Banlists.merge(Banlists.parse(SAMPLE), Set.of("2026.10", "合并表"));
        check(active.limitOf(53582587) == 0, "两边都有时取更严的（禁止）");
        check(active.limitOf(53129443) == 1, "一边有限制就用限制");
        check(active.limitOf(11111111) == 1, "只在一张表里的卡也认");
        check(active.limitOf(89999999) == 3, "都没提的卡不限制（开的表里没有白名单）");
        check(active.reason(53582587).equals("2026.10 禁止"), "说明是哪张表管的：" + active.reason(53582587));
        check(active.summary().equals("2026.10 ＋ 合并表"), "状态行：" + active.summary());
        Banlists.Active none = Banlists.merge(Banlists.parse(SAMPLE), Set.of());
        check(none.isEmpty() && none.summary().equals("未启用"), "一张都没开");
        check(none.limitOf(53582587) == 3 && none.reason(53582587).isEmpty(), "没开表就不限制");
        check(Banlists.merge(Banlists.parse(SAMPLE), Set.of("表名大小写")).isEmpty(), "匹配不上的表名等于没开");
        check(Banlists.merge(Banlists.parse(SAMPLE), Set.of("合并表")).limitOf(53582587) == 2,
                "只开一张表时按它算");
        check(Banlists.merge(Banlists.parse(SAMPLE), Set.of("旧表", "2026.10")).limitOf(11111111) == 0,
                "白名单模式开着时，没列出来的卡一律不能用");
    }

    private static void whitelistTables() {
        Banlists.Active active = Banlists.merge(Banlists.parse(SAMPLE), Set.of("旧表"));
        check(active.limitOf(53129443) == 1, "白名单里列出来的按写的算");
        check(active.limitOf(53582587) == 0, "白名单模式：没列出来的不能用");
        check(active.reason(53582587).equals("旧表 允许列表之外"), "白名单的说明：" + active.reason(53582587));
    }

    private static void deckBuildingFollowsConfig() throws Exception {
        Path root = Files.createTempDirectory("mdpromod-banlist");
        Files.writeString(root.resolve(BanlistFile.DATA_FILE), SAMPLE, StandardCharsets.UTF_8);
        writeConfig(root, "on 2026.10\n");
        withRoot(root, () -> {
            DeckData deck = deck(40, 53582587, 53129443, 53129443);
            check(DeckRules.limitOf(53582587) == 0, "生效的表说这张卡是禁止卡");
            check(DeckRules.limitOf(53129443) == 1, "生效的表说这张卡限 1");
            check(DeckRules.limitOf(89999999) == 3, "没提的卡不受影响");
            check(DeckRules.banlistStatus().equals("禁卡表：2026.10"), "卡盒标题上的状态：" + DeckRules.banlistStatus());
            check(DeckRules.limitText(53129443).equals("限制 1（2026.10 限制）"), "悬停那句：" + DeckRules.limitText(53129443));

            List<String> problems = DeckRules.problems(deck);
            check(problems.stream().anyMatch(text -> text.contains("是禁止卡") && text.contains("2026.10")),
                    "禁止卡被拦下来：" + problems);
            check(problems.stream().anyMatch(text -> text.contains("最多 1 张")),
                    "超张数被拦下来：" + problems);
        });

        // 把开关关掉：同一副卡组立刻恢复合法
        writeConfig(root, "off 2026.10\n");
        withRoot(root, () -> {
            BanlistFile.invalidate();
            DeckData deck = deck(40, 53582587, 53129443, 53129443);
            check(DeckRules.banlistStatus().equals("禁卡表：未启用"), "关掉后状态行变了：" + DeckRules.banlistStatus());
            check(DeckRules.limitOf(53582587) == 3, "关掉后不限制");
            check(DeckRules.problems(deck).isEmpty(), "关掉后卡组没问题：" + DeckRules.problems(deck));
        });
        deleteQuietly(root);
    }

    /** 配置里没写过 banlist.conf 时：自动生成一份，默认打开最新的那张表。 */
    private static void reloadAfterConfigEdit() throws Exception {
        Path root = Files.createTempDirectory("mdpromod-banlist-fresh");
        Files.writeString(root.resolve(BanlistFile.DATA_FILE), SAMPLE, StandardCharsets.UTF_8);
        withRoot(root, () -> {
            BanlistFile.invalidate();
            check(DeckRules.limitOf(53582587) == 0, "没有配置文件时默认打开最新的表");
            Path config = configFile(root);
            check(Files.isRegularFile(config), "顺手在 config 目录生成了 " + config);
            String text = readString(config);
            check(text.contains("on  2026.10") && text.contains("off 旧表"), "生成的内容：第一张开、其余关");
        });
        deleteQuietly(root);
    }

    /** 老位置的开关文件（运行时根目录）要自动搬到 config 目录，内容不能丢。 */
    private static void configMovesOutOfRuntimeRoot() throws Exception {
        Path root = Files.createTempDirectory("mdpromod-banlist-legacy");
        Files.writeString(root.resolve(BanlistFile.DATA_FILE), SAMPLE, StandardCharsets.UTF_8);
        Files.writeString(root.resolve(BanlistFile.CONFIG_FILE), "on 合并表\n", StandardCharsets.UTF_8);
        withRoot(root, () -> {
            check(DeckRules.banlists().summary().equals("合并表"),
                    "旧位置的开关照样生效：" + DeckRules.banlists().summary());
            check(Files.isRegularFile(configFile(root)), "搬到了 config 目录：" + configFile(root));
            check(!Files.isRegularFile(root.resolve(BanlistFile.CONFIG_FILE)), "旧位置不再留一份");
            check(readString(configFile(root)).contains("on"), "搬过去的内容没丢：" + readString(configFile(root)));
        });
        deleteQuietly(root);
    }

    /** config 目录里自带一份 lflist.conf 时以它为准（玩家可以放自己的卡表）。 */
    private static void configFolderCanCarryItsOwnLflist() throws Exception {
        Path root = Files.createTempDirectory("mdpromod-banlist-custom");
        Files.writeString(root.resolve(BanlistFile.DATA_FILE), SAMPLE, StandardCharsets.UTF_8);
        Path folder = configFolder(root);
        Files.createDirectories(folder);
        Files.writeString(folder.resolve(BanlistFile.DATA_FILE), """
                !自己的表
                53582587 1 --自己这张只准放 1
                """, StandardCharsets.UTF_8);
        Files.writeString(folder.resolve(BanlistFile.CONFIG_FILE), "on 自己的表\n", StandardCharsets.UTF_8);
        withRoot(root, () -> {
            check(DeckRules.banlists().summary().equals("自己的表"),
                    "用 config 里那份表：" + DeckRules.banlists().summary());
            check(DeckRules.limitOf(53582587) == 1, "按自己那份表算");
            check(DeckRules.limitOf(53129443) == 3, "自己那份表里没提的卡不限制");
        });
        deleteQuietly(root);
    }

    // ------------------------------------------------------------ 工具

    /**
     * alias：同一张卡的不同版本要共用额度（和 MD 一样按 alias 记账）。
     * 需要卡库数据，所以离线跑测试时 classpath 上加了 gson 才会执行这几条断言。
     */
    private static void aliasGrouping() {
        int[] pair = aliasPair();
        if (pair == null) {
            System.out.println("（跳过 alias 分组断言：这次卡库只载入了 " + CardRegistry.size()
                    + " 张，原因：" + CardRegistry.loadError()
                    + "；把 gson-2.10.jar 加进 classpath 就能一起测）");
            return;
        }
        DeckData deck = new DeckData();
        deck.mainDeck().add(pair[0]);
        deck.mainDeck().add(pair[1]);
        deck.mainDeck().add(pair[0]);
        check(DeckRules.copies(deck, pair[0]) == 3,
                "不同版本算同一张卡：" + DeckRules.copies(deck, pair[0]));
        check(DeckRules.copies(deck, pair[1]) == 3, "换个版本问还是同一组");
        check(DeckRules.groupMateText(deck, pair[0]).contains("算同一张卡"),
                "提示会说清楚和谁算一张：" + DeckRules.groupMateText(deck, pair[0]));
    }

    /** 卡库里找一对 alias 关系（code, alias）；没有就返回 null。 */
    private static int[] aliasPair() {
        for (CardInfo info : CardRegistry.all()) {
            if (info.alias() != 0 && CardRegistry.contains(info.alias())) {
                return new int[]{info.code(), info.alias()};
            }
        }
        return null;
    }

    /** 游戏 config 目录（模组就是把它登记进来的那个）。 */
    private static Path configFolder(Path root) {
        return root.resolve("gameconfig").resolve(ModPaths.FOLDER);
    }

    /** 开关文件的最终位置：{@code <游戏config>/mdpromod/banlist.conf}。 */
    private static Path configFile(Path root) {
        return configFolder(root).resolve(BanlistFile.CONFIG_FILE);
    }

    private static void writeConfig(Path root, String text) throws Exception {
        Files.createDirectories(configFolder(root));
        Files.writeString(configFile(root), text, StandardCharsets.UTF_8);
    }

    /** 换运行时根目录和游戏 config 目录（模组就是靠这两个找 lflist.conf / banlist.conf）。 */
    private static void withRoot(Path root, Runnable body) {
        String previous = System.getProperty(ModPaths.ROOT_PROPERTY);
        String previousConfig = System.getProperty(ModPaths.CONFIG_PROPERTY);
        System.setProperty(ModPaths.ROOT_PROPERTY, root.toString());
        System.setProperty(ModPaths.CONFIG_PROPERTY, root.resolve("gameconfig").toString());
        BanlistFile.invalidate();
        try {
            body.run();
        } finally {
            if (previous == null) System.clearProperty(ModPaths.ROOT_PROPERTY);
            else System.setProperty(ModPaths.ROOT_PROPERTY, previous);
            if (previousConfig == null) System.clearProperty(ModPaths.CONFIG_PROPERTY);
            else System.setProperty(ModPaths.CONFIG_PROPERTY, previousConfig);
            BanlistFile.invalidate();
        }
    }

    /** 主卡组填满 40 张（其余用各不相同的卡号凑，免得踩到"每种最多 3 张"），再放指定的卡。 */
    private static DeckData deck(int size, int... extras) {
        DeckData deck = new DeckData();
        for (int code : extras) deck.mainDeck().add(code);
        int filler = 0;
        while (deck.mainDeck().size() < size) deck.mainDeck().add(70000000 + filler++);
        return deck;
    }

    private static String readString(Path path) {
        try {
            return Files.readString(path, StandardCharsets.UTF_8);
        } catch (Exception ex) {
            throw new AssertionError("读不了 " + path + "：" + ex);
        }
    }

    private static void deleteQuietly(Path root) {
        try (var files = Files.walk(root)) {
            files.sorted(java.util.Comparator.reverseOrder()).forEach(path -> {
                try {
                    Files.deleteIfExists(path);
                } catch (Exception ignored) {
                }
            });
        } catch (Exception ignored) {
        }
    }

    private static void check(boolean condition, String message) {
        assertions++;
        if (!condition) throw new AssertionError(message);
    }

    /** 两张表、带注释与白名单标记的一小段 lflist。 */
    private static final String SAMPLE = """
            # 表头注释
            !2026.10
            #forbidden
            53582587 0 --禁止的卡
            53129443 1 --限一的卡
            27243130 2 --准限的卡
            !旧表
            $whitelist
            53129443 1 --还是限一
            !合并表
            53582587 2 --这张更松
            11111111 1 --只在这张表里
            """;

    /** 开关文件：一张 on、一张 1、一张 off，外加注释。 */
    private static final String CONFIG = """
            # 禁卡表开关
            on  2026.10   # 这条开
            1 旧表
            off 2026.7
            """;
}

