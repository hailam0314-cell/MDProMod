# 包内资源与首次解包（2026-10-16）

目标：**玩家不需要再装 MDPro3、也不用手工摆资源目录**。卡图、立绘、召唤特效、决斗宿主和它的
运行时全部塞进模组 jar，第一次启动时在后台解到 `<游戏目录>/mdpromod/`，之后直接用那份。

## 一、包里现在有什么

| 解包目录 | 内容 | 数量 | 大小 |
| --- | --- | --- | --- |
| `art/` | 卡图（176×256 jpg） | 15,019 | 203.7 MB |
| `closeup/` | 怪兽立绘（高 128 png） | 11,091 | 77.4 MB |
| `summonfx/` | 召唤特效贴图 | 13 | 0.2 MB |
| `summonmesh/` | 召唤 3D 网格（顶点/UV/索引 json） | 10 | 0.4 MB |
| `runtime/` | 宿主工作目录：`cards.cdb` + `cards.json` + `lflist.conf` + `script.zip`（13.3 MB，13,577 个 Lua 脚本）+ `ocgcore.dll` / `ygoserver.dll` / `sqlite3.dll` + `Data/locales/zh-CN/*` + `Data/Windbot/{Decks,Dialogs}` | 225 | 42.3 MB |
| `duelhost/` | 宿主 exe/dll + **它自己的 .NET 运行时**（`host/fxr` + `shared/Microsoft.NETCore.App`） | 190 | 72.0 MB |

合计 **26,548 个文件 / 396 MB**（解包后）。jar 本身 **566.8 MB**。

## 二、解包器（`ModBundle`）

- 打包时 `stageBundle` 任务把上面这些东西按目录摆好，并生成 `data/mdpromod/bundle/manifest.txt`：
  每行 `相对路径 <TAB> 字节数`，共 26,548 行（清单与 jar 内容逐条核对过，零差异）。
- 启动时（`MDProMod` 构造函数）在后台线程按清单逐条解：**目标已存在且字节数一致就跳过**，
  否则从 jar 流式写出（先写 `.part` 再 move，避免半截文件被读到）。
  于是它是幂等的：升级模组只补差异、中断了再启动会接着补、文件被误删会自动补回来。
- **客户端解全套；专用服务器只解 `runtime/` + `duelhost/`**（卡图立绘特效是客户端才用的），
  省 280 MB。
- 解包期间 `DuelHostProcess.start` 会拒绝开局（聊天栏报"资源还在解包（x/y 个文件）"），
  `ClientBundleNotice` 在 action bar 上滚动显示进度、完成/失败在聊天栏各报一次。
- 离线自检：`java -Dfile.encoding=UTF-8 -cp mod\build\libs\mdpromod-0.1.0.jar net.mdpromod.ModBundleSelfTest [目录]`
  → 14 条断言（清单全覆盖、字节数一致、关键文件存在、第二次全跳过、删掉的能补回）。

实测：首次解包 **25 秒**（26,548 个文件 / 396 MB），第二次 **2 秒**（全部跳过）。

## 三、路径怎么选（`ModPaths`）

优先级：**`-D` 覆盖 > 解包目录 > 旧的开发目录**。

| 资源 | 系统属性 | 解包目录 | 旧默认 |
| --- | --- | --- | --- |
| 卡图 | `mdpromod.cardTextureRoot` | `<游戏目录>/mdpromod/art` | `D:\MDProMod\runtime\card_textures_176x256_q80` |
| 立绘 | `mdpromod.closeupRoot` | `.../closeup` | `D:\MDProMod\runtime\closeup_128` |
| 召唤特效 / 网格 | `mdpromod.summonFxRoot` / `mdpromod.summonMeshRoot` | `.../summonfx` / `.../summonmesh` | `D:\MDProMod\runtime\summon_*` |
| 宿主工作目录 | `mdpromod.runtimeRoot` | `.../runtime` | `D:\MDProMod\tools\ygo_tcp_probe\staging` |
| 宿主 exe | `mdpromod.duelHost` / `mdpromod.dotnet` | `.../duelhost/duel_host.exe` | 旧 dll + 系统 dotnet |
| 玩家卡组 | `mdpromod.configDir` | `config/mdpromod/decks` | 以前是 `D:\MDProMod\runtime\decks` |

**踩过的坑（都会让"解包完了还是没图"）**：

- `CardTextureCache` / `CloseupTextureCache` / `SummonFxTextures` / `SummonMeshes` 都有"缺文件记一次
  不再查"的缓存。解包还没跑完时若把"暂时没有"记成"这张卡没有"，解包完成也不会再去看——
  所以四处都加了 `if (ModBundle.ready())` 才记账。**以后再加类似的加载器要照做。**
- 玩家卡组目录换成 `config/mdpromod/decks`；第一次启动会把旧目录（`D:\MDProMod\runtime\decks`、
  staging 根）里的 `*.ydk` 拷一份过去，不会"卡组不见了"。

## 四、宿主为什么这么打包

**用「框架依赖的宿主 + 同目录共享运行时」，不要 PublishSingleFile，也不要自包含发布。**

- `PublishSingleFile` / `--self-contained` 出来的 exe，一跑到 WindBot 初始化就
  `System.TypeLoadException: Method 'get_Database' in type 'Mono.Data.Sqlite.SqliteConnection' does not have an implementation`
  ——`Mono.Data.Sqlite.dll` 是 MDPro3 那边的 .NET Framework 程序集，合并/自包含发布会换掉
  `System.Data.Common` 的解析路径，它就直接崩（实测三种发布方式，只有框架依赖那套能跑）。
