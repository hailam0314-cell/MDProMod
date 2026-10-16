package net.mdpromod;

import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;

/**
 * 对着真实的运行时根目录检查人机目录解析：
 * 能不能列出卡组、bot.conf 的中文名有没有配上、卡组文件读不读得出来。
 *
 * 运行：java "-Dmdpromod.runtimeRoot=<staging>" -cp <jar>;<test-classes> net.mdpromod.AiDeckCatalogTest
 */
public final class AiDeckCatalogTest {
    private static int assertions;

    private static void check(boolean value, String message) {
        assertions++;
        if (!value) throw new AssertionError(message);
    }

    private static Path root() {
        return Path.of(System.getProperty("mdpromod.runtimeRoot",
                "D:\\MDProMod\\tools\\ygo_tcp_probe\\staging"));
    }

    public static void main(String[] args) throws Exception {
        Path root = root();
        List<AiDeckCatalog.Entry> entries = AiDeckCatalog.entries(root);
        check(!entries.isEmpty(), "人机目录不是空的");
        System.out.println("人机条目 " + entries.size() + " 个，运行时根 " + root);

        int named = 0;
        for (AiDeckCatalog.Entry entry : entries) {
            check(entry.key().startsWith("AI_"), "键名以 AI_ 开头：" + entry.key());
            check(!entry.label().isBlank(), "有显示名：" + entry.key());
            check(Files.isRegularFile(root.resolve(entry.file())),
                    "卡组文件存在：" + entry.file());
            if (!entry.label().equals(entry.key().substring(3))) named++;
        }
        check(named * 2 > entries.size(), "多数条目拿到了 bot.conf 的中文名（" + named + "/"
                + entries.size() + "）");

        AiDeckCatalog.Entry blueEyes = AiDeckCatalog.find(root, "AI_BlueEyes");
        check(blueEyes != null, "能找到 AI_BlueEyes");
        System.out.println("AI_BlueEyes -> " + blueEyes.label() + " / " + blueEyes.name()
                + " / " + blueEyes.dialog() + " / " + blueEyes.description());
        check(blueEyes.dialog().endsWith(".zh-CN"), "蓝眼人机有人格：" + blueEyes.dialog());

        DeckData deck = AiDeckCatalog.deck(root, "AI_BlueEyes");
        System.out.println("卡组：" + deck.summary());
        check(deck.mainDeck().size() >= 40, "主卡组至少 40 张");
        check(deck.name().equals(blueEyes.label()), "卡组名用中文标签");

        System.out.println("AiDeckCatalogTest passed " + assertions + " assertions");
    }
}
