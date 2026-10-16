package net.mdpromod;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Set;

/**
 * 禁卡表（ygopro 的 lflist 格式）和"开关配置文件"的解析，纯函数、可离线测试。
 *
 * <p>lflist 的每一段是：
 * <pre>
 *   !2026.10
 *   #forbidden
 *   20292186 0 --アーティファクト－デスサイズ
 *   53582587 1 --限制
 * </pre>
 * 第二列就是张数上限（0 禁止 / 1 限制 / 2 准限制 / 3 明确的无限制），后面的注释忽略；
 * {@code $whitelist} 表示"只列出来的卡能用"（没列出来的一律 0 张）。
 *
 * <p>开关文件每行 {@code on/off 卡表名}（见 {@link #template}）；可以同时开多个，
 * 同一张卡按**最严**的那条算（这就是"打开或关闭禁卡表，组建卡组受其影响"的规则）。
 */
final class Banlists {

    /** 一张卡表。limits 只存非默认（非 3）的条目也行，这里原样保留。 */
    record Table(String name, Map<Integer, Integer> limits, boolean whitelistOnly) {
    }

    /** 张数上限的人话：0 禁止 / 1 限制 / 2 准限制 / 3 无限制。 */
    static String limitName(int limit) {
        return switch (limit) {
            case 0 -> "禁止";
            case 1 -> "限制";
            case 2 -> "准限制";
            default -> "无限制";
        };
    }

    /** lflist.conf 的文本 → 卡表列表（顺序和文件里一致）。 */
    static List<Table> parse(String text) {
        if (text == null) return List.of();
        List<Table> tables = new ArrayList<>();
        String name = null;
        Map<Integer, Integer> limits = null;
        boolean whitelist = false;
        for (String raw : text.split("\\R")) {
            String line = raw.trim();
            if (line.isEmpty() || line.startsWith("#")) continue;
            if (line.startsWith("!")) {
                if (name != null) tables.add(new Table(name, limits, whitelist));
                name = line.substring(1).trim();
                limits = new LinkedHashMap<>();
                whitelist = false;
                continue;
            }
            if (line.startsWith("$")) {
                if (line.equalsIgnoreCase("$whitelist")) whitelist = true;
                continue;
            }
            if (limits == null) continue;
            String[] fields = line.split("\\s+");
            if (fields.length < 2) continue;
            try {
                int code = Integer.parseInt(fields[0]);
                int limit = Integer.parseInt(fields[1]);
                if (code > 0 && limit >= 0 && limit <= 3) limits.put(code, limit);
            } catch (NumberFormatException ignored) {
                // 注释行/表头之类：跳过，别让一行坏数据毁掉整张表
            }
        }
        if (name != null) tables.add(new Table(name, limits, whitelist));
        return tables;
    }

    /** 开关文件的文本 → 打开着的卡表名（顺序按文件里的先后，重复的只算一次）。 */
    static Set<String> parseConfig(String text) {
        Set<String> enabled = new LinkedHashSet<>();
        if (text == null) return enabled;
        for (String raw : text.split("\\R")) {
            String line = stripComment(raw).trim();     // 行尾的 # 注释也一起去掉
            if (line.isEmpty()) continue;
            int split = 0;
            while (split < line.length() && !Character.isWhitespace(line.charAt(split))) split++;
            String head = line.substring(0, split).toLowerCase(Locale.ROOT);
            String tail = unquote(line.substring(split).trim());
            boolean on = isOn(head);
            if (!on && !isOff(head)) {
                // 没写开关（或者拼错了）：整行当卡表名，按"打开"处理
                tail = unquote(line);
                on = true;
            }
            if (on && !tail.isEmpty()) enabled.add(tail);
        }
        return enabled;
    }

    /** 砍掉行尾注释；卡表名里不会出现 # ; // 这几种。 */
    private static String stripComment(String raw) {
        String line = raw;
        int hash = line.indexOf('#');
        if (hash >= 0) line = line.substring(0, hash);
        int semicolon = line.indexOf(';');
        if (semicolon >= 0) line = line.substring(0, semicolon);
        int slashes = line.indexOf("//");
        if (slashes >= 0) line = line.substring(0, slashes);
        return line;
    }

