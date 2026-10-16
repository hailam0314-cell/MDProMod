# MDPro3 决斗音效触发审计（2026-10-15）

## 依据和可复现数据

本次以当前安装版而不是旧仓库为准：

- 安装目录：`D:\MyCardLibrary\mdpro3`。
- 当前程序集反编译：`D:\MDProMod\analysis\latest_decompiled`。
- 扫描器：`D:\MDProMod\tools\audit_duel_audio.py`。不解码纹理、不修改 MDPro3 安装文件。
- `D:\MDProMod\analysis\duel_audio_audit.json`：扫描 Built-in/duel、所有 Timeline、MonsterCutin、MonsterCutin2 共 11,573 个 bundle；43 个 bundle 含 SoundPlayable 引用、214 个音轨事件、67 个音轨 label，0 个 bundle 错误。
- `D:\MDProMod\analysis\duel_audio_extended.json`：进一步扫描 Built-in/outduel、Card、Effects、Mat、BG、Grave、AvatarStand、Mate 与 MDPro3 UI prefab。含逐音轨的 `timeline/start/duration/sound/muted`、逐 AnimationClip 的 `clip/time/function/sound`、逐 prefab 的音效字段及所属对象、Cross Duel 的 EffectSeLabelData 效果/时间表。该文件是完整的机器可读清单，不能仅根据本文的家族通配符选择几个代表音效。

当前安装版有 2,443 个 `sound_assets_*.bundle`；旧源码 Sound 下只有 1,976 个 OGG（其中含 BGM、局外 UI、伙伴等）。旧文件数不能当作最新版决斗音效总数。音效的“存在”与“当前模组已有相应功能并接上触发”也必须分开统计。

扩展扫描共检查 2,225 个 bundle，183 个含音效记录，bundle 和对象解析错误均为 0；另记录 243 处 C# 音频调用和 407 行音频字符串。各类记录如下，列间存在重复引用，不能直接相加为音频文件数：

| 来源 | 事件/字段记录 | 不同 label |
|---|---:|---:|
| SoundPlayable Timeline | 228 | 78 |
| AnimationClip 动画事件 | 768 | 756 |
| Prefab 音效字段 | 1,725 | 100 |
| Cross Duel EffectSeLabelData | 1,265 | 629 |
| AudioSource 直接音频引用 | 0 | 0 |

所有 bundle 引用合并后有 1,485 个不同 label。其中 39 个没有同名安装资源，8 个可按原版 `_oneshot` 后备规则找到资源，剩余 31 个来自伙伴、Cross Duel 或未使用的 prefab 字段，当前安装版及旧源码均缺少对应音频。完整名单及来源保留在 extended JSON 的 `catalog_validation` 和原始记录中。78 个 Timeline label 与展开后的 69 个核心动态家族 label 均有资源。不能把原包本身缺失的可选资源报告成压缩失败，也不能将序列化字段引用直接等同于实际播放。

源码字符串检查里的 `SE_CARD_MOVE_0`、`SE_FIELD_MAT`、`SE_PHASE` 是拼接前缀，不是缺音频；`SE_LP_ZERO` 只出现在 `YgomGame.Duel/DuelLP.cs` 的常量桩中，未发现当前 MDPro 运行分支使用。

## 一、决斗进出、回合与阶段

| 触发 | MDPro 音效 | 原始时机 |
|---|---|---|
| 双方 VS 入场 | `SE_DUEL_ENTRY_VS` | DuelEntry_Timeline +0.25 秒；组队版本 +0.266667 秒 |
| VS 循环入场 | `SE_DUEL_ENTRY_LOOP_02` | 普通 +6.1 秒；组队 +11.116667 秒 |
| 牌组出现 | `SE_DUEL_DECK_ENTRY` | DuelDeckAppearanceTL +0.15 秒 |
| DUEL START | `SE_DUEL_START` | DuelTextStartTimeline +0 秒 |
| 第二回合起换回合 | `SE_NEXT_TURN_PLAYER` / `SE_NEXT_TURN_RIVAL` | 本方 +0.483333 秒；对方 +0.416667 秒 |
| 阶段横幅 | `SE_PHASE[_OPP]_{DRAW,STANDBY,MAIN1,BATTLE,MAIN2,END}` | 阶段横幅开始；`_OPP` 是对方回合 |
| 胜利 | `SE_DUEL_WIN` | 胜利横幅 +0 秒 |
| 败北/投降败北 | `SE_DUEL_LOSE` | 对应横幅 +0 秒 |
| 平局 | `SE_DUEL_DRAW` | 平局横幅 +0 秒 |

