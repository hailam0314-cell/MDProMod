package preview;

import com.badlogic.gdx.files.FileHandle;
import com.badlogic.gdx.graphics.Texture;
import com.badlogic.gdx.graphics.g2d.TextureAtlas;
import com.badlogic.gdx.utils.Array;
import com.esotericsoftware.spine.Animation;
import com.esotericsoftware.spine.SkeletonData;
import com.esotericsoftware.spine.SkeletonJson;

import java.io.File;
import java.io.FileInputStream;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.util.TreeMap;

/**
 * 数运行时解析出来的时间轴轨道，和 JSON 里写了多少条对比。
 *
 * 少了就说明这个版本的运行时**不认识**某些轨道（丢轨道 = 骨骼停在 setup pose = 部件散开）。
 *
 * 用法：TimelineCountProbe <cutin 目录>
 */
public final class TimelineCountProbe {

    public static void main(String[] args) throws Exception {
        File folder = new File(args[0]);
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
        Texture.PMA = atlasText.contains("pma: true") || atlasText.contains("pma:true");
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

        System.out.printf("=== %s  骨骼=%d 插槽=%d ik=%d 变换=%d 路径=%d 皮肤=%d 动画=%d%n",
                folder.getName(),
                skeletonData.getBones().size, skeletonData.getSlots().size,
                skeletonData.getIkConstraints().size, skeletonData.getTransformConstraints().size,
                skeletonData.getPathConstraints().size, skeletonData.getSkins().size,
                skeletonData.getAnimations().size);

        TreeMap<String, Integer> counts = new TreeMap<>();
        int total = 0;
        for (int i = 0; i < skeletonData.getAnimations().size; i++) {
            Animation animation = skeletonData.getAnimations().get(i);
            Array<com.esotericsoftware.spine.Animation.Timeline> timelines = animation.getTimelines();
            for (int t = 0; t < timelines.size; t++) {
                String name = timelines.get(t).getClass().getSimpleName();
                counts.merge(name, 1, Integer::sum);
                total++;
            }
            System.out.printf("    动画 %s: 轨道 %d 条%n", animation.getName(), timelines.size);
        }
        System.out.println("    轨道类型统计: " + counts);
        System.out.println("    合计 " + total + " 条");
    }
}
