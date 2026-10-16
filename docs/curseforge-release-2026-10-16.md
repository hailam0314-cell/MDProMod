# CurseForge 发布材料（2026-10-16）

英文是主文本，直接往 CurseForge 上传页里粘；中文版留着给中文社群（B 站 / 群公告 / 论坛）用。
最下面是分类、标签、截图清单，以及上传前必须补的三件事。

---

## Summary (short description field)

Play a full Yu-Gi-Oh! duel inside Minecraft: place the Duel Field, load a Card Case and face 57 WindBot
opponents under real ocgcore rules — with Master Duel summon sequences, ace-monster cut-ins, monster
closeups, 110 BGM tracks and 2,443 sound effects. Everything ships in one jar; no external install needed.

## Summary（中文短描述）

把《游戏王》的完整对战搬进 Minecraft：摆下决斗盘、放上卡盒，和 57 套 WindBot 人机用真正的 ocgcore
规则打一场。召唤法阵、王牌登场动画、怪兽立绘、110 首 BGM 全部照搬 Master Duel——一个 jar 全自带，
不需要安装任何外部程序。

---

## Description (English)

# MDProMod — Play a full Yu-Gi-Oh! duel inside Minecraft

Place a **Duel Field**, load a **Card Case**, press the green button: the opening deal plays, the phase
buttons light up one by one, and your opponent is one of 57 WindBot AIs. This isn't a card skin — it's a
complete duel running on the real ocgcore rules engine.

## What a duel looks like

* The **Duel Field** is a large in-world structure: 16×14 blocks with **32 zones** laid out like a real
  play mat — Monster Zones, Spell & Trap Zones, Field Spell Zone and Extra Monster Zones.
* Put your **Card Case** on your half and an **AI Deck** on the far half. With both sides set, the green
  button on your side turns **Ready**.
* The opening hands are dealt with an animation, then the six **real phase buttons** appear: Draw, Standby,
  Main 1, Battle, Main 2, End. Which phases you may enter is decided by the engine — trying to skip ahead
  is simply refused.
* Everything is decided in the world, not in menus:
  * Left-click your cards (hand or field) for an **action menu** — Summon, Special Summon, Set, Activate,
    Attack, Change Position — showing only what the engine actually allows.
  * When a card needs a location, the legal squares **glow green**; look at one and left-click.
  * Card selection, chain, sorting, material, option and confirmation prompts open as **Minecraft-styled
    windows** with card-art grids and tooltips showing each card's name and where it is.
* **Live LP**: damage, recovery, costs and set-to-value are all tracked, and the LP screen under the field
  keeps score.
* Gold **Victory** and grey **Defeat** states — right-click to go back to Ready and duel again.

## Everything from Master Duel, ported over

* **Six summon sequences** (Fusion, Synchro, Xyz, Link, Ritual, Pendulum): materials drawn in → a magic
  circle unfolds at the centre of the field → the monster lands, using Master Duel's own 3D meshes and
  **measured timings** (Fusion 3.3 s, Link 6.8 s, …).
* **497 ace-monster cut-ins**, played back as real Spine skeletal animations — scaled to the field, facing
  every player's camera, visible to everyone nearby.
* **11,091 monster closeups** floating above the field (popping in after 0.3 s, breathing for 5 s, tinted
  by attribute).
* **110 BGM tracks** and **2,443 sound effects**. Pick one track each for duel / ace summon / low-LP climax
  with the **Music Settings** item; summons, attacks, phase changes and menu clicks follow Master Duel's
  own cue timings.

## Card data

* **15,019 cards** built in, with the ocgcore rules and card text — fully offline.
* **57 WindBot decks** with names and personalities: Blue-Eyes, Branded, Tearlaments, Swordsoul and more.
* The **deck editor lives inside the Card Case**: search, filter, build from the entire card pool, with
  deck-size and **banlist checks**, plus `.ydk` import and export.

## Survival mode

| Item | Recipe | What it does |
| --- | --- | --- |
| **Card Case** | 6 Paper + 3 Leather | Build and carry a deck |
| **Duel Field** | 4 Iron Ingot + 4 Redstone + Card Case | Places the 16×14 duel field |
| **AI Deck** | Card Case + Observer | Pick an AI opponent for the far half |
| **Music Settings** | Card Case + Note Block | Choose a BGM track for each moment |

## Requirements

* **Minecraft 1.20.1 with Forge 47.4.20** (any 47.x works)
* **Windows x64** — the bundled duel host is a Windows program with its own .NET runtime. macOS and Linux
  are not supported yet.
* Install on **both client and server** (singleplayer counts — the duel runs on the server side).
* About **1 GB of disk** (566 MB mod jar + 396 MB unpacked). 4 GB+ of RAM recommended.
* **No external install required** — no MDPro3, no ygopro, nothing else.