- 所以 `duelhost/` 里放的是：`bin/Release/net8.0` 的 5 个文件（把 `win-x64/` 子目录排除掉，
  那是 `-r win-x64` 构建留下的自包含产物）+ `tools/dotnet/host/fxr/8.0.31` +
  `tools/dotnet/shared/Microsoft.NETCore.App/8.0.31`（70 MB，不是整个 714 MB 的 dotnet 目录）。
- apphost **不会**自己找同目录的 `host/fxr`，必须给 `DOTNET_ROOT`；这件事由模组做：
  `DuelHostProcess` 起进程时把 `DOTNET_ROOT` 设成 `dotnet` 那个路径的父目录
  （自带运行时 = `duelhost/`，系统 dotnet = 安装根，两种都对）。
- `mdpromod.duelHost` 和 `mdpromod.dotnet` 都指向 `duel_host.exe`，命令行于是变成
  `duel_host.exe duel_host.exe --root <runtime>`——宿主的参数解析只认 `--root`，
  多一个位置参数无副作用（实测跑完整局）。
- **`runtime/cards.json` 不能省**：WindBot 初始化时读它当卡名表，缺了会回退到 sqlite 那条路，
  就是上面那个 TypeLoadException。这条是端到端跑出来才发现的。

## 五、怎么重建

```powershell
# 1) 宿主（框架依赖，别加 -r / --self-contained / PublishSingleFile）
D:\MDProMod\tools\dotnet\dotnet.exe publish D:\MDProMod\tools\duel_host\duel_host.csproj -c Release

# 2) 打包（stageBundle 会把资源同步到 build/bundle 再进 jar；换机器用 -Pmdpromod.bundleRoot=）
cd D:\MDProMod\mod; .\gradlew.bat build
```

`build/bundle` 会临时占 ~397 MB；jar 566.8 MB（`3138B909…`，2026-10-16 00:14）。

## 六、验证记录

- `ModBundleSelfTest` 14 断言（对着正式 jar 跑，见上）。
- 端到端：从 jar 解出来的那份运行时直接跑完整人机对局 ——
  `duelhost/duel_host.exe duelhost/duel_host.exe --smoke-ai Data/Windbot/Decks/AI_Albaz.ydk --root <解包出来的 runtime>`，
  `DOTNET_ROOT=<解包出来的 duelhost>`：退出码 0、3,407 条对局消息、`MSG_WIN`、stderr 空。
  说明 ocgcore + Lua 脚本 + 卡库 + WindBot 卡组 + 人机对话全部能从解包目录工作。
- 回归：13 套离线测试全过（AiDeckCatalog 177、Banlist 64、DuelVisualStateAudio 526、
  DuelVisualState 71、DuelAudioRules 117、DuelCardMarks 23、DuelTimingMode 24、DuelTiming 21、
  ExtraDeckTracker 17、DuelLocation 12、PileOrder 12、DuelBgmRules、DuelAudioPerspective）。

### 实机验证（2026-10-16 00:20，客户端「光明MOD服1.12」）

- 解包真的跑了：日志 `[mdpromod-unpack] 开始解包资源：26548 个文件 / 396 MB → …`，
  **35 秒**后 `资源解包完成：复制 26548 个、跳过 0 个`。
- 落点是**版本目录**而不是 `.minecraft` 根：这个整合包开了版本隔离，游戏目录 =
  `.minecraft\versions\1.20.1-Forge_47.4.20\`，所以解包到 `…\1.20.1-Forge_47.4.20\mdpromod\`
  （26,548 个文件 / 396 MB，各子目录数量与自检完全一致）。
- 宿主确实用的是解包那份：日志 `Duel Host ready: {"type":"ready","root":"D:\\…\\.minecraft\\versions\\1.20.1-Forge_47.4.20\\mdpromod\\runtime"}`。
- 上边的"玩家看到的是准备完成而不是进度"是正常的：解包在标题界面那段就跑完了。
- AI 卡名表生效：`Loaded 15019 card definitions from cards.json`（就是解包出的 `runtime/cards.json`）。
- 卡图正常（用户确认）；一局人机对局正常推进到 `SelectIdleCmd`，随后关世界产生的
  `message handling failed for SelectIdleCmd: The operation was canceled` 是收尾噪音（宿主被关掉时正好在等应答）。
- 顺带修掉：**卡组导出还写死在 `D:\MDProMod\runtime\decks`**（`DuelSessionManager`、
  `DuelCommands`、`ModNetwork` 三处）——换台没有 D 盘的机器会失败。现在统一走
  `ModPaths.deckDir()`（`config/mdpromod/decks`）。产物 `E0C1C431…` 已部署，**这部分待实机复验**。

## 七、待办 / 已知限制

1. 实机已验证解包 / 宿主 / 卡图 / 一局对局推进；**还没看**的是解包期间的 action bar 进度
   （解包太快，标题界面就结束了）、立绘与召唤特效、以及卡组导出的新目录。
2. 首次启动会占 ~396 MB 磁盘（在 `.minecraft/mdpromod/` 下），jar 566.8 MB；
   以后要瘦身可以考虑把美术资源拆成可选包。
3. `ygoserver.dll`（3.2 MB）仍打进包里，但当前模组流程只用 `ocgcore.dll`（ygoserver 是
   TCP 房间服务器，`tools/ygo_tcp_probe` 才用）。
4. 宿主源码若再改动，记得重新 `dotnet publish` 并重打包，否则包里还是旧 exe。
