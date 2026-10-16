package preview;

import com.badlogic.gdx.files.FileHandle;
import com.badlogic.gdx.graphics.Texture;
import com.badlogic.gdx.graphics.g2d.TextureAtlas;
import com.badlogic.gdx.utils.Array;
import com.esotericsoftware.spine.AnimationState;
import com.esotericsoftware.spine.AnimationStateData;
import com.esotericsoftware.spine.Skeleton;
import com.esotericsoftware.spine.SkeletonData;
import com.esotericsoftware.spine.SkeletonJson;
import com.esotericsoftware.spine.Slot;
import com.esotericsoftware.spine.attachments.MeshAttachment;
import com.esotericsoftware.spine.attachments.RegionAttachment;

import java.io.File;
import java.io.FileInputStream;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.util.ArrayList;
import java.util.List;

/**
 * 量出每个附件在世界里的包围盒，按 y 排序打印，并标出相邻附件之间的空隙。
 *
 * "部件是不是真的分开"用眼睛在深色图上判断不可靠，这里直接给数字。
 *
 * 用法：AttachmentBoxProbe <cutin 目录> [时间秒]
 */
public final class AttachmentBoxProbe {

    public static void main(String[] args) throws Exception {
        File folder = new File(args[0]);
        float at = args.length > 1 ? Float.parseFloat(args[1]) : 1.4F;
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
        Skeleton skeleton = new Skeleton(skeletonData);
        AnimationState state = new AnimationState(new AnimationStateData(skeletonData));
        state.setAnimation(0, animationName, false);
        state.update(at);
        state.apply(skeleton);
        skeleton.updateWorldTransform(Skeleton.Physics.update);

        List<Object[]> rows = new ArrayList<>();
        Array<Slot> order = skeleton.getDrawOrder();
        for (int i = 0; i < order.size; i++) {
            Slot slot = order.get(i);
            Object attachment = slot.getAttachment();
            float[] vertices;
            float alpha = skeleton.getColor().a * slot.getColor().a;
            if (attachment instanceof RegionAttachment region) {
                vertices = new float[8];
                region.computeWorldVertices(slot, vertices, 0, 2);
                alpha *= region.getColor().a;
            } else if (attachment instanceof MeshAttachment mesh) {
                int count = mesh.getWorldVerticesLength();
                vertices = new float[count];
                mesh.computeWorldVertices(slot, 0, count, vertices, 0, 2);
                alpha *= mesh.getColor().a;
            } else {
                continue;
            }
            float minX = Float.MAX_VALUE, minY = Float.MAX_VALUE;
            float maxX = -Float.MAX_VALUE, maxY = -Float.MAX_VALUE;
            for (int v = 0; v < vertices.length; v += 2) {
                minX = Math.min(minX, vertices[v]);
                maxX = Math.max(maxX, vertices[v]);
                minY = Math.min(minY, vertices[v + 1]);
                maxY = Math.max(maxY, vertices[v + 1]);
            }
            rows.add(new Object[]{slot.getData().getName(), attachment.getClass().getSimpleName(),
                    minX, maxX, minY, maxY, alpha});
        }
        rows.sort((a, b) -> Float.compare((float) b[4], (float) a[4]));

        System.out.printf("=== %s  t=%.2fs  附件 %d 个（按 y 从高到低）%n",
                folder.getName(), at, rows.size);
        System.out.printf("%-18s %-16s %9s %9s %9s %9s %6s  %s%n",
                "插槽", "类型", "x0", "x1", "y0", "y1", "alpha", "与上一块的竖直关系");
        float previousBottom = Float.NaN;
        for (Object[] row : rows) {
            float minY = (float) row[4];
            float maxY = (float) row[5];
            String relation = "";
            if (!Float.isNaN(previousBottom)) {
                float gap = previousBottom - maxY;      // 上一块的下沿 - 本块的上沿
                relation = gap > 0 ? String.format("空隙 %.1f", gap)
                        : String.format("重叠 %.1f", -gap);
            }
            System.out.printf("%-18s %-16s %9.0f %9.0f %9.0f %9.0f %6.2f  %s%n",
                    row[0], row[1], row[2], row[3], minY, maxY, row[6], relation);
            previousBottom = minY;
        }
    }
}
