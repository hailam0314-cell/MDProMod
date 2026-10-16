package preview;

import com.badlogic.gdx.files.FileHandle;
import com.badlogic.gdx.graphics.Texture;
import com.badlogic.gdx.graphics.g2d.TextureAtlas;
import com.badlogic.gdx.utils.Array;
import com.badlogic.gdx.utils.JsonReader;
import com.esotericsoftware.spine.AnimationState;
import com.esotericsoftware.spine.AnimationStateData;
import com.esotericsoftware.spine.Skeleton;
import com.esotericsoftware.spine.SkeletonData;
import com.esotericsoftware.spine.SkeletonJson;
import com.esotericsoftware.spine.Slot;
import com.esotericsoftware.spine.attachments.MeshAttachment;
import com.esotericsoftware.spine.attachments.RegionAttachment;

import javax.imageio.ImageIO;
import java.awt.AlphaComposite;
import java.awt.Color;
import java.awt.Graphics2D;
import java.awt.RenderingHints;
import java.awt.Shape;
import java.awt.geom.AffineTransform;
import java.awt.geom.Path2D;
import java.awt.geom.Point2D;
import java.awt.image.BufferedImage;
import java.io.File;
import java.io.BufferedReader;
import java.io.FileInputStream;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.util.HashMap;
import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * 把一套 cutin 的 Spine 动画离线渲染成 PNG 序列，用来在进游戏之前验证骨骼数学。
 *
 * 用的是官方 spine 运行时 + 官方 gdx 图集解析，只有最底层的 Texture 换成了不打 GL 的桩。
 * 所以只要渲染结果对，说明"运行时能用"这件事成立，剩下就只是把它接到 Minecraft 的渲染管线。
 *
 * 用法：java preview.CutinPreview <cutin 目录> <输出目录> [帧数]
 */
public final class CutinPreview {

    private static final int CANVAS = 720;
    private static final Pattern ATLAS_JSON = Pattern.compile("\"([A-Za-z0-9_]+)\"\\s*:\\s*(\\{[^{}]*\\})");

