package com.badlogic.gdx.graphics;

import com.badlogic.gdx.files.FileHandle;
import com.badlogic.gdx.Files.FileType;

import java.io.DataInputStream;
import java.io.File;
import java.io.IOException;
import java.io.InputStream;

/**
 * 桩：顶替 libGDX 真正的 Texture（那个一构造就要 GL 上下文）。Spine 运行时只在
 * 图集装载时用到这里的两件事——**贴图尺寸**（算 UV）和过滤/环绕设置（我们空实现）。
 *
 * 尺寸直接从 PNG 的 IHDR 读，不解码像素，所以哪怕 4096² 的图也只是一次 24 字节的读。
 * 真正的贴图上传在 Minecraft 侧做（见 {@code ClientCutinTextures}），用这里记下的
 * {@link #openStream()} 去注册一个 DynamicTexture，支持模组内资源与开发目录。
 *
 * 之所以要自己写一份而不是直接改 spine 的代码：libGDX 的 TextureAtlas / TextureRegion
 * 是**按类名**引用 Texture 的，把同名的桩放在 classpath 前面就能让它们全部链接过来，
 * 于是 UV、旋转区域、scale 这些算法仍然是官方的。
 */
public class Texture {

    public enum TextureFilter {
        Nearest, Linear, MipMap, MipMapNearestNeighbor, MipMapLinearNearestNeighbor,
        MipMapNearestNeighborLinear, MipMapLinearLinear;

        /** gdx 解析图集 "filter:" 行时会调这个。 */
        public boolean isMipMap() {
            return this != Nearest && this != Linear;
        }

        public int getGLEnum() {
            return 0;
        }
    }

    public enum TextureWrap {
        MirroredRepeat, ClampToEdge, Repeat;

        public int getGLEnum() {
            return 0;
        }
    }

    /** 开发模式下的分页文件；模组内资源没有磁盘文件。 */
    public final File file;
    private final FileHandle source;
    /** 该分页是不是预乘 alpha（atlas 里的 {@code pma:true}）——决定渲染时的混合模式。 */
    public final boolean pma;
    private final int width;
    private final int height;
    private final int pixelWidth;
    private final int pixelHeight;

    public Texture(FileHandle handle) {
        this(handle, false);
    }

    public Texture(FileHandle handle, boolean pma) {
        this(handle, pma, 0, 0);
    }

    /** Atlas coordinates stay in logical pixels while the uploaded PNG may be downsampled. */
    public Texture(FileHandle handle, boolean pma, int logicalWidth, int logicalHeight) {
        this.source = handle;
        this.file = handle.type() == FileType.Classpath ? null : handle.file();
        this.pma = pma;
        int[] size = readPngSize(handle);
        this.pixelWidth = size[0];
        this.pixelHeight = size[1];
        this.width = logicalWidth > 0 ? logicalWidth : size[0];
        this.height = logicalHeight > 0 ? logicalHeight : size[1];
    }

    public Texture(FileHandle handle, Pixmap.Format format, boolean useMipMaps) {
        this(handle, false);
    }

    /** 只读 PNG 头，拿宽高（PNG 签名 8 + 长度 4 + "IHDR" 4 + 宽 4 + 高 4）。 */
    private static int[] readPngSize(FileHandle source) {
        try (InputStream stream = source.read();
             DataInputStream in = new DataInputStream(new java.io.BufferedInputStream(stream))) {
            byte[] header = new byte[24];
            in.readFully(header);
            if (header[0] != (byte) 0x89 || header[1] != 'P' || header[2] != 'N' || header[3] != 'G') {
                throw new IOException("不是 PNG: " + source);
            }
            int width = ((header[16] & 0xff) << 24) | ((header[17] & 0xff) << 16)
                    | ((header[18] & 0xff) << 8) | (header[19] & 0xff);
            int height = ((header[20] & 0xff) << 24) | ((header[21] & 0xff) << 16)
                    | ((header[22] & 0xff) << 8) | (header[23] & 0xff);
            if (width <= 0 || height <= 0) {
                throw new IOException("无效的 PNG 尺寸: " + source);
            }
            return new int[]{width, height};
        } catch (IOException error) {
            throw new RuntimeException("读不出图集分页 " + source, error);
        }
    }

    public InputStream openStream() {
        return source.read();
    }

    public String sourceKey() {
        return file == null ? "classpath:/" + source.path() : file.getAbsolutePath();
    }

    public int getPixelWidth() {
        return pixelWidth;
    }

    public int getPixelHeight() {
        return pixelHeight;
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
