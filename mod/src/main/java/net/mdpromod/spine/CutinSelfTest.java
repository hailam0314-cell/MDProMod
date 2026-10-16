package net.mdpromod.spine;

import com.badlogic.gdx.utils.Array;
import com.esotericsoftware.spine.AnimationState;
import com.esotericsoftware.spine.AnimationStateData;
import com.esotericsoftware.spine.Skeleton;
import com.esotericsoftware.spine.Slot;
import com.esotericsoftware.spine.attachments.MeshAttachment;
import com.esotericsoftware.spine.attachments.RegionAttachment;

import java.io.BufferedReader;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.Arrays;
import java.util.List;

/**
 * 脱离 Minecraft 的自检入口，用来验证"Spine 运行时 + 抽出来的素材"在**模组自己的 jar**里跑得起来。
 *
 * 直接对着打包好的模组 jar 跑即可（这条路径不碰任何 MC 类）：
 *
 * <pre>
 * java -cp mod\build\libs\mdpromod-0.1.0.jar net.mdpromod.spine.CutinSelfTest --all
 * </pre>
 *
 * 它会逐帧走一遍动画，报出每帧画了多少附件、多少个顶点，以及世界包围盒随时间的漂移，
 * 用来判断"动画真的在放"还是"停在 setup pose"。
 */
public final class CutinSelfTest {

    private CutinSelfTest() {
    }

    public static void main(String[] args) {
        System.out.println("素材根目录: " + CutinAssets.root());
        System.out.println("清单里的卡数: " + CutinRoster.size());
        boolean all = Arrays.asList(args).contains("--all");
        boolean quiet = all || Arrays.asList(args).contains("--quiet");
        List<String> codes = all ? allCodes() : Arrays.stream(args)
                .filter(value -> !value.startsWith("--")).toList();
        if (codes.isEmpty()) {
            codes = List.of("89631139", "46986417", "10000020");
        }
        int failed = 0;
        for (String code : codes) {
            try {
                if (!run(code, quiet)) {
                    failed++;
                }
            } catch (Exception error) {
                System.out.printf("=== %s: 验证失败: %s%n", code, error);
                failed++;
            } finally {
                CutinAssets.clear();
            }
        }
        System.out.printf("验证 %d 张：通过 %d，失败 %d%n", codes.size(), codes.size() - failed, failed);
        if (failed > 0) {
            System.exit(1);
        }
    }

    private static List<String> allCodes() {
        try (InputStream in = CutinSelfTest.class.getResourceAsStream("/data/mdpromod/cutins.txt")) {
            if (in == null) {
                throw new IllegalStateException("缺少模组内 cut-in 清单");
            }
            try (BufferedReader reader = new BufferedReader(new InputStreamReader(in, StandardCharsets.UTF_8))) {
                List<String> codes = reader.lines().map(String::trim)
                        .filter(line -> !line.isEmpty() && !line.startsWith("#"))
                        .map(line -> line.split("\\s+")[0]).distinct()
                        .sorted((left, right) -> Integer.compare(Integer.parseInt(left), Integer.parseInt(right)))
                        .toList();
                if (codes.isEmpty()) {
                    throw new IllegalStateException("模组内 cut-in 清单为空");
                }
                return codes;
            }
        } catch (java.io.IOException error) {
            throw new IllegalStateException("读不出模组内 cut-in 清单", error);
        }
    }

