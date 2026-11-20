package net.mdpromod;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Collections;
import java.util.LinkedHashMap;
import java.util.Map;

public final class SystemStrings {
    private static volatile Map<Integer, String> values;
    private static volatile boolean missingLogged;

    private SystemStrings() {
    }

    public static String get(int id) {
        if (id == 0) {
            return "";
        }

        String value = load().get(id);
        return value == null ? "" : value;
    }

    public static String format(int id, String... args) {
        return formatTemplate(get(id), args);
    }

    static SystemStrings.Entry parseLine(String line) {
        if (line == null) {
            return null;
        }

        String text = line.stripLeading();
        if (!text.startsWith("!system")) {
            return null;
        }

        int cursor = "!system".length();
        if (cursor < text.length() && Character.isWhitespace(text.charAt(cursor))) {
            while (cursor < text.length() && Character.isWhitespace(text.charAt(cursor))) {
                cursor++;
            }

            int start = cursor;

            while (cursor < text.length() && Character.isDigit(text.charAt(cursor))) {
                cursor++;
            }

            if (start == cursor) {
                return null;
            }

            int id;
            try {
                id = Integer.parseInt(text.substring(start, cursor));
            } catch (NumberFormatException ignored) {
                return null;
            }

            while (cursor < text.length() && Character.isWhitespace(text.charAt(cursor))) {
                cursor++;
            }

            String value = cursor < text.length() ? text.substring(cursor) : "";
            return new SystemStrings.Entry(id, value);
        } else {
            return null;
        }
    }

    static String formatTemplate(String template, String... args) {
        if (template != null && !template.isEmpty()) {
            String[] actual = args == null ? new String[0] : args;
            StringBuilder result = new StringBuilder(template.length());
            int argument = 0;

            for (int i = 0; i < template.length(); i++) {
                char current = template.charAt(i);
                if (current == '%' && i + 1 < template.length()) {
                    char next = template.charAt(i + 1);
                    if (next == '%') {
                        result.append('%');
                        i++;
                    } else {
                        int tokenLength = next == 'l' && i + 2 < template.length() && template.charAt(i + 2) == 's' ? 3 : (next == 'd' ? 2 : 0);
                        if (tokenLength == 0) {
                            result.append(current);
                        } else {
                            if (argument < actual.length) {
                                String replacement = actual[argument++];
                                result.append(replacement == null ? "" : replacement);
                            } else {
                                result.append(template, i, i + tokenLength);
                            }

                            i += tokenLength - 1;
                        }
                    }
                } else {
                    result.append(current);
                }
            }

            return result.toString();
        } else {
            return template == null ? "" : template;
        }
    }

    static void resetForTests() {
        synchronized (SystemStrings.class) {
            values = null;
            missingLogged = false;
        }
    }

    private static Map<Integer, String> load() {
        Map<Integer, String> loaded = values;
        if (loaded != null) {
            return loaded;
        }

        synchronized (SystemStrings.class) {
            loaded = values;
            if (loaded != null) {
                return loaded;
            }

            Map<Integer, String> parsed = new LinkedHashMap<>();
            Path file = ModPaths.runtimeRoot().resolve("Data").resolve("locales").resolve("zh-CN").resolve("strings.conf");

            try {
                for (String line : Files.readAllLines(file, StandardCharsets.UTF_8)) {
                    SystemStrings.Entry entry = parseLine(line);
                    if (entry != null && entry.id() != 0) {
                        parsed.put(entry.id(), entry.text());
                    }
                }
            } catch (IOException | RuntimeException error) {
                if (!missingLogged) {
                    missingLogged = true;
                    ModLog.warn("读取系统文案失败：{}", file);
                }
            }

            loaded = Collections.unmodifiableMap(parsed);
            values = loaded;
            return loaded;
        }
    }

    record Entry(int id, String text) {
    }
}
