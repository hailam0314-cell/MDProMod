package net.mdpromod;

import java.io.BufferedReader;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.stream.Stream;

/**
 * 解包器的离线自检：对着打好的正式 jar 跑，验证
 * ①清单里的每个文件都能解出来且字节数对得上；②再解一遍全部跳过（幂等）。
 *
 * <pre>
 * java -Dfile.encoding=UTF-8 -cp mod\build\libs\mdpromod-0.1.0.jar net.mdpromod.ModBundleSelfTest [目录]
 * </pre>
 */
public final class ModBundleSelfTest {
    private static int assertions;

    public static void main(String[] args) throws Exception {
        Path target = args.length > 0 ? Path.of(args[0])
                : Files.createTempDirectory("mdpromod-bundle-selftest");
        System.out.println("目标目录：" + target);

        check(ModBundle.bundled(), "这个构建带着包内资源（manifest 在 jar 里）");
        check(!ModBundle.ready() || ModBundle.state() == ModBundle.State.IDLE, "还没开始解包");

        long started = System.currentTimeMillis();
        ModBundle.start(true, target);
        waitForFinish();
        long seconds = (System.currentTimeMillis() - started) / 1000;
        check(ModBundle.ready(), "解包完成：" + ModBundle.detail() + "（首次 " + seconds + " 秒）");

        // 逐条对账：清单里的每个文件都要在，且大小一致
        int checked = 0;
        int wrongSize = 0;
        int absent = 0;
        for (String line : manifestLines()) {
            int tab = line.lastIndexOf('\t');
            if (tab <= 0) continue;
            Path file = target.resolve(line.substring(0, tab));
            long size = Long.parseLong(line.substring(tab + 1));
            checked++;
            if (!Files.isRegularFile(file)) absent++;
            else if (Files.size(file) != size) wrongSize++;
        }
        check(checked > 20000, "清单条目数 " + checked);
        check(absent == 0, "清单里的文件都解出来了（缺 " + absent + "）");
        check(wrongSize == 0, "字节数都对得上（不一致 " + wrongSize + "）");

        // 抽查几个关键文件：宿主、卡库、脚本、卡图、立绘
        check(Files.isRegularFile(target.resolve("duelhost").resolve(ModPaths.HOST_EXE)), "自包含宿主已解出");
        check(Files.isRegularFile(target.resolve("runtime").resolve("cards.cdb")), "卡库 cards.cdb 已解出");
        check(Files.isRegularFile(target.resolve("runtime").resolve("Data").resolve("script.zip")), "Lua 脚本已解出");
        check(count(target.resolve("runtime").resolve("Data").resolve("Windbot").resolve("Decks")) >= 50,
                "人机卡组 " + count(target.resolve("runtime").resolve("Data").resolve("Windbot").resolve("Decks")) + " 副");
        long art = count(target.resolve("art"));
        long closeup = count(target.resolve("closeup"));
        check(art > 10000, "卡图 " + art + " 张");
        check(closeup > 10000, "立绘 " + closeup + " 张");

        // 幂等：再解一遍应该一个都不复制
        ModBundle.resetForTest();
        long again = System.currentTimeMillis();
        ModBundle.start(true, target);
        waitForFinish();
        check(ModBundle.detail().startsWith("复制 0 个"),
                "第二次解包全部跳过：" + ModBundle.detail() + "（" + (System.currentTimeMillis() - again) / 1000 + " 秒）");

        // 少一个文件也能补回来
        Path victim = target.resolve("art").resolve("483.jpg");
        if (Files.deleteIfExists(victim)) {
            ModBundle.resetForTest();
            ModBundle.start(true, target);
            waitForFinish();
            check(Files.isRegularFile(victim) && ModBundle.detail().contains("复制 1 个"),
                    "删掉的文件会补回来：" + ModBundle.detail());
        }

        System.out.println("ModBundleSelfTest passed " + assertions + " assertions");
    }

    private static void waitForFinish() throws InterruptedException {
        for (int i = 0; i < 3600 && ModBundle.state() == ModBundle.State.RUNNING; i++) {
            Thread.sleep(100);
        }
    }

    private static Iterable<String> manifestLines() throws Exception {
        try (InputStream raw = ModBundleSelfTest.class
                .getResourceAsStream("/" + ModBundle.RESOURCE_ROOT + "/manifest.txt")) {
            if (raw == null) throw new IllegalStateException("jar 里没有清单");
            BufferedReader reader = new BufferedReader(new InputStreamReader(raw, StandardCharsets.UTF_8));
            return reader.lines().filter(text -> !text.isBlank() && !text.startsWith("#")).toList();
        }
    }

    private static long count(Path directory) throws Exception {
        if (!Files.isDirectory(directory)) return 0;
        try (Stream<Path> files = Files.list(directory)) {
            return files.count();
        }
    }

    private static void check(boolean condition, String what) {
        assertions++;
        if (!condition) throw new AssertionError("失败：" + what);
        System.out.println("  ok  " + what);
    }
}
