package net.mdpromod;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.TreeMap;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;

/**
 * 配方数据的离线自检：形状 / 键 / 物品 id / 配方书解锁，错了在游戏里只会"静默没有配方"，
 * 所以在这里挡住。数据放在 {@code data/mdpromod/recipes/} 与
 * {@code data/mdpromod/advancements/recipes/}（配方书靠 advancement 解锁）。
 *
 * <pre>
 * java -Dfile.encoding=UTF-8 -cp "mod\build\classes\java\test;mod\build\libs\mdpromod-0.1.0.jar;&lt;gson.jar&gt;" net.mdpromod.RecipeDataTest
 * </pre>
 *
 * gson 在游戏里有，离线跑要自己加一个（和 DuelBgmRulesTest 要 slf4j 是一回事）。
 */
public final class RecipeDataTest {
    /** 模组注册的物品（改 ModItems / MDProMod.BLOCKS 时同步这里）。 */
    private static final Set<String> MOD_ITEMS = Set.of(
            "duel_field", "card_case", "duel_card", "music_settings", "ai_deck", "duel_anchor");
    /** 这四个配方里允许出现的原版物品（防拼错 id）。 */
    private static final Set<String> VANILLA_ITEMS = Set.of(
            "paper", "leather", "iron_ingot", "redstone", "observer", "note_block");
    /** 必须存在的四个配方。 */
    private static final List<String> EXPECTED = List.of(
            "card_case", "duel_field", "ai_deck", "music_settings");

    private static int assertions;

    public static void main(String[] args) throws Exception {
        Map<String, String> recipes = readResources("data/mdpromod/recipes/");
        Map<String, String> advancements = readResources("data/mdpromod/advancements/recipes/");

        check(recipes.size() >= EXPECTED.size(), "配方文件 " + recipes.size() + " 个");
        for (String name : EXPECTED) {
            check(recipes.containsKey(name), "有配方 " + name);
        }

        for (Map.Entry<String, String> entry : new TreeMap<>(recipes).entrySet()) {
            JsonObject recipe = JsonParser.parseString(entry.getValue()).getAsJsonObject();
            String type = recipe.get("type").getAsString();
            String result = recipe.getAsJsonObject("result").get("item").getAsString();
            checkItemId(result, entry.getKey(), "结果");
            if (type.equals("minecraft:crafting_shaped")) {
                checkShaped(entry.getKey(), recipe);
            } else if (type.equals("minecraft:crafting_shapeless")) {
                checkShapeless(entry.getKey(), recipe);
            } else {
                check(false, entry.getKey() + " 的 type 是 " + type);
            }

            String advancement = advancements.get(entry.getKey());
            check(advancement != null, entry.getKey() + " 有配方书解锁 advancement");
            if (advancement != null) {
                JsonObject unlock = JsonParser.parseString(advancement).getAsJsonObject();
                check("minecraft:recipes/root".equals(unlock.get("parent").getAsString()),
                        entry.getKey() + " 的 advancement parent 是 recipes/root");
                String rewarded = unlock.getAsJsonObject("rewards")
                        .getAsJsonArray("recipes").get(0).getAsString();
                check(rewarded.equals("mdpromod:" + entry.getKey()),
                        entry.getKey() + " 的 advancement 解锁自己（" + rewarded + "）");
                check(unlock.getAsJsonObject("criteria").has("has_the_recipe"),
                        entry.getKey() + " 的 advancement 带 has_the_recipe");
            }
            System.out.println("  " + describe(entry.getKey(), recipe, type));
        }

        check(advancements.size() == recipes.size(),
                "advancement 数量与配方一致（" + advancements.size() + "）");
        System.out.println("RecipeDataTest passed " + assertions + " assertions");
    }

    private static String describe(String name, JsonObject recipe, String type) {
        String result = recipe.getAsJsonObject("result").get("item").getAsString();
        if (type.equals("minecraft:crafting_shaped")) {
            JsonArray pattern = recipe.getAsJsonArray("pattern");
            StringBuilder shape = new StringBuilder();
            for (JsonElement row : pattern) shape.append(row.getAsString()).append('/');
            if (shape.length() > 0) shape.setLength(shape.length() - 1);
            JsonObject key = recipe.getAsJsonObject("key");
            List<String> materials = new ArrayList<>();
            for (String letter : key.keySet()) {
                materials.add(letter + "=" + itemOf(key.getAsJsonObject(letter)));
            }
            return String.format("%-16s 有序 %s  [%s] -> %s", name, shape, String.join(" ", materials), result);
        }
        StringBuilder materials = new StringBuilder();
        for (JsonElement ingredient : recipe.getAsJsonArray("ingredients")) {
            if (materials.length() > 0) materials.append(" + ");
            materials.append(itemOf(ingredient.getAsJsonObject()));
        }
        return String.format("%-16s 无序 [%s] -> %s", name, materials, result);
    }

