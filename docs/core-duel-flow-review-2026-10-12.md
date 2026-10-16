# MDProMod 核心决斗流程验收报告

日期：2026-10-12

## 结论

核心规则流程已经跑通到“完整单局可以打到 `MSG_WIN`”。

| 层级 | 状态 | 说明 |
| --- | --- | --- |
| 原生规则引擎 | 通过 | 15,019 张卡、13,575 个脚本，headless 完整单局胜利 |
| Duel Host 手动协议 | 通过到首个决策点 | 成功加载并推进到 `SelectIdleCmd`，等待响应 |
| Duel Host 自动冒烟 | 通过完整单局 | `MSG_WIN`，winner=0，无错误 |
| Forge 进程层 | 编译通过 | 支持 auto 冒烟、状态跟踪、状态查询 |
| Minecraft 手动操作 | 尚未接入 | 需要选择消息解析、UI、响应回传 |
| 卡牌可视化 | 尚未接入 | Blockbench 模型和贴图已就绪，但还没有实体渲染 |

## 验收环境

- 目标：Minecraft 1.20.1 / Forge 47.4.20
- Forge 模组：`D:\MDProMod\mod\build\libs\mdpromod-0.1.0.jar`
- Duel Host 运行时：`D:\MDProMod\tools\ygo_tcp_probe\staging`
- `cards.cdb`：15,019 张卡
- `script.zip`：13,575 个 Lua 脚本
- `ocgcore.dll`、`sqlite3.dll` 均存在

## 阶段 1：原生引擎 headless 完整单局

命令：

```powershell
$env:DOTNET_CLI_HOME='D:\MDProMod\cache\dotnet-headless'
& D:\MDProMod\tools\dotnet\dotnet.exe D:\MDProMod\tools\headless_smoke\bin\Release\net8.0\headless_smoke.dll
```

结果：

| 项目 | 数值 |
| --- | ---: |
| cardCount | 15,019 |
| scriptCount | 13,575 |
| deckCount | 40 |
| loops | 1,465 |
| ended | true |
| responses | 264 |
| retries | 0 |
| MSG_WIN | 1 |

证据：

- `docs/headless-smoke-rerun-2026-10-12.json`
- `docs/headless-smoke-rerun-summary-2026-10-12.json`

结论：原生规则引擎、卡库、脚本、消息循环和自动应答可以完整推进到胜利条件。

## 阶段 2：Duel Host 手动模式

命令：

```powershell
$env:DOTNET_CLI_HOME='D:\MDProMod\cache\dotnet-headless'
@'
{"cmd":"start","deck0":"starter.ydk","deck1":"starter.ydk"}
{"cmd":"stop"}
'@ | & D:\MDProMod\tools\dotnet\dotnet.exe D:\MDProMod\tools\duel_host\bin\Release\net8.0\duel_host.dll --root D:\MDProMod\tools\ygo_tcp_probe\staging
```

观察到的消息：

```text
ready
Draw x2
NewTurn
NewPhase x3
SelectIdleCmd
state waiting=true function=11
```

证据：

- `docs/duel-host-manual-probe-2026-10-12.json`
- `tmp/duel_host_probe/stdout.txt`

结论：Duel Host 能正确加载 SQLite、卡库、脚本并推进到第一个玩家决策点。它不会自动回答，必须等待外部发送 `response` 或 `responseBytes`。

## 阶段 3：Duel Host 自动冒烟模式

命令：

```powershell
$env:DOTNET_CLI_HOME='D:\MDProMod\cache\dotnet-headless'
@'
{"cmd":"start","auto":true,"deck0":"starter.ydk","deck1":"starter.ydk"}
{"cmd":"stop"}
'@ | & D:\MDProMod\tools\dotnet\dotnet.exe D:\MDProMod\tools\duel_host\bin\Release\net8.0\duel_host.dll --root D:\MDProMod\tools\ygo_tcp_probe\staging
```

结果：

| 项目 | 数值 |
| --- | ---: |
| 输出事件行 | 2,179 |
| message 事件 | 1,547 |
| state 事件 | 630 |
| errors | 0 |
| MSG_WIN | function 5，winner=0 |
| ended | 1 |

证据：

- `docs/duel-host-auto-smoke-2026-10-12.json`
- `tmp/duel_host_auto/stdout.jsonl`

