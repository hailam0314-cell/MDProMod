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
import java.util.HashSet;
import java.util.Map;
import java.util.Set;

/**
 * 召唤演出用的贴图：从 {@code {root}\{名字}.png} 按需加载（默认
 * D:\MDProMod\runtime\summon_fx，是从 MD 的 built-in/duel 里抽出来并压过的）。
 * 缺图就返回 null，调用方跳过这一层效果——不报错、不空白。
 */
@OnlyIn(Dist.CLIENT)
public final class SummonFxTextures {
    private static final Map<String, ResourceLocation> CACHE = new HashMap<>();
    private static final Set<String> MISSING = new HashSet<>();

    /**
     * 这几张在 MD 里是**加法**用的：黑底 + 白图案，完全没有 alpha 通道。
     * MC 这边走的是普通 alpha 混合，黑底会当作"半透明黑"糊在地面/模型上——
     * 原来那根光柱用的 {@code pendulum_spot} 就是这样一根硬邦邦的深色矩形。
     * 这几张载入时用亮度烘成 alpha：黑底全透明，白色图案按亮度发光，顶点色只管上色。
     */
    private static final Set<String> LUMINANCE_TO_ALPHA = Set.of(
            "ritual_circle_01", "ritual_circle_03", "ritual_fire_01", "pendulum_mask", "pendulum_spot");

    private SummonFxTextures() {
    }

    public static ResourceLocation get(String name) {
        if (name == null || name.isEmpty()) return null;
        ResourceLocation cached = CACHE.get(name);
        if (cached != null) return cached;
        if (MISSING.contains(name)) return null;
        Path path = ModPaths.resolveFile("summonfx", "mdpromod.summonFxRoot",
                ModPaths.DEFAULT_SUMMON_FX, name + ".png");
        if (!Files.isRegularFile(path)) {
            // 解包跑完之前不记"缺图"，不然特效会在这次游戏里一直缺席
            if (ModBundle.ready()) MISSING.add(name);
            return null;
        }
        try {
            BufferedImage image = ImageIO.read(path.toFile());
            if (image == null) {
                MISSING.add(name);
                return null;
            }
            BufferedImage argb = image;
            if (image.getType() != BufferedImage.TYPE_INT_ARGB) {
                argb = new BufferedImage(image.getWidth(), image.getHeight(), BufferedImage.TYPE_INT_ARGB);
                java.awt.Graphics2D graphics = argb.createGraphics();
                graphics.drawImage(image, 0, 0, null);
                graphics.dispose();
            }
            int width = argb.getWidth();
            int height = argb.getHeight();
            boolean luminanceAlpha = LUMINANCE_TO_ALPHA.contains(name);
            NativeImage nativeImage = new NativeImage(width, height, false);
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int pixel = argb.getRGB(x, y);
                    if (luminanceAlpha) {
                        int red = (pixel >> 16) & 0xFF;
                        int green = (pixel >> 8) & 0xFF;
                        int blue = pixel & 0xFF;
                        int alpha = Math.max(red, Math.max(green, blue));
                        pixel = (alpha << 24) | 0xFFFFFF;      // 图案留白，靠顶点色上色
                    }
                    int abgr = (pixel & 0xFF00FF00)
                            | ((pixel & 0x00FF0000) >> 16)
                            | ((pixel & 0x000000FF) << 16);
                    nativeImage.setPixelRGBA(x, y, abgr);
                }
            }
            ResourceLocation location = ResourceLocation.fromNamespaceAndPath(
                    MDProMod.MODID, "dynamic/summonfx/" + name);
            DynamicTexture texture = new DynamicTexture(nativeImage);
            // 注意：DynamicTexture 不会生成 mipmap，开了 mipmap 的过滤会让"被缩小采样"的贴图全黑
            // （第一版就是这样：光柱能看见，法阵全黑）。卡面/立绘同样只开线性、不开 mipmap。
            texture.setFilter(true, false);
            Minecraft.getInstance().getTextureManager().register(location, texture);
            CACHE.put(name, location);
            return location;
        } catch (Exception ex) {
            MISSING.add(name);
            return null;
        }
    }
}
