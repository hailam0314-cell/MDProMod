# MDPro 对局中的特殊动画触发点（2026-10-13）

来源：`MDPro3-main/Assets/Scripts/MDPro3/Servants/OcgCore.cs`（消息处理器，
约 8200 行，所有演出都在这里触发）、`Duel/GameCard.cs`（卡自身的动画 API）、
`Managers/TimeLineManager.cs`（时间轴演出）、`Duel/DuelBGManager` 等。

## 一、按消息分类的触发点

| 消息 | 触发的表现 | 备注 |
| --- | --- | --- |
| `Summoning` / `SpSummoning` | `AnimationPositon`（表示形式归位）+ `AnimationLandShake`（落地震动）+ `CameraManager.ShakeCamera` | 震动分普通 / 强力两档 |
| `FlipSummoning` | `AnimationPositon` | 翻转召唤 |
| `Set` | `SE_LAND_MT_SET` + 落地 | 盖放 |
| `PosChange` | `AnimationPositon` | 攻守表示切换 |
| `Chaining` | `AnimationActivate` | 效果发动（卡会亮起/抬起） |
| `ChainSolving` | `AnimationNegate`（无效演出）+ 震屏 + **28 个名场面专属音效** | 见下「名场面音效」 |
| `ChainDisabled` | `AnimationNegate` | 效果被无效 |
| `BecomeTarget` / `RandomSelected` | `AnimationTarget` + `EffectTarget` | 被指定对象时高亮/连线 |
| `ConfirmCards` / `ConfirmDecktop` | `AnimationConfirm` / `AnimationConfirmDeckTop` | 确认手牌、确认卡组顶 |
| `ShuffleHand` / `TagSwap` | `AnimationShuffle` | 洗牌、换人 |
| `Draw`（OcgCore 内） | 抽卡音效 + 手牌归位（`AnimationHandDefault`） | 手牌入列 |
| `Attack` | `SE_DA_TEXT` + 攻击动画 | 攻击宣言 |
| `Battle` | `CameraManager.ShakeCamera` | 战斗步骤 |
| 战斗结算（`GetSpecialFinalAttackType`） | `AnimationFinalAttack_*` | **决定性一击专属演出**，见下 |
| `Damage` / `PayLpCost` / `Recover` / `LpUpdate` | `AnimationLpChange` + `UpdateBgEffect` + 震屏 | LP 变化与场地阶段 |
| `TossCoin` / `TossDice` | DOTween 序列（硬币翻转 / 骰子滚动）+ 专属音效 | 随机效果 |
| `NewTurn` | `ShowDisquiet` | 回合切换提示 |
| `ReloadField` | 场地重整特效 | |
| 指示物相关 | `AddCounter` / `RemoveCounter` / `ClearCounter` | 卡上指示物增减 |
| 其它 | `ShowLabel` / `HideLabel` / `ShowFaceDownCardOrNot` | 标签、背面卡显示 |

## 二、强力召唤（StrongSummon）专属演出

- `GameCard.NeedStrongSummon(data)` 的判定：
  连接怪兽连接数 > 2、超量阶 > 3、同步 ≥ 6 星、融合 ≥ 6 星、仪式 ≥ 6 星、其它 ≥ 7 星。
- 命中后走 `TimeLineManager` 的 `StrongSummon` 时间轴（额外镜头、光效、落地定格），
  普通召唤只走普通落地动画。资源里有独立的 Timeline/Animator，属于「重演出」。

## 三、决定性一击（Final Attack）专属演出

- 触发点在战斗结算里：`GetSpecialFinalAttackType(attackCard, attackedPosition)`
  按**卡号或 Alias** 从 `Settings.Data.FinalAttack*` 列表里匹配。
- 目前支持 6 种专属演出：`BlueEyes`（青眼）、`DarkM`（黑魔术师）、`RedEyes`（真红眼）、
  `Slifer`（欧西里斯的天空龙）、`Obelisk`（欧贝利斯克的巨神兵）、`Ra`（拉之翼神龙），
  其余走 `Normal`（无专属演出）。