阶段检查没有给 Damage、DamageCal、BattleStep 等战斗子步骤另播一次阶段音效。证据：`MDPro3.Duel/DuelBGManager.cs:3445-3483`；音轨时间来自 bundle，不是猜测的统一延迟。

## 二、抽卡、移动、区域、破坏、素材

| 触发 | 音效 | 条件/时机 |
|---|---|---|
| 牌组到手牌 | `SE_CARD_MOVE_01`～`04` | 每次随机一种；不是任意区域移动都播抽卡声 |
| 手牌抬起查看 | 同上 | AnimationHandAppeal 首次抬起 |
| 洗手牌/洗牌组 | 依序 `SE_CARD_MOVE_01`～`04` | 每段间隔为前一段音频时长的 0.5 倍 |
| 场上/手牌被破坏 | `SE_CARDBREAK_01` | reason 含 DESTROY，原区域是场上或手牌 |
| 解放 | `SE_SUMMON_ADVANCE` | reason 含 RELEASE |
| 仪式素材离场 | `SE_SUMMON_RITUAL_MATERIAL` | 素材动画开始 |
| 融合素材离场 | `SE_SUMMON_FUS_MATERIAL` | 同上 |
| 同调素材离场 | `SE_SUMMON_SYNC_MATERIAL` | 同上 |
| 连接素材离场 | `SE_SUMMON_LINK_MATERIAL` | 同上 |
| 超量素材化 | `SE_SUMMON_XYZ_MATERIAL` | 当前版正确拼写为 XYZ；旧源码 OGG 存在 EYZ 拼写 |
| 超量素材附着 | `SE_CARD_XYZ_IN` | 移动结束后 |
| 超量素材取除/离开 | `SE_CARD_XYZ_OUT` | 不是 RULE 移动且满足离开/效果分离条件 |
| 衍生物产生 | `SE_CARD_TOKEN_SUMMON` | 原 location=0 且类型 Token |
| 消失到 location=0 | `SE_CARD_TOKEN_BREAK` | 消失演出开始 |
| 非衍生物凭空生成 | `SE_CARDCHEAT_APPEAR` | CardCheatAppear 演出开始 |
| 进入墓地 | `SE_CEMETARY_ABSORB` | 场景 GraveIn 特效的 delay 后 |
| 离开墓地 | `SE_CEMETARY_GOOUT` | GraveOut 特效 delay 后 |
| 进入除外 | `SE_EXCLUSION_ABSORB` | ExcludeIn 特效 delay 后 |
| 离开除外 | `SE_EXCLUSION_GOOUT` | ExcludeOut 特效 delay 后 |
| 卡片成为效果目标 | `SE_CEMETERY_CARD` | `AnimationTarget()`；注意此 label 的 CEMETERY 拼写与上述 CEMETARY 不同 |

证据：`MDPro3/GameCard.cs:1400-1698`、`MDPro3/AudioManager.cs:1072-1089`、`MDPro3.Duel/DuelBGManager.cs:2023-2069`。reason/原位置是关键，不能只比较当前快照把所有移卡都套一个声音。

## 三、召唤落地、召唤过程与王牌登场

### 落地

| 触发 | 音效 |
|---|---|
| 盖放 | `SE_LAND_MT_SET` |
| 通常召唤等级 ≤4 | `SE_LAND_NORMAL` |
| 通常召唤等级 5～6 | `SE_LAND_ADVANCE_MIDDLE` |
| 通常召唤等级 ≥7 | `SE_LAND_ADVANCE_HIGH` |
| 特殊召唤，有相应素材记录 | `SE_LAND_{FUSION,SYNCHRO,XYZ,LINK,RITUAL}_{MIDDLE,HIGH}` |
| 灵摆召唤，无前述素材分支 | `SE_LAND_PENDULUM_{MIDDLE,HIGH}` |
| 其他特殊召唤 | `SE_LAND_NORMAL_{MIDDLE,HIGH}` |