    private static boolean run(String code, boolean quiet) {
        CutinAssets.Recipe recipe = CutinAssets.recipe(code);
        if (recipe == null) {
            System.out.printf("=== %s: 缺少素材%n", code);
            return false;
        }
        CutinAssets.Loaded loaded = CutinAssets.load(code);
        if (loaded == null) {
            System.out.printf("=== %s: 装载失败%n", code);
            return false;
        }
        Skeleton skeleton = new Skeleton(loaded.data());
        AnimationState state = new AnimationState(new AnimationStateData(loaded.data()));
        state.setAnimation(0, recipe.animation(), false);

        int frames = 8;
        float dt = Math.max(0.001F, recipe.duration()) / (frames - 1);
        float[] previous = null;
        float drift = 0.0F;
        boolean visible = false;
        System.out.printf("=== %s 动画=%s 时长=%.3fs 骨骼=%d 插槽=%d 分页=%d%n",
                code, recipe.animation(), recipe.duration(),
                loaded.data().getBones().size, loaded.data().getSlots().size,
                recipe.pages().size());
        for (int frame = 0; frame < frames; frame++) {
            state.update(frame == 0 ? 0.0F : dt);
            state.apply(skeleton);
            skeleton.updateWorldTransform(Skeleton.Physics.update);
            float[] bounds = bounds(skeleton);
            int drawn = drawnAttachments(skeleton);
            visible |= drawn > 0 && bounds != null && bounds[1] > bounds[0] && bounds[3] > bounds[2];
            if (bounds != null && previous != null) {
                drift = Math.max(drift, Math.abs(bounds[0] - previous[0])
                        + Math.abs(bounds[1] - previous[1]));
            }
            previous = bounds;
            if (!quiet && bounds != null) {
                System.out.printf("   t=%.2fs 附件=%2d 包围盒 x[%.0f,%.0f] y[%.0f,%.0f]%n",
                        frame * dt, drawn, bounds[0], bounds[1], bounds[2], bounds[3]);
            }
        }
        if (!quiet) {
            System.out.printf("   随时间最大位移: %.1f%n", drift);
        }
        if (!visible) {
            throw new IllegalStateException("采样帧没有可见附件");
        }
        return true;
    }

    private static int drawnAttachments(Skeleton skeleton) {
        int count = 0;
        Array<Slot> order = skeleton.getDrawOrder();
        for (int i = 0; i < order.size; i++) {
            Slot slot = order.get(i);
            Object attachment = slot.getAttachment();
            float alpha = attachment instanceof RegionAttachment region ? region.getColor().a
                    : attachment instanceof MeshAttachment mesh ? mesh.getColor().a : 0.0F;
            if (alpha * slot.getColor().a * skeleton.getColor().a > 0.00001F) {
                count++;
            }
        }
        return count;
    }

    private static float[] bounds(Skeleton skeleton) {
        float minX = Float.MAX_VALUE;
        float minY = Float.MAX_VALUE;
        float maxX = -Float.MAX_VALUE;
        float maxY = -Float.MAX_VALUE;
        Array<Slot> order = skeleton.getDrawOrder();
        for (int i = 0; i < order.size; i++) {
            Slot slot = order.get(i);
            Object attachment = slot.getAttachment();
            float[] vertices;
            if (attachment instanceof RegionAttachment region) {
                vertices = new float[8];
                region.computeWorldVertices(slot, vertices, 0, 2);
                validateUvs(region.getUVs(), slot);
            } else if (attachment instanceof MeshAttachment mesh) {
                int count = mesh.getWorldVerticesLength();
                vertices = new float[count];
                mesh.computeWorldVertices(slot, 0, count, vertices, 0, 2);
                validateUvs(mesh.getUVs(), slot);
            } else {
                continue;
            }
            for (int v = 0; v < vertices.length; v += 2) {
                if (!Float.isFinite(vertices[v]) || !Float.isFinite(vertices[v + 1])) {
                    throw new IllegalStateException("动画生成了非有限顶点坐标");
                }
                minX = Math.min(minX, vertices[v]);
                maxX = Math.max(maxX, vertices[v]);
                minY = Math.min(minY, vertices[v + 1]);
                maxY = Math.max(maxY, vertices[v + 1]);
            }
        }
        return minX == Float.MAX_VALUE ? null : new float[]{minX, maxX, minY, maxY};
    }

    private static void validateUvs(float[] uvs, Slot slot) {
        for (float uv : uvs) {
            if (!Float.isFinite(uv) || uv < -0.0001F || uv > 1.0001F) {
                throw new IllegalStateException("图集 UV 越界: " + slot.getData().getName() + " = " + uv);
            }
        }
    }
}
