package net.mdpromod;

import java.net.URLDecoder;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Base64;
import java.util.List;
import java.util.Locale;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public final class DeckShareCode {
    private static final String URL_PREFIX = "http://deck.ourygo.top?ygotype=deck&v=1&d=";
    private static final Pattern DECK_PARAM = Pattern.compile("(?:[?&])d=([^&\\s]+)");

    private DeckShareCode() {
    }

    public static String exportUrl(DeckData deck) {
        return "http://deck.ourygo.top?ygotype=deck&v=1&d=" + encode(deck);
    }

    public static String encode(DeckData deck) {
        List<Integer> main = deck == null ? List.of() : deck.mainDeck();
        List<Integer> extra = deck == null ? List.of() : deck.extraDeck();
        List<Integer> side = deck == null ? List.of() : deck.sideDeck();
        int mainRuns = runCount(main);
        int extraRuns = runCount(extra);
        int sideRuns = runCount(side);
        if (mainRuns <= 255 && extraRuns <= 15 && sideRuns <= 15) {
            StringBuilder bits = new StringBuilder();
            appendFixed(bits, Integer.toBinaryString(mainRuns), 8);
            appendFixed(bits, Integer.toBinaryString(extraRuns), 4);
            appendFixed(bits, Integer.toBinaryString(sideRuns), 4);
            appendCards(bits, main);
            appendCards(bits, extra);
            appendCards(bits, side);

            while (bits.length() % 8 != 0) {
                bits.append('0');
            }

            byte[] bytes = new byte[bits.length() / 8];

            for (int i = 0; i < bytes.length; i++) {
                bytes[i] = (byte)Integer.parseInt(bits.substring(i * 8, i * 8 + 8), 2);
            }

            return Base64.getUrlEncoder().withoutPadding().encodeToString(bytes);
        } else {
            throw new IllegalArgumentException("卡组过大，无法生成分享码");
        }
    }

    public static DeckData decode(String input, String name) {
        if (input != null && !input.isBlank()) {
            String text = input.trim();
            if (!text.contains("#main") && !text.toLowerCase(Locale.ROOT).contains("!side")) {
                String token = extractToken(text);
                byte[] bytes = decodeBase64(token);
                StringBuilder bits = new StringBuilder(bytes.length * 8);

                for (byte value : bytes) {
                    appendFixed(bits, Integer.toBinaryString(value & 255), 8);
                }

                if (bits.length() < 16) {
                    throw new IllegalArgumentException("卡组码长度不足");
                }

                int mainRuns = Integer.parseInt(bits.substring(0, 8), 2);
                int extraRuns = Integer.parseInt(bits.substring(8, 12), 2);
                int sideRuns = Integer.parseInt(bits.substring(12, 16), 2);
                String bitString = bits.toString();
                int cursor = 16;
                List<Integer> main = decodeCards(bitString, cursor, mainRuns);
                cursor += mainRuns * 29;
                List<Integer> extra = decodeCards(bitString, cursor, extraRuns);
                cursor += extraRuns * 29;
                List<Integer> side = decodeCards(bitString, cursor, sideRuns);
                return DeckData.of(name, toArray(main), toArray(extra), toArray(side));
            } else {
                return DeckData.fromYdkText(text, name);
            }
        } else {
            throw new IllegalArgumentException("卡组码为空");
        }
    }

    static String extractToken(String input) {
        String text = input.trim();
        Matcher matcher = DECK_PARAM.matcher(text);
        if (matcher.find()) {
            text = URLDecoder.decode(matcher.group(1), StandardCharsets.UTF_8);
        } else if (text.contains("d=")) {
            int at = text.lastIndexOf("d=");
            text = text.substring(at + 2);
            int amp = text.indexOf(38);
            if (amp >= 0) {
                text = text.substring(0, amp);
            }

            text = URLDecoder.decode(text, StandardCharsets.UTF_8);
        }

        text = text.replaceAll("\\s+", "").replace(' ', '+').replace('-', '+').replace('_', '/');

        while (text.endsWith("=")) {
            text = text.substring(0, text.length() - 1);
        }

        if (text.isEmpty()) {
            throw new IllegalArgumentException("没有找到卡组码");
        } else {
            return text;
        }
    }

    private static byte[] decodeBase64(String token) {
        int padding = (4 - token.length() % 4) % 4;
        String padded = token + "=".repeat(padding);

        try {
            return Base64.getDecoder().decode(padded);
        } catch (IllegalArgumentException error) {
            throw new IllegalArgumentException("卡组码格式不正确", error);
        }
    }

    private static void appendCards(StringBuilder bits, List<Integer> cards) {
        int index = 0;

        while (index < cards.size()) {
            int code = cards.get(index);
            if (code <= 0) {
                index++;
            } else {
                int count = 1;

                while (count < 3 && index + count < cards.size() && cards.get(index + count) == code) {
                    count++;
                }
                bits.append(switch (count) {
                    case 2 -> "10";
                    case 3 -> "11";
                    default -> "01";
                });
                appendFixed(bits, Integer.toBinaryString(code), 27);
                index += count;
            }
        }
    }

    private static int runCount(List<Integer> cards) {
        int runs = 0;
        int index = 0;

        while (index < cards.size()) {
            int code = cards.get(index);
            if (code <= 0) {
                index++;
            } else {
                int count = 1;

                while (count < 3 && index + count < cards.size() && cards.get(index + count) == code) {
                    count++;
                }

                runs++;
                index += count;
            }
        }

        return runs;
    }

    private static List<Integer> decodeCards(String bits, int start, int runs) {
        if (start >= 0 && runs >= 0 && start + runs * 29 <= bits.length()) {
            List<Integer> cards = new ArrayList<>();
            int cursor = start;

            for (int run = 0; run < runs; run++) {
                int count = switch (bits.substring(cursor, cursor + 2)) {
                    case "01" -> 1;
                    case "10" -> 2;
                    case "11" -> 3;
                    default -> throw new IllegalArgumentException("卡组码包含未知张数标记");
                };
                cursor += 2;
                int code = Integer.parseInt(bits.substring(cursor, cursor + 27), 2);
                cursor += 27;

                for (int i = 0; i < count; i++) {
                    cards.add(code);
                }
            }

            return cards;
        } else {
            throw new IllegalArgumentException("卡组码数据不完整");
        }
    }

    private static void appendFixed(StringBuilder bits, String value, int length) {
        int padding = Math.max(0, length - value.length());
        bits.append("0".repeat(padding)).append(value);
    }

    private static int[] toArray(List<Integer> values) {
        int[] result = new int[values.size()];

        for (int i = 0; i < values.size(); i++) {
            result[i] = values.get(i);
        }

        return result;
    }
}