    public static void main(String[] args) throws Exception {
        if (args.length < 2) {
            System.out.println("用法: CutinPreview <cutin 目录> <输出目录> [帧数]");
            return;
        }
        File cutinDir = new File(args[0]);
        File outDir = new File(args[1]);
        int frames = args.length > 2 ? Integer.parseInt(args[2]) : 12;
        boolean setupPoseOnly = args.length > 3 && args[3].equals("setup");
        outDir.mkdirs();

        Meta meta = readMeta(cutinDir);
        System.out.println("骨架: " + meta.skeleton + "  动画: " + meta.animation
                + "  时长: " + meta.duration + "s  图集: " + meta.atlas);

        File atlasFile = new File(cutinDir, meta.atlas);
        String atlasText = new String(Files.readAllBytes(atlasFile.toPath()), StandardCharsets.UTF_8);
        Texture.PMA = atlasText.contains("pma: true") || atlasText.contains("pma:true");
        System.out.println("pma=" + Texture.PMA);

        TextureAtlas atlas = loadAtlas(atlasFile, cutinDir);
        System.out.println("图集区域数: " + atlas.getRegions().size);

        SkeletonJson json = new SkeletonJson(atlas);
        SkeletonData data;
        try (InputStream in = new FileInputStream(new File(cutinDir, meta.skeleton))) {
            data = json.readSkeletonData(new JsonReader().parse(
                    new InputStreamReader(in, StandardCharsets.UTF_8)));
        }
        Skeleton skeleton = new Skeleton(data);

        AnimationState state = new AnimationState(new AnimationStateData(data));
        state.setAnimation(0, meta.animation, false);

        float dt = frames > 1 ? meta.duration / (frames - 1) : 0f;
        float ax = Float.MAX_VALUE, ay = Float.MAX_VALUE;
        float bx = -Float.MAX_VALUE, by = -Float.MAX_VALUE;
        float[] verts = new float[0];
        for (int i = 0; i < frames; i++) {
            if (setupPoseOnly) {
                skeleton.setToSetupPose();
                skeleton.updateWorldTransform(Skeleton.Physics.update);
            } else {
                state.update(i == 0 ? 0f : dt);
                state.apply(skeleton);
                skeleton.updateWorldTransform(Skeleton.Physics.update);
            }
            verts = collect(verts, skeleton);
            for (int v = 0; v < verts.length; v += 2) {
                ax = Math.min(ax, verts[v]);
                ay = Math.min(ay, verts[v + 1]);
                bx = Math.max(bx, verts[v]);
                by = Math.max(by, verts[v + 1]);
            }
        }
        System.out.printf("世界包围盒: x %.1f..%.1f  y %.1f..%.1f%n", ax, bx, ay, by);

        double scale = Math.min(CANVAS / (double) (bx - ax), CANVAS / (double) (by - ay)) * 0.92;
        double cx = (ax + bx) / 2.0;
        double cy = (ay + by) / 2.0;

        // 重新从头播一遍，边播边出图
        state.clearTracks();
        state.setAnimation(0, meta.animation, false);
        Map<String, BufferedImage> tintCache = new HashMap<>();
        for (int i = 0; i < frames; i++) {
            if (setupPoseOnly) {
                skeleton.setToSetupPose();
                skeleton.updateWorldTransform(Skeleton.Physics.update);
            } else {
                state.update(i == 0 ? 0f : dt);
                state.apply(skeleton);
                skeleton.updateWorldTransform(Skeleton.Physics.update);
            }
            BufferedImage canvas = new BufferedImage(CANVAS, CANVAS, BufferedImage.TYPE_INT_ARGB);
            Graphics2D g = canvas.createGraphics();
            g.setRenderingHint(RenderingHints.KEY_INTERPOLATION,
                    RenderingHints.VALUE_INTERPOLATION_BILINEAR);
            g.setColor(new Color(60, 60, 68));
            g.fillRect(0, 0, CANVAS, CANVAS);
            int drawn = drawSkeleton(g, skeleton, scale, cx, cy, tintCache);
            g.dispose();
            File out = new File(outDir, String.format("frame_%02d.png", i));
            ImageIO.write(canvas, "png", out);
            System.out.printf("  第 %2d 帧 (t=%.2fs) 画了 %d 个附件 -> %s%n",
                    i, i * dt, drawn, out.getName());
        }
    }

    private static float[] collect(float[] buffer, Skeleton skeleton) {
        Array<Slot> order = skeleton.getDrawOrder();
        int needed = 0;
        for (int i = 0; i < order.size; i++) {
            Slot slot = order.get(i);
            Object attachment = slot.getAttachment();
            if (attachment instanceof RegionAttachment) {
                needed += 8;
            } else if (attachment instanceof MeshAttachment) {
                needed += ((MeshAttachment) attachment).getWorldVerticesLength();
            }
        }
        if (buffer.length < needed) {
            buffer = new float[needed];
        }
        int at = 0;
        for (int i = 0; i < order.size; i++) {
            Slot slot = order.get(i);
            Object attachment = slot.getAttachment();
            if (attachment instanceof RegionAttachment) {
                float[] one = new float[8];
                ((RegionAttachment) attachment).computeWorldVertices(slot, one, 0, 2);
                System.arraycopy(one, 0, buffer, at, 8);
                at += 8;
            } else if (attachment instanceof MeshAttachment) {
                MeshAttachment mesh = (MeshAttachment) attachment;
                int n = mesh.getWorldVerticesLength();
                float[] one = new float[n];
                mesh.computeWorldVertices(slot, 0, n, one, 0, 2);
                System.arraycopy(one, 0, buffer, at, n);
                at += n;
            }
        }
        return at == buffer.length ? buffer : java.util.Arrays.copyOf(buffer, at);
    }

