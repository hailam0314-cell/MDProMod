package net.mdpromod;

import com.badlogic.gdx.graphics.Texture;
import com.mojang.blaze3d.platform.NativeImage;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Set;
import java.util.UUID;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.texture.DynamicTexture;
import net.minecraft.resources.ResourceLocation;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;
import org.slf4j.LoggerFactory;

@OnlyIn(Dist.CLIENT)
public final class ClientCutinTextures {
    private static final int MAX_CACHED_PAGES = 64;
    private static final Map<String, ResourceLocation> CACHE = new LinkedHashMap<>(16, 0.75F, true);
    private static final Set<String> MISSING = new HashSet<>();

    private ClientCutinTextures() {
    }

    public static ResourceLocation of(Texture texture) {
        if (texture == null) {
            return null;
        }

        String key = texture.sourceKey();
        ResourceLocation cached = CACHE.get(key);
        if (cached != null) {
            return cached;
        }

        if (MISSING.contains(key)) {
            return null;
        }

        try (InputStream in = texture.openStream()) {
            NativeImage image = NativeImage.read(in);
            ResourceLocation location = ResourceLocation.fromNamespaceAndPath(
                "mdpromod", "dynamic/cutin/" + UUID.nameUUIDFromBytes(key.getBytes(StandardCharsets.UTF_8))
            );
            DynamicTexture dynamic = new DynamicTexture(image);
            dynamic.setFilter(true, false);
            Minecraft.getInstance().getTextureManager().register(location, dynamic);
            CACHE.put(key, location);
            if (CACHE.size() > 64) {
                String oldest = CACHE.keySet().iterator().next();
                Minecraft.getInstance().getTextureManager().release(CACHE.remove(oldest));
            }

            return location;
        } catch (Exception error) {
            LoggerFactory.getLogger("mdpromod").warn("cut-in 分页贴图注册失败 {}: {}", key, error.toString());
            MISSING.add(key);
            return null;
        }
    }

    public static void clear() {
        for (ResourceLocation location : CACHE.values()) {
            Minecraft.getInstance().getTextureManager().release(location);
        }

        CACHE.clear();
        MISSING.clear();
    }
}
