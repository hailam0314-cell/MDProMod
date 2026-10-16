"""MDProMod 决斗场地按钮：贴图与规格生成器。

输出（16 单位 = 1 方块，与 assets/duel_field 同一套约定）：
  textures/*.png        按钮贴图分块表（顶面 / 侧面 / 底面各一块）
  control_spec.json     几何、UV 分块与状态表，Blockbench 与 Java 渲染器共用
  preview_sheet.png     贴图速览图
并把贴图同步复制到 mod 资源目录 assets/mdpromod/textures/entity/。
"""
from pathlib import Path
import json
import shutil

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
MOD_ENTITY_TEXTURES = Path(r"D:\MDProMod\mod\src\main\resources\assets\mdpromod\textures\entity")
FONT_BOLD = "C:/Windows/Fonts/msyhbd.ttc"
SS = 3  # 超采样倍率，画完再缩回去，边缘更干净


def hex_rgb(value):
    value = value.lstrip("#")
    return tuple(int(value[i:i + 2], 16) for i in (0, 2, 4))


def lerp(a, b, t):
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))


PALETTES = {
    # 灰色：不能按
    "disabled": dict(base_hi="#84898e", base_lo="#6b6f73", light="#b3b9bf", dark="#42464a",
                     edge="#2c2f32", plate="#5c6064", plate_edge="#303335", side="#4a4e52"),
    # 绿色：可以按
    "available": dict(base_hi="#63b45c", base_lo="#478f42", light="#a6e59f", dark="#2a582b",
                      edge="#1d401e", plate="#3a7737", plate_edge="#24502a", side="#336b31"),
    # 蓝色：当前阶段 / 正在进行
    "current": dict(base_hi="#5493dd", base_lo="#386db5", light="#9ec8f7", dark="#20406e",
                    edge="#17304f", plate="#2e5b9b", plate_edge="#1b3a63", side="#2a5185"),
    # 红色：对方回合
    "opponent": dict(base_hi="#c2554f", base_lo="#96403c", light="#f09a92", dark="#5a2220",
                     edge="#3d1817", plate="#8a3a36", plate_edge="#5a2220", side="#7a3430"),
    # 金色：这一局赢了（决胜后重开）
    "victory": dict(base_hi="#f0c85c", base_lo="#c39a2c", light="#fff2c0", dark="#8a6a14",
                    edge="#5c4708", plate="#bb9124", plate_edge="#7a5c10", side="#a8801c"),
    # 灰暗：这一局输了
    "defeat": dict(base_hi="#8d9297", base_lo="#6f7479", light="#c2c8ce", dark="#4a4e52",
                   edge="#2f3235", plate="#63686d", plate_edge="#3a3e42", side="#565b60"),
}


def canvas(width, height):
    return Image.new("RGBA", (width * SS, height * SS), (0, 0, 0, 0))


def downscale(image, width, height):
    return image.resize((width, height), Image.LANCZOS)


def border(draw, width, height, color, size=2):
    draw.rectangle([0, 0, width * SS - 1, height * SS - 1], outline=color, width=size * SS)


def bevel(draw, width, height, light, dark, size=3):
    draw.rectangle([0, 0, width * SS - 1, size * SS - 1], fill=light)
    draw.rectangle([0, 0, size * SS - 1, height * SS - 1], fill=light)
    draw.rectangle([0, height * SS - size * SS, width * SS - 1, height * SS - 1], fill=dark)
    draw.rectangle([width * SS - size * SS, 0, width * SS - 1, height * SS - 1], fill=dark)


def rounded_plate(draw, box, radius, fill, outline, width=2):
    draw.rounded_rectangle([v * SS for v in box], radius=radius * SS, fill=fill,
                           outline=outline, width=width * SS)


