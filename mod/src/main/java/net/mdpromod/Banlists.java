package net.mdpromod;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Set;
import java.util.Map.Entry;

final class Banlists {
    static String limitName(int limit) {
        return switch (limit) {
            case 0 -> "禁止";
            case 1 -> "限制";
            case 2 -> "准限制";
            default -> "无限制";
        };
    }

    static List<Banlists.Table> parse(String text) {
        if (text == null) {
            return List.of();
        }

        List<Banlists.Table> tables = new ArrayList<>();
        String name = null;
        Map<Integer, Integer> limits = null;
        boolean whitelist = false;

        for (String raw : text.split("\\R")) {
            String line = raw.trim();
            if (!line.isEmpty() && !line.startsWith("#")) {
                if (line.startsWith("!")) {
                    if (name != null) {
                        tables.add(new Banlists.Table(name, limits, whitelist));
                    }

                    name = line.substring(1).trim();
                    limits = new LinkedHashMap<>();
                    whitelist = false;
                } else if (line.startsWith("$")) {
                    if (line.equalsIgnoreCase("$whitelist")) {
                        whitelist = true;
                    }
                } else if (limits != null) {
                    String[] fields = line.split("\\s+");
                    if (fields.length >= 2) {
                        try {
                            int code = Integer.parseInt(fields[0]);
                            int limit = Integer.parseInt(fields[1]);
                            if (code > 0 && limit >= 0 && limit <= 3) {
                                limits.put(code, limit);
                            }
                        } catch (NumberFormatException var13) {
                        }
                    }
                }
            }
        }

        if (name != null) {
            tables.add(new Banlists.Table(name, limits, whitelist));
        }

        return tables;
    }

    static Set<String> parseConfig(String text) {
        Set<String> enabled = new LinkedHashSet<>();
        if (text == null) {
            return enabled;
        }

        for (String raw : text.split("\\R")) {
            String line = stripComment(raw).trim();
            if (!line.isEmpty()) {
                int split = 0;

                while (split < line.length() && !Character.isWhitespace(line.charAt(split))) {
                    split++;
                }

                String head = line.substring(0, split).toLowerCase(Locale.ROOT);
                String tail = unquote(line.substring(split).trim());
                boolean on = isOn(head);
                if (!on && !isOff(head)) {
                    tail = unquote(line);
                    on = true;
                }

                if (on && !tail.isEmpty()) {
                    enabled.add(tail);
                }
            }
        }

        return enabled;
    }

    private static String stripComment(String raw) {
        String line = raw;
        int hash = line.indexOf(35);
        if (hash >= 0) {
            line = line.substring(0, hash);
        }

        int semicolon = line.indexOf(59);
        if (semicolon >= 0) {
            line = line.substring(0, semicolon);
        }

        int slashes = line.indexOf("//");
        if (slashes >= 0) {
            line = line.substring(0, slashes);
        }

        return line;
    }

    static String template(List<String> names, Set<String> enabled) {
        Set<String> lowercase = new LinkedHashSet<>();
        if (enabled != null) {
            for (String name : enabled) {
                lowercase.add(name.toLowerCase(Locale.ROOT));
            }
        }

        StringBuilder text = new StringBuilder();
        text.append("# MDProMod 禁卡表开关\n")
            .append("# on = 启用，off = 关掉。可以同时开多个：同一张卡按最严的那条算。\n")
            .append("# 名字要和 lflist.conf 里的 !名字 一致（大小写随意）；这里没列出来的卡表一律算关闭。\n")
            .append("# 改完存盘即生效：卡盒和开局校验最多 2 秒后重新读一次，不用重启游戏。\n")
            .append("# lflist.conf 里一共 ")
            .append(names.size())
            .append(" 个卡表：\n\n");

        for (String name : names) {
            text.append(lowercase.contains(name.toLowerCase(Locale.ROOT)) ? "on  " : "off ").append(name).append('\n');
        }

        return text.toString();
    }

    static Banlists.Active merge(List<Banlists.Table> tables, Set<String> enabled) {
        List<Banlists.Table> active = new ArrayList<>();
        if (tables != null) {
            for (Banlists.Table table : tables) {
                for (String name : enabled) {
                    if (table.name().equalsIgnoreCase(name)) {
                        active.add(table);
                        break;
                    }
                }
            }
        }

        return new Banlists.Active(active);
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
        return text.length() < 2
                || (text.charAt(0) != '"' || text.charAt(text.length() - 1) != '"') && (text.charAt(0) != '\'' || text.charAt(text.length() - 1) != '\'')
            ? text
            : text.substring(1, text.length() - 1).trim();
    }

    private Banlists() {
    }

    static final class Active {
        private final List<Banlists.Table> tables;
        private final List<String> names;
        private final List<Banlists.Table> whitelists = new ArrayList<>();
        private final Map<Integer, Integer> strictest = new LinkedHashMap<>();

        Active(List<Banlists.Table> tables) {
            this.tables = List.copyOf(tables);
            List<String> collected = new ArrayList<>();

            for (Banlists.Table table : this.tables) {
                collected.add(table.name());
                if (table.whitelistOnly()) {
                    this.whitelists.add(table);
                }

                for (Entry<Integer, Integer> entry : table.limits().entrySet()) {
                    this.strictest.merge(entry.getKey(), entry.getValue(), Math::min);
                }
            }

            this.names = List.copyOf(collected);
        }

        boolean isEmpty() {
            return this.tables.isEmpty();
        }

        List<String> names() {
            return this.names;
        }

        int limitOf(int code) {
            int limit = this.strictest.getOrDefault(code, 3);

            for (Banlists.Table white : this.whitelists) {
                Integer value = white.limits().get(code);
                if (value == null) {
                    return 0;
                }

                limit = Math.min(limit, value);
            }

            return limit;
        }

        String reason(int code) {
            if (this.tables.isEmpty()) {
                return "";
            }

            int limit = this.limitOf(code);
            if (limit >= 3) {
                return "";
            }

            for (Banlists.Table table : this.tables) {
                Integer value = table.limits().get(code);
                if (value != null && value == limit) {
                    return table.name() + " " + Banlists.limitName(value);
                }

                if (value == null && table.whitelistOnly()) {
                    return table.name() + " 允许列表之外";
                }
            }

            return "禁卡表 " + Banlists.limitName(limit);
        }

        String summary() {
            return this.names.isEmpty() ? "未启用" : String.join(" ＋ ", this.names);
        }
    }

    record Table(String name, Map<Integer, Integer> limits, boolean whitelistOnly) {
    }
}
