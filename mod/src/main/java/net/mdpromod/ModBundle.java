package net.mdpromod;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HexFormat;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Set;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicLong;

final class ModBundle {
    static final String RESOURCE_ROOT = "data/mdpromod/bundle";
    private static final String MANIFEST = "/data/mdpromod/bundle/manifest.txt";
    private static final String STAMP = ".bundle-stamp";
    private static final Set<String> CLIENT_ONLY = Set.of("art/", "closeup/", "summonfx/", "summonmesh/");
    private static final Object LOCK = new Object();
    private static volatile ModBundle.State state = ModBundle.State.IDLE;
    private static volatile String detail = "";
    private static volatile long startedAt;
    private static volatile Boolean bundled;
    private static final AtomicInteger doneFiles = new AtomicInteger();
    private static final AtomicLong doneBytes = new AtomicLong();
    private static final AtomicInteger allFiles = new AtomicInteger();
    private static final AtomicLong allBytes = new AtomicLong();

    private ModBundle() {
    }

    static boolean bundled() {
        Boolean known = bundled;
        if (known == null) {
            known = ModBundle.class.getResource("/data/mdpromod/bundle/manifest.txt") != null;
            bundled = known;
        }

        return known;
    }

    static ModBundle.State state() {
        return state;
    }

    static String detail() {
        return detail;
    }

    static boolean ready() {
        return state == ModBundle.State.DONE || state == ModBundle.State.IDLE && !bundled();
    }

    static String progressText() {
        if (state != ModBundle.State.RUNNING) {
            return "";
        }

        int files = doneFiles.get();
        int total = allFiles.get();
        double mb = doneBytes.get() / 1024.0 / 1024.0;
        double totalMb = allBytes.get() / 1024.0 / 1024.0;
        return String.format(Locale.ROOT, "%d/%d 个文件（%.0f/%.0f MB）", files, total, mb, totalMb);
    }

    static void start(boolean client, Path dataRoot) {
        if (dataRoot != null) {
            synchronized (LOCK) {
                if (state != ModBundle.State.IDLE) {
                    return;
                }

                if (!bundled()) {
                    state = ModBundle.State.DONE;
                    detail = "这个构建没带包内资源";
                    return;
                }

                state = ModBundle.State.RUNNING;
            }

            Thread worker = new Thread(() -> run(client, dataRoot), "mdpromod-unpack");
            worker.setDaemon(true);
            worker.start();
        }
    }

    static synchronized void resetForTest() {
        if (state == ModBundle.State.RUNNING) {
            throw new IllegalStateException("还在解包，别重置");
        }

        state = ModBundle.State.IDLE;
        detail = "";
        doneFiles.set(0);
        doneBytes.set(0L);
    }

    private static void run(boolean client, Path dataRoot) {
        startedAt = System.currentTimeMillis();

        try {
            List<ModBundle.Entry> entries = readManifest(client);
            String digest = manifestDigest();
            ModBundle.StampFile previous = readStamp(dataRoot);
            boolean fast = !digest.isEmpty() && digest.equals(previous.digest());
            long totalBytes = 0L;

            for (ModBundle.Entry entry : entries) {
                totalBytes += entry.size;
            }

            allFiles.set(entries.size());
            allBytes.set(totalBytes);
            ModLog.info("开始校验资源：{} 个文件 / {} MB → {}（{}）", entries.size(), Math.round(totalBytes / 1024.0 / 1024.0), dataRoot, fast ? "清单未变，只查改动" : "清单变了，逐文件核对");
            Files.createDirectories(dataRoot);
            int copied = 0;
            int skipped = 0;

            for (ModBundle.Entry entry : entries) {
                Path target = dataRoot.resolve(entry.path);
                boolean unchanged = false;
                if (fast) {
                    ModBundle.Stamp stamp = previous.files().get(entry.path);
                    if (stamp != null
                        && stamp.size() == entry.size
                        && Files.isRegularFile(target)
                        && Files.size(target) == entry.size
                        && Files.getLastModifiedTime(target).toMillis() == stamp.modified()) {
                        unchanged = true;
                    }
                }

                if (!unchanged && matches(target, entry.size, entry.sha256)) {
                    unchanged = true;
                }

                if (unchanged) {
                    skipped++;
                } else {
                    copy(entry, target);
                    copied++;
                }

                doneFiles.incrementAndGet();
                doneBytes.addAndGet(entry.size);
            }

            writeStamp(dataRoot, digest, entries);
            long seconds = (System.currentTimeMillis() - startedAt) / 1000L;
            detail = String.format(Locale.ROOT, "复制 %d 个、跳过 %d 个", copied, skipped);
            state = ModBundle.State.DONE;
            ModLog.info("资源校验完成：{}（用时 {} 秒）→ {}", detail, seconds, dataRoot);
        } catch (Throwable error) {
            state = ModBundle.State.FAILED;
            detail = String.valueOf(error);
            ModLog.warn("资源解包失败：{}（目标目录 {}）", error, dataRoot);
        }
    }

    private static String manifestDigest() throws IOException {
        try (InputStream in = ModBundle.class.getResourceAsStream("/data/mdpromod/bundle/manifest.txt")) {
            if (in == null) {
                return "";
            }

            try {
                MessageDigest digest = MessageDigest.getInstance("SHA-256");
                byte[] buffer = new byte[65536];

                int read;
                while ((read = in.read(buffer)) != -1) {
                    digest.update(buffer, 0, read);
                }

                return HexFormat.of().formatHex(digest.digest());
            } catch (NoSuchAlgorithmException ex) {
                throw new IllegalStateException(ex);
            }
        }
    }

