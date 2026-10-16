# 卡牌动画设计：怎么让它动得流畅（2026-10-13）

## 一、先定约束

- **逻辑 20Hz、渲染 60Hz**：服务端/主机每 tick（50ms）推一次状态，画面每帧（16.7ms）刷。
  只要动画的位置由「每 tick 一次」决定，看起来就会一格一格跳（可视化里的方案①）。
- **实体同步有延迟**：位置同步包晚到、丢包时，实体位置会突然回弹。
- **卡片是实体**：还有碰撞、拾取、右键交互要跟着走，不能只做画面上的假动作。

结论：**动画不能跟网络位置绑死**，要做成「事件 + 客户端本地时间轴」。

## 二、四层架构

1. **事件层（服务端 / 决斗主机）**：只发「演出事件」，不发每帧坐标。

   ```json
   { "type": "anim", "id": 17, "kind": "draw",
     "card": 89631139, "from": "p1_deck", "to": "p1_hand",
     "startedAt": 1723456789012, "duration": 600 }
   ```

   事件里带 `id`（去重）、`startedAt`（迟到也能对表）、`duration`、`kind` 和起止区域。

2. **时间轴层（客户端 `CardAnimator`）**：维护一个动画队列，每个动画有自己的
   `startTime / duration / kind / 起止点`；重复事件按 `id` 丢弃，新事件不打断
   正在播的动画，而是排队（`AnimationQueue`），保证节奏稳定。

3. **渲染层**：`EntityRenderer.render(...)` 收到 `partialTick`，用
   `t = (now - startTime + partialTick * 50) / duration` 每帧算插值位置，
   再用 `PoseStack` 施加位移 / 抬升 / 旋转 / 缩放。**实体的逻辑位置保持不动**，
   动画结束的那一刻才瞬移到终点（或者干脆销毁重建）。

4. **表现层**：粒子、音效、镜头震动、屏幕特效都在时间轴的固定进度点触发
   （例如 `t = 0.72` 触发「落地尘」）。

## 三、流畅度的六条规则

1. **帧插值优先**：任何位置都在渲染阶段按 `partialTick` 插值；不要每 tick 直接 setPos。
2. **用缓动曲线，不用线性**：
   - 落地/收尾用 `easeOutCubic`（起步快、收尾软）；
   - 攻击冲刺用 `easeInQuad`（起步慢、加速撞上去）；
   - 往返用 `easeInOutQuad`。
3. **走弧线，不走直线**：抽卡、召唤、攻击用二次贝塞尔（控制点比直线高 0.6~1.2 格），
   卡片「飞」起来才像实物。
4. **旋转走最短弧**：把角度差规整到 -180°~180° 再插值，避免 359°→1° 绕一大圈。
5. **节奏与顿帧**：单段动画 0.4~0.8s；命中瞬间插入 2~3 tick 的 hit-stop（暂停推进），
   同时放粒子 + 震屏，观感立刻「重」起来。
6. **容错**：动画只是表现，**最终状态永远以服务端为准**；
   事件晚到就从中途开始播（按 `startedAt` 算 progress），丢包就直接跳到终态。

## 四、各动画配方（时长 / 曲线 / 附加表现）

