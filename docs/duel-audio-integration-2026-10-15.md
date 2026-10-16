# 决斗音效移植与验收记录（2026-10-15）

本次将当前 MDPro3 安装版的全部 2,333 个 SE 转换为 Ogg Vorbis，并为模组已有的
决斗和交互功能加入事件触发。资源已完整收录；原版存在、模组尚未实现的场地装饰、
伙伴和部分界面，其声音保留资源与原始触发表，不虚构游戏内触发。

## 资源与原版审计

| 项目 | 结果 |
|---|---:|
| 原版 SE AssetBundle | 2,333 个，159,709,225 bytes |
| 解码 WAV 等价体积 | 1,491,942,524 bytes |
| 输出 OGG | 2,333 个，69,859,425 bytes（69.86 MB / 66.62 MiB） |
| 相对 WAV 减少 | 95.32% |
| 总时长 | 7,998.9678 秒 |
| 编码 | Vorbis quality 3，单声道，保留原采样率、完整帧数 |
| 输出 WAV | 0 |

单声道使声音可以在 Minecraft 中按世界位置衰减。原始双声道在内存中取均值后编码，
不是先输出庞大 WAV 再打包。原 MDPro3 文件只读，没有覆盖安装目录。

每个文件的来源、哈希、时长、采样率与大小见
`tools/audio_pipeline/audio-assets.json`，全量解码验证见
`tools/audio_pipeline/audio-assets-verification.json`（2,333/2,333 通过）。

原版什么时候播放哪一声，见 `docs/duel-audio-audit-2026-10-15.md`。
该审计基于 `analysis/latest_decompiled` 的当前安装版反编译，不只搜索旧仓库 OGG。
`analysis/duel_audio_audit.json`、`analysis/duel_audio_extended.json`、
`analysis/duel_audio_rules.json` 保留完整来源、条件、时间点、伙伴动画事件和缺失资源说明。
另完整遍历 745 个召唤/连锁/入场等 Timeline 来源，0 个解析错误，导出 244 个运行时条目、
448 个声音时间点、74 种标签。

## 模组触发方式

服务端把声音语义与卡片动画放在同一个场景队列，避免规则核心快速结算导致声音提前堆叠。
LP、阶段、回合和最终结果显示也等待对应的呈现队列。公共音效从决斗场位置广播，
附近双方和观众均可听到；声音注册的衰减距离为 64 格，网络发送范围为 128 格。
玩家站在对侧时，本/对方回合、阶段、LP、直接攻击及胜负声音按当前场地视角切换。
这延续现有场地按站位判断本方的规则，不表示新增了真人 PvP 决斗系统。

| 当前模组功能 | 接入内容 |
|---|---|
| 开局、回合、阶段、结果 | 牌组出现、DUEL START、第二回合起换回合、六个主阶段、胜/负/平 |
| 卡片移动 | 抽卡、洗牌、破坏、解放、素材、衍生物、墓地/除外进出、超量附着/取除 |
| 召唤与表示变更 | 通常/特殊/盖放的落地分支、等级/阶级/连接值门限、攻击/守备表示声音 |
| 召唤过程 | 按原始 Timeline 调度融合、仪式、同调、超量、连接、灵摆多段声音 |
| 王牌登场 | 成功开始播放 Spine 后按七种属性播背景音效，不在预排队时抢先播放 |
| 连锁与卡片效果 | 连锁堆叠/数字/结算、无效、目标、原版指定卡号的效果分支 |
| 特殊融合素材 | 超融合（含异画）与召唤魔术原画/异画按结算卡原始 ID 选择专用素材展示声 |
| 战斗 | 属性攻击起手/命中、直接攻击、守备、特殊终结攻击的分支 |
| 数值与随机事件 | LP 伤害/恢复/支付、攻击力增减、种族/属性变更、计数器、硬币/骰子 |
| 本地决斗交互 | 卡片选中/确认/取消、选卡窗滑入/滑出、提示、卡图展示、可发动提示 |

公共音效使用 `ModNetwork.DuelSound`（网络协议版本 3）。客户端和服务端需使用同版模组。
选卡和确认等界面声音仅本地播放，避免把个人操作广播给对手。

`DuelSceneAudio` 与 `DuelAudioRules` 管服务端规则；`DuelAudioTimelines` 读取时间表；
`ClientDuelAudio` 管延迟、循环、停止、100 ms 去重、缺少 `_oneshot` 时的原版后备规则；
`DuelAudioPerspective` 管本/对方标签变换。退出/清场/换世界会清除待播和正在播放的声音，
并清理本地王牌登场声音。初始快照、重复状态同步和隐藏卡第一次恢复数值不会回放历史音效。

声音分类为 Minecraft 的“玩家”音量，可在游戏声音设置中调节。
启动参数 `-Dmdpromod.duelAudio=false` 可关闭这套声音。

## 保留资源但尚无对应功能的范围

- 原版 VS 入场长动画：模组当前从牌组出现/开局开始，没有 VS 画面与长循环入场。
- 原版场地装饰点击、阶段破坏、附加动画事件，以及 Master Duel / Cross Duel 伙伴。
- 模组不存在的原版日志、缩放、侧边面板等界面声音。
- 原安装本身缺少的 31 个可选/未使用资源引用；详见原版审计中的缺失表。

这些范围的规则均已调查，所有实际存在的 SE 已随包收录；没有将它们任意挂到其他动作上。
110 个 BGM bundle 与可下载角色语音是独立系统，本次没有混作 SE 压缩或宣称接入。

## 验收与交付

2026-10-15 使用 Microsoft OpenJDK 17.0.19 执行 `clean build --offline`，构建成功。

| 验收项 | 结果 |
|---|---:|
| `DuelVisualStateTest` | 71 assertions，通过 |
| `DuelVisualStateAudioTest` | 526 assertions，通过 |
| `DuelAudioRulesTest` | 117 assertions，通过 |
| `DuelAudioPerspectiveTest` | 通过 |
| OGG 全量解码/哈希/帧数检查 | 2,333/2,333，通过 |
| 源码资源与最终 JAR 声音事件 / OGG | 2,333 / 2,333 |
| 最终 JAR OGG 体积 | 69,859,425 bytes |
| Timeline | 244 条目 / 448 cues / 74 种声音 key |
| 未注册引用 / WAV | 0 / 0 |
| Spine `CutinSelfTest --all` | 497/497，通过 |

最终构建产物为 `mod/build/libs/mdpromod-0.1.0.jar`，大小 136,765,571 bytes
（136.77 MB / 130.43 MiB），构建时间 2026-10-15 11:39:32，SHA-256：

`4B3DA64BC2F811C35CE3A3AC0B6F4E40D9B80A941D5BDAE01417445122B8D435`

已部署到
`D:\光明MOD服1.12客户端\.minecraft\versions\1.20.1-Forge_47.4.20\mods\mdpromod-0.1.0.jar`；
部署后大小和 SHA-256 与构建产物一致。旧包保留为同目录下
`mdpromod-0.1.0.jar.pre-audio-20261015.bak`，不是 `.jar` 扩展，Forge 不会同时加载。
最终包和部署包的资源审计分别写入
`analysis/duel_audio_integration_verification.json` 与
`analysis/duel_audio_deployed_verification.json`。

尚未进行 Minecraft 内实机听感和双客户端联调。仪式子轨的父轨起点目前按 1,500 ms
对齐估算；灵摆 `ScaleSet` 因模组尚无其严格的双目标、阶段、连锁和 Master Rule 状态，
没有错误地挂到普通刻度放置。资源存在、静态规则验证与游戏内完整复现是不同验收项。