    private static int drawSkeleton(Graphics2D g, Skeleton skeleton, double scale,
                                    double cx, double cy, Map<String, BufferedImage> tintCache) {
        g.setComposite(AlphaComposite.SrcOver);
        float skA = skeleton.getColor().a;
        Array<Slot> order = skeleton.getDrawOrder();
        int drawn = 0;
        for (int i = 0; i < order.size; i++) {
            Slot slot = order.get(i);
            Object attachment = slot.getAttachment();
            if (!(attachment instanceof RegionAttachment) && !(attachment instanceof MeshAttachment)) {
                continue;
            }
            float a = skA * slot.getColor().a;
            float r = slot.getColor().r, gg = slot.getColor().g, b = slot.getColor().b;
            if (attachment instanceof RegionAttachment) {
                RegionAttachment region = (RegionAttachment) attachment;
                float[] v = new float[8];
                region.computeWorldVertices(slot, v, 0, 2);
                float[] uv = region.getUVs();
                a *= region.getColor().a;
                r *= region.getColor().r;
                gg *= region.getColor().g;
                b *= region.getColor().b;
                BufferedImage page = pageImage(region.getRegion(), tintCache, r, gg, b);
                if (page == null || a <= 0.003f) {
                    continue;
                }
                g.setComposite(AlphaComposite.getInstance(AlphaComposite.SRC_OVER,
                        Math.max(0f, Math.min(1f, a))));
                triangle(g, page, v, uv, 0, 1, 2, scale, cx, cy);
                triangle(g, page, v, uv, 0, 2, 3, scale, cx, cy);
                drawn++;
            } else {
                MeshAttachment mesh = (MeshAttachment) attachment;
                int n = mesh.getWorldVerticesLength();
                float[] v = new float[n];
                mesh.computeWorldVertices(slot, 0, n, v, 0, 2);
                float[] uv = mesh.getUVs();
                short[] tris = mesh.getTriangles();
                a *= mesh.getColor().a;
                r *= mesh.getColor().r;
                gg *= mesh.getColor().g;
                b *= mesh.getColor().b;
                BufferedImage page = pageImage(mesh.getRegion(), tintCache, r, gg, b);
                if (page == null || uv == null || tris == null || a <= 0.003f) {
                    continue;
                }
                g.setComposite(AlphaComposite.getInstance(AlphaComposite.SRC_OVER,
                        Math.max(0f, Math.min(1f, a))));
                for (int t = 0; t + 2 < tris.length; t += 3) {
                    triangle(g, page, v, uv, tris[t], tris[t + 1], tris[t + 2], scale, cx, cy);
                }
                drawn++;
            }
        }
        return drawn;
    }

    private static BufferedImage pageImage(com.badlogic.gdx.graphics.g2d.TextureRegion region,
                                           Map<String, BufferedImage> tintCache,
                                           float r, float g, float b) {
        if (region == null) {
            return null;
        }
        Texture texture = region.getTexture();
        if (texture == null) {
            return null;
        }
        int ri = Math.round(r * 255);
        int gi = Math.round(g * 255);
        int bi = Math.round(b * 255);
        if (ri == 255 && gi == 255 && bi == 255) {
            return texture.image;
        }
        String key = texture.file.getName() + '#' + ri + ',' + gi + ',' + bi;
        BufferedImage cached = tintCache.get(key);
        if (cached != null) {
            return cached;
        }
        BufferedImage source = texture.image;
        BufferedImage out = new BufferedImage(source.getWidth(), source.getHeight(),
                BufferedImage.TYPE_INT_ARGB);
        for (int y = 0; y < source.getHeight(); y++) {
            for (int x = 0; x < source.getWidth(); x++) {
                int argb = source.getRGB(x, y);
                int a = (argb >>> 24) & 0xff;
                int cr = ((argb >> 16) & 0xff) * ri / 255;
                int cg = ((argb >> 8) & 0xff) * gi / 255;
                int cb = (argb & 0xff) * bi / 255;
                out.setRGB(x, y, (a << 24) | (cr << 16) | (cg << 8) | cb);
            }
        }
        tintCache.put(key, out);
        return out;
    }

