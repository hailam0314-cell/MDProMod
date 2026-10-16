# 背景音乐（BGM）：全量转入 + 「音乐设置」物品（2026-10-15）

把 MDPro 安装版里的 **110 首 BGM 全部**转成 Ogg Vorbis 打进模组，开局演出结束后播放；
并新增「音乐设置」物品，右键分页选择想听哪一首。选择结果存在物品自己的 NBT 里，
每个客户端各放各的，互不影响。

## 一、素材与参数

| 项目 | 数值 |
| --- | ---: |
| 源 bundle | `sound_assets_bgm_*.bundle` 110 个，259.1 MiB |
| 总时长 | 13,509 秒（3.75 小时），全部 48 kHz 立体声 |
| 输出 | 110 个 OGG，**101.7 MiB**（106,612,137 字节） |
| 编码 | libvorbis **quality 0，立体声**，保留原采样率与完整帧数 |
| 最终 JAR | 243,112,058 字节（约 231.8 MiB） |

### 为什么是立体声 q0，而不是像 SE 那样单声道 q3

SE 用单声道是为了让声音能按世界位置衰减（OpenAL 只能定位单声道源）。
BGM 走的是 `SoundSource.MUSIC` + `Attenuation.NONE` 的**非定位**播放，不需要降成单声道。
实测（`tmp/probe_bgm_encode.py`，取三首代表曲目真实编码后外推全量）：

| 参数 | 外推体积 | 相对 WAV |
| --- | ---: | ---: |
| 单声道 q3 | 107.3 MiB | 2.2% |
| 单声道 q0 | 79.3 MiB | 1.6% |
| **立体声 q0（采用）** | **105.6 MiB** | 2.1% |
| 立体声 q1 | 126.5 MiB | 2.6% |
| 立体声 q3 | 164.7 MiB | 3.3% |

**立体声 q0 和单声道 q3 体积几乎一样，但保住了声场**，所以选了它。
想要更好的音质：`python tools/audio_pipeline/build_bgm_assets.py --quality 1`（多约 20 MiB）。

## 二、播放时机

沿用已有管线，**没有新增时序**：`DuelAnimationScene` 在开局演出（发牌 + 落场）结束、
`!opening && !bgmStarted` 之后发一次 `DuelSound` 的 BGM cue，客户端再延迟
`DuelBgmRules.START_DELAY_MS = 250 ms` 播放——留出最后一声开局 SE 的释放时间。

客户端收到 cue 时**放自己选的那首**：`ClientDuelAudio.currentChoice()` 的优先级是
`-Dmdpromod.duelBgm` 系统属性（调试覆盖）→ 「音乐设置」物品里的选择 → 默认曲目。

对局进行中换曲子会**立刻生效**：`ClientMusicSettingsBridge.apply()` → `ClientDuelAudio.refreshMusic()`
把正在放的那首停掉、换成新的。

## 三、「音乐设置」物品

- 注册名 `mdpromod:music_settings`，显示名「音乐设置」（`en_us`: Music Settings），
  **图标直接用现成的卡背** `mdpromod:item/card_back_icon`；已加入创造模式物品栏
  「MDProMod 决斗」标签页。
- 右键 → 服务端下发 `MusicSettingsOpen`（110 首 + 「不播放」，键 + 中文标签）→
  客户端打开分页界面：**9 行/页、滚轮或 ◀ ▶ 翻页**，当前曲目打 ✓。
- 点一首 → 客户端立刻换（`MusicSettingsAction`）→ 服务端写进物品 NBT 并同步物品栏，
  同时给自己一条 action bar 提示。
- 物品栏第一位是**「不播放」**，适合只想听音效的人。
- 上线时服务端会把物品里存的曲子**同步给客户端**（`MusicSettingsOpen.sync`，只应用不开界面），
  所以重连后不会退回默认曲目。
- 默认曲目从原来借用的 SE 短循环 `SE_M08092_F_LOOP` 换成了真正的决斗 BGM
  **`BGM_DUEL_NORMAL_01`**。

## 四、曲目标签

键名是 `BGM_DUEL_EX_05_KEYCARD` 这种机器名，中文标签由 `build_bgm_assets.py` 的
`label_for()` 生成（`TOKENS` 表 + 分组数字合并），例：

| 键 | 标签 |
| --- | --- |
| `BGM_DUEL_NORMAL_01` | 决斗 · 常规 01 |
| `BGM_DUEL_CLIMAX_16` | 决斗 · 高潮 16 |
| `BGM_DUEL_KEYCARD_08` | 决斗 · 王牌登场 08 |
| `BGM_DUEL_F05_PHASE_A` | 决斗 · F05 · A 段 |
| `BGM_DUEL_EX_08_ALL` | 决斗 · EX08 · 完整 |
| `BGM_MENU_RETRO` | 菜单 · 复古 |
| `BGM_DICERALLY` | 骰子拉力 |