自动模式使用确定性策略回答选择消息，不是竞技 AI。它的作用是证明 JSON 协议、卡库、脚本和规则核心可以完整跑完一局。

## 阶段 4：Forge 编译与游戏接入

已完成：

- `DuelHostProcess` 支持 `auto` 参数。
- `DuelHostProcess` 解析 `ready`、`state`、`message`、`error`、`ended`。
- 跟踪最后功能号、功能名、winner 和错误信息。
- `DuelSessionManager` 支持自动/手动模式。
- 增加 `/mdpromod duel smoke` 自动冒烟命令。
- `/mdpromod duel status` 输出完整状态。
- `gradlew build` 成功。
- jar 已复制到：

```text
D:\光明MOD服1.12客户端\.minecraft\versions\1.20.1-Forge_47.4.20\mods\mdpromod-0.1.0.jar
```

## 阶段 5：命令级手动 prompt/respond 闭环

Duel Host 现在会在手动模式下输出结构化 `prompt` 事件，包含功能号、名称、摘要、可选选项、卡片列表、区域列表、`min`/`max`/`cancelable` 和 `responseKind`。

Forge 侧新增命令：

```text
/mdpromod duel prompt
/mdpromod duel respond <value>
/mdpromod duel respondbytes <hex>
```

手动策略探针使用同一套 prompt 现场选择响应：

```text
python D:\MDProMod\tools\duel_host\manual_policy_probe.py
```

结果：

| 项目 | 数值 |
| --- | ---: |
| prompt 数量 | 350 |
| 事件数量 | 2,039 |
| 解析失败 | 0 |
| errors | 0 |
| MSG_WIN | function 5，winner=0 |
| ended | true |

证据：

- `docs/manual-policy-probe-2026-10-12.json`

这说明“规则引擎 → 结构化提示 → 玩家响应 → response/responseBytes”的命令级闭环已经跑通。距离真正的图形界面只差把同一份 prompt 数据渲染成 GUI。

## 游戏内冒烟步骤

1. 启动 `1.20.1-Forge_47.4.20` 版本。
2. 进入一个开启作弊的单人世界，或使用有 OP 权限的服务器。
3. 执行：

```text
/mdpromod duel smoke
```

4. 等待几秒后执行：

```text
/mdpromod duel status
```

预期状态类似：

```text
MDProMod duel: alive=true running=false ready=true waiting=false ended=true last=Win(5) winner=0 error=
```

`/mdpromod duel start` 是手动模式。现在可以用 `/mdpromod duel prompt` 查看当前选择，再用 `/mdpromod duel respond` 或 `/mdpromod duel respondbytes` 推进；客户端图形界面尚未实现。

命令级手动交互示例：

```text
/mdpromod duel start
/mdpromod duel prompt
/mdpromod duel respond 7
/mdpromod duel respondbytes 0100
```

其中 `respond 7` 通常表示结束回合，`respondbytes 0100` 表示从卡片列表中选择第一张卡。具体选项以 `/mdpromod duel prompt` 的输出为准。

## 尚未完成

1. **客户端 GUI**：命令级 prompt/respond 已经可用；下一步是把 `prompt` 渲染成按钮、卡片列表和区域列表。
2. **卡牌可视化**：把 `Draw`、`Move`、`Summoning`、`Set` 等消息映射到场地实体或方块实体。
3. **场地状态同步**：怪兽区、魔陷区、额外怪兽区、墓地、除外区、卡组数量。
4. **隐藏信息过滤**：Duel Host 当前输出完整 hex，联机时必须按玩家过滤。
5. **卡组选择**：目前硬编码 `starter.ydk`，需要玩家/服务器选组。
6. **网络协议**：阶段按钮、手牌、LP、选择提示需要客户端-服务端包。
7. **卡面渲染**：完整的 `176×256` 卡图缓存已经准备好，但还没有接入卡片渲染器。
8. **复杂规则回归**：目前冒烟使用简单卡组，下一步要用融合、同步、超量、连接、灵摆和复杂连锁卡组回归。

## 下一里程碑验收标准

- 游戏内 `/mdpromod duel smoke` 能报告 `winner=0`。
- 手动模式能从游戏内回答一次 `SelectIdleCmd` 并推进到下一阶段。
- 一条 `MSG_MOVE` 能在正确区域生成或移动一张可见卡片。
- 正面朝上的卡能绑定完整的 `176×256` 卡图。
