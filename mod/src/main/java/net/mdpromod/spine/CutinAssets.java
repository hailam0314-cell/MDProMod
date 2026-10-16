package net.mdpromod.spine;

import com.badlogic.gdx.files.FileHandle;
import com.badlogic.gdx.Files.FileType;
import com.badlogic.gdx.graphics.g2d.TextureAtlas;
import com.badlogic.gdx.utils.JsonReader;
import com.esotericsoftware.spine.SkeletonData;
import com.esotericsoftware.spine.SkeletonJson;

import java.io.File;
import java.io.BufferedReader;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/**
 * 王牌怪兽 cut-in 的素材装载：把离线管线抽出来的 Spine 骨架/图集/贴图读成运行时对象。
 *
 * 这段**完全不依赖 Minecraft**（只依赖 vendored 的 Spine 运行时和 libGDX 基础类），
 * 所以可以脱离游戏用 {@link CutinSelfTest} 直接跑，验证"素材 + 运行时"这条路是通的。
 *
 * 默认直接读取模组 jar 内的 {@code assets/mdpromod/cutin}，无须外置文件。
 * 开发时可用 {@code -Dmdpromod.cutinRoot=目录} 覆盖；每个卡号一个子目录。
 */
public final class CutinAssets {

    private static final String RESOURCE_ROOT = "assets/mdpromod/cutin";
    private static final String EXTERNAL_ROOT = System.getProperty("mdpromod.cutinRoot", "").trim();
    private static final int MAX_LOADED = 16;

    /** 一套 cut-in 的配方（由 {@code cutin.txt} 读出来）。 */
    public record Recipe(String skeleton, String animation, float duration, String atlas,
                         List<String> pages, float[] cameraPosition, String folder) {

        public boolean valid() {
            return skeleton != null && animation != null && atlas != null && folder != null;
        }
    }

    /** 装载好的运行时对象。 */
    public record Loaded(Recipe recipe, SkeletonData data, TextureAtlas atlas) {
    }

    private static final Map<String, Recipe> RECIPES = new HashMap<>();
    private static final Map<String, Loaded> LOADED = new LinkedHashMap<>(16, 0.75F, true) {
        @Override
        protected boolean removeEldestEntry(Map.Entry<String, Loaded> eldest) {
            return size() > MAX_LOADED;
        }
    };

    private CutinAssets() {
    }

    public static String root() {
        return EXTERNAL_ROOT.isEmpty() ? "classpath:/" + RESOURCE_ROOT : EXTERNAL_ROOT;
    }

    /** 读（并缓存）配方；没有素材目录就返回 null。 */
    public static synchronized Recipe recipe(String code) {
        if (RECIPES.containsKey(code)) {
            return RECIPES.get(code);
        }
        Recipe recipe = readRecipe(code);
        RECIPES.put(code, recipe);
        return recipe;
    }

    /** 按卡号装载骨架与图集；失败返回 null（并记住失败，避免每帧重试）。 */
    public static synchronized Loaded load(String code) {
        if (LOADED.containsKey(code)) {
            return LOADED.get(code);
        }
        Loaded loaded = null;
        Recipe recipe = recipe(code);
        if (recipe != null && recipe.valid()) {
            try {
                FileHandle folder = folder(code);
                TextureAtlas atlas = loadAtlas(folder.child(recipe.atlas()), folder);
                SkeletonJson json = new SkeletonJson(atlas);
                SkeletonData data;
                try (InputStream in = folder.child(recipe.skeleton()).read()) {
                    // Both the JSON and atlas contain UTF-8 attachment names.
                    // Spine's InputStream overload uses the OS default charset.
                    data = json.readSkeletonData(new JsonReader().parse(
                            new InputStreamReader(in, StandardCharsets.UTF_8)));
                }
                loaded = new Loaded(recipe, data, atlas);
            } catch (Exception error) {
                System.getLogger("mdpromod").log(System.Logger.Level.WARNING,
                        "cut-in 素材装载失败 code=" + code, error);
            }
        }
        LOADED.put(code, loaded);
        return loaded;
    }