## First launch unpacks itself (this is normal)

On the first launch the mod extracts card art, closeups, summon effects and the duel host out of the jar
into `<game dir>/mdpromod/`: roughly **26,500 files / 396 MB / ~35 seconds**. A progress readout scrolls on
the action bar, and a "resources ready" note appears in chat afterwards. Later launches only compare
snapshots (~2 seconds). You can **delete that folder at any time** — it is rebuilt from the jar.

## How to start a duel (30 seconds)

1. Right-click the ground with the **Duel Field** — the field appears.
2. Right-click with the **Card Case** — the deck editor opens (craft one first in survival).
3. Sneak-right-click to place the Card Case on **your half**; place the **AI Deck** on the **opposite** half.
4. Press the green button on your side → **Ready** → **Start**.
5. Left-click your own cards to see what they can do; watch for green squares when you need to place
   something.

## Commands (optional — for testing and screenshots)

| Command | What it does |
| --- | --- |
| `/mdpromod duel start` / `stop` / `stats` / `status` | Start or stop a duel, read stats and state |
| `/mdpromod duel prompt` / `respond <value>` / `places` / `place <id>` | Inspect the current prompt, answer it manually, list or choose locations |
| `/mdpromod case give [name]` / `export` / `load <name>` / `info` | Get a Card Case, export the current deck, load a `.ydk`, print deck info |
| `/mdpromod field place` / `bind` / `info` | Place, bind or inspect the duel field |
| `/mdpromod card give <code> [count]` / `spawn <code>` | Give or spawn a specific card |
| `/mdpromod hand test` / `reveal` / `clear` | Hand HUD self-check |
| `/mdpromod anim demo` / `stop` | Play every animation, or stop |

## FAQ

**Can I duel a friend?**
This build is player **vs. the built-in AI** — the far half must hold an AI Deck for a duel to start.
Human-vs-human is not implemented yet. On a shared server, players nearby can watch your animations and
hear your duel, but only one duel runs at a time.

**Why does the first launch take half a minute?**
Because every asset ships inside the jar — that is exactly why nothing else needs installing. It happens
once. (Delete the folder if you want to watch it happen again.)

**Why do some monsters have no closeup?**
Closeups exist only for the 11,091 monsters Master Duel made them for. Missing ones are skipped silently
and do not affect the duel.

**Can I turn effects off to save performance?**
Yes, with launch flags: `-Dmdpromod.closeup=false` (closeups), `-Dmdpromod.cutin=false` (ace cut-ins),
`-Dmdpromod.summonFx=false` (summon sequences), `-Dmdpromod.duelAudio=false` (duel sound effects),
`-Dmdpromod.duelBgm=false` (background music).

**Why Windows only?**
The bundled duel host (ocgcore + .NET) is currently published for Windows x64 only.

**Where do the cards and art come from?**
Rules and card data come from ygopro's ocgcore and card database; art and audio come from Yu-Gi-Oh!
Master Duel and community art packs. This is a fan project for study and personal use — please support
the official release.

## Known limitations (0.1.0)

* AI opponents only; no human-vs-human duels yet.
* A few decision windows are still chat-only (sorting chains, counters, tribute/material counts,
  declarations) — they print the engine function in chat when they come up.
* Hidden-information filtering for multiplayer is still being finished.
* Windows x64 only.
* The Duel Anchor block is currently decorative.

## Credits & license

* Rules and card data: **ygopro / ocgcore** and **MDPro3** (GPL-3.0) — this mod would not exist without them.
* Art and audio: **Konami's Yu-Gi-Oh! Master Duel** and MyCard community art packs (for study and personal
  use; please support the official release).
* Bundled runtimes: **Spine Runtimes** (Esoteric Software) and **libGDX** (Apache-2.0).
* This mod is released under **GPL-3.0-or-later**. Source: `<GitHub link>`.

---

## Description（中文正文）

# MDProMod —— 在 Minecraft 里打一场真正的游戏王

摆下**决斗盘**，放上**卡盒**，点一下绿色按钮：发牌演出开始，五个阶段的按键依次亮起来，
对手是 57 套 WindBot 人机里的一个。这不是卡牌皮肤，是完整规则的一整局对战。

## 一场对局是什么样

* **决斗盘**是世界里的大实体：16×14 方块、32 个区域，怪兽区 / 魔陷区 / 场地魔法区 / 额外怪兽区按真实布局排。
* 自己这半场放**卡盒**，对侧放**人机卡组**；两侧都就位后，绿色确认键变「准备」。
* 开局有**发牌演出**，之后是抽牌 / 准备 / 主要 1 / 战斗 / 主要 2 / 结束六个阶段的**真实阶段键**——
  哪些阶段能进完全由核心判定，硬切会被拒绝。
