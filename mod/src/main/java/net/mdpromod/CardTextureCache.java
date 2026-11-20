package net.mdpromod;

import com.mojang.blaze3d.platform.NativeImage;
import java.awt.Graphics2D;
import java.awt.RenderingHints;
import java.awt.image.BufferedImage;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.HashMap;
import java.util.Iterator;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Map.Entry;
import javax.imageio.ImageIO;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.texture.DynamicTexture;
import net.minecraft.resources.ResourceLocation;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public final class CardTextureCache {
    public static final ResourceLocation CARD_BACK = ResourceLocation.fromNamespaceAndPath("mdpromod", "textures/entity/card_back.png");
    private static final int WIDTH = 176;
    private static final int HEIGHT = 256;
    private static final int MAX_CACHE = 128;
    private static final LinkedHashMap<Integer, CardTextureCache.CachedTexture> CACHE = new LinkedHashMap<>(32, 0.75F, true);
    private static final LinkedHashMap<Integer, CardTextureCache.CachedTexture> GRAY_CACHE = new LinkedHashMap<>(16, 0.75F, true);
    private static final Map<Integer, Boolean> FAILED = new HashMap<>();

    private CardTextureCache() {
    }

    public static ResourceLocation get(int code) {
        return get(code, false);
    }

    public static ResourceLocation get(int code, boolean gray) {
        if (code <= 0) {
            return CARD_BACK;
        }

        CardTextureCache.CachedTexture cached = (gray ? GRAY_CACHE : CACHE).get(code);
        if (cached != null) {
            return cached.location();
        }

        if (gray) {
            ResourceLocation colored = get(code, false);
            if (colored == CARD_BACK) {
                return CARD_BACK;
            }

            boolean failedBefore = FAILED.containsKey(code);
            ResourceLocation built = build(code, true);
            if (built != CARD_BACK) {
                return built;
            }

            if (!failedBefore) {
                FAILED.remove(code);
            }

            return colored;
        } else {
            return FAILED.containsKey(code) ? CARD_BACK : build(code, false);
        }
    }

    private static ResourceLocation build(int code, boolean gray) {
        Path path = texturePath(code);
        if (path == null) {
            if (ModBundle.ready()) {
                FAILED.put(code, Boolean.TRUE);
            }

            return CARD_BACK;
        } else {
            try {
                BufferedImage image = ImageIO.read(path.toFile());
                if (image == null) {
                    FAILED.put(code, Boolean.TRUE);
                    return CARD_BACK;
                }

                if (image.getWidth() != 176 || image.getHeight() != 256) {
                    image = resize(image, 176, 256);
                }

                NativeImage nativeImage = new NativeImage(176, 256, false);

                for (int y = 0; y < 256; y++) {
                    for (int x = 0; x < 176; x++) {
                        int argb = image.getRGB(x, y);
                        int abgr;
                        if (gray) {
                            int luminance = (int)((argb >> 16 & 0xFF) * 0.299 + (argb >> 8 & 0xFF) * 0.587 + (argb & 0xFF) * 0.114);
                            abgr = argb & 0xFF000000 | luminance << 16 | luminance << 8 | luminance;
                        } else {
                            abgr = argb & -16711936 | (argb & 0xFF0000) >> 16 | (argb & 0xFF) << 16;
                        }

                        nativeImage.setPixelRGBA(x, y, abgr);
                    }
                }

                ResourceLocation location = ResourceLocation.fromNamespaceAndPath("mdpromod", "dynamic/card/" + (gray ? "gray/" : "") + code);
                DynamicTexture texture = new DynamicTexture(nativeImage);
                texture.setFilter(false, false);
                Minecraft.getInstance().getTextureManager().register(location, texture);
                LinkedHashMap<Integer, CardTextureCache.CachedTexture> cache = gray ? GRAY_CACHE : CACHE;
                cache.put(code, new CardTextureCache.CachedTexture(location, texture));
                evict(cache);
                return location;
            } catch (Exception ex) {
                FAILED.put(code, Boolean.TRUE);
                return CARD_BACK;
            }
        }
    }

    private static Path texturePath(int code) {
        Path path = ModPaths.resolveFile("art", "mdpromod.cardTextureRoot", "D:\\MDProMod\\runtime\\card_textures_176x256_q80", code + ".jpg");
        return Files.isRegularFile(path) ? path : null;
    }

    private static void evict(LinkedHashMap<Integer, CardTextureCache.CachedTexture> cache) {
        while (cache.size() > 128) {
            Iterator<Entry<Integer, CardTextureCache.CachedTexture>> iterator = cache.entrySet().iterator();
            if (!iterator.hasNext()) {
                return;
            }

            Entry<Integer, CardTextureCache.CachedTexture> entry = iterator.next();
            Minecraft.getInstance().getTextureManager().release(entry.getValue().location());
            iterator.remove();
        }
    }

    private static BufferedImage resize(BufferedImage source, int width, int height) {
        BufferedImage result = new BufferedImage(width, height, 2);
        Graphics2D graphics = result.createGraphics();
        graphics.setRenderingHint(RenderingHints.KEY_INTERPOLATION, RenderingHints.VALUE_INTERPOLATION_BILINEAR);
        graphics.drawImage(source, 0, 0, width, height, null);
        graphics.dispose();
        return result;
    }

    private record CachedTexture(ResourceLocation location, DynamicTexture texture) {
    }
}
