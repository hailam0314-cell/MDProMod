# 怪兽立绘（Closeup）批量压缩到高度 128（2026-10-14）

## 背景

MDPro 的立绘在 `Picture/Closeup/{卡号}.png`（见 `docs` 里关于"怪兽立绘"的调研：
`FileGroupConfig.CardCloseup`、`GameCard.NeedShowCloseup/LoadCloseupAsync`），
本机源目录是 `D:\MyCardLibrary\mdpro3\Picture\Closeup`。

原始数据：**11,091 张，284.8 MiB，单张中位 24.9 KB（最大 189.4 KB）**。
它们本来就是"带 alpha 的调色板 PNG"，所以**重新编码成 PNG-32 反而更大**
（青眼白龙 228×266 原 22.8 KB → PNG-32 47 KB）。正确的压缩方向是
**保留调色板、只降分辨率**：`RGBA → LANCZOS 缩放 → quantize(256 色, FASTOCTREE, 无抖动) → PNG optimize`，
透明边缘仍然是柔和的（青眼白龙 128 档还有 108 个不同 alpha 值，不是硬边）。

## 产物

| 项 | 值 |
| --- | --- |
| 输出目录 | `D:\MDProMod\runtime\closeup_128\` |
| 命名 | `{卡号}.png`（与原目录一致，模组按卡号直读） |
| 数量 | 11,091（11,090 张缩放到高 128；1 张原本就比 128 矮，直接复制） |
| 体积 | 284.8 MiB → **77.4 MiB**（单张中位 6.9 KB，最大 14.3 KB） |
| 失败 | 0 |
| 耗时 | 96 秒 |
| 清单 | `runtime/closeup_128_manifest.json`（源目录 / 参数 / 数量 / 字节数） |
| 源目录 | **只读，未做任何修改** |

生成脚本：`tmp\batch-closeup-128.py`（可重复运行，已存在的文件会跳过）。

## 备选档位（同一套流程，抽 150 张估算全量）

| 高度 | 单张中位 | 全量约 |
| --- | --- | --- |
| 192 | 12.6 KB | ≈142 MiB |
| 160 | 9.6 KB | ≈108 MiB |
| **128** | **7.0 KB** | **≈78 MiB（本次采用）** |
| 96 | 4.8 KB | ≈53 MiB |

## 下一步（立绘接入模组）

1. 模组新增 `CloseupTextureCache`（和 `CardTextureCache` 同一套路，但单独根目录
   `-Dmdpromod.closeupRoot`，默认指向 `D:\MDProMod\runtime\closeup_128`）。
2. 渲染：在表侧怪兽上方画一块面向玩家的四边形（billboard）——位置 `卡上方约 1 单位`、
   绕 X 倾 70°、高度按比例固定（先取约 2 倍卡高，再按观感调）。
3. 显示条件照抄 MDPro：**表侧 + 在怪兽区 + 该卡号有立绘文件**；里侧 / 离场 / 送墓立刻隐藏。
4. 动画：0.3 秒从 0 缩放到目标（弹出）；5 秒循环上下浮动 + 前倾（对应
   `Ani_Closeup Loop.anim` 的 `(0,1,0)→(0,1.3,0.83)`）、发光呼吸（`GlowControl 0→3→0`）。
5. 颜色：按怪兽属性乘一层色（光黄/暗品红/水青/火红/地黄橙/风绿/神黄）；
   被无效化时变灰（对应材质的 `RGBA=0 / Mono=1`）。
6. 开关：`-Dmdpromod.closeup=false` 关闭；只在视距内渲染。