清单落在 `data/mdpromod/bgm_tracks.txt`（制表符分隔），模组侧由 `DuelBgmTracks` 读取，
两边都不需要 JSON 解析。

## 五、改动的文件

| 位置 | 说明 |
| --- | --- |
| `tools/audio_pipeline/build_bgm_assets.py` | 新增：转换 + 生成清单与注册表 |
| `tools/audio_pipeline/publish_registry.py` | 合并 SE 与 BGM 两批注册；SE 才有 64 格衰减 |
| `assets/mdpromod/sounds/mdpro/bgm/*.ogg` | 110 首（`stream: true`，全部 > 20 s） |
| `assets/mdpromod/sounds.json` | 2,443 条事件（2,333 SE + 110 BGM） |
| `assets/mdpromod/models/item/music_settings.json`、`lang/{zh_cn,en_us}.json` | 物品模型与名称 |
| `data/mdpromod/bgm_tracks.txt` | 曲目键 + 标签 |
| `DuelBgmRules` | 默认曲目改为 `BGM_DUEL_NORMAL_01` |
| `DuelBgmTracks`（新） | 读曲目清单；`label()` 负责显示名 |
| `MusicSettingsItem` / `MusicSettingsScreen` / `ClientMusicSettingsBridge` | 物品与选择界面 |
| `ClientDuelAudio` | 选择来源、`startMusic`/`refreshMusic`、BGM 上下文随场地清理 |
| `ModNetwork` | 新增 `MusicSettingsOpen` / `MusicSettingsAction`（协议仍是 3） |
| `ModItems` / `MDProMod` | 物品注册、创造模式标签页、上线同步 |

## 六、验证

| 项 | 结果 |
| --- | ---: |
| BGM 转换 | 110/110，0 失败 |
| `DuelBgmRulesTest` | 通过（含曲目清单 ≥110、标签、`off`、未知键回退） |
| `DuelAudioRulesTest` / `DuelVisualStateAudioTest` / `DuelVisualStateTest` | 117 / 526 / 71 断言，通过 |
| 包内 BGM OGG / SE OGG / 清单 / 模型 | 110 / 2,333 / 1 / 1 |
| JAR SHA-256 | `C3DBB5D8332E0ED560267844C95A5EA049AAD9045A730F06462CC04FFBBEE374` |

测试是**对着打包好的 JAR** 跑的（`-cp build/libs/mdpromod-0.1.0.jar;build/classes/java/test`），
所以清单资源确实是"在包里能读到"的那份。

已部署到 `D:\光明MOD服1.12客户端\.minecraft\versions\1.20.1-Forge_47.4.20\mods\mdpromod-0.1.0.jar`，
部署包与构建产物哈希一致；上一版保留为同目录 `mdpromod-0.1.0.jar.pre-bgm-20261015.bak`。

复现：

```powershell
python tools\audio_pipeline\build_bgm_assets.py            # 转 110 首 + 生成清单
python tools\audio_pipeline\publish_registry.py            # 合并进 sounds.json
cd mod; .\gradlew.bat build --offline
java -cp "build\libs\mdpromod-0.1.0.jar;build\classes\java\test" net.mdpromod.DuelBgmRulesTest
```

## 七、尚未做 / 边界

1. **没有实机听感验证**（和音效那批一样）。建议先试：开局是否在发牌结束后 250 ms 起播、
   对局中换曲子是否立刻切、重连后是否记得住。
2. **没有按 LP 切高潮曲**。原版在 LP < 25% 会 `PlayBgmClimax()`；资源里
   `BGM_DUEL_CLIMAX_*` 已经可以手动选，但自动切换属于新逻辑，本次没做。
3. **菜单 BGM（`BGM_MENU_*`、`BGM_DICERALLY`）只能手动选**，模组没有对应的原版界面去自动触发。
4. 未压缩的 110 首共 13,509 秒；若嫌包大，可只保留常用的一部分（改
   `build_bgm_assets.py` 的 `bundles` 过滤）再重跑后两步。
5. `mod_version` 仍是 0.1.0，只是包体从 136 MB 涨到 243 MB；
   发布前建议升版本号，避免用户分不清手里是哪一版。

## 八、改版：按「时机」分栏 + 试听（同日修订，取代第三节）

第一版是"全局选一首"，实际用起来不对路：一幅对局里音乐该在**不同时机**换不同的曲子。
现在改成**按时机分栏**，每栏各选一首。

### 8.1 三个时机与优先级

