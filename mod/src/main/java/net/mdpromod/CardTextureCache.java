package net.mdpromod;

import com.mojang.blaze3d.platform.NativeImage;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.texture.DynamicTexture;
import net.minecraft.resources.ResourceLocation;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

import javax.imageio.ImageIO;
import java.awt.Graphics2D;
import java.awt.RenderingHints;
import java.awt.image.BufferedImage;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.HashMap;
import java.util.Iterator;
import java.util.LinkedHashMap;
import java.util.Map;

@OnlyIn(Dist.CLIENT)
public final class CardTextureCache {
    public static final ResourceLocation CARD_BACK =
            ResourceLocation.fromNamespaceAndPath(MDProMod.MODID, "textures/entity/card_back.png");
    private static final int WIDTH = 176;
    private static final int HEIGHT = 256;
    private static final int MAX_CACHE = 128;
    private static final LinkedHashMap<Integer, CachedTexture> CACHE =
            new LinkedHashMap<>(32, 0.75F, true);
    /** 灰度版本（效果被无效时用），按需生成，不额外占磁盘。 */
    private static final LinkedHashMap<Integer, CachedTexture> GRAY_CACHE =
            new LinkedHashMap<>(16, 0.75F, true);
    private static final Map<Integer, Boolean> FAILED = new HashMap<>();

    private CardTextureCache() {
    }

    private record CachedTexture(ResourceLocation location, DynamicTexture texture) {
    }

    public static ResourceLocation get(int code) {
        return get(code, false);
    }

    /** gray = true 时返回"效果被无效"用的灰度版；生成不出来就退回原色，绝不空白。 */
    public static ResourceLocation get(int code, boolean gray) {
        if (code <= 0) return CARD_BACK;
        CachedTexture cached = (gray ? GRAY_CACHE : CACHE).get(code);
        if (cached != null) return cached.location();
        if (gray) {
            ResourceLocation colored = get(code, false);
            if (colored == CARD_BACK) return CARD_BACK;
            boolean failedBefore = FAILED.containsKey(code);
            ResourceLocation built = build(code, true);
            if (built != CARD_BACK) return built;
            // 灰度版没生成出来：别把它记成"这张卡没有卡图"，退回原色
            if (!failedBefore) FAILED.remove(code);
            return colored;
        }
        if (FAILED.containsKey(code)) return CARD_BACK;
        return build(code, false);
    }

    private static ResourceLocation build(int code, boolean gray) {
        Path path = texturePath(code);
        if (path == null) {
            // 解包还没跑完时别把"暂时没有"记成"这张卡没有卡图"，否则解包完了还是卡背
            if (ModBundle.ready()) FAILED.put(code, Boolean.TRUE);
            return CARD_BACK;
        }
        try {
            BufferedImage image = ImageIO.read(path.toFile());
            if (image == null) {
                FAILED.put(code, Boolean.TRUE);
                return CARD_BACK;
            }
            if (image.getWidth() != WIDTH || image.getHeight() != HEIGHT) {
                image = resize(image, WIDTH, HEIGHT);
            }
            NativeImage nativeImage = new NativeImage(WIDTH, HEIGHT, false);
            for (int y = 0; y < HEIGHT; y++) {
                for (int x = 0; x < WIDTH; x++) {
                    int argb = image.getRGB(x, y);
                    int abgr;
                    if (gray) {
                        // 转灰度：卡面还有卡名/数值这类信息，这里只去色、不压暗，保持可读
                        int luminance = (int) (((argb >> 16 & 0xFF) * 0.299)
                                + ((argb >> 8 & 0xFF) * 0.587)
                                + ((argb & 0xFF) * 0.114));
                        abgr = (argb & 0xFF000000) | (luminance << 16) | (luminance << 8) | luminance;
                    } else {
                        abgr = (argb & 0xFF00FF00)
                                | ((argb & 0x00FF0000) >> 16)
                                | ((argb & 0x000000FF) << 16);
                    }
                    nativeImage.setPixelRGBA(x, y, abgr);
                }
            }
            ResourceLocation location = ResourceLocation.fromNamespaceAndPath(
                    MDProMod.MODID, "dynamic/card/" + (gray ? "gray/" : "") + code);
            DynamicTexture texture = new DynamicTexture(nativeImage);
            texture.setFilter(false, false);
            Minecraft.getInstance().getTextureManager().register(location, texture);
            LinkedHashMap<Integer, CachedTexture> cache = gray ? GRAY_CACHE : CACHE;
            cache.put(code, new CachedTexture(location, texture));
            evict(cache);
            return location;
        } catch (Exception ex) {
            FAILED.put(code, Boolean.TRUE);
            return CARD_BACK;
        }
    }

    private static Path texturePath(int code) {
        Path path = ModPaths.resolveFile("art", "mdpromod.cardTextureRoot",
                ModPaths.DEFAULT_ART, code + ".jpg");
        return Files.isRegularFile(path) ? path : null;
    }

    private static void evict(LinkedHashMap<Integer, CachedTexture> cache) {
        while (cache.size() > MAX_CACHE) {
            Iterator<Map.Entry<Integer, CachedTexture>> iterator = cache.entrySet().iterator();
            if (!iterator.hasNext()) return;
            Map.Entry<Integer, CachedTexture> entry = iterator.next();
            Minecraft.getInstance().getTextureManager().release(entry.getValue().location());
            iterator.remove();
        }
    }

    private static BufferedImage resize(BufferedImage source, int width, int height) {
        BufferedImage result = new BufferedImage(width, height, BufferedImage.TYPE_INT_ARGB);
        Graphics2D graphics = result.createGraphics();
        graphics.setRenderingHint(RenderingHints.KEY_INTERPOLATION,
                RenderingHints.VALUE_INTERPOLATION_BILINEAR);
        graphics.drawImage(source, 0, 0, width, height, null);
        graphics.dispose();
        return result;
    }
}