特殊召唤的 HIGH 不是统一的“等级≥7”：`Card.IsHighLevel()` 还包括 Link 值>2、Xyz 阶级>3。衍生物跳过普通特殊召唤落地分支。证据：`DuelMessage.cs:846-1059`、`MDPro3.Duel.YGOSharp/Card.cs:270`。

### 王牌怪兽 cutin

MonsterCutin 和 MonsterCutin2 本体没有发现 SoundPlayable 音轨。实际声音来自 `Built-in/outduel` 的属性背景：

`SE_MONSTER_CUTIN_{DARK,LIGHT,EARTH,WATER,FIRE,WIND,DIVINE}`，背景动画 +0 秒播放。

因此应跟随当前播放的 cutin 按怪兽属性选声音；不是给每张怪兽杜撰专属声，也不是只播放落地声。`CutinViewer.Play` 同时负责王牌 BGM 阶段切换，但那是 BGM，不是这条音效。

### 仪式/融合/同调/超量/连接/灵摆完整动画

原版由 Timeline 内多段音轨触发；不能在开始时同时播完。所有变体和精确秒数在 JSON 中完整列出：

- 通用素材展示：`SE_SMN_CMN_CARD_01/02/SUPER`。
- 融合：`SE_SMN_FUSION_01_01/01_02/01_03`、`SE_SMN_FUSION_02`。
- 仪式：`SE_SMN_RITUAL_01`、`02_01/02_02/02_03`、`03/04`。
- 同调：`SE_SMN_SYNCHRO_01_01/01_04/02`、`03_01/03_02/03_03/03_04`、`04_01/04_02/04_03/04_04`、`05`。
- 超量：`SE_SMN_XYZ_01`、`02_01/02_02/02_03`、`03/04`。
- 连接：`SE_SMN_LINK_01/02`、`03_01/03_02`、`04/05`。
- 灵摆：`SE_SMN_PENDULUM_01`、`01_01/01_02/01_03`、`02`。
- 灵摆召唤中的刻度展示（MR≥4）：`SE_SUMMON_PENDULUM_SET` +0.033333 秒、`SE_LP_STOP` +0.266667 秒；不是单张灵摆卡放到刻度区时都播放。
- 特定融合素材展示：连锁结算卡原始 ID 为 `48130397`（超融合，含异画）时使用
  `SE_SMN_CMN_CARD_SUPER`，随素材数分别在 +0.100/+0.067/+0.050/+0 秒播放；原始
  ID 为 `74063034`（召唤魔术）时，实际 ID `74063035` 使用 `SE_EV_045_SPECIAL`
  （03432 系列），其他版本使用 `SE_EV_045_NORMAL`（12852 系列），两者均在
  +0.016667 秒播放。不是所有融合都使用这些专用声音。

例如 SummonLink01_01TL 的开始点分别为：0.0 通用卡、1.333333 LINK_01、1.816667 LINK_02、2.1 LINK_03_01、2.783333 LINK_04、3.633333 LINK_05。不同素材数量的变体时间不同，应选择对应轨。

`SummonPendulumScaleSet` 的前置条件来自 `DuelMessage.GameMessage_BecomeTarget`：Main1/Main2、无连锁、恰好两张目标均在同一方灵摆区，并允许当前决斗/观战/回放模式的灵摆动画。它随 `PlaySummonPendulum()` 的完整灵摆召唤演出开始，且只有 MasterRule≥4 才创建。不能挂到每一次刻度区移卡。详见 `D:\MDProMod\tools\audio_pipeline\special-duration-handoff.md`。

## 四、卡片发动、连锁、无效、数值与标记

