package net.mdpromod;

import com.badlogic.gdx.graphics.Texture;
import com.mojang.blaze3d.platform.NativeImage;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.texture.DynamicTexture;
import net.minecraft.resources.ResourceLocation;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Set;
import java.util.UUID;

/**
 * 把 cut-in 图集的分页 PNG 注册成 Minecraft 贴图。
 *
 * 关键一点：**像素原样上传，不做任何处理**。Spine 图集里写了 {@code pma:true} 的那批
 * 是预乘 alpha，正确的做法是保留它、用"预乘混合"去画（见 {@link ClientCutinEffects}
 * 的 {@code ONE, ONE_MINUS_SRC_ALPHA}），而不是把颜色除回去——除回去会在半透明边缘
 * 出现一圈亮边。
 */
@OnlyIn(Dist.CLIENT)
public final class ClientCutinTextures {

    private static final int MAX_CACHED_PAGES = 64;
    private static final Map<String, ResourceLocation> CACHE = new LinkedHashMap<>(16, 0.75F, true);
    private static final Set<String> MISSING = new HashSet<>();

    private ClientCutinTextures() {
    }

    /** Spine 运行时那边的贴图桩 -> Minecraft 的贴图位置；失败返回 null（调用方跳过）。 */
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
            // 用 MC 自带的 PNG 解码（原生、快），不经过 ImageIO
            NativeImage image = NativeImage.read(in);
            ResourceLocation location = ResourceLocation.fromNamespaceAndPath(
                    MDProMod.MODID, "dynamic/cutin/" + UUID.nameUUIDFromBytes(key.getBytes(StandardCharsets.UTF_8)));
            DynamicTexture dynamic = new DynamicTexture(image);
            // DynamicTexture 没有 mipmap：开 mipmap 会让"被缩小采样"的贴图整片变黑（踩过一次）
            dynamic.setFilter(true, false);
            Minecraft.getInstance().getTextureManager().register(location, dynamic);
            CACHE.put(key, location);
            if (CACHE.size() > MAX_CACHED_PAGES) {
                String oldest = CACHE.keySet().iterator().next();
                Minecraft.getInstance().getTextureManager().release(CACHE.remove(oldest));
            }
            return location;
        } catch (Exception error) {
            org.slf4j.LoggerFactory.getLogger("mdpromod").warn(
                    "cut-in 分页贴图注册失败 {}: {}", key, error.toString());
            MISSING.add(key);
            return null;
        }
    }

    /** 换素材目录 / 释放显存时清缓存。 */
    public static void clear() {
        for (ResourceLocation location : CACHE.values()) {
            Minecraft.getInstance().getTextureManager().release(location);
        }
        CACHE.clear();
        MISSING.clear();
    }
}
