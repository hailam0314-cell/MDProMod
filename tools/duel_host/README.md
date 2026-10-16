# MDProMod Duel Host 骨架

这是 Minecraft 模组接入前的独立进程骨架。它直接加载新版 `ocgcore.dll`，通过标准输入/输出使用 JSON Lines：

- 输入 `{"cmd":"start","deck0":"...","deck1":"..."}` 创建一局并推进核心；
- 输入 `{"cmd":"start","auto":true,"deck0":"...","deck1":"..."}` 使用内置确定性策略自动回答选择消息，跑完一局冒烟决斗；
- 输入 `{"cmd":"start","seed":305419896,...}` 可以固定随机种子，方便回放测试；
- 输入 `{"cmd":"response","value":N}` 回答整数选择；
- 输入 `{"cmd":"responseBytes","hex":"..."}` 回答字节选择；
- 输入 `{"cmd":"stop"}` 结束当前决斗。

输出的 `message` 事件包含 `GameMessage` 功能号、名称、原始 payload，`MSG_WIN` 还会带 `winner`；`state` 事件包含当前核心等待状态，自动回答时带 `"auto":true`。这个协议会作为 Forge 服务端和 Duel Host 之间的第一版桥接接口，后续再补充卡片 GPS、隐藏信息过滤和权限字段。

手动模式在等待响应时会额外输出 `prompt` 事件：

```json
{
  "type": "prompt",
  "function": 11,
  "name": "SelectIdleCmd",
  "waiting": true,
  "summary": "SelectIdleCmd: summon=0, set=0, activate=0, battle=no, end=index 7",
  "responseKind": "integer",
  "options": [
    {"index": 6, "label": "Battle Phase"},
    {"index": 7, "label": "End Phase"}
  ]
}
```

`prompt` 事件会尽量包含 `player`、`options`、`cards`、`places`、`min`、`max`、`cancelable` 等字段。`responseKind` 为 `integer` 时用 `response`，为 `bytes` 时用 `responseBytes`；`bytes_or_integer_cancel` 表示可以发送 `response:-1` 取消，也可以发送字节选择。

`auto` 模式只用于规则流程冒烟和回归测试，不是竞技 AI。手动模式仍然等待外部客户端发送 `response` / `responseBytes`。

运行时目录应包含：

- `ocgcore.dll`；
- `Data/locales/zh-CN/cards.cdb`；
- `Data/script.zip`。

建议从 `D:\MDProMod\tools\ygo_tcp_probe\staging` 运行，避免读取或修改原安装目录。

`duel_host` 会在标准输入读取 JSON Lines，并将消息写到标准输出。最小启动示例：

```powershell
$env:DOTNET_CLI_HOME='D:\MDProMod\cache\dotnet-headless'
@'
{"cmd":"start","deck0":"starter.ydk","deck1":"starter.ydk"}
{"cmd":"response","value":1}
{"cmd":"stop"}
'@ | & D:\MDProMod\tools\dotnet\dotnet.exe D:\MDProMod\tools\duel_host\bin\Release\duel_host.dll --root D:\MDProMod\tools\ygo_tcp_probe\staging
```

实际客户端应等待 `state.waiting=true` 后提交响应；示例中的固定响应只适合协议冒烟测试。`stop` 会唤醒等待中的核心线程、等待其退出，再释放 DLL 资源。

自动冒烟示例：

```json
{"cmd":"start","auto":true,"deck0":"starter.ydk","deck1":"starter.ydk"}
```

自动模式会一直推进到 `MSG_WIN` 或 `MSG_DUEL_WINNER`，然后输出 `{"type":"ended"}`。

对局动画协议验证使用固定种子的双 WindBot 对局，必须从 staging 目录执行：

```powershell
& D:\MDProMod\tools\dotnet\dotnet.exe D:\MDProMod\tools\duel_host\bin\Release\net8.0\duel_host.dll --root D:\MDProMod\tools\ygo_tcp_probe\staging --smoke-ai Data/Windbot/Decks/AI_BlueEyes.ydk --smoke-seed 4962
```

`--smoke-ai-deck0` / `--smoke-ai-name0` 可以覆盖 0 号座位卡组，默认 `AI_Albaz`。核心和 AI 都使用 `--smoke-seed`（默认 305419896）。卡组文件标识会映射到对应 WindBot 策略，例如 `AI_BlueEyes` → `Blue-Eyes`，避免选中不相干的随机策略。

`Native.Process` 的合包现在逐条拆分输出与喂给 AI，包含当前 4962 的 `ConfirmCards` 额外可见性字节和每张连锁候选的 forced 字段。未知或截断格式会报告错误并停止这局，避免用猜测的字节边界污染状态。

选择消息仅交给被询问的 AI。每个选择前输出服务端私有的 `UpdateData(6)` 查询（双方手牌、怪兽区、魔陷区、墓地、除外、额外卡组）：`function / player / location / [recordLength32 / queryFlags32 / queryFields...]`。每个记录长度包含自身 4 字节；空记录长度为 4。全量查询包含卡号和位置，手牌位置的表侧位可表示持续公开与撤销。Minecraft 服务端必须按观看者过滤隐藏身份后才下发，查询本身不触发移动动画。
