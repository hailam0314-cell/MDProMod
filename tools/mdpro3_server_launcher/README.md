# MDPro3 ygoserver / WindBot 测试工具

此目录只包含启动器源码，不会修改 `D:\MyCardLibrary\mdpro3`。默认从 D 盘 staging 镜像运行。服务器的 `ygoserver.dll` 需要和 `ocgcore.dll`、`sqlite3.dll`、`Data\script.zip`、卡库等放在同一运行目录；目录结构可直接参考 `D:\MDProMod\tools\ygo_tcp_probe\staging`。

## 启动 ygoserver

```powershell
& 'D:\MDProMod\tools\dotnet\dotnet.exe' `
  'D:\MDProMod\tools\mdpro3_server_launcher\bin\Release\net8.0\mdpro3_server_launcher.dll' `
  --root 'D:\MDProMod\tools\ygo_tcp_probe\staging' `
  --server-args '17911 -1 5 0 F F F 8000 5 1 180 0'
```

参数含义与 MDPro3 源码 `gframe.cpp` 相同：

```text
端口 lflist规则 卡池模式 大师规则 检查卡组 不洗牌 LP 起始手牌 抽卡 时间限制 replay模式
```

常用单人参数是：`<port> -1 5 0 F F F 8000 5 1 180 0`。安装包内的 SoloSelector 就是按这个格式生成参数的。

## WindBot 接入

安装包没有单独的 `WindBot.exe`。WindBot 编译在 `MDPro3_Data\Managed\Assembly-CSharp.dll` 内，Unity 客户端通过 `WindBot.Program.Main` 启动。WindBot 使用标准 YGOPro TCP 客户端连接服务器：

```text
ExternalAddress(23) -> PlayerInfo(16) -> JoinGame(18) -> UpdateDeck(2)
```

可复用的启动参数键值包括：

```text
Name=WindBot
Deck=MokeyMokey
DeckFile=...          # 自选 .ydk 时使用
Dialog=Mokey.zh-CN
Host=127.0.0.1
Port=17911
HostInfo=
Version=4962
Hand=0
Chat=False
Debug=False
```

`Data\locales\zh-CN\bot.conf` 中的命令行就是可用配置示例，牌组位于 `Data\Windbot\Decks\AI_*.ydk`。

要把 WindBot 作为真正的第二个 TCP 客户端启动，需要先把 `Assembly-CSharp.dll` 中的 WindBot 及其依赖拆出为独立 .NET 进程，或在模组服务器中实现等价的客户端。当前已验证的 server launcher 不假装包含 WindBot；直接执行 `Assembly-CSharp.dll` 会加载 Unity/Mono 依赖，不能当作独立 .NET 程序运行。

## 已验证

- `ygoserver.dll` 可从独立 .NET 8 进程加载；在 staging 目录以参数 `17917 -1 5 0 F F F 8000 5 1 180 0` 启动成功。
- TCP `127.0.0.1:17917` 能连接。
- 服务器参数和 WindBot 默认端口 `7911` 与源码一致。

源码文件：`Program.cs`、`mdpro3_server_launcher.csproj`。