def cap_top(width, height, palette):
    """按钮帽顶面：状态色 + 内嵌标签板。"""
    img = canvas(width, height)
    draw = ImageDraw.Draw(img)
    hi, lo = hex_rgb(palette["base_hi"]), hex_rgb(palette["base_lo"])
    for y in range(height):
        draw.rectangle([0, y * SS, width * SS - 1, (y + 1) * SS - 1],
                       fill=lerp(hi, lo, y / max(1, height - 1)))
    border(draw, width, height, hex_rgb(palette["edge"]), 2)
    bevel(draw, width, height, hex_rgb(palette["light"]), hex_rgb(palette["dark"]), 3)
    inset_x, inset_y = max(6, width // 14), max(4, height // 9)
    rounded_plate(draw, (inset_x, inset_y, width - inset_x - 1, height - inset_y - 1),
                  radius=max(2, inset_y // 2), fill=hex_rgb(palette["plate"]),
                  outline=hex_rgb(palette["plate_edge"]), width=1)
    draw.rectangle([inset_x * SS, inset_y * SS,
                    (width - inset_x) * SS - 1, (inset_y + 1) * SS - 1],
                   fill=hex_rgb(palette["light"]))
    return downscale(img, width, height)


def cap_side(width, height, palette):
    img = canvas(width, height)
    draw = ImageDraw.Draw(img)
    base = hex_rgb(palette["side"])
    draw.rectangle([0, 0, width * SS - 1, height * SS - 1], fill=base)
    draw.rectangle([0, 0, width * SS - 1, SS - 1], fill=hex_rgb(palette["light"]))
    draw.rectangle([0, (height - 1) * SS, width * SS - 1, height * SS - 1],
                   fill=hex_rgb(palette["dark"]))
    return downscale(img, width, height)


def pad(width, height):
    img = canvas(width, height)
    draw = ImageDraw.Draw(img)
    draw.rectangle([0, 0, width * SS - 1, height * SS - 1], fill=(18, 20, 22, 255))
    return downscale(img, width, height)


def frame_top(width, height):
    """按钮底座顶面：深色金属 + 四角螺钉。"""
    img = canvas(width, height)
    draw = ImageDraw.Draw(img)
    hi, lo = hex_rgb("#5c6267"), hex_rgb("#43484c")
    for y in range(height):
        draw.rectangle([0, y * SS, width * SS - 1, (y + 1) * SS - 1],
                       fill=lerp(hi, lo, y / max(1, height - 1)))
    border(draw, width, height, hex_rgb("#26292c"), 2)
    bevel(draw, width, height, hex_rgb("#7f868c"), hex_rgb("#222528"), 3)
    r = max(1, min(width, height) // 24)
    for cx, cy in ((r * 3, r * 2), (width - r * 3, r * 2),
                   (r * 3, height - r * 2), (width - r * 3, height - r * 2)):
        draw.ellipse([(cx - r) * SS, (cy - r) * SS, (cx + r) * SS, (cy + r) * SS],
                     fill=hex_rgb("#8b9298"), outline=hex_rgb("#2b2e31"))
    return downscale(img, width, height)


def frame_side(width, height):
    img = canvas(width, height)
    draw = ImageDraw.Draw(img)
    draw.rectangle([0, 0, width * SS - 1, height * SS - 1], fill=hex_rgb("#33383c"))
    draw.rectangle([0, 0, width * SS - 1, SS - 1], fill=hex_rgb("#565c61"))
    draw.rectangle([0, (height - 1) * SS, width * SS - 1, height * SS - 1],
                   fill=hex_rgb("#1b1e20"))
    return downscale(img, width, height)


def screen_top(width, height, label="LP"):
    """LP 屏面：深色底 + 扫描线，左侧烧一个 LP 标记，中间留给动态数字。"""
    img = canvas(width, height)
    draw = ImageDraw.Draw(img)
    draw.rectangle([0, 0, width * SS - 1, height * SS - 1], fill=hex_rgb("#0e1416"))
    border(draw, width, height, hex_rgb("#2a3438"), 2)
    draw.rectangle([3 * SS, 3 * SS, width * SS - 3 * SS - 1, 3 * SS + SS - 1],
                   fill=hex_rgb("#1b2528"))
    for y in range(6, height - 4, 3):
        draw.rectangle([3 * SS, y * SS, width * SS - 4 * SS, y * SS + SS - 1],
                       fill=hex_rgb("#141d1f"))
    size = int(height * 0.52 * SS)
    font = ImageFont.truetype(FONT_BOLD, size)
    draw.text((8 * SS, height * SS // 2), label, font=font, fill=hex_rgb("#5d9a74"),
              anchor="lm")
    return downscale(img, width, height)


def screen_side(width, height):
    img = canvas(width, height)
    draw = ImageDraw.Draw(img)
    draw.rectangle([0, 0, width * SS - 1, height * SS - 1], fill=hex_rgb("#20272a"))
    draw.rectangle([0, 0, width * SS - 1, SS - 1], fill=hex_rgb("#39444a"))
    draw.rectangle([0, (height - 1) * SS, width * SS - 1, height * SS - 1],
                   fill=hex_rgb("#141a1c"))
    return downscale(img, width, height)


def sheet(top, side, pad_tile):
    width = top.width
    img = Image.new("RGBA", (width, top.height + side.height + pad_tile.height), (0, 0, 0, 0))
    img.paste(top, (0, 0))
    img.paste(side, (0, top.height))
    img.paste(pad_tile, (0, top.height + side.height))
    return img


TEXTURES = {}


def emit(name, image, tiles):
    image.save(ROOT / "textures" / f"{name}.png")
    TEXTURES[name] = {
        "file": f"textures/{name}.png",
        "size": [image.width, image.height],
        "tiles": tiles,
    }
    return tiles


def main():
    (ROOT / "textures").mkdir(parents=True, exist_ok=True)

    # ---------------------------------------------------------------- 底座
    frame_w, frame_h = 64, 48
    frame_tiles = {
        "top": [0, 0, frame_w, 32],
        "side": [0, 32, frame_w, 40],
        "pad": [0, 40, frame_w, 48],
    }
    emit("control_frame",
         sheet(frame_top(frame_w, 32), frame_side(frame_w, 8), pad(frame_w, 8)),
         frame_tiles)

    # ---------------------------------------------------------------- 阶段键
    phase_cap_w, phase_cap_h = 160, 72
    phase_side_h, phase_pad_h = 16, 8
    phase_tiles = {
        "top": [0, 0, phase_cap_w, phase_cap_h],
        "side": [0, phase_cap_h, phase_cap_w, phase_cap_h + phase_side_h],
        "pad": [0, phase_cap_h + phase_side_h, phase_cap_w, phase_cap_h + phase_side_h + phase_pad_h],
    }
    # 阶段键只有灰/绿/蓝三态；红色留给"对方回合"的指示键
    for state in ("disabled", "available", "current"):
        palette = PALETTES[state]
        emit(f"control_phase_{state}",
             sheet(cap_top(phase_cap_w, phase_cap_h, palette),
                   cap_side(phase_cap_w, phase_side_h, palette),
                   pad(phase_cap_w, phase_pad_h)),
             phase_tiles)

    # ---------------------------------------------------------------- 开始 / 准备键
    confirm_cap_w, confirm_cap_h = 320, 76
    confirm_side_h, confirm_pad_h = 16, 8
    confirm_tiles = {
        "top": [0, 0, confirm_cap_w, confirm_cap_h],
        "side": [0, confirm_cap_h, confirm_cap_w, confirm_cap_h + confirm_side_h],
        "pad": [0, confirm_cap_h + confirm_side_h, confirm_cap_w,
                confirm_cap_h + confirm_side_h + confirm_pad_h],
    }
    emit("control_confirm_disabled", sheet(cap_top(confirm_cap_w, confirm_cap_h, PALETTES["disabled"]),
                                           cap_side(confirm_cap_w, confirm_side_h, PALETTES["disabled"]),
                                           pad(confirm_cap_w, confirm_pad_h)), confirm_tiles)
    emit("control_confirm_ready", sheet(cap_top(confirm_cap_w, confirm_cap_h, PALETTES["available"]),
                                        cap_side(confirm_cap_w, confirm_side_h, PALETTES["available"]),
                                        pad(confirm_cap_w, confirm_pad_h)), confirm_tiles)
    emit("control_confirm_started", sheet(cap_top(confirm_cap_w, confirm_cap_h, PALETTES["current"]),
                                          cap_side(confirm_cap_w, confirm_side_h, PALETTES["current"]),
                                          pad(confirm_cap_w, confirm_pad_h)), confirm_tiles)
    emit("control_confirm_turn_mine",
         sheet(cap_top(confirm_cap_w, confirm_cap_h, PALETTES["available"]),
               cap_side(confirm_cap_w, confirm_side_h, PALETTES["available"]),
               pad(confirm_cap_w, confirm_pad_h)), confirm_tiles)
    emit("control_confirm_turn_opponent",
         sheet(cap_top(confirm_cap_w, confirm_cap_h, PALETTES["opponent"]),
               cap_side(confirm_cap_w, confirm_side_h, PALETTES["opponent"]),
               pad(confirm_cap_w, confirm_pad_h)), confirm_tiles)
    # 决胜后的两种状态：金色 victory / 灰暗 defeat（右键这张键会回到「准备」）
    emit("control_confirm_victory",
         sheet(cap_top(confirm_cap_w, confirm_cap_h, PALETTES["victory"]),
               cap_side(confirm_cap_w, confirm_side_h, PALETTES["victory"]),
               pad(confirm_cap_w, confirm_pad_h)), confirm_tiles)
    emit("control_confirm_defeat",
         sheet(cap_top(confirm_cap_w, confirm_cap_h, PALETTES["defeat"]),
               cap_side(confirm_cap_w, confirm_side_h, PALETTES["defeat"]),
               pad(confirm_cap_w, confirm_pad_h)), confirm_tiles)

    # ---------------------------------------------------------------- LP 屏
    lp_w, lp_h = 336, 76
    lp_side_h, lp_pad_h = 16, 8
    lp_tiles = {
        "top": [0, 0, lp_w, lp_h],
        "side": [0, lp_h, lp_w, lp_h + lp_side_h],
        "pad": [0, lp_h + lp_side_h, lp_w, lp_h + lp_side_h + lp_pad_h],
    }
    emit("control_lp_screen",
         sheet(screen_top(lp_w, lp_h), screen_side(lp_w, lp_side_h), pad(lp_w, lp_pad_h)),
         lp_tiles)

    # ---------------------------------------------------------------- 几何规格
    spec = {
        "name": "mdpromod_duel_controls",
        "units_per_block": 16,
        "note": "所有 Y 都是相对决斗场地实体原点的模型单位；场地表面在 y = 0.16。",
        "textures": TEXTURES,
        "frame": {
            "texture": "control_frame",
            "thickness": 0.8,
            "bottom": 0.2,
        },
        "phase_button": {
            "footprint": [20, 10],
            "cap": [17.2, 7.8],
            "cap_top": 1.8,
            "cap_bottom": 1.0,
            "label_plane": 1.82,
            "states": {"disabled": "control_phase_disabled",
                       "available": "control_phase_available",
                       "current": "control_phase_current"},
        },
        "confirm_button": {
            "footprint": [42, 12],
            "cap": [38.8, 9.4],
            "cap_top": 1.8,
            "cap_bottom": 1.0,
            "label_plane": 1.82,
            "states": {"disabled": "control_confirm_disabled",
                       "ready": "control_confirm_ready",
                       "started": "control_confirm_started",
                       "my_turn": "control_confirm_turn_mine",
                       "opponent_turn": "control_confirm_turn_opponent"},
        },
        "lp_panel": {
            "footprint": [42, 12],
            "slab_top": 1.1,
            "slab_bottom": 0.2,
            "rim_top": 1.8,
            "rim_width": 1.6,
            "screen": {
                "half_x": 19.4,
                "half_z": 4.6,
                "surface": 1.1,
                "text_height": 5.5,
                "text_color": "#b9e8b0",
                "max_text_width": 36.0,
            },
            "texture": "control_lp_screen",
        },
        "top_height": 1.8,
        "states": {
            "phase": {"disabled": "灰色，不能按", "available": "绿色，可以按",
                      "current": "蓝色，当前阶段"},
            "confirm": {"disabled": "灰色，开始决斗", "ready": "绿色，准备",
                        "started": "蓝色，游戏中", "my_turn": "绿色，我的回合",
                        "opponent_turn": "红色，对方回合"},
        },
    }
    (ROOT / "control_spec.json").write_text(
        json.dumps(spec, ensure_ascii=False, indent=2), encoding="utf-8")

    # ---------------------------------------------------------------- 预览图
    preview_names = list(TEXTURES)
    preview = Image.new("RGBA", (max(TEXTURES[n]["size"][0] for n in preview_names),
                                 sum(TEXTURES[n]["size"][1] + 6 for n in preview_names)),
                        (24, 26, 30, 255))
    y = 0
    for name in preview_names:
        tile = Image.open(ROOT / "textures" / f"{name}.png")
        preview.paste(tile, (0, y), tile)
        y += tile.height + 6
    preview.save(ROOT / "preview_sheet.png")

    # ---------------------------------------------------------------- 同步到 mod
    MOD_ENTITY_TEXTURES.mkdir(parents=True, exist_ok=True)
    copied = []
    for name in TEXTURES:
        shutil.copyfile(ROOT / "textures" / f"{name}.png", MOD_ENTITY_TEXTURES / f"{name}.png")
        copied.append(name)

    print(json.dumps({
        "textures": list(TEXTURES),
        "copied_to_mod": copied,
        "spec": str(ROOT / "control_spec.json"),
    }, ensure_ascii=False))


if __name__ == "__main__":
    main()