| 触发 | 音效 | 时机 |
|---|---|---|
| 发动卡片展示 | `SE_CARDVIEW_01` | `AnimationActivate()` 开始，即 CHAINING |
| 卡片确认等其他展示 | `SE_CARDVIEW_02` | `AnimationConfirm` 等对应展示分支 |
| 连锁点亮 | `SE_DUELCHAIN_01` | CHAINED 后 `AddChain` 创建 ChainSpot，轨 +0 秒 |
| 连锁2叠加 | `SE_DUELCHAIN_STACK01`、`SE_DUEL_CHAIN_NUMEFF_01_2nd` | +0.016667、+0.65 秒 |
| 连锁≥3叠加 | `SE_DUELCHAIN_STACK02` | +0.0166 秒 |
| 连锁3/4/≥5数字 | `SE_DUEL_CHAIN_NUMEFF_01/02/03` | +0.767 秒 |
| 结算连锁1 | `SE_DUELCHAIN_RESOLVE01` | 轨 +0.216667 秒 |
| 结算连锁2 | `SE_DUELCHAIN_RESOLVE02`、`SE_DUELCHAIN_STACK02` | 轨 +0.05、+1.133333 秒 |
| 结算连锁≥3 | `SE_DUELCHAIN_RESOLVE03`、`SE_DUELCHAIN_STACK02` | 轨 +0.016667、+1.183333 秒 |
| 效果无效 | `SE_EFFECT_INVALID` | AnimationNegate 开始；部分卡另有专属无效音 |
| 可发动提示 | `SE_DUEL_ACTIVE_POSSIBLE` | ShowHint 可行动激活提示，不等于实际发动 |
| ATK上升/下降 | `SE_BUFF_ATTACK` / `SE_DEBUFF_ATTACK` | 当前 ATK 相对上次显示值发生改变 |
| 属性/种族改变 | `SE_BUFF_CHANGE` | 新属性/种族不同于上次值，避免每帧重复 |
| 攻击/守备表示动画 | `SE_DISP_ATTACK` / `SE_DISP_DEFENS` | `AnimationPosition` 对应表示；音量 0.6，原 DEFENS 拼写需保留 |
| 添加/移除指示物 | `SE_CARD_COUNTER` | 两种操作共用 |

单卡连锁不播放大型连锁叠加/结算横幅；但仍有卡片发动展示、ChainSpot。结算序号>1且不是本条连锁最先结算时，原版把 resolve timeline 起播时间跳到 0.83 秒，早于该点的 RESOLVE 音效不会再播；其后 STACK02 时间应减去起播点。原版还能关闭连锁动画。证据：`DuelBGManager.cs:2446-2708`。

## 五、攻击与生命值

| 触发 | 音效 | 选择/时机 |
|---|---|---|
| 直接攻击宣言文字 | `SE_DA_TEXT` | ATTACK 消息，只有直接攻击 |
| 普通战斗发射 | `SE_ATTACK_A_<属性>_<强度>_01` | BATTLE 消息演出开始；不是 ATTACK 宣言时 |
| 普通战斗命中 | `SE_ATTACK_A_<属性>_<强度>_02` | ATK<2000 约+0.4秒，否则+0.65秒 |
| 被防住 | `SE_ATTACK_GUARD` | 覆盖命中声；守备 DEF≥我方 ATK 或攻击表示对手 ATK≥我方 ATK |
| 直接攻击命中 | `SE_DIRECT_ATTACK_RIVAL` / `SE_DIRECT_ATTACK_PLAYER` | 本地视角的受击方，覆盖属性命中声 |
| LP伤害/支付费用 | `SE_COST_DAMAGE` | DAMAGE/PAY_LP_COST；LP_UPDATE 仅减少时播放 |
| LP数字递减 | `SE_LP_COUNT_PLAYER` / `SE_LP_COUNT_RIVAL` | 数字滚动开始，不是伤害消息一来同时播放 |
| LP恢复 | `SE_LP_RECOVERY_PLAYER` / `SE_LP_RECOVERY_RIVAL` | 同上，正向变化 |

属性为 DARK/EARTH/FIRE/LIGHT/WATER/WIND/DIVINE；强度在 ATK<2000 时用 DEFAULT，否则 SPECIAL，共 28 条攻击/命中组合。`SE_COST_DAMAGE` 与 LP 数字声是不同演出阶段，可以同时存在，不应互相替代。

原版的特定终结攻击会替代普通攻击演出：青眼 BE、黑魔导 BM、真红眼 RE、太阳神 RA、天空龙 SLIFER、巨神兵 OBELISK；声音为 `SE_MONSTERATTACK_<代号>_01`（起手）和 `_02`（命中）。是否使用以 `NeedSpecialFinalAttackAsync` 的卡牌及终结条件为准。证据：`DuelMessage.cs:1188-1648`、`DuelBGManager.cs:3900-4560`、`OcgCore.cs:1945-2030`。