* 全部决策都在世界里做：
  * 左键自己场上的卡或手牌 → 弹出动作按钮（召唤 / 特殊召唤 / 盖放 / 发动 / 攻击 / 表示变更，
    按钮只显示核心真的允许你做的事）；
  * 需要落点时，可放的格子会**绿色高亮**，看向格子左键即可；
  * 选卡、连锁、排序、素材、选项、是否发动这些窗口都是 Minecraft 风格的弹窗（卡图网格 + 悬停显示卡名和位置）。
* **LP 实时结算**：伤害 / 回复 / 支付 / 设为某值四类都接，右下角的 LP 屏跟着跳。
* 胜负两态：金色胜利 / 灰色失败，右键回到「准备」就能重开一局。

## 演出：从 Master Duel 里搬过来的

* **六类召唤演出**（融合 / 同调 / 超量 / 连接 / 仪式 / 灵摆）：素材吸入 → 场地中央展开法阵
  → 用 MD 真正的 3D 网格与**实测时长**（融合 3.3 秒、连接 6.8 秒…）→ 怪兽落地。
* **497 套王牌登场 cut-in**：直接实时播放 MD 的 Spine 骨骼动画，按场地尺寸铺满、朝向每个玩家的相机，
  附近的人都能看到。
* **11,091 张怪兽立绘**悬浮在场上（0.3 秒弹出、5 秒呼吸浮动、按属性上色）。
* **110 首 BGM** + **2,443 条音效**：BGM 分「决斗 / 王牌登场 / 低 LP 高潮」三个时机各选一首，
  卡组上面放一张「音乐设置」就能改；召唤、攻击、阶段切换、菜单音效全部对齐 MD 的时点。

## 卡片数据

* 内置 **15,019 张卡**（ocgcore 规则 + 中文卡名与描述），不需要联网。
* **57 套 WindBot 人机卡组**：青眼、烙印、珠泪、相剑……每个都有中文名与人格。
* 卡组编辑器在卡盒里：搜索、筛选、从全卡池组牌，带张数与**禁卡表校验**，还能导入导出 `.ydk`。

## 生存模式

| 物品 | 合成 | 用途 |
| --- | --- | --- |
| **卡盒** | 纸 ×6 + 皮革 ×3 | 组牌 / 放到决斗盘上 |
| **决斗盘** | 铁锭 ×4 + 红石 ×4 + 卡盒 | 摆出 16×14 的决斗场 |
| **人机卡组** | 卡盒 + 观察者 | 选一个 AI 对手，放到对侧半场 |
| **音乐设置** | 卡盒 + 音符盒 | 三个时机各选一首 BGM |

## 运行要求

* **Minecraft 1.20.1 + Forge 47.4.20**（47.x 均可）
* **Windows x64**：内置的决斗宿主是 Windows 程序（自带 .NET 运行时）。macOS / Linux 暂时不支持。
* 客户端和服务端都要装（单人游戏也要，因为决斗跑在整合服务器一侧）。
* 磁盘约 1 GB（jar 566 MB + 首次解出的 396 MB）；建议给游戏 4 GB 以上内存。
* **不需要安装 MDPro3 / ygopro / 任何外部程序**。

## 首次启动会解包（这是正常现象）

第一次进游戏时，模组会把卡图、立绘、召唤特效、决斗宿主从 jar 里解包到
`<游戏目录>/mdpromod/`：约 **26,500 个文件 / 396 MB / 35 秒**。
解包期间 action bar 会滚动显示进度，完成后聊天栏报一句「资源准备完成」。
之后每次启动只做一次快照比对（约 2 秒）。这个目录**删掉也没关系**，下次启动会自动重解。

## 怎么开一局（30 秒）

1. 手拿**决斗盘**右键地面 —— 场地出现。
2. 拿**卡盒**右键 —— 打开卡组编辑器组牌（生存模式先按上面的配方合成）。
3. 潜行右键把卡盒放到**自己这半场**；把**人机卡组**放到**对侧**。
4. 点自己那侧的绿色按钮 →「准备」→「开始」。
5. 左键自己的卡看能做什么；需要落点时看绿色格子。

## 命令（可选，调试与摆拍用）

| 命令 | 作用 |
| --- | --- |
| `/mdpromod duel start` / `stop` / `stats` / `status` | 直接开局 / 停局 / 看统计与状态 |
| `/mdpromod duel prompt` / `respond <值>` / `places` / `place <编号>` | 查看当前提示、手动应答、列出与选择落点 |
| `/mdpromod case give [名字]` / `export` / `load <名字>` / `info` | 拿一个卡盒 / 导出当前卡组 / 载入 .ydk / 看卡组信息 |
| `/mdpromod field place` / `bind` / `info` | 直接摆盘 / 绑定 / 查看场地信息 |
| `/mdpromod card give <卡号> [数量]` / `spawn <卡号>` | 拿一张指定卡号的卡 / 在场上生成 |
| `/mdpromod hand test` / `reveal` / `clear` | 手牌 HUD 自检 |
| `/mdpromod anim demo` / `stop` | 放一遍全部演出 / 停 |