    /** 清掉缓存（换素材目录后用得上）。 */
    public static synchronized void clear() {
        RECIPES.clear();
        LOADED.clear();
    }

    private static Recipe readRecipe(String code) {
        // Card IDs are also resource path components.
        if (code == null || !code.matches("[0-9]{1,10}")) {
            return null;
        }
        FileHandle file = folder(code).child("cutin.txt");
        if (!file.exists()) {
            return null;
        }
        String skeleton = null;
        String animation = null;
        String atlas = null;
        float duration = 0.0F;
        List<String> pages = new ArrayList<>();
        List<Float> camera = new ArrayList<>();
        try (BufferedReader reader = new BufferedReader(new InputStreamReader(file.read(), StandardCharsets.UTF_8))) {
            String raw;
            while ((raw = reader.readLine()) != null) {
                String line = raw.trim();
                if (line.startsWith("skeleton=")) {
                    if (skeleton == null) {
                        skeleton = line.substring("skeleton=".length());
                    }
                } else if (line.startsWith("animation=")) {
                    if (animation == null) {
                        animation = line.substring("animation=".length());
                    }
                } else if (line.startsWith("duration=")) {
                    duration = Math.max(duration, Float.parseFloat(line.substring("duration=".length())));
                } else if (line.startsWith("atlas=")) {
                    if (atlas == null) {
                        atlas = line.substring("atlas=".length());
                    }
                } else if (line.startsWith("page=")) {
                    String[] parts = line.substring("page=".length()).trim().split("\\s+");
                    if (parts.length > 0) {
                        pages.add(parts[0]);
                    }
                } else if (line.startsWith("camera=")) {
                    for (String value : line.substring("camera=".length()).split(",")) {
                        try {
                            camera.add(Float.parseFloat(value.trim()));
                        } catch (NumberFormatException ignored) {
                            // 相机字段坏了不影响演出
                        }
                    }
                }
            }
        } catch (Exception error) {
            System.getLogger("mdpromod").log(System.Logger.Level.WARNING,
                    "读不出 cutin.txt: " + file + " (" + error + ")");
            return null;
        }
        float[] cameraPosition = null;
        if (camera.size() >= 3) {
            cameraPosition = new float[]{camera.get(0), camera.get(1), camera.get(2)};
        }
        return new Recipe(skeleton, animation, duration, atlas, pages, cameraPosition,
                root() + "/" + code);
    }

    private static FileHandle folder(String code) {
        return EXTERNAL_ROOT.isEmpty() ? new ClasspathHandle(RESOURCE_ROOT + "/" + code)
                : new FileHandle(new File(EXTERNAL_ROOT, code));
    }

    /** FileHandle's children preserve the classpath type, including atlas page references. */
    private static final class ClasspathHandle extends FileHandle {
        ClasspathHandle(String path) {
            super(path, FileType.Classpath);
        }
    }

    /**
     * 用 gdx 的 TextureAtlas 解析图集（UV、旋转区域、scale 全是官方算法），
     * 只把分页的贴图换成我们的 Texture 桩（不做 GL 上传，只记尺寸和 pma）。
     */
    private static TextureAtlas loadAtlas(FileHandle atlasFile, FileHandle folder) {
        // gdx 1.12's atlas parser also requests a reader without an encoding.
        FileHandle utf8Atlas = new FileHandle(atlasFile.path(), atlasFile.type()) {
            @Override
            public BufferedReader reader(int bufferSize) {
                return super.reader(bufferSize, StandardCharsets.UTF_8.name());
            }
        };
        TextureAtlas.TextureAtlasData data = new TextureAtlas.TextureAtlasData(
                utf8Atlas, folder, false);
        for (TextureAtlas.TextureAtlasData.Page page : data.getPages()) {
            page.texture = new com.badlogic.gdx.graphics.Texture(page.textureFile, page.pma,
                    (int) page.width, (int) page.height);
        }
        return new TextureAtlas(data);
    }
}