## 六、硬币、骰子、特殊胜利

| 触发 | 音效 | 时间 |
|---|---|---|
| 抛硬币 | `SE_COIN_THROW` | 每枚开始 |
| 硬币落面0 | `SE_COIN_DECIDE_02` | +2.0秒 |
| 硬币落面1 | `SE_COIN_DECIDE` | +2.0秒 |
| 掷骰子 | `SE_DICE_ROLL` | 每枚开始 |
| 骰子结果 | `SE_DICE_DECIDE` | +0.6秒 |
| 特殊胜利展示卡片 | `SE_DUEL_SPECIALWIN_CARD` | 对应 CommonCard timeline +0秒 |
| 艾克佐迪亚 | `SE_EV_EXODIA` | +0.3秒 |
| 通灵盘 | `SE_EV_DESTINYBOARD` | +1.216667秒 |
| 惰天使 | `SE_EV_GHOSTRICK_ANGEL` | +0.083333秒 |
| 相扑魂 横纲 | `SE_EV_MUSICALSUMO_DICE` | +0秒 |

特殊胜利音轨和其“结果 WIN 声”是不同层，不应把通用胜利声当作特效声。

原版的 `WinStart` 事件会在完整动画结束前调用 `OcgCore.endingAction`：艾克佐迪亚9.6秒、通灵盘8.6秒、惰天使5秒、相扑魂5.4秒、CommonCard01 3.11秒、CommonCard03 3.12秒。相应整体 Timeline 时长分别是11.4、8.916667、5.166667、5.566667、3.216667、3.233333秒。结果横幅的时机不能用音轨触发框或整体动画结束时间代替。

## 七、卡牌专属效果音

以下是 `ShowCardEffectAnimation()` 中的原始卡号（部分分支取原始别名卡号）。通常在 CHAIN_SOLVING 的卡牌特效开始播放，不是任意发动即播。原版还检查特效设置、目标、卡片模型、连锁位置等条件。

| 原始卡号 | 音效 suffix（均加 `SE_EV_`） | 特别条件 |
|---|---|---|
| 5318639 | CYCLONE | 需要目标模型 |
| 2263869 | ULTIMATE_SLAYER | 需要目标模型 |
| 12580477 | RAIGEKI | |
| 14558127 | ASH_BLOSSOM_v2 | 结算序号>1 |
| 18144506 | HARPIESFEATHER_DUSTER_3D | |
| 23002292 | REDREBOOT | |
| 24224830 | CALLED_GRAVE | |
| 24224831 | EF21233_R / EF21233_P | 本方控制用 R，否则 P；不要根据后缀字面反过来 |
| 24299458 | FORBIDDEN_DROPLET | |
| 25311006 | TRIPLETACTICS_TALENT | |
| 41420027 | SOLEMNJUDGMENT | |
| 44095762 | MIRRORFORCE | |
| 53129443 | BLACKHOLE | |
| 54693926 | DARKRULER_NOMORE | |
| 61740673 | IMPERIAL_ORDER | |
| 62279055 | MAGIC_CYLINDER | |
| 63391643 | THOUSANDKNIVES | 需要目标模型 |
| 65681983 | CROSSOUT_DESIGNATOR | |
| 72302403 | GOFUKEN | |
| 75500286 | GOLD_SARCOPHAGUS | |
| 83764718 / 83764719 | MONSTER_REBORN | |
| 63166095 / 63166096 | ENGAGE | 后续移卡开始对应效果轨+0.383333秒，不是上述直接 PlaySE 分支 |
| 19613556 | HEAVY_STORM | |
| 90448279 | AZEUS | |
| 32807846 | 039_NORMAL | |
| 32807848 | 039_SPECIAL | |
| 59438930 | 038_NORMAL | 结算序号>1、上条连锁卡在场上 |
| 84192580 | 040_NORMAL | |
| 42141493 | 041_NORMAL | |
| 87126721 | 042_NORMAL | |
| 40366667 | 044_NORMAL | 在后续无效回调中，不是特效分支立即播放 |
| 97045737 | 043_NORMAL | 同上 |
| 94145021 | EF09279_v1 | |
| 49238329 | EF14144_v2 | |
| 6498707 | EF22188 | |
| 35269905 | EF22189 | |
| 82732705 | SKILLDRAIN | 需要本卡模型 |
| 10045474 | INFINITE_IMPERMANENCE | 需要目标模型 |
| 14532163 | LIGHTNINGSTORM_P / LIGHTNINGSTORM_R | 本方控制用 P，否则 R |
| 97268402 | EFFECT_VEILER | 需要目标模型 |
| 73642296 | GHOSTBELLE | 连锁序号≥2；后续 nextNegateAction 回调 |
| 73642297 | GHOSTBELLE + 037_SPECIAL | 上项异画额外特效 |
| 58297729 | TRANSMISSION_GEAR | 资源中有 Win/Draw/Lose 轨；t0通用声，胜/负轨+0.75秒另播 TRANSMISSION_GEAR_WIN/LOSE |