## 常见问题

**能和朋友对战吗？**
这一版是「玩家 vs 内置 AI」。远半场必须放人机卡组才能开局；真人 PvP 还没做。
多人同服时，附近的玩家能看到你的演出和音效，但同一时间只跑一场对局。

**为什么第一次启动要等半分钟？**
因为资源全在 jar 里（这也是它不需要装别的东西的原因），第一次要解出来。之后就好了。

**为什么有的怪兽没有立绘？**
立绘只有 Master Duel 里做过的那 11,091 张，没有的会安静跳过，不影响对局。

**能关掉演出/立绘省性能吗？**
可以，启动参数按需关：`-Dmdpromod.closeup=false`（立绘）、`-Dmdpromod.cutin=false`（王牌登场）、
`-Dmdpromod.summonFx=false`（召唤演出）、`-Dmdpromod.duelAudio=false`（对局音效）、
`-Dmdpromod.duelBgm=false`（BGM）。设置里把音乐音量拉到 0 也会静音——那是最常见的一次"音效没触发"误会。

**为什么只有 Windows？**
内置的决斗宿主（ocgcore + .NET）目前只发布 Windows x64 版本。

**卡图/卡名从哪来？**
卡片数据来自 ygopro 的 ocgcore 与卡库；美术与音频来自《游戏王 Master Duel》与社区图包，仅供学习交流，请支持正版。

## 已知限制（0.1.0）

* 只有「对 AI」；真人 PvP 未实现。
* 少数决策窗口还没有界面（排序连锁、指示物、祭品数量、宣言类），遇到时会在聊天栏说明函数号与摘要。
* 多人环境下的隐藏信息过滤（对手手牌/盖卡）还在完善。
* Windows x64 限定。
* 「决斗场锚点」方块目前只是装饰，没有玩法作用。

## 致谢与协议

* 规则与数据：ygopro / ocgcore、MDPro3（GPL-3.0）——没有它们就没有这个模组。
* 美术 / 音频：Konami《游戏王 Master Duel》与 MyCard 社区图包。
* 运行时：Spine Runtimes（Esoteric Software）、libGDX（Apache-2.0）。
* 本模组以 **GPL-3.0-or-later** 发布，源码：`<填 GitHub 链接>`。

---

## Upload metadata

* **Project name**: `MDProMod — Yu-Gi-Oh! Duel in Minecraft`
* **Category**: Adventure and RPG (or Miscellaneous)
* **Game version**: 1.20.1 · **Modloader**: Forge (47.x)
* **Tags**: `yugioh` `card game` `tcg` `anime` `duel` `ai` `singleplayer` `forge`
* **License**: GPL-3.0-or-later (matches `mod_license` in `gradle.properties`)

## Screenshot checklist (6 shots, in this order)

1. Wide shot of the Duel Field: both rows of cards, LP screen and phase buttons (daylight, facing it).
2. A summon at its peak: magic circle at the centre of the field with the monster core.
3. One frame of a full-screen ace-monster cut-in.
4. Monster closeups floating above the field.
5. The deck editor (card-art grid + search).
6. The four recipes at a crafting table — or a chain/selection prompt window.

## Pre-upload checklist

1. **Publish the source first.** The mod is GPL-3.0-or-later, so shipping the binary means people must be
   able to get the corresponding source. Fill in the `<GitHub link>` above and the homepage field in
   `mods.toml`.
2. **Ship third-party notices.** The Spine Runtimes license requires its copyright and license notice to
   accompany the product. Suggested file: `THIRD-PARTY-NOTICES`, covering Spine / libGDX / ygopro-ocgcore /
   MDPro3 / Mono.Data.Sqlite. (Ask me and I'll draft it.)
3. **Decide how you feel about the assets.** Card art, closeups, BGM, sound effects and cut-ins are
   Konami's Master Duel content plus community art packs — GPL does not cover them, and CurseForge
   projects built on anime IP with original game assets have been taken down before. The safer route is
   an "assets-free" build (jar without art/audio, players point at their own folder or an optional asset
   pack); both routes can coexist as two packaging targets.

〔中文提醒〕上面 1 / 2 / 3 分别是：先公开源码、补第三方声明、想清楚素材风险（必要时做"无素材版"）。
这三件事我都可以直接帮你做。