    /** 生成开关文件：所有卡表都列出来，只把 enabled 里的写成 on。 */
    static String template(List<String> names, Set<String> enabled) {
        Set<String> lowercase = new LinkedHashSet<>();
        if (enabled != null) for (String name : enabled) lowercase.add(name.toLowerCase(Locale.ROOT));
        StringBuilder text = new StringBuilder();
        text.append("# MDProMod 禁卡表开关\n")
                .append("# on = 启用，off = 关掉。可以同时开多个：同一张卡按最严的那条算。\n")
                .append("# 名字要和 lflist.conf 里的 !名字 一致（大小写随意）；这里没列出来的卡表一律算关闭。\n")
                .append("# 改完存盘即生效：卡盒和开局校验最多 2 秒后重新读一次，不用重启游戏。\n")
                .append("# lflist.conf 里一共 ").append(names.size()).append(" 个卡表：\n\n");
        for (String name : names) {
            text.append(lowercase.contains(name.toLowerCase(Locale.ROOT)) ? "on  " : "off ")
                    .append(name).append('\n');
        }
        return text.toString();
    }

    /** 启用的卡表合成一份生效限制。 */
    static Active merge(List<Table> tables, Set<String> enabled) {
        List<Table> active = new ArrayList<>();
        if (tables != null) for (Table table : tables) {
            for (String name : enabled) {
                if (table.name().equalsIgnoreCase(name)) {
                    active.add(table);
                    break;
                }
            }
        }
        return new Active(active);
    }

    /** 生效中的限制：同一张卡取所有启用卡表里最严的那条。 */
    static final class Active {
        private final List<Table> tables;
        private final List<String> names;
        private final List<Table> whitelists = new ArrayList<>();
        /** 合并后的条目（只放被限制到的卡）。 */
        private final Map<Integer, Integer> strictest = new LinkedHashMap<>();

        Active(List<Table> tables) {
            this.tables = List.copyOf(tables);
            List<String> collected = new ArrayList<>();
            for (Table table : this.tables) {
                collected.add(table.name());
                if (table.whitelistOnly()) whitelists.add(table);
                for (Map.Entry<Integer, Integer> entry : table.limits().entrySet()) {
                    strictest.merge(entry.getKey(), entry.getValue(), Math::min);
                }
            }
            this.names = List.copyOf(collected);
        }

        boolean isEmpty() {
            return tables.isEmpty();
        }

        List<String> names() {
            return names;
        }

        int limitOf(int code) {
            int limit = strictest.getOrDefault(code, 3);
            for (Table white : whitelists) {
                Integer value = white.limits().get(code);
                if (value == null) return 0;      // 白名单模式：没列出来就是不能用
                limit = Math.min(limit, value);
            }
            return limit;
        }

        /** 「2026.10 禁止」这类说明；没被限制就返回空串。 */
        String reason(int code) {
            if (tables.isEmpty()) return "";
            int limit = limitOf(code);
            if (limit >= 3) return "";
            for (Table table : tables) {
                Integer value = table.limits().get(code);
                if (value != null && value == limit) return table.name() + " " + limitName(value);
                if (value == null && table.whitelistOnly()) return table.name() + " 允许列表之外";
            }
            return "禁卡表 " + limitName(limit);
        }

        /** 「2026.10」「2026.10 ＋ 2026.9 TCG」「未启用」 */
        String summary() {
            return names.isEmpty() ? "未启用" : String.join(" ＋ ", names);
        }
    }

    private static boolean isOn(String token) {
        return switch (token) {
            case "on", "true", "yes", "y", "1", "开", "开启", "启用", "打开" -> true;
            default -> false;
        };
    }

    private static boolean isOff(String token) {
        return switch (token) {
            case "off", "false", "no", "n", "0", "关", "关闭", "禁用", "停用" -> true;
            default -> false;
        };
    }

    private static String unquote(String value) {
        String text = value == null ? "" : value.trim();
        if (text.length() >= 2 && (text.charAt(0) == '"' && text.charAt(text.length() - 1) == '"'
                || text.charAt(0) == '\'' && text.charAt(text.length() - 1) == '\'')) {
            return text.substring(1, text.length() - 1).trim();
        }
        return text;
    }

    private Banlists() {
    }
}
