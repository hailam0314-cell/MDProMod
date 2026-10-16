package com.badlogic.gdx.graphics;

/**
 * 桩：只为了让 gdx 的 TextureAtlas / Texture 签名能链接上，不实现任何像素操作。
 * 桌面预览器不碰 GL，真正的纹理载入在自己的 Texture 里用 ImageIO 做。
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