| 动画 | 位移与曲线 | 附加表现 |
| --- | --- | --- |
| 抽卡 | 卡组顶 → 手牌，二次贝塞尔抬高 0.8 格，0.5s（easeIn 0.2 / easeOut 0.3） | 起手翻卡音 + 轻微旋转 -8°→0 |
| 召唤 | 手牌/额外 → 怪兽区，抬高 1.5 格后落下，0.55s easeOutCubic | 落地 3 tick 顿帧 + 尘土粒子 + 附近玩家镜头震 |
| 强力召唤 | 同召唤但抬高 2.5 格、0.8s，落点加光柱/扩散环 | 专属音效 + 更强震屏（后续再做专属 cutscene） |
| 盖放 | 同召唤但落点更低，0.45s | 落地「啪」声 + 卡片翻到背面 |
| 表示切换 | 原地 0.25s 翻转（最短弧）+ 3% 回弹 | 轻微转动音 |
| 攻击 | 前冲 0.15s easeInQuad → 命中 → 回弹 0.25s easeOutCubic | 命中 2 tick 顿帧 + 火花粒子 + 震屏 |
| 伤害 | 受击卡 0.15s 抖动（±0.05 格随机）+ 飘字 | 屏幕红晕 200ms + LP 滚动 |
| 效果发动 | 原地 0.2s 上浮 0.1 格 + 呼吸（发光描边） | 发动音；连锁时依次点亮 |
| 效果无效 | 0.2s 灰白闪 3 次（每次 60ms） | 低音效 + 卡面变灰 |
| 决定性一击 | 镜头 FOV 90→70（0.3s）+ 4 tick 顿帧 + 粒子爆发 + 120ms 白闪 | 之后恢复正常节奏 |
| 硬币 / 骰子 | 0.8s 抛物线 + 多圈旋转 | 落地弹跳 2 次 + 音效 |

## 五、Minecraft / Forge 1.20.1 的具体钩子

| 需求 | 实现位置 |
| --- | --- |
| 卡片位移/抬升/旋转/缩放 | 已有的 `EntityRenderer` + `PoseStack`，用 `partialTick` 插值 |
| 镜头震动 | `ViewportEvent.ComputeCameraAngles`（叠加噪声，按强度衰减） |
| 决定性一击拉近 | `ViewportEvent.ComputeFov`（插值 FOV） |
| 屏幕红晕 / 白闪 / 伤害飘字 | `RenderGuiEvent.Post` 画覆盖层 |
| 尘土 / 火花 / 光柱 | `ServerLevel.sendParticles`（权威）或客户端 `addParticle`（纯表现） |
| 音效 | 客户端 `SoundManager.play(SimpleSoundInstance)`，挂在时间轴进度点上 |
| 顿帧（节奏） | **由我们的决斗主机控制**：在关键消息之间插入 200~600ms 停顿，演出结束再推进引擎 |

最后一条是我们的结构性优势：引擎消息流本来就由 `duel_host` 驱动，
所以「演出时长」可以精确控制，不会出现客户端还在播动画、引擎已经进入下一阶段的情况。

## 六、分期实现

1. **框架期**：`CardAnimator` + `AnimationQueue` + 缓动函数库 + `anim` 事件包
   （服务端→客户端），先把抽卡 / 召唤 / 盖放跑通。
2. **常用期**：攻击 / 伤害 / 效果发动 / 无效，加上震屏、粒子、飘字、LP 三档场地变化。
3. **演出期**：决定性一击（先做通用版，再按 `FinalAttack*` 清单做专属版）、
   StrongSummon 时间轴、名场面音效（需要自制或替代音效）。

## 七、和现有代码的衔接

- 事件由 Java 侧的消息流触发（`Draw` / `Move` / `Summoning` / `SpSummoning` / `Attack` /
  `Damage` / `Chaining` / `ChainSolving`…），需要的字段照 `DuelMessage.cs` 的解析顺序补。
- 演出事件通过 `ModNetwork` 新增的 `anim` 包下发，客户端动画器按 `kind` 播放。
- 现有实体（`HandCardEntity` / `DuelCardEntity`）已经有正反面与浮动渲染，
  动画器只需接管它们的**渲染偏移**，逻辑位置不动。

## 八、第一版实现（抽卡 / 召唤 / 盖放，已完成）

### 服务端

