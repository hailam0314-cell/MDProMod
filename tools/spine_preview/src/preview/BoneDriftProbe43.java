package preview;

import com.badlogic.gdx.files.FileHandle;
import com.badlogic.gdx.graphics.Texture;
import com.badlogic.gdx.graphics.g2d.TextureAtlas;
import com.badlogic.gdx.utils.Array;
import com.badlogic.gdx.utils.JsonReader;
import com.badlogic.gdx.utils.JsonValue;
import com.esotericsoftware.spine.AnimationState;
import com.esotericsoftware.spine.AnimationStateData;
import com.esotericsoftware.spine.Bone;
import com.esotericsoftware.spine.Physics;
import com.esotericsoftware.spine.Skeleton;
import com.esotericsoftware.spine.SkeletonData;
import com.esotericsoftware.spine.SkeletonJson;

import java.io.File;
import java.io.FileInputStream;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * 诊断"部件摆错位"：动画把哪些骨骼挪动了？这些骨骼用的是哪种轨道格式？
 *
 * 如果某个骨骼在 JSON 里有 `translatex`/`translatey` 轨道，而运行时**没**把它的
 * 局部坐标挪到那些关键帧的值上，就说明运行时没吃这条轨道——那部件就会停在 setup pose
 * 的"炸开"位置。
 *
 * 用法：BoneDriftProbe <cutin 目录> [时间秒]
 */
public final class BoneDriftProbe43 {

    public static void main(String[] args) throws Exception {
        File folder = new File(args[0]);
        float at = args.length > 1 ? Float.parseFloat(args[1]) : 0.8F;

        String skeletonFile = null;
        String atlasFile = null;
        String animationName = null;
        for (String line : Files.readAllLines(new File(folder, "cutin.txt").toPath(),
                StandardCharsets.UTF_8)) {
            String trimmed = line.trim();
            if (trimmed.startsWith("skeleton=")) skeletonFile = trimmed.substring(9);
            else if (trimmed.startsWith("atlas=")) atlasFile = trimmed.substring(6);
            else if (trimmed.startsWith("animation=")) animationName = trimmed.substring(10);
        }

        String atlasText = new String(Files.readAllBytes(new File(folder, atlasFile).toPath()),
                StandardCharsets.UTF_8);
        boolean pma = atlasText.contains("pma: true") || atlasText.contains("pma:true");
        Texture.PMA = pma;
        TextureAtlas.TextureAtlasData data = new TextureAtlas.TextureAtlasData(
                new FileHandle(new File(folder, atlasFile)), new FileHandle(folder), false);
        for (TextureAtlas.TextureAtlasData.Page page : data.getPages()) {
            page.texture = new Texture(new FileHandle(new File(page.textureFile.path())));
        }
        TextureAtlas atlas = new TextureAtlas(data);

        SkeletonData skeletonData;
        try (InputStream in = new FileInputStream(new File(folder, skeletonFile))) {
            skeletonData = new SkeletonJson(atlas).readSkeletonData(in);
        }
        // 另建一个 Skeleton 当"setup pose"基准（新建时就是 setup pose），避免依赖
        // BoneData 上的取值方法——那个在 4.2/4.3 之间改过名
        Skeleton skeleton = new Skeleton(skeletonData);
        AnimationState state = new AnimationState(new AnimationStateData(skeletonData));
        state.setAnimation(0, animationName, false);
        state.update(at);
        state.apply(skeleton);
        skeleton.updateWorldTransform(Physics.update);

        Skeleton setup = new Skeleton(skeletonData);
        setup.updateWorldTransform(Physics.update);

        // JSON 里每根骨骼有哪几条轨道
        JsonValue root = new JsonReader().parse(
                new String(Files.readAllBytes(new File(folder, skeletonFile).toPath()),
                        StandardCharsets.UTF_8));
        JsonValue bonesNode = root.get("animations").get(animationName).get("bones");
        Map<String, List<String>> tracks = new HashMap<>();
        if (bonesNode != null) {
            for (JsonValue bone = bonesNode.child; bone != null; bone = bone.next) {
                List<String> names = new ArrayList<>();
                for (JsonValue track = bone.child; track != null; track = track.next) {
                    names.add(track.name);
                }
                tracks.put(bone.name, names);
            }
        }

        List<double[]> rows = new ArrayList<>();
        for (int i = 0; i < skeleton.getBones().size; i++) {
            Bone bone = skeleton.getBones().get(i);
            float sx = setup.getBones().get(i).getX();
            float sy = setup.getBones().get(i).getY();
            double delta = Math.hypot(bone.getX() - sx, bone.getY() - sy);
            rows.add(new double[]{delta, i});
        }
        rows.sort((a, b) -> Double.compare(b[0], a[0]));

        System.out.printf("=== %s  t=%.2fs  动画=%s%n", folder.getName(), at, animationName);
        System.out.printf("%-28s %10s %10s %10s %10s  %s%n",
                "骨骼", "setup x", "setup y", "当前 x", "当前 y", "轨道");
        for (int k = 0; k < Math.min(14, rows.size()); k++) {
            int i = (int) rows.get(k)[1];
            Bone bone = skeleton.getBones().get(i);
            Bone setupBone = setup.getBones().get(i);
            String name = bone.getData().getName();
            List<String> names = tracks.get(name);
            System.out.printf("%-28s %10.1f %10.1f %10.1f %10.1f  %s%n",
                    name, setupBone.getX(), setupBone.getY(),
                    bone.getX(), bone.getY(),
                    names == null ? "(无轨道)" : names.toString());
        }
        int withXy = 0;
        for (List<String> names : tracks.values()) {
            if (names.contains("translatex") || names.contains("translatey")) {
                withXy++;
            }
        }
        System.out.printf("有 translatex/translatey 轨道的骨骼数: %d / %d%n",
                withXy, tracks.size());
    }
}

