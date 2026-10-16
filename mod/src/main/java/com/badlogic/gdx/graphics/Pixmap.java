package com.badlogic.gdx.graphics;

/**
 * 桩：libGDX 的 Pixmap 要 GL/原生库，这里只保留 gdx 的 Texture/TextureAtlas 签名里
 * 引用到的那个 {@link Format} 枚举，好让类链接得上。模组里不做任何像素操作。
 */
public final class Pixmap {

    public enum Format {
        Alpha, Intensity, LuminanceAlpha, RGB565, RGBA4444, RGB888, RGBA8888;
    }

    public enum PixmapType {
        Pixmap, UnsignedByte, Byte, Int;
    }

    private Pixmap() {
    }
}