    private static void checkShaped(String name, JsonObject recipe) {
        JsonArray pattern = recipe.getAsJsonArray("pattern");
        check(pattern.size() >= 1 && pattern.size() <= 3, name + " 行数 1..3（" + pattern.size() + "）");
        int width = -1;
        for (JsonElement row : pattern) {
            int length = row.getAsString().length();
            if (width < 0) width = length;
            check(length == width, name + " 每行等宽（" + row.getAsString() + "）");
            check(length >= 1 && length <= 3, name + " 列数 1..3（" + length + "）");
        }
        JsonObject key = recipe.getAsJsonObject("key");
        StringBuilder used = new StringBuilder();
        for (JsonElement row : pattern) used.append(row.getAsString());
        for (String letter : key.keySet()) {
            check(used.indexOf(letter) >= 0, name + " 的键 '" + letter + "' 在图案里用到了");
            checkItemId(itemOf(key.getAsJsonObject(letter)), name, "材料 " + letter);
        }
        for (int i = 0; i < used.length(); i++) {
            String letter = String.valueOf(used.charAt(i));
            if (letter.equals(" ")) continue;
            check(key.has(letter), name + " 图案里的 '" + letter + "' 在 key 里有定义");
        }
    }

    private static void checkShapeless(String name, JsonObject recipe) {
        JsonArray ingredients = recipe.getAsJsonArray("ingredients");
        check(ingredients.size() >= 1 && ingredients.size() <= 9,
                name + " 材料数 1..9（" + ingredients.size() + "）");
        for (JsonElement ingredient : ingredients) {
            checkItemId(itemOf(ingredient.getAsJsonObject()), name, "材料");
        }
    }

    /** 支持 {@code {"item": ...}} 与 {@code {"tag": ...}} 两种写法，返回可读文本。 */
    private static String itemOf(JsonObject ingredient) {
        if (ingredient.has("item")) return ingredient.get("item").getAsString();
        if (ingredient.has("tag")) return "#" + ingredient.get("tag").getAsString();
        return "(空)";
    }

    private static void checkItemId(String id, String recipe, String what) {
        if (id.startsWith("#")) return;                     // 标签交给原版校验
        String[] parts = id.split(":", 2);
        check(parts.length == 2, recipe + " 的" + what + " id 带命名空间（" + id + "）");
        if (parts.length != 2) return;
        if (parts[0].equals("mdpromod")) {
            check(MOD_ITEMS.contains(parts[1]), recipe + " 的" + what + " 是模组物品（" + id + "）");
        } else if (parts[0].equals("minecraft")) {
            check(VANILLA_ITEMS.contains(parts[1]),
                    recipe + " 的" + what + " 在原版白名单里（" + id + "）");
        } else {
            check(false, recipe + " 的" + what + " 命名空间不认识（" + id + "）");
        }
    }

    /** 从 classpath（开发目录或 jar）里读出某个前缀下的所有 json，键是去掉目录和后缀的文件名。 */
    private static Map<String, String> readResources(String prefix) throws IOException {
        Map<String, String> found = new LinkedHashMap<>();
        for (String entry : System.getProperty("java.class.path").split(java.io.File.pathSeparator)) {
            Path path = Path.of(entry);
            if (Files.isDirectory(path)) {
                Path root = path.resolve(prefix);
                if (!Files.isDirectory(root)) continue;
                try (var files = Files.walk(root)) {
                    for (Path file : files.filter(p -> p.toString().endsWith(".json")).toList()) {
                        found.putIfAbsent(key(file.getFileName().toString()), Files.readString(file, StandardCharsets.UTF_8));
                    }
                }
            } else if (Files.isRegularFile(path) && entry.endsWith(".jar")) {
                try (ZipFile zip = new ZipFile(path.toFile())) {
                    for (ZipEntry file : zip.stream()
                            .filter(e -> !e.isDirectory() && e.getName().startsWith(prefix)
                                    && e.getName().endsWith(".json")).toList()) {
                        try (InputStream in = zip.getInputStream(file)) {
                            found.putIfAbsent(key(file.getName().substring(file.getName().lastIndexOf('/') + 1)),
                                    new String(in.readAllBytes(), StandardCharsets.UTF_8));
                        }
                    }
                }
            }
        }
        return found;
    }

    private static String key(String fileName) {
        return fileName.substring(0, fileName.length() - ".json".length());
    }

    private static void check(boolean condition, String what) {
        assertions++;
        if (!condition) throw new AssertionError("失败：" + what);
    }

    private RecipeDataTest() {
    }
}
