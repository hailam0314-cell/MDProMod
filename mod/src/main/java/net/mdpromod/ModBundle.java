package net.mdpromod;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import java.util.Set;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicLong;

/**
 * 包内资源（卡图 / 立绘 / 召唤特效 / 决斗宿主运行时）的首次解包。
 *
 * <p>打包时这些文件被塞进 jar 的 {@code data/mdpromod/bundle/}（清单见同目录的
 * {@code manifest.txt}：每行 {@code 相对路径<TAB>字节数}）。启动时在后台线程把它们解到
 * {@code <游戏目录>/mdpromod/}，玩家就不需要另外装 MDPro3、也不需要外置资源目录。
 *
 * <p>解包是**幂等**的：目标文件已存在且字节数对得上就跳过，所以升级模组只会补差异，
 * 中断了再启动一次也会接着补齐。文件被误删也会自动补回来。
 *
 * <p>这里刻意不引用任何 Minecraft 类型：离线测试可以直接对着 jar 跑。
 */
final class ModBundle {
    /** jar 内的资源根目录。 */
    static final String RESOURCE_ROOT = "data/mdpromod/bundle";
    private static final String MANIFEST = "/" + RESOURCE_ROOT + "/manifest.txt";
    /** 只有客户端用得上的目录；其余（宿主运行时）单机和服务器都要。 */
    private static final Set<String> CLIENT_ONLY = Set.of("art/", "closeup/", "summonfx/", "summonmesh/");

    enum State {
        /** 还没解包（或这个构建没带资源）。 */
        IDLE,
        RUNNING,
        DONE,
        FAILED
    }

    /** 清单里的一条：相对路径 + 字节数。 */
    private record Entry(String path, long size) {
    }

    private static final Object LOCK = new Object();
    private static volatile State state = State.IDLE;
    private static volatile String detail = "";
    private static volatile long startedAt;
    private static volatile Boolean bundled;
    private static final AtomicInteger doneFiles = new AtomicInteger();
    private static final AtomicLong doneBytes = new AtomicLong();
    private static final AtomicInteger allFiles = new AtomicInteger();
    private static final AtomicLong allBytes = new AtomicLong();

    private ModBundle() {
    }

    /** 这个构建里有没有带资源包（开发环境跑 runClient 时没有，那时直接用外置目录）。 */
    static boolean bundled() {
        Boolean known = bundled;
        if (known == null) {
            known = ModBundle.class.getResource(MANIFEST) != null;
            bundled = known;
        }
        return known;
    }

    static State state() {
        return state;
    }

    static String detail() {
        return detail;
    }

    /** 宿主和卡图能不能用了：解包完成，或者这个构建根本没带资源（用外置目录）。 */
    static boolean ready() {
        return state == State.DONE || state == State.IDLE && !bundled();
    }

    /** 人话进度，给日志和界面用；没在解包时返回空串。 */
    static String progressText() {
        if (state != State.RUNNING) return "";
        int files = doneFiles.get();
        int total = allFiles.get();
        double mb = doneBytes.get() / 1024.0 / 1024.0;
        double totalMb = allBytes.get() / 1024.0 / 1024.0;
        return String.format(Locale.ROOT, "%d/%d 个文件（%.0f/%.0f MB）", files, total, mb, totalMb);
    }

    /**
     * 开始解包（重复调用只生效一次）。
     *
     * @param client   客户端要多解一套卡图/立绘/特效；专用服务器只解宿主运行时
     * @param dataRoot 目标目录（{@code <游戏目录>/mdpromod}）；null 表示不在游戏里，直接不管
     */
    static void start(boolean client, Path dataRoot) {
        if (dataRoot == null) return;
        synchronized (LOCK) {
            if (state != State.IDLE) return;
            if (!bundled()) {
                state = State.DONE;
                detail = "这个构建没带包内资源";
                return;
            }
            state = State.RUNNING;
        }
        Thread worker = new Thread(() -> run(client, dataRoot), "mdpromod-unpack");
        worker.setDaemon(true);
        worker.start();
    }

    /** 自检用：把状态清回 IDLE，好让同一个进程里再解一遍（生产代码不调用）。 */
    static synchronized void resetForTest() {
        if (state == State.RUNNING) throw new IllegalStateException("还在解包，别重置");
        state = State.IDLE;
        detail = "";
        doneFiles.set(0);
        doneBytes.set(0);
    }

    private static void run(boolean client, Path dataRoot) {
        startedAt = System.currentTimeMillis();
        try {
            List<Entry> entries = readManifest(client);
            long totalBytes = 0;
            for (Entry entry : entries) totalBytes += entry.size;
            allFiles.set(entries.size());
            allBytes.set(totalBytes);
            ModLog.info("开始解包资源：{} 个文件 / {} MB → {}", entries.size(),
                    Math.round(totalBytes / 1024.0 / 1024.0), dataRoot);
            Files.createDirectories(dataRoot);

            int copied = 0;
            int skipped = 0;
            for (Entry entry : entries) {
                Path target = dataRoot.resolve(entry.path);
                if (Files.isRegularFile(target) && Files.size(target) == entry.size) {
                    skipped++;
                } else {
                    copy(entry, target);
                    copied++;
                }
                doneFiles.incrementAndGet();
                doneBytes.addAndGet(entry.size);
            }
            long seconds = (System.currentTimeMillis() - startedAt) / 1000;
            detail = String.format(Locale.ROOT, "复制 %d 个、跳过 %d 个", copied, skipped);
            state = State.DONE;
            ModLog.info("资源解包完成：{}（用时 {} 秒）→ {}", detail, seconds, dataRoot);
        } catch (Throwable error) {
            state = State.FAILED;
            detail = String.valueOf(error);
            ModLog.warn("资源解包失败：{}（目标目录 {}）", error, dataRoot);
        }
    }

    private static void copy(Entry entry, Path target) throws IOException {
        try (InputStream in = ModBundle.class.getResourceAsStream("/" + RESOURCE_ROOT + "/" + entry.path)) {
            if (in == null) throw new IOException("包里没有 " + entry.path);
            Path parent = target.getParent();
            if (parent != null) Files.createDirectories(parent);
            Path temporary = parent == null ? target : parent.resolve(target.getFileName() + ".part");
            Files.copy(in, temporary, StandardCopyOption.REPLACE_EXISTING);
            Files.move(temporary, target, StandardCopyOption.REPLACE_EXISTING);
        }
    }

    private static List<Entry> readManifest(boolean client) throws IOException {
        List<Entry> entries = new ArrayList<>();
        try (InputStream raw = ModBundle.class.getResourceAsStream(MANIFEST)) {
            if (raw == null) throw new IOException("包里没有 " + MANIFEST);
            BufferedReader reader = new BufferedReader(new InputStreamReader(raw, StandardCharsets.UTF_8));
            String line;
            while ((line = reader.readLine()) != null) {
                String text = line.trim();
                if (text.isEmpty() || text.startsWith("#")) continue;
                int tab = text.lastIndexOf('\t');
                if (tab <= 0) continue;
                String path = text.substring(0, tab).trim();
                long size;
                try {
                    size = Long.parseLong(text.substring(tab + 1).trim());
                } catch (NumberFormatException ignored) {
                    continue;
                }
                if (!client && isClientOnly(path)) continue;
                entries.add(new Entry(path, size));
            }
        }
        return entries;
    }

    private static boolean isClientOnly(String path) {
        for (String prefix : CLIENT_ONLY) {
            if (path.startsWith(prefix)) return true;
        }
        return false;
    }
}
