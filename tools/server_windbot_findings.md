# MDPro3 Server/WindBot 检查记录

- 最新安装包包含 `D:\MyCardLibrary\mdpro3\MDPro3_Data\Plugins\x86_64\ygoserver.dll`（导出 `start_server` / `stop_server`）以及完整的 `Data\Windbot\Decks` 与本地化 `bot.conf`。
- 安装包没有独立 WindBot.exe；WindBot 的 `WindBot.Program`、`WindBot.Game`、`YGOSharp.Network` 和 AI 执行器都编译在 `MDPro3_Data\Managed\Assembly-CSharp.dll` 中，由 Unity 直接调用 `WindBot.Program.Main(args)`。
- `SoloSelector` 的参数格式：`<port> -1 5 0 F <noCheck> <noShuffle> <LP> <startHand> <drawCount> 0 0`。
- WindBot 连接标准 YGOPro TCP：`ExternalAddress(23)`、`PlayerInfo(16)`、`JoinGame(18)`，收到 `STOC_JOIN_GAME` 后发送 `UpdateDeck(2)`。
- 新增独立服务器启动器：`D:\MDProMod\tools\mdpro3_server_launcher`。它从指定 D 盘 staging 目录加载 ygoserver，不修改安装目录。
- 启动器验证：在 `D:\MDProMod\tools\ygo_tcp_probe\staging` 用 `17917 -1 5 0 F F F 8000 5 1 180 0` 启动，TCP `127.0.0.1:17917` 可连接。
- WindBot 独立拆包尚未完成；当前不要把启动器描述成“可直接启动 AI”。下一步应复用安装包的 `WindBot` 代码/依赖，或实现等价 TCP AI 客户端。
