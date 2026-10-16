package net.mdpromod;

import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.HashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.util.stream.Stream;

/**
 * 「人机卡组」目录：扫运行时根目录下的 {@code Data/Windbot/Decks/AI_*.ydk}，
 * 再到 {@code Data/locales/zh-CN/bot.conf} 里找中文名 / 人格 / 一句话描述。
 *
 * 条目**以磁盘上的卡组文件为准**（能真放上场的才算），bot.conf 只提供显示信息；
 * 匹配不上的卡组用文件名兜底，不会凭空消失。
 */
final class AiDeckCatalog {

    /** {@code key} 是不带扩展名的卡组名（如 AI_BlueEyes），{@code file} 是相对运行时根的路径。 */
    record Entry(String key, String file, String label, String name, String dialog, String description) {
    }

    /** bot.conf 里 {@code Name=xx Deck='yy' Dialog=zz} 那一行。 */
    private static final Pattern FIELDS =
            Pattern.compile("(\\w+)=('[^']*'|\"[^\"]*\"|\\S+)");
    private static final Map<String, String> ROMAN = Map.of(
            "II", "2", "III", "3", "IV", "4", "V", "5",
            "VI", "6", "VII", "7", "VIII", "8", "IX", "9", "X", "10");

    private static Path cachedRoot;
    private static List<Entry> cached;

    private AiDeckCatalog() {
    }

    static synchronized List<Entry> entries(Path root) {
        if (root != null && root.equals(cachedRoot) && cached != null) {
            return cached;
        }
        cached = load(root);
        cachedRoot = root;
        return cached;
    }

    static synchronized void invalidate() {
        cached = null;
        cachedRoot = null;
    }

    static Entry find(Path root, String key) {
        if (key == null || key.isBlank()) return null;
        for (Entry entry : entries(root)) {
            if (entry.key().equalsIgnoreCase(key)) return entry;
        }
        return null;
    }

    /** 读一份人机卡组；名称用中文标签，方便决斗盘上显示。 */
    static DeckData deck(Path root, String key) throws IOException {
        Entry entry = find(root, key);
        if (entry == null) throw new IOException("没有这个人机卡组：" + key);
        DeckData deck = DeckData.fromYdk(root.resolve(entry.file()));
        deck.setName(entry.label());
        return deck;
    }

    private static List<Entry> load(Path root) {
        List<Entry> entries = new ArrayList<>();
        if (root == null) return entries;
        Path decksDir = root.resolve("Data/Windbot/Decks");
        if (!Files.isDirectory(decksDir)) {
            LoggerFactory.getLogger("mdpromod").warn("没有人机卡组目录：{}", decksDir);
            return entries;
        }
        Map<String, String[]> profiles = parseBotConf(root.resolve("Data/locales/zh-CN/bot.conf"));
        try (Stream<Path> files = Files.list(decksDir)) {
            for (Path file : files.sorted(Comparator.comparing(Path::getFileName)).toList()) {
                String fileName = file.getFileName().toString();
                if (!fileName.startsWith("AI_") || !fileName.endsWith(".ydk")) continue;
                String key = fileName.substring(0, fileName.length() - 4);
                // bot.conf 里的 Deck= 名字经过规范化后大小写可能不同（TimeThief / Timethief）
                String[] profile = profiles.get(fileName.toLowerCase(Locale.ROOT));
                String label = profile != null && !profile[0].isBlank() ? profile[0] : key.substring(3);
                String name = profile != null && !profile[1].isBlank() ? profile[1] : "MDProBot";
                String dialog = profile != null && !profile[2].isBlank() ? profile[2] : "default";
                String description = profile != null ? profile[3] : "";
                entries.add(new Entry(key, "Data/Windbot/Decks/" + fileName,
                        label, name, dialog, description));
            }
        } catch (IOException error) {
            LoggerFactory.getLogger("mdpromod").warn("扫人机卡组失败: {}", error.toString());
        }
        return entries;
    }

    /**
     * 解析 bot.conf：
     * <pre>
     * !标题
     * Name=人机名 Deck='卡组名' Dialog=人格
     * 一句话描述
     * </pre>
     * 返回 {@code 小写 ydk 文件名 -> [标题, 人机名, 人格, 描述]}。
     */
    private static Map<String, String[]> parseBotConf(Path conf) {
        Map<String, String[]> profiles = new HashMap<>();
        if (!Files.isRegularFile(conf)) return profiles;
        try {
            String title = "";
            String[] pending = null;
            for (String raw : Files.readAllLines(conf, StandardCharsets.UTF_8)) {
                String line = raw.strip();
                if (line.isEmpty()) continue;
                if (line.startsWith("!")) {
                    title = line.substring(1).strip();
                    pending = null;
                    continue;
                }
                if (line.startsWith("Name=")) {
                    Matcher matcher = FIELDS.matcher(line);
                    Map<String, String> fields = new HashMap<>();
                    while (matcher.find()) {
                        fields.put(matcher.group(1), matcher.group(2).replace("'", "").replace("\"", ""));
                    }
                    String ydk = ydkName(fields.get("Deck"));
                    if (ydk == null) continue;
                    pending = new String[]{title, fields.getOrDefault("Name", ""),
                            fields.getOrDefault("Dialog", ""), ""};
                    profiles.put(ydk.toLowerCase(Locale.ROOT), pending);
                    continue;
                }
                // 紧跟其后的第一行当描述
                if (pending != null && pending[3].isEmpty() && !line.startsWith("AI_")) {
                    pending[3] = line;
                    pending = null;
                }
            }
        } catch (IOException error) {
            LoggerFactory.getLogger("mdpromod").warn("读 bot.conf 失败: {}", error.toString());
        }
        return profiles;
    }

    /** {@code 'Toadally Awesome'} / {@code 'Level VIII'} → AI_ToadallyAwesome.ydk / AI_Level8.ydk。 */
    private static String ydkName(String deck) {
        if (deck == null || deck.isBlank()) return null;
        StringBuilder builder = new StringBuilder("AI_");
        for (String token : deck.strip().split("[\\s\\-]+")) {
            if (token.isEmpty()) continue;
            builder.append(ROMAN.getOrDefault(token.toUpperCase(Locale.ROOT), token));
        }
        return builder.append(".ydk").toString();
    }
}