| 时机 | 键 | 什么时候放 | 默认曲目 |
| --- | --- | --- | --- |
| 决斗 | `duel` | 开局演出结束后起播，常驻 | `BGM_DUEL_NORMAL_01` |
| 王牌登场 | `keycard` | 有王牌登场演出（cut-in）在播时临时接管，放完回到「决斗」 | `BGM_DUEL_KEYCARD_01` |
| 低 LP 高潮 | `climax` | 本地玩家 LP ≤ **2000**（8000 的 25%，对应原版 `PlayBgmClimax()`） | `BGM_DUEL_CLIMAX_01` |

**优先级：王牌登场 &gt; 低 LP 高潮 &gt; 决斗。**
某一栏选「不播放」表示**该时机不打断常驻 BGM**（而不是静音）——比如「王牌登场」选「不播放」，
cut-in 期间会继续放「决斗」那首。

时机由客户端每 tick 判定（`ClientDuelAudio.activeMoment()` + `refreshMusic()`），
**时机或曲目变了才换曲**，没变什么都不做，所以不会把音乐反复重播。
服务端只负责：开局发一条 cue，以及保存玩家在界面里选的曲子。

### 8.2 界面

`MusicSettingsScreen` 重做：

- 顶部三个**栏目标签**（决斗 / 王牌登场 / 低 LP 高潮），当前栏目带 ▶；
- 每栏 9 行一页（滚轮或 ◀ ▶ 翻页），行 = `[曲目名]` + `[♪]`；
  点名字 = 设为该栏曲子（✓ 自动跟着走），点 ♪ = 单独试听这一首；
- 底部 **「♪ 试听 / ■ 停止试听」** 试听当前栏已设好的那首；
- **关闭界面、按任意键、被别的界面顶掉（`removed()`），都会结束试听**；
  开始下一首试听也会先停掉上一首。

试听是纯客户端的：直接把该 OGG 用 `SoundSource.MUSIC` 放出来，期间**让出**正在播的对决 BGM，
停止试听后再按当前时机恢复，不会两首叠在一起。

### 8.3 数据与同步

- 物品 NBT：`Music.<时机> = 曲目键`（空串 = 不播放）。旧版的单字段 `MusicTrack` 仍能读，
  当作「决斗」那一栏，不会让老物品失效。
- 包：`MusicSettingsOpen(selected[], keys[], labels[], open)`（时机顺序两端同源，
  不必上网络传输时机表）与 `MusicSettingsAction(moment, track)`。
- 上线同步仍是只应用、不开界面。

### 8.4 一个排查记录（不是 bug）

第一次实机反馈"音乐没触发"，最后查明是**玩家设置里「音乐」音量滑块为 0**。
BGM 走 `SoundSource.MUSIC`，受原版音乐音量控制——以后遇到"只没音乐、音效正常"，
先看这个滑块，再看日志里有没有 `Missing bundled duel sound`。

### 8.5 修订后的验证

| 项 | 结果 |
| --- | ---: |
| `DuelBgmRulesTest` | 通过（新增：三时机、默认曲目、2000 阈值、每栏默认都能解析出标签） |
| `DuelAudioRulesTest` / `DuelVisualStateAudioTest` / `DuelVisualStateTest` | 117 / 526 / 71 断言，通过 |
| JAR | 243,115,700 字节，SHA-256 `53CF73807D42C5368AE65EA83EE8C1A0D1A45F7219EBAE58427244191B918CE2` |

仍未做的：实机听感（三个时机是否都按预期切、试听是否被按键/关闭正确打断）。
界面里控件重建是**延迟到 tick** 执行的——MC 正在遍历 children 时不能在按钮回调里重建，
否则有并发修改风险，这一点改动时别退回去。

## 九、实机反馈后的修正：时机要「闩锁」，并压住原版音乐（同日）

第一次实机暴露两个**设计错**（不是笔误）：

### 9.1 王牌登场 / 高潮被立刻切回普通音乐 → 时机必须闩锁

原来把时机做成**每 tick 重算的纯函数**（`isPlaying()` 为真才算是王牌登场），
于是 cut-in 一结束（1.6 s）就立刻退回「决斗」曲。但 MD 的行为是
**王牌 BGM 接管之后一直放**（`PlayBgmKeyCard()` 之后不会自己切回去），
低 LP 高潮同理。

改法：两个时机都改成**闩锁**（`keycardLatched` / `climaxLatched`），一旦触发就保持：

| 闩锁 | 触发 | 解除 |
| --- | --- | --- |
| 王牌登场 | 有 cut-in 开始（**上升沿**，并让这首从头再放一遍） | 本场决斗结束 / 清场 |
| 低 LP 高潮 | **本地玩家** LP ≤ 2000 | 本场决斗结束 / 清场 |

优先级仍是 王牌登场 &gt; 高潮 &gt; 决斗；「不播放」依旧是"该时机不打断"。
决斗开始时两个闩锁都复位（`initialize` 那条 cue 到达时）。

### 9.2 和原版音乐同时响 → 要主动压住 MusicManager

