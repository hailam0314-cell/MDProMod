# 怪兽立绘（Closeup）接进 Minecraft（2026-10-14）

配合 `docs\closeup-compress-2026-10-14.md`（资源压缩）和之前对 MDPro 立绘链路的调研。

## 新增的两个类

| 文件 | 作用 |
| --- | --- |
| `mod/src/main/java/net/mdpromod/CloseupTextureCache.java` | 按卡号从 `{root}\{卡号}.png` 加载立绘；**保留透明通道**（调色板 PNG 先转 INT_ARGB 再逐像素搬进 `NativeImage`）；LRU 缓存 96 张；缺图只查一次磁盘；线性过滤（斜着看不发糊）；**另有一套"灰度版"按需生成**（效果被无效时用，不占磁盘） |
| `mod/src/main/java/net/mdpromod/ClientCloseups.java` | 世界渲染层：扫附近的 `DuelCardEntity`，给符合条件的卡在**卡片上方画一块始终面向玩家的立绘**（billboard） |

接入点：`ClientForgeEvents.onRenderLevel` 里追加一行 `ClientCloseups.render(...)`
（和连锁展示、落点高亮同一批 3D 绘制）。

## 行为（对齐 MDPro 的 `GameCard.NeedShowCloseup`）

- **显示条件**：卡面朝上（`code > 0`）+ 在**怪兽区**（`*_mzone_*` 或共享额外怪兽区）
  + **该卡号有立绘文件**；里侧 / 离场 / 送墓 / 回手立刻消失。
- **弹出**：出现时 0.3 秒从 0 缩放到目标（easeOutCubic）——对应 MDPro 的 `ResizeCloseup`。
- **待机**：5 秒一个循环（对应 `Ani_Closeup Loop.anim`）：上下浮动 ±0.07 格、
  朝观察者方向轻倾 0.10 格、亮度 0.90~1.00 呼吸（对应材质的 `GlowControl 0→3→0`）。
- **不上属性色**（最初按 MDPro 做了属性乘色，实际看着太花，已按需求去掉；立绘保持原色）。
- **效果被无效 → 变灰**：核心查询状态位 `QUERY_STATUS`(0x80000) 的 **bit0**（和 MDPro 的
  `Disabled = (status & 1) == 1` 同一个来源）驱动；变灰用的是同一张图的灰度版
  （亮度 0.299/0.587/0.114，再压暗到 0.82），等价于 MDPro 材质上的 `Mono=1`。
  **卡面也一起变灰**：`CardTextureCache` 同样按需生成灰度版（卡面只去色、不压暗，
  卡名/数值仍可读），`DuelCardRenderer` 按 `isDisabled()` 取图——对齐 MDPro 的 `_Monochrome`。
- **光照**：用 `LightTexture.FULL_BRIGHT`（立绘自带发光感，不受场地明暗影响）。
- 双方场上的表侧怪兽都会显示。

## 可调参数（都在 `ClientCloseups` 顶部）

| 常量 | 默认 | 说明 |
| --- | --- | --- |
| `HEIGHT` | 1.9 | 立绘高度（方块）。场上卡片本身是 0.86 × 1.25，嫌大/嫌小改这里 |
| `GAP` | 0.30 | 底边离卡面多高 |
| `BOB` / `LEAN` | 0.07 / 0.10 | 待机浮动与前倾幅度 |
| `POP_MS` / `LOOP_MS` | 300 / 5000 | 弹出时长、待机周期 |

（原来的属性色强度 `TINT` 已删除；"变灰"的压暗系数写在 `CloseupTextureCache.build()` 里。）

系统属性：

- `-Dmdpromod.closeup=false` 整体关闭；
- `-Dmdpromod.closeupRoot=<目录>` 换立绘目录（默认 `D:\MDProMod\runtime\closeup_128`）。

## 产物与验证

| 项 | 结果 |
| --- | --- |
| 构建 | `cd D:\MDProMod\mod; .\gradlew.bat build --offline` → BUILD SUCCESSFUL |
| 包内确认 | `CloseupTextureCache.class` / `ClientCloseups.class` 都在 |
| 状态位解析 | 用实机日志里 **8,831 条**查询记录验证字段布局：按这套尺寸走完正好等于记录长度（0 不吻合） |
| 单元测试 | `java -cp build/classes/java/main;build/classes/java/test net.mdpromod.DuelVisualStateTest` → **passed 71 assertions**（含新增"无效位"用例：置位→事件里带 disabled、清位→恢复） |
| 产物 | `mdpromod-0.1.0.jar` 2,349,421 字节，SHA-256 `56CEC11A…E9EFBD`，已复制到客户端 mods；旧包备份 `tmp\mdpromod-0.1.0.jar.prev-2026-10-14-0758` |

## 需要你在游戏里确认

1. 召唤一只怪兽（比如青眼白龙）→ 卡片上方是否出现立绘，出现时有 0.3 秒弹出；
2. 立绘是否**始终朝向自己**、悬浮着慢慢上下浮动；
3. 盖放 / 里侧怪兽不应出现立绘；送墓后立绘立刻消失；
4. 大小是否合适——不合适我调 `HEIGHT`（1.9 → 1.5 或 2.4 之类）。
5. 效果被无效时（例如「效果无效」类卡处理过的怪兽）**立绘和卡面是否都变灰**。

## 还没做

- 立绘的**描边 / 发光**只做了亮度呼吸，没有单独的 bloom/outline。
- 手牌/卡堆这类"信息界面"里的卡图不跟随后场状态变灰（只影响场上卡面与立绘）。
