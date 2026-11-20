package net.mdpromod;

import com.mojang.blaze3d.platform.NativeImage;
import java.awt.Graphics2D;
import java.awt.image.BufferedImage;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.HashMap;
import java.util.Iterator;
import java.util.LinkedHashMap;
import java.util.Map;
import javax.imageio.ImageIO;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.texture.DynamicTexture;
import net.minecraft.resources.ResourceLocation;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public final class CloseupTextureCache {
    private static final int MAX_CACHE = 96;
    private static final LinkedHashMap<Integer, CloseupTextureCache.Entry> CACHE = new LinkedHashMap<>(32, 0.75F, true);
    private static final LinkedHashMap<Integer, CloseupTextureCache.Entry> GRAY_CACHE = new LinkedHashMap<>(16, 0.75F, true);
    private static final Map<Integer, Boolean> MISSING = new HashMap<>();

    private CloseupTextureCache() {
    }

    public static CloseupTextureCache.Entry get(int code) {
        return get(code, false);
    }

    public static CloseupTextureCache.Entry get(int code, boolean gray) {
        if (code <= 0) {
            return null;
        }

        CloseupTextureCache.Entry cached = (gray ? GRAY_CACHE : CACHE).get(code);
        if (cached != null) {
            return cached;
        }

        CloseupTextureCache.Entry colored = gray ? get(code, false) : null;
        if (gray && colored == null) {
            return null;
        }

        if (gray && colored != null) {
            boolean missingBefore = MISSING.containsKey(code);
            CloseupTextureCache.Entry built = build(code, true);
            if (built != null) {
                return built;
            }

            if (!missingBefore) {
                MISSING.remove(code);
            }

            return colored;
        } else {
            return MISSING.containsKey(code) ? null : build(code, false);
        }
    }

    private static CloseupTextureCache.Entry build(int code, boolean gray) {
        Path path = texturePath(code);
        if (path == null) {
            if (ModBundle.ready()) {
                MISSING.put(code, Boolean.TRUE);
            }

            return null;
        } else {
            try {
                BufferedImage image = ImageIO.read(path.toFile());
                if (image != null && image.getWidth() > 0 && image.getHeight() > 0) {
                    BufferedImage argb = image;
                    if (image.getType() != 2) {
                        argb = new BufferedImage(image.getWidth(), image.getHeight(), 2);
                        Graphics2D graphics = argb.createGraphics();
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
                                int alpha = pixel >>> 24 & 0xFF;
                                int luminance = (int)((pixel >> 16 & 0xFF) * 0.299 + (pixel >> 8 & 0xFF) * 0.587 + (pixel & 0xFF) * 0.114);
                                int value = Math.min(255, (int)(luminance * 0.82));
                                abgr = alpha << 24 | value << 16 | value << 8 | value;
                            } else {
                                abgr = pixel & -16711936 | (pixel & 0xFF0000) >> 16 | (pixel & 0xFF) << 16;
                            }

                            nativeImage.setPixelRGBA(x, y, abgr);
                        }
                    }

                    ResourceLocation location = ResourceLocation.fromNamespaceAndPath("mdpromod", "dynamic/closeup/" + (gray ? "gray/" : "") + code);
                    DynamicTexture texture = new DynamicTexture(nativeImage);
                    texture.setFilter(true, false);
                    Minecraft.getInstance().getTextureManager().register(location, texture);
                    CloseupTextureCache.Entry entry = new CloseupTextureCache.Entry(location, (float)width / height);
                    LinkedHashMap<Integer, CloseupTextureCache.Entry> cache = gray ? GRAY_CACHE : CACHE;
                    cache.put(code, entry);
                    evict(cache);
                    return entry;
                } else {
                    MISSING.put(code, Boolean.TRUE);
                    return null;
                }
            } catch (Exception ex) {
                MISSING.put(code, Boolean.TRUE);
                return null;
            }
        }
    }

    private static Path texturePath(int code) {
        Path path = ModPaths.resolveFile("closeup", "mdpromod.closeupRoot", "D:\\MDProMod\\runtime\\closeup_128", code + ".png");
        return Files.isRegularFile(path) ? path : null;
    }

    private static void evict(LinkedHashMap<Integer, CloseupTextureCache.Entry> cache) {
        while (cache.size() > 96) {
            Iterator<Map.Entry<Integer, CloseupTextureCache.Entry>> iterator = cache.entrySet().iterator();
            if (!iterator.hasNext()) {
                return;
            }

            Map.Entry<Integer, CloseupTextureCache.Entry> entry = iterator.next();
            Minecraft.getInstance().getTextureManager().release(entry.getValue().location());
            iterator.remove();
        }
    }

    public record Entry(ResourceLocation location, float aspect) {
    }
}