58297729 的轨存在于 Card 资源，但当前 `ShowCardEffectAnimation` 没找到启用它的代码分支；不能把“资源存在”误写成“每次该卡结算都会播放”。本文专门保留这一差异。

## 八、决斗交互/UI

决斗过程中可出现局外共用的 SE_SYS/SE_MENU 音效，不应只打包 SE_DUEL 目录。

| 操作 | 音效 |
|---|---|
| 卡片/区域/墓地交互选中 | `SE_DUEL_SELECT` |
| 确认决斗操作 | `SE_DUEL_DECIDE` |
| 取消决斗操作 | `SE_DUEL_CANCEL` |
| 阶段选择窗开/关 | `SE_PHASE_WINDOW_OPEN` / `SE_PHASE_WINDOW_CLOSE` |
| 决斗选择卡窗滑入/滑出 | `SE_MENU_SLIDE_03` / `SE_MENU_SLIDE_04` |
| 是/否或确认提示弹出 | `SE_SYS_VERIFY` |
| 日志开/关 | `SE_LOG_OPEN` / `SE_LOG_CLOSE` |
| 大图展示/放大/缩小 | `SE_CARDEXPAND_DISPLAY` / `SE_CARDEXPAND_ZOOMIN` / `SE_CARDEXPAND_ZOOMOUT` |
| 通用卡信息/全屏面板 | `SE_DECK_WINDOW_OPEN` / `SE_MENU_CANCEL` |
| 侧边面板滑入/滑出 | `SE_MENU_SLIDE_01` / `SE_MENU_SLIDE_02` |
| 通用按钮/标签悬停、确认、取消 | prefab 的 `SoundLabelClick` / `SoundLabelClickInactive` / `SoundLabelPointerEnter` / `SoundLabelSelectedGamePad` |
| 通用开关开/关 | prefab 的 `SoundLabelClickOn/Off`；常用 `SE_MENU_S_DECIDE_01/02` |

prefab 还引用 `SE_MENU_SELECT_01/02`、`SE_MENU_OVERLAP_02`、`SE_MENU_DECIDE`、`SE_DECK_CARD_SELECT` 等；不同 UI 元素字段不相同，逐对象清单在 extended JSON，不能把所有悬停都映射成“决斗确认”。

## 九、场地、伙伴和可选角色语音

### 场地

- 点击本/对方场地：尝试 `SE_FIELD_MAT<三位ID>_TAP`，再尝试 `_TAP_P`（near）或 `_TAP_R`（far）。不是两个 label 都一定存在。
- LP 首次跨越初始上限的 75%、50%、25% 时：`SE_FIELD_MAT<ID>_PHASE1_P`、`PHASE2_P`、`PHASE3_P`。当前 C# 两个场地均构造 P，不能擅自把远侧改成 R 后声称是原逻辑。
- 败北的最终破坏：`SE_FIELD_MAT<ID>_PHASE4_P/R`。
- `BgEffectManagerInner.additionalSeList` 会按 `animationName/time` 加播特定声音（音量0.6）。例如 Mat013/Mat045 far 的 OP_01/OP_02、Mat036 的 LAST_P/R；Mat038 还有 TAP_SHARK 专属点击声。
- 这些不是“背景音乐”，需要对应场地/破坏动画状态；当前模组若没有该场地系统，单纯将它们乱接到每次 LP 减少并不等价。

