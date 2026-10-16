# ygo_tcp_probe

这是一个只负责线协议的最小 MDPro3/YGOPro TCP 客户端，用来验证决斗房间和后续 Minecraft 桥接层的网络实现。它不会渲染卡片，也不会实现 AI。

## 构建

项目使用 D 盘上的 .NET SDK，避免占用系统盘：

```powershell
$env:DOTNET_ROOT = 'D:\MDProMod\tools\dotnet'
$env:PATH = 'D:\MDProMod\tools\dotnet;' + $env:PATH
dotnet build D:\MDProMod\tools\ygo_tcp_probe\ygo_tcp_probe.csproj -c Release -o D:\MDProMod\tools\ygo_tcp_probe\bin
```

## 运行

进程的工作目录需要能加载 `ygoserver.dll`，并且 ygoserver 的相对路径资源需要存在。建议使用单独的 staging 目录，复制最新版安装包中的：

- `MDPro3_Data\Plugins\x86_64\ygoserver.dll`
- `MDPro3_Data\Plugins\x86_64\sqlite3.dll`
- `Data\locales\zh-CN\cards.cdb` 到 staging 根目录
- `Data\lflist.conf`
- `Data\script.zip` 和 `Data\locales\zh-CN\*`

启动一个本地房间并自动连接：

```powershell
$env:DOTNET_ROOT = 'D:\MDProMod\tools\dotnet'
$env:PATH = 'D:\MDProMod\tools\dotnet;' + $env:PATH
dotnet D:\MDProMod\tools\ygo_tcp_probe\bin\ygo_tcp_probe.dll `
  --deck D:\MyCardLibrary\mdpro3\Deck\A-Starter-Deck.ydk `
  --port 17911 `
  --server-args "17911 -1 5 0 F F F 8000 5 1 180 0" `
  --ready --start --timeout 15000 --log D:\MDProMod\tools\ygo_tcp_probe\probe.log
```

参数含义：

- `--deck`：读取 `.ydk`，发送 `CTOS_UPDATE_DECK`。
- `--server-args`：调用最新版 `ygoserver.dll` 的 `start_server` 导出函数；省略时连接已经存在的房间。
- `--ready`：收到 `STOC_TYPE_CHANGE` 后发送 `CTOS_HS_READY`。
- `--start`：房主看到座位准备事件时发送 `CTOS_HS_START`。
- `--auto`：自动回答猜拳、先后手和时间确认；收到第一条 `STOC_GAME_MSG` 后认输，用于快速验证结束链。
- `--log`：同时将每个 CTOS/STOC 包的函数号、长度和前 64 字节写入文件。

## 已验证链路

2026-10-12 使用最新版安装包的 DLL、卡库和脚本测试了两个 probe 进程：

```text
CTOS_EXTERNAL_ADDRESS (23)
CTOS_PLAYER_INFO      (16)
CTOS_JOIN_GAME        (18)
STOC_JOIN_GAME        (18)
CTOS_UPDATE_DECK     (2)
STOC_TYPE_CHANGE      (19)
CTOS_HS_READY         (34)
CTOS_HS_START         (37)
STOC_DUEL_START       (21)
STOC_GAME_MSG         (1)
STOC_DUEL_END         (22)
```

测试端口为 `17914`，主卡组均为 40 张、额外卡组 15 张；握手、卡组提交、准备、决斗开始和结束消息均被正确解析。原始测试输出保存在 `D:\MDProMod\tools\ygo_tcp_probe\p1.out` 和 `p2.out`。

## 协议注意事项

- 包头为 little-endian `uint16`，长度包含 function byte，不包含包头本身。
- Unicode 字段是固定宽度 UTF-16LE；MDPro3 客户端的固定字段会用 `0xCC` 填充剩余空间。
- `UpdateDeck` 的两个计数依次是 `main + extra` 和 `side`，随后依次写入主卡组、额外卡组和副卡组的 32 位卡号。
- 该 probe 只验证房间/决斗生命周期；要完成真正的单人 AI，需要再启动 WindBot，或在后续桥接层实现全部 `STOC_GAME_MSG` 响应。