    private static ModBundle.StampFile readStamp(Path dataRoot) {
        Path path = dataRoot.resolve(".bundle-stamp");
        if (!Files.isRegularFile(path)) {
            return new ModBundle.StampFile("", Map.of());
        }

        try {
            List<String> lines = Files.readAllLines(path, StandardCharsets.UTF_8);
            if (lines.isEmpty()) {
                return new ModBundle.StampFile("", Map.of());
            }

            Map<String, ModBundle.Stamp> files = new HashMap<>();

            for (int i = 1; i < lines.size(); i++) {
                String[] columns = lines.get(i).split("\t", -1);
                if (columns.length >= 3) {
                    try {
                        files.put(columns[0], new ModBundle.Stamp(Long.parseLong(columns[1]), Long.parseLong(columns[2])));
                    } catch (NumberFormatException var7) {
                    }
                }
            }

            return new ModBundle.StampFile(lines.get(0).trim(), files);
        } catch (IOException ex) {
            ModLog.warn("读资源快照失败，改为逐文件核对：{}", ex.toString());
            return new ModBundle.StampFile("", Map.of());
        }
    }

    private static void writeStamp(Path dataRoot, String digest, List<ModBundle.Entry> entries) {
        if (!digest.isEmpty()) {
            Path path = dataRoot.resolve(".bundle-stamp");
            StringBuilder text = new StringBuilder(digest).append('\n');

            for (ModBundle.Entry entry : entries) {
                Path target = dataRoot.resolve(entry.path);

                try {
                    if (Files.isRegularFile(target) && Files.size(target) == entry.size) {
                        text.append(entry.path).append('\t').append(entry.size).append('\t').append(Files.getLastModifiedTime(target).toMillis()).append('\n');
                    }
                } catch (IOException var10) {
                }
            }

            try {
                Path temporary = dataRoot.resolve(".bundle-stamp.part");
                Files.writeString(temporary, text, StandardCharsets.UTF_8);
                Files.move(temporary, path, StandardCopyOption.REPLACE_EXISTING);
            } catch (IOException ex) {
                ModLog.warn("写资源快照失败（下次会重新核对）：{}", ex.toString());
            }
        }
    }

    private static void copy(ModBundle.Entry entry, Path target) throws IOException {
        try (InputStream in = ModBundle.class.getResourceAsStream("/data/mdpromod/bundle/" + entry.path)) {
            if (in == null) {
                throw new IOException("包里没有 " + entry.path);
            }

            Path parent = target.getParent();
            if (parent != null) {
                Files.createDirectories(parent);
            }

            Path temporary = parent == null ? target : parent.resolve(target.getFileName() + ".part");
            Files.copy(in, temporary, StandardCopyOption.REPLACE_EXISTING);
            Files.move(temporary, target, StandardCopyOption.REPLACE_EXISTING);
        }
    }

    private static List<ModBundle.Entry> readManifest(boolean client) throws IOException {
        List<ModBundle.Entry> entries = new ArrayList<>();

        try (InputStream raw = ModBundle.class.getResourceAsStream("/data/mdpromod/bundle/manifest.txt")) {
            if (raw == null) {
                throw new IOException("包里没有 /data/mdpromod/bundle/manifest.txt");
            }

            BufferedReader reader = new BufferedReader(new InputStreamReader(raw, StandardCharsets.UTF_8));

            while (true) {
                String[] columns;
                String path;
                long size;
                while (true) {
                    String line;
                    if ((line = reader.readLine()) == null) {
                        return entries;
                    }

                    String text = line.trim();
                    if (!text.isEmpty() && !text.startsWith("#")) {
                        columns = text.split("\t", -1);
                        if (columns.length >= 2) {
                            path = columns[0].trim();

                            try {
                                size = Long.parseLong(columns[1].trim());
                                break;
                            } catch (NumberFormatException ignored) {
                            }
                        }
                    }
                }

                if (client || !isClientOnly(path)) {
                    entries.add(new ModBundle.Entry(path, size, columns.length >= 3 ? columns[2].trim() : ""));
                }
            }
        }
    }

    private static boolean isClientOnly(String path) {
        for (String prefix : CLIENT_ONLY) {
            if (path.startsWith(prefix)) {
                return true;
            }
        }

        return false;
    }

    static boolean matches(Path file, long size, String sha256) throws IOException {
        if (!Files.isRegularFile(file) || Files.size(file) != size) {
            return false;
        }

        if (sha256 != null && !sha256.isEmpty()) {
            try (InputStream in = Files.newInputStream(file)) {
                MessageDigest digest = MessageDigest.getInstance("SHA-256");
                byte[] buffer = new byte[65536];

                int read;
                while ((read = in.read(buffer)) != -1) {
                    digest.update(buffer, 0, read);
                }

                return HexFormat.of().formatHex(digest.digest()).equalsIgnoreCase(sha256);
            } catch (NoSuchAlgorithmException ex) {
                throw new IllegalStateException(ex);
            }
        } else {
            return true;
        }
    }

    private record Entry(String path, long size, String sha256) {
    }

    private record Stamp(long size, long modified) {
    }

    private record StampFile(String digest, Map<String, ModBundle.Stamp> files) {
    }

    enum State {
        IDLE,
        RUNNING,
        DONE,
        FAILED;
    }
}