### 伙伴

- Master Duel 伙伴依动画事件调用 `PlayAnimationEventSe` 或 `NewEvent`，音量0.4；覆盖 Entry、Tap、Attack、Damage、Victory、Defeat、Random、形态变化等。
- extended JSON 记录每一事件的 bundle、clip、time、function、sound；不要靠 `_c/_d/_e` 字母猜固定含义，不同伙伴的事件表不同。
- I:P（1003005）和 S:P（1003105）有事件缺失时的语音候选后备，等待两帧；若 EventSEPlayer 已收到同前缀事件则不再重复。
- 特定变身还可播 `SE_LAUNDRY_CHANGE/RETURN`、`SE_M13671_j` 等组件字段指定的声音。
- Cross Duel 伙伴不是上述事件机制，而是 `EffectSeLabelData`：按效果名匹配最多三个 label 和 start，统一转大写后按延迟播放。该表也必须审计，不能只搜索 C# 字面量。

### 角色台词

`VoiceMessage` 是另一个可选系统，默认关闭。实际语音路径为 `Sound/Voice/<语音名的前五字符>/<语音名>_<分段序号>.ogg`，源码另定义 `Sound/CustomVoice/`。角色语音词典位于 `Data/locales/ja-JP/voice/V<角色原始ID>.json`，本地化台词位于 `Data/locales/<语言>/voice/SN<角色原始ID>.json`。其触发覆盖 Start、NewTurn、NewPhase、Win、PosChange、Move（召唤、解放等）、Chaining、Draw、Damage、PayLpCost、Recover、LpUpdate、Attack、BecomeTarget、UpdateData；具体台词按角色词典/卡号选择。证据：`MDPro3.Duel/VoicePlayer.cs:28-88`、`MDPro3.Duel/VoiceMessage.cs`。

角色语音不是固定的 SE 标签集合，也不是与伙伴叫声同一类。当前安装包或用户未提供角色语音包时，不能报告“所有角色台词已压缩并接入”。BGM 同理应单独统计，不隐含在音效压缩量里。

## 十、BGM 与播放公共规则

虽然本次用户重点是音效，原版决斗声音审计仍需区分 BGM：

- 进入决斗场地按本/对方场地设置选主题 Normal；小概率使用 Rate 主题。
- 王牌 cutin 触发 KeyCard；LP 低于25%或进入可能斩杀的战斗阶段触发 Climax。
- 三段主题 Normal→KeyCard→Climax；部分主题只有一/两段，用当前列表中相应最后一段。
- 原 AudioManager 内有主题循环起止点，不是所有音乐从0循环；BGM若移植需要保留循环表。

所有 `AudioManager.PlaySE` 共用以下规则：空 label 不播；同一 label 在0.1秒内抑制重复；`nextMuteSE` 可抑制指定下一声；请求 `_oneshot` 不存在时尝试去掉后缀；否则缺资源静默失败。音轨 `SoundPlayableBehaviour.OnBehaviourPlay` 直接调 PlaySE。字段里的 `mute` 与 track muted 需分别记录，不能把音频原始时长当作轨的下一事件时间。

## 移植与验证边界

本文件记录原版规则，不代表每个分支已在当前模组实现。资源压缩、模组触发覆盖、场地/伙伴/角色功能覆盖要各自验收。当前模组已有的决斗事件应按事件队列和动画起止时间挂声：

1. 非局部交互事件对同一场地的双方各播放一次，各自按本地视角选择 PLAYER/RIVAL、OPP。
2. UI悬停/点击只在本地播放；不得广播导致双方重复或把隐私选卡操作广播。
3. 起手、命中、落地、连锁、LP数字变化分别跟随相应动画，不应一次性提前叠放。
4. 重连/初始快照不回放历史音效；轮询相同状态不重复触发；取消/退出/换场地清理待播队列。
5. 资源逐文件检查 Ogg/Vorbis 头、时长、解码成功、注册 label 与输出路径闭合；最终 jar 不含 WAV。