我们的 BGM 和原版背景乐都走 `SoundSource.MUSIC`，互不干扰是不成立的。
现在只要**我们这边有音乐该放**（BGM 或试听）：

```java
if (ownsMusic()) minecraft.getMusicManager().stopPlaying();
```

在 `ClientDuelAudio.tick()` 里每 tick 压一次，原版那首刚起就被掐掉。
两栏都选「不播放」时 `ownsMusic()` 为假，原版音乐照旧（那种情况下它不该被我们管）。

### 9.3 排查用的日志

每次真正换曲都会打一行：

```
BGM 切换：[决斗] BGM_DUEL_NORMAL_01
BGM 切换：[王牌登场] BGM_DUEL_KEYCARD_01
BGM 切换：[低 LP 高潮] BGM_DUEL_CLIMAX_01
```

下次实机如果还有"该切不切/切了又回"，看这几行的**顺序和时间**就能判定是时机判定错了
还是播放被掐了。

### 9.4 产物

JAR 243,116,153 字节，SHA-256
`B03C145E7CAD7EB07C7CB602E0B96B7ADD417316CD652C888A4FC6B989AF199B`；
5 套离线测试通过；已部署（哈希一致）。

## 十、再修正：照抄原版的"只前进不后退"阶段机（同日）

第九节的"闩锁"仍然不对：王牌那栏是**每次 cut-in 上升沿都重放**，所以一场里连召几只王牌，
音乐就被反复从头开始。用户要求先查清原版时机，于是回去读了
`MDPro3\Managers\AudioManager.cs`（第 299~320 行）与 `OcgCore.cs`（2818 / 2975 行）：

```csharp
static int bgmState = 0;                       // 0 常规 / 1 王牌 / 2 高潮
public static void PlayBgmKeyCard() {
    if (bgmState > 0) return;                  // 已经进过王牌或高潮 → 直接返回，不重放
    bgmState = 1; PlayBGM(KEYCARD 变体);
}
public static void PlayBgmClimax() {
    if (bgmState == 2) return;                 // 已经高潮 → 不重放
    bgmState = 2; PlayBGM(CLIMAX 变体);
}
// 复位：PlayBgmNormal() → bgmState = 0，在开局牌组演出结束的回调里调一次（每场一次）
```

**所以原版是"只前进不后退"**：

| 事件 | 状态 | 结果 |
| --- | --- | --- |
| 开局演出结束 | — → 0 | 播常规曲 |
| **第一只**王牌登场 | 0 → 1 | 播王牌曲 |
| 第二只 / 第三只王牌 | 1 → 1 | **什么都不做**（音乐继续放） |
| LP 跌破 25% | 1 → 2 | 播高潮曲（把王牌顶掉） |
| 之后再跌破 / 再召王牌 | 2 → 2 | **什么都不做**（高潮一直放到本场结束） |

### 10.1 实现

状态机抽成 **纯函数** `DuelBgmRules.advanceBgmStage(stage, cutinStarted, lowLifePoints)`，
客户端只负责喂三个输入（上升沿 / 本地 LP / 开局复位）：

```java
if (stage == STAGE_NORMAL && cutinStarted) stage = STAGE_KEYCARD;
if (stage != STAGE_CLIMAX && lowLifePoints) stage = STAGE_CLIMAX;
```

阶段**真的变了**才清空 `playingMoment/playingTrack` 去换曲，所以：

- 多只王牌 → 阶段停在 1，`refreshMusic()` 发现"没变"，**不会重播**；
- 高潮只触发一次，之后连召王牌也不会把高潮曲顶掉（和原版一致，优先级是**高潮 &gt; 王牌**）；
- 每场开局那条 cue 到达时把阶段复位成 0（对应原版 `PlayBgmNormal`）。

### 10.2 回归测试

`DuelBgmRulesTest.stageMachine()` 把这张表逐条断言了，包括
"second ace does not restart the keycard stage"、"ace cannot override climax"、
"climax does not re-fire"。以后谁再动这块逻辑，跑测试就能立刻发现。

### 10.3 与原版仍有的一处差异（已知，未做）

原版 `AudioManager.loops` 表给每首 BGM 记了**循环起止点**（例如
`BGM_DUEL_NORMAL_01` 是 9.600 s ~ 115.200 s），也就是"前奏只放一次，之后循环中段"。
MC 的 `SimpleSoundInstance` 只能整首从头循环，没有循环点，所以我们的 BGM 每轮会
把前奏重放一遍。要做到一致，需要"前奏 + 循环段"两段拼接播放，属于独立改动，本次没做。

### 10.4 产物

JAR 243,116,382 字节，SHA-256
`A736092B6859D99E3869753B3B011A073A3966EE9A6C03CE16D310CC55FC8F62`；
5 套离线测试通过；已部署（哈希一致）。
