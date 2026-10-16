# 决斗盘实时状态（2026-10-13）

## 目标
让决斗盘不只是模型：把无头决斗主机（.NET + ocgcore）的消息流接进场地实体，
卡组剩余、墓地、除外堆随对局实时变化。

## 引擎消息解析
决斗主机把每条引擎消息按 `{type:"message", function, name, hex}` 发出来，
`hex` 是原始消息（含 1 字节 function）。这一轮在 `DuelHostProcess` 里解析两类：

- **Draw（function 90）**：`player(1) count(1) cards(4*count)`
  → 抽卡数累加到该玩家；用「初始张数 − 抽卡数」作为卡组剩余。
- **Move（function 50，长度 17）**：`code(4) from(controller,location,sequence,position) to(同) reason(4)`
  → `location` 位：`0x02` 手牌、`0x04` 怪兽区、`0x10` 墓地、`0x20` 除外、`0x40` 额外卡组。
  进墓地 / 除外 / 额外分别累加，离开这些区域再减回去（例如死者苏生把卡从墓地拉回来）。
  消息布局取自 MDPro3 反编译源码 `MDPro3.Duel.DuelMessage.GameMessage_Move` 与
  `MDPro3.BinaryExtensions.ReadGPS`，长度 17 与实测抓包完全一致。

## 场地侧
- `DuelFieldEntity` 新增同步字段：每侧「卡组剩余 / 墓地 / 除外」，
  并且只在数值变化时才 set（不会每 tick 刷同步包）。
- `DuelSessionManager` 记住「服务器 + 决斗盘 UUID + 维度」，服务器每 tick
  （`MDProMod.onServerTick`）把主机统计写进那个决斗盘；实体不在加载区块里时自动跳过。
- 渲染：主卡组堆用**剩余张数**（每 10 张一层、最多 6 层），墓地 / 除外堆在各自区域
  按每 5 张一层、最多 6 层显示；没有卡组时这些堆都不会出现。

## 命令
- `/mdpromod duel start`：开对局时自动绑定 32 格内最近的决斗盘，并提示已绑定。
- `/mdpromod duel stats`：打印双方 draw / grave / banish / extra 计数。
- `/mdpromod field info`：现在是「剩余 / 初始，墓地 N，除外 N」。
- `/mdpromod field bind`：把背包里的卡盒绑到最近的决斗盘（不依赖瞄准）。

## 验证
- `smoke\HostStatsSmoke.java`：直接起真决斗主机跑完整自动对局，每秒打印统计。
  实测一局结束后 `p1[draw=40 grave=34 banish=0 extra=0] p2[draw=40 grave=34 …]`
  （40 张卡组全部抽完、34 张进墓地，符合自动对局的结果），
  说明 Draw / Move 解析链路可用。日志 `mod\smoke\hoststats.log`。
- `gradlew.bat build --offline` 通过，产物 `mdpromod-0.1.0.jar`
  （2,070,364 字节，SHA256 `CACEAD1645412437EFD81186928177E0F35C652830D62DB0B81CC6F67E267EE1`），
  已复制到 `D:\光明MOD服1.12客户端\.minecraft\versions\1.20.1-Forge_47.4.20\mods\`。
- 服务端加载：`mod\server-test8.out.log`
  `Loaded 15019 card definitions` / `Loaded 32 duel field zones` / `Done (9.606s)!`。

## 下一步
- 用同一套消息解析把 MSG_SUMMONING / MSG_SPSUMMONING / MSG_SET / MSG_POS_CHANGE
  映射成场上卡片实体（现在场上卡还是手动放的开发功能）。
- 墓地 / 除外堆显示具体张数文字（现在只用厚度表达）。
- 手牌映射到玩家快捷栏（Draw → 快捷栏加卡，用牌 → 扣掉）。
- 阶段条按钮接管回合阶段（等引擎侧 phase 授权）。
