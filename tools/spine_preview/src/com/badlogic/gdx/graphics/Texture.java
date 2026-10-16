package com.badlogic.gdx.graphics;

import com.badlogic.gdx.files.FileHandle;

import javax.imageio.ImageIO;
import java.awt.image.BufferedImage;
import java.io.File;
import java.io.IOException;

/**
 * 桩：替掉 libGDX 真正的 Texture（那个要 GL 上下文），只保留 spine 运行时用得着的部分——
 * 尺寸、过滤/环绕设置（空实现）、以及从 PNG 读进内存的方法。
 *
 * 预览器靠 classpath 顺序把自己这版放到 gdx.jar 前面，于是 gdx 的 TextureAtlas /
 * TextureRegion 全部链接到这一个，UV、旋转、scale 的算法就还是官方的。
 */
public class Texture {

    public enum TextureFilter {
        Nearest, Linear, MipMap, MipMapNearestNeighbor, MipMapLinearNearestNeighbor,
        MipMapNearestNeighborLinear, MipMapLinearLinear;

        /** gdx 的 TextureAtlas 解析 "filter:" 那行时会调这个。 */
        public boolean isMipMap() {
            return this != Nearest && this != Linear;
        }

        public int getGLEnum() {
            return 0;
        }
    }

    public enum TextureWrap {
        MirroredRepeat, ClampToEdge, Repeat;
    }

    /** atlas 里写了 pma:true 时由预览器置位，读图时做反预乘。 */
    public static boolean PMA = false;

    public final File file;
    public BufferedImage image;
    private final int width;
    private final int height;

    public Texture(FileHandle handle) {
        this(handle, PMA, 0, 0);
    }

    public Texture(FileHandle handle, boolean pma, int logicalWidth, int logicalHeight) {
        this.file = new File(handle.path());
        BufferedImage loaded;
        try {
            loaded = ImageIO.read(this.file);
        } catch (IOException error) {
            throw new RuntimeException("读不出贴图 " + this.file, error);
        }
        if (loaded == null) {
            throw new RuntimeException("不是图片: " + this.file);
        }
        this.image = unPremultiplyIfNeeded(loaded, pma);
        this.width = logicalWidth > 0 ? logicalWidth : loaded.getWidth();
        this.height = logicalHeight > 0 ? logicalHeight : loaded.getHeight();
    }

    public Texture(FileHandle handle, Pixmap.Format format, boolean useMipMaps) {
        this(handle);
    }

    private static BufferedImage unPremultiplyIfNeeded(BufferedImage source, boolean pma) {
        if (!pma) {
            return source;
        }
        BufferedImage out = new BufferedImage(source.getWidth(), source.getHeight(),
                BufferedImage.TYPE_INT_ARGB);
        for (int y = 0; y < source.getHeight(); y++) {
            for (int x = 0; x < source.getWidth(); x++) {
                int argb = source.getRGB(x, y);
                int a = (argb >>> 24) & 0xff;
                if (a == 0 || a == 255) {
                    out.setRGB(x, y, a == 0 ? 0 : (argb | 0xff000000));
                    continue;
                }
                int r = Math.min(255, ((argb >> 16) & 0xff) * 255 / a);
                int g = Math.min(255, ((argb >> 8) & 0xff) * 255 / a);
                int b = Math.min(255, (argb & 0xff) * 255 / a);
                out.setRGB(x, y, (a << 24) | (r << 16) | (g << 8) | b);
            }
        }
        return out;
    }

    public int getWidth() {
        return width;
    }

    public int getHeight() {
        return height;
    }

    public void setFilter(TextureFilter minFilter, TextureFilter magFilter) {
    }

    public void setFilter(TextureFilter filter) {
    }

    public void setWrap(TextureWrap u, TextureWrap v) {
    }

    public void dispose() {
    }
}
