package net.mdpromod;

import com.mojang.blaze3d.platform.NativeImage;
import java.awt.Graphics2D;
import java.awt.image.BufferedImage;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;
import javax.imageio.ImageIO;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.texture.DynamicTexture;
import net.minecraft.resources.ResourceLocation;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public final class SummonFxTextures {
    private static final Map<String, ResourceLocation> CACHE = new HashMap<>();
    private static final Set<String> MISSING = new HashSet<>();
    private static final Set<String> LUMINANCE_TO_ALPHA = Set.of(
        "ritual_circle_01", "ritual_circle_03", "ritual_fire_01", "pendulum_mask", "pendulum_spot", "flash", "synchro_noise", "techring_03"
    );

    private SummonFxTextures() {
    }

    public static ResourceLocation get(String name) {
        if (name != null && !name.isEmpty()) {
            ResourceLocation cached = CACHE.get(name);
            if (cached != null) {
                return cached;
            }

            if (MISSING.contains(name)) {
                return null;
            }

            Path path = ModPaths.resolveFile("summonfx", "mdpromod.summonFxRoot", "D:\\MDProMod\\runtime\\summon_fx", name + ".png");
            if (!Files.isRegularFile(path)) {
                if (ModBundle.ready()) {
                    MISSING.add(name);
                }

                return null;
            } else {
                try {
                    BufferedImage image = ImageIO.read(path.toFile());
                    if (image == null) {
                        MISSING.add(name);
                        return null;
                    }

                    BufferedImage argb = image;
                    if (image.getType() != 2) {
                        argb = new BufferedImage(image.getWidth(), image.getHeight(), 2);
                        Graphics2D graphics = argb.createGraphics();
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
                                int red = pixel >> 16 & 0xFF;
                                int green = pixel >> 8 & 0xFF;
                                int blue = pixel & 0xFF;
                                int alpha = Math.max(red, Math.max(green, blue));
                                pixel = alpha << 24 | 16777215;
                            }

                            int abgr = pixel & -16711936 | (pixel & 0xFF0000) >> 16 | (pixel & 0xFF) << 16;
                            nativeImage.setPixelRGBA(x, y, abgr);
                        }
                    }

                    ResourceLocation location = ResourceLocation.fromNamespaceAndPath("mdpromod", "dynamic/summonfx/" + name);
                    DynamicTexture texture = new DynamicTexture(nativeImage);
                    texture.setFilter(true, false);
                    Minecraft.getInstance().getTextureManager().register(location, texture);
                    CACHE.put(name, location);
                    return location;
                } catch (Exception ex) {
                    MISSING.add(name);
                    return null;
                }
            }
        } else {
            return null;
        }
    }
}