    private static void triangle(Graphics2D g, BufferedImage image, float[] v, float[] uv,
                                 int i0, int i1, int i2, double scale, double cx, double cy) {
        if (i0 * 2 + 1 >= v.length || i1 * 2 + 1 >= v.length || i2 * 2 + 1 >= v.length) {
            return;
        }
        if (i0 * 2 + 1 >= uv.length || i1 * 2 + 1 >= uv.length || i2 * 2 + 1 >= uv.length) {
            return;
        }
        double x0 = (v[i0 * 2] - cx) * scale + CANVAS / 2.0;
        double y0 = (cy - v[i0 * 2 + 1]) * scale + CANVAS / 2.0;
        double x1 = (v[i1 * 2] - cx) * scale + CANVAS / 2.0;
        double y1 = (cy - v[i1 * 2 + 1]) * scale + CANVAS / 2.0;
        double x2 = (v[i2 * 2] - cx) * scale + CANVAS / 2.0;
        double y2 = (cy - v[i2 * 2 + 1]) * scale + CANVAS / 2.0;
        double area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0);
        if (Math.abs(area) < 0.6) {
            return;
        }
        double w = image.getWidth();
        double h = image.getHeight();
        AffineTransform at = affine(uv[i0 * 2] * w, uv[i0 * 2 + 1] * h,
                uv[i1 * 2] * w, uv[i1 * 2 + 1] * h,
                uv[i2 * 2] * w, uv[i2 * 2 + 1] * h,
                x0, y0, x1, y1, x2, y2);
        if (at == null) {
            return;
        }
        Path2D path = new Path2D.Double();
        path.moveTo(x0, y0);
        path.lineTo(x1, y1);
        path.lineTo(x2, y2);
        path.closePath();
        Shape old = g.getClip();
        g.clip(path);
        g.drawImage(image, at, null);
        g.setClip(old);
    }

    /** 由三组对应点解出仿射变换；三角形退化时返回 null。 */
    private static AffineTransform affine(double x0, double y0, double x1, double y1,
                                          double x2, double y2, double X0, double Y0,
                                          double X1, double Y1, double X2, double Y2) {
        double det = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0);
        if (Math.abs(det) < 1e-9) {
            return null;
        }
        double a = ((X1 - X0) * (y2 - y0) - (X2 - X0) * (y1 - y0)) / det;
        double b = ((X2 - X0) * (x1 - x0) - (X1 - X0) * (x2 - x0)) / det;
        double c = X0 - a * x0 - b * y0;
        double d = ((Y1 - Y0) * (y2 - y0) - (Y2 - Y0) * (y1 - y0)) / det;
        double e = ((Y2 - Y0) * (x1 - x0) - (Y1 - Y0) * (x2 - x0)) / det;
        double f = Y0 - d * x0 - e * y0;
        return new AffineTransform(a, d, b, e, c, f);
    }

    private static TextureAtlas loadAtlas(File atlasFile, File dir) {
        FileHandle utf8Atlas = new FileHandle(atlasFile) {
            @Override
            public BufferedReader reader(int bufferSize) {
                return super.reader(bufferSize, StandardCharsets.UTF_8.name());
            }
        };
        TextureAtlas.TextureAtlasData data = new TextureAtlas.TextureAtlasData(
                utf8Atlas, new FileHandle(dir), false);
        for (TextureAtlas.TextureAtlasData.Page page : data.getPages()) {
            File image = new File(page.textureFile.path());
            page.texture = new Texture(new FileHandle(image), page.pma,
                    (int) page.width, (int) page.height);
        }
        return new TextureAtlas(data);
    }

    /** 读抽取脚本写出的扁平清单 cutin.txt（每段以 [rig N] 开头）。 */
    private static Meta readMeta(File cutinDir) throws Exception {
        Meta meta = new Meta();
        for (String raw : Files.readAllLines(new File(cutinDir, "cutin.txt").toPath(),
                StandardCharsets.UTF_8)) {
            String line = raw.trim();
            int eq = line.indexOf('=');
            if (eq < 0) {
                continue;
            }
            String key = line.substring(0, eq);
            String value = line.substring(eq + 1);
            switch (key) {
                case "skeleton" -> meta.skeleton = value;
                case "animation" -> meta.animation = value;
                case "duration" -> meta.duration = Float.parseFloat(value);
                case "atlas" -> meta.atlas = meta.atlas == null ? value : meta.atlas;
                default -> {
                }
            }
        }
        if (meta.skeleton == null || meta.atlas == null) {
            throw new IllegalStateException("cutin.txt 里缺 skeleton/atlas");
        }
        return meta;
    }

    private static final class Meta {
        String skeleton;
        String animation;
        float duration;
        String atlas;
    }
}
