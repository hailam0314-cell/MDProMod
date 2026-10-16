package net.mdpromod;

import com.mojang.blaze3d.platform.NativeImage;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.texture.DynamicTexture;
import net.minecraft.resources.ResourceLocation;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

import javax.imageio.ImageIO;
import java.awt.image.BufferedImage;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.HashMap;
import java.util.Iterator;
import java.util.LinkedHashMap;
import java.util.Map;

/**
 * 怪兽立绘（MDPro 的 {@code Picture/Closeup/{卡号}.png}）按需加载。
 *
 * 和 {@link CardTextureCache} 同一套路：LRU 缓存 + "缺图只查一次磁盘"，
 * 区别是立绘**带透明通道**（所以不能走 JPEG），并且要把宽高比带出去给渲染器摆位。
 * 根目录用 {@code -Dmdpromod.closeupRoot} 覆盖，默认 D:\MDProMod\runtime\closeup_128。
 */
@OnlyIn(Dist.CLIENT)
public final class CloseupTextureCache {
    private static final int MAX_CACHE = 96;
    private static final LinkedHashMap<Integer, Entry> CACHE = new LinkedHashMap<>(32, 0.75F, true);
    /** 灰度版本（效果被无效时用），按需生成，不额外占磁盘。 */
    private static final LinkedHashMap<Integer, Entry> GRAY_CACHE = new LinkedHashMap<>(16, 0.75F, true);
    private static final Map<Integer, Boolean> MISSING = new HashMap<>();

    private CloseupTextureCache() {
    }

    /** location = 贴图位置；aspect = 宽 / 高（摆位时按高度定宽）。 */
    public record Entry(ResourceLocation location, float aspect) {
    }

    public static Entry get(int code) {
        return get(code, false);
    }

    /** gray = true 时返回"效果被无效"用的灰度版；没有灰版就退回原色，绝不显示空白。 */
    public static Entry get(int code, boolean gray) {
        if (code <= 0) return null;
        Entry cached = (gray ? GRAY_CACHE : CACHE).get(code);
        if (cached != null) return cached;
        Entry colored = gray ? get(code, false) : null;
        if (gray && colored == null) return null;      // 连原图都没有 → 这张卡没有立绘
        if (gray && colored != null) {
            boolean missingBefore = MISSING.containsKey(code);
            Entry built = build(code, true);
            if (built != null) return built;
            if (!missingBefore) MISSING.remove(code);   // 灰度失败别记成"这张卡没立绘"
            return colored;
        }
        if (MISSING.containsKey(code)) return null;
        return build(code, false);
    }

    private static Entry build(int code, boolean gray) {
        Path path = texturePath(code);
        if (path == null) {
            // 解包没跑完时先别记账，否则解包完成后这张立绘还是不出来
            if (ModBundle.ready()) MISSING.put(code, Boolean.TRUE);
            return null;
        }
        try {
            BufferedImage image = ImageIO.read(path.toFile());
            if (image == null || image.getWidth() <= 0 || image.getHeight() <= 0) {
                MISSING.put(code, Boolean.TRUE);
                return null;
            }
            // 立绘是调色板 PNG：先统一成 INT_ARGB，再逐像素取（alpha 才拿得准）
            BufferedImage argb = image;
            if (image.getType() != BufferedImage.TYPE_INT_ARGB) {
                argb = new BufferedImage(image.getWidth(), image.getHeight(), BufferedImage.TYPE_INT_ARGB);
                java.awt.Graphics2D graphics = argb.createGraphics();
                graphics.drawImage(image, 0, 0, null);
                graphics.dispose();
            }
            int width = argb.getWidth();
            int height = argb.getHeight();
            NativeImage nativeImage = new NativeImage(width, height, false);
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int pixel = argb.getRGB(x, y);
                    int abgr;
                    if (gray) {
                        int alpha = (pixel >>> 24) & 0xFF;
                        int luminance = (int) (((pixel >> 16 & 0xFF) * 0.299)
                                + ((pixel >> 8 & 0xFF) * 0.587)
                                + ((pixel & 0xFF) * 0.114));
                        int value = Math.min(255, (int) (luminance * 0.82));   // 略微压暗，读起来像"被无效"
                        abgr = (alpha << 24) | (value << 16) | (value << 8) | value;
                    } else {
                        abgr = (pixel & 0xFF00FF00)
                                | ((pixel & 0x00FF0000) >> 16)
                                | ((pixel & 0x000000FF) << 16);
                    }
                    nativeImage.setPixelRGBA(x, y, abgr);
                }
            }
            ResourceLocation location = ResourceLocation.fromNamespaceAndPath(
                    MDProMod.MODID, "dynamic/closeup/" + (gray ? "gray/" : "") + code);
            DynamicTexture texture = new DynamicTexture(nativeImage);
            texture.setFilter(true, false);      // 立绘是斜着看的，缩小也要顺滑
            Minecraft.getInstance().getTextureManager().register(location, texture);
            Entry entry = new Entry(location, (float) width / height);
            LinkedHashMap<Integer, Entry> cache = gray ? GRAY_CACHE : CACHE;
            cache.put(code, entry);
            evict(cache);
            return entry;
        } catch (Exception ex) {
            MISSING.put(code, Boolean.TRUE);
            return null;
        }
    }

    private static Path texturePath(int code) {
        Path path = ModPaths.resolveFile("closeup", "mdpromod.closeupRoot",
                ModPaths.DEFAULT_CLOSEUP, code + ".png");
        return Files.isRegularFile(path) ? path : null;
    }

    private static void evict(LinkedHashMap<Integer, Entry> cache) {
        while (cache.size() > MAX_CACHE) {
            Iterator<Map.Entry<Integer, Entry>> iterator = cache.entrySet().iterator();
            if (!iterator.hasNext()) return;
            Map.Entry<Integer, Entry> entry = iterator.next();
            Minecraft.getInstance().getTextureManager().release(entry.getValue().location());
            iterator.remove();
        }
    }
}