- 因为列表是**数据驱动**的，我们完全可以自己定义 MC 版清单。

## 四、LP 与场地的「伤害阶段」

`UpdateBgEffect(player)` 按剩余 LP 阈值切换场地演出（两边独立）：

| 阈值 | 表现 |
| --- | --- |
| LP < 75% | 场地进入 DamagePhase 2（触发 field / grave / stand / mate 的动画与专属音效） |
| LP < 50% | DamagePhase 3 |
| LP < 25% | DamagePhase 4 + **BGM 高潮切换**（`AudioManager.PlayBgmClimax()`） |
| LP ≤ 0 | 结束演出（当前版本里相关触发被注释掉了） |

## 五、名场面音效（`ChainSolving` 里硬编码）

共 28 条专属音效，例如：羽毛扫 `SE_EV_HARPIESFEATHER_DUSTER_3D`、黑洞 `SE_EV_BLACKHOLE`、
神圣防护罩 `SE_EV_MIRRORFORCE`、神之宣告 `SE_EV_SOLEMNJUDGMENT`、灰流丽 `SE_EV_ASH_BLOSSOM_v2`、
无限泡影 `SE_EV_INFINITE_IMPERMANENCE`、墓穴指名者 `SE_EV_CALLED_GRAVE`、
闪电风暴 `SE_EV_LIGHTNINGSTORM_P/R`、雷击 `SE_EV_RAIGEKI`、红色重启 `SE_EV_REDREBOOT` 等。
另外攻击音效按怪物分档（普通 / 神之卡专属 `SE_MONSTERATTACK_RA/SLIFER/OBELISK`）。

## 六、移植到 Minecraft 的建议分级

**第一梯队（改动小、体感提升大）**
1. 召唤落地：卡牌实体落下 + 轻微扬尘粒子 + 附近玩家镜头轻震。
2. 盖放 / 表示形式切换：翻转、翻面动画（我们的卡片实体已经支持正反面）。
3. 效果发动 / 无效：卡牌亮起（发光描边）+ 音效；被无效时闪一下灰光。
4. 攻击宣言 + 战斗：卡牌向目标短暂前移 / 回弹，命中时扬尘 + 震屏。
5. 伤害与 LP：场地上方飘字（伤害数值）、受击方屏幕边缘发红；
   按 75% / 50% / 25% 三档给场地换色调（可以直接体现在我们已有的决斗盘模型上）。
6. 决定性一击：缩水版——镜头拉近、短暂停顿、粒子爆发（先不区分青眼/黑魔导，
   用同一套，之后按 `FinalAttack*` 列表做专属版）。

**第二梯队**：抽卡（卡组 → 手牌飞行的实体动画）、洗牌、确认卡（短暂展示）、
目标标记（被指定卡高亮 + 连线）、硬币/骰子（简单的 3D 或 HUD 演出）。

**第三梯队（重活）**：`StrongSummon` 时间轴演出、三幻神/青眼等专属 cutscene、
名场面专属音效（需要音效资源，MC 里很可能要换成自制音效或原版音效替代）。

## 七、我们这边的接口现状

- Java 侧已经能拿到每一条引擎消息（`{"type":"message","function":N,"name":"...","hex":"..."}`），
  并且已经在解析 `Draw` / `Move` / `NewTurn`（手牌、墓地、除外、卡组剩余）✔。
- 所以做动画只需要在客户端按**消息名**触发表现即可，服务端不用再改协议；
  召唤/攻击/伤害这类需要更多字段的消息，可以照 `DuelMessage.cs` 的解析顺序在 Java 侧补字段
  （合并后的实现会顺手把这些字段带给客户端）。
- 演出资源方面：MC 端用粒子 + 我们的卡牌/场地实体 + 原版音效即可起步，
  不需要引入 MDPro 的美术资源。