- `DuelHostProcess` 新增 `FieldEvent` 事件流：
  - `Summoning` / `SpSummoning`（function 60/61）：`code(4) + controller(1) + location(1) + sequence(1) + position(1)`；
  - `Set`（function 54）：同样布局，但**卡号是引擎的隐藏值**（背面卡不下发真实卡号），
    实测样本 `36 00 2E 0D 05 00 08 02 0A` 里的 code = 0x050D2E00 是无效值，
    而同一条里的 location=0x08（魔陷区）/sequence=2 是准的 → 盖放只按区域生成「卡背」实体，
    顺带天然满足「对手看不到盖的是什么卡」。
  - `Summoning` 样本 `3C 47 BC 7D 00 01 04 02 01`：code=8239175（有效）、
    controller=1、location=0x04（怪兽区）、sequence=2、position=1（表侧攻击）→ 全部可用。
- `DuelSessionManager` 收到事件后：把区域 id 映射成 `p{1|2}_mzone_{seq}` / `p{1|2}_szone_{seq}`，
  在目标位置生成 `DuelCardEntity`（召唤 = 表侧、盖放 = 里侧），并发一条 `Anim` 事件；
  起点取该玩家手牌行的中间位置。
- 手牌侧：`reconcileHandEntities` 生成**新**手牌实体时，会发一条 `draw` 动画，
  起点是该侧卡组区、终点是手牌位。

### 客户端

- `ModNetwork.Anim`：`entityId / kind / from / to / durationMs / rollDegrees`，
  用 `PacketDistributor.NEAR` 发给决斗盘周围 64 格内的玩家。
- `ClientAnimBridge`：每帧按缓动曲线算 `Transform(offset, roll)`：
  - `draw` 用 easeInOut + 抬升 0.45 格；
  - `summon` 用 easeOutCubic + 抬升 0.85 格 + 侧倾 -14°（时长 560ms）；
  - `set` 用 easeOutCubic + 抬升 0.6 格 + 侧倾 -10°（时长 470ms）；
  动画结束自动移除状态，卡片回到实体逻辑位置（不再有位置回弹问题）。
- `DuelCardRenderer` / `HandCardRenderer` 在 `PoseStack` 上应用这个偏移与侧倾，
  逻辑位置与碰撞位置完全不动。

### 顺带修掉的 WindBot 直连问题（为验证召唤动画铺路）

1. 直连引擎时收不到 `MSG_START` → 在桥上按同样布局补一条，
   WindBot 的 LP / 卡组 / 状态初始化才真正跑起来（之前 LP=0、手牌全是 id=0）。
2. `MSG_DRAW` 的 payload 里带着抽到的卡号，但 WindBot 只把它当占位，
   真实卡号原本来依赖 `MSG_MOVE`——直连时那个字段是无效值 →
   改成在桥上从 Draw 里取号补进手牌。
3. `Duel.AddCard` 不再用无效卡号覆盖已有卡号。
4. 决斗主机现在支持**双 AI**（`--smoke-ai` 同时给两侧装 WindBot），
   无人值守也能跑出召唤/盖放消息，用于验证动画链路。

### 验证产物

- `staging\ai_smoke10.out`：双 AI 对战，出现 `Set`(54) ×N、`Summoning`(60) ×N 的真实样本
  （见上面对样本的逐字段解析），`SelectPlace` 12 次、`Move` 64 次、`Draw` 57 次。
- jar：`mdpromod-0.1.0.jar`（2,146,557 字节，
  SHA256 `925F916D50A94D6B5E7BD113BDDEC02EB5658C27A80A8F8B1A16453FA49AD86A`），
  已复制到 `D:\光明MOD服1.12客户端\.minecraft\versions\1.20.1-Forge_47.4.20\mods\`。

## 九、下一步

- 手感调优：目前时长/抬升高度是初值，等主人进游戏试过再微调。
- 攻击 / 伤害 / 效果发动 / 无效四种动画（同样是「事件 + 客户端时间轴」）。
- 抽卡时手牌排布的重排动画（新卡插进手牌时其余卡平移让位）。
- 由 `duel_host` 控制的演出停顿（召唤后 0.3s、盖放后 0.2s），把节奏也一并调好。
