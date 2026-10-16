# 脱离 MDPro3 单独运行：清理清单（2026-10-16）

目标：电脑上删掉 MDPro3 之后，模组照样能玩，而且**还能重新打包**。

## 一、游玩到底需要什么

只要两样东西，都在 Minecraft 这边：

| 东西 | 位置 | 大小 |
| --- | --- | --- |
| 模组 jar | `…\.minecraft\versions\1.20.1-Forge_47.4.20\mods\mdpromod-0.1.0.jar` | 566.8 MB |
| 首次启动解出来的资源 | `…\versions\1.20.1-Forge_47.4.20\mdpromod\` | 396 MB |

解包目录**删掉也没事**：下次启动会照 jar 里的清单重新解一遍（约 35 秒）。
换台机器只拷 jar 就能玩——卡图、立绘、召唤特效、宿主和它的 .NET 运行时都在包里。

运行期的路径优先级（`ModPaths`）：`-D` 覆盖 &gt; 解包目录 &gt; 旧的外置目录。
只要 jar 里带着资源（`ModBundle.bundled()`），游戏里就一律走解包目录——这条在实机上验过：
宿主日志的 `ready.root` 指向 `…\mdpromod\runtime`，卡名表 `cards.json` 也是从那儿读的。

## 二、可以删的（约 28.6 GB）

| 目录 | 大小 | 用途 | 删了会怎样 |
| --- | --- | --- | --- |
| `D:\MyCardLibrary`（含 `mdpro3`） | 19.0 GB | MDPro3 游戏本体（美术/音效原料 + 编译 C# 宿主用的 DLL） | 游玩、打包都不受影响；只有"重新提取素材"或"重新编译宿主"才需要——DLL 已经拷进 `refs\`，所以编译也不需要了 |
| `D:\MDProMod\MDPro3-main` | 1.51 GB | MDPro3 源码副本（研究用） | 只是参考资料 |
| `D:\MDProMod\Windows` | 4.50 GB | 解包出来的 MD 安装（素材提取原料） | 只有重跑提取流水线才需要 |
| `D:\MDProMod\Android` | 3.56 GB | 同上（安卓包） | 同上 |

## 三、要留着的（约 400 MB，重新打包用）

`gradlew build` 里的 `stageBundle` 会从这些地方把资源收进 jar：

| 目录 | 大小 |
| --- | --- |
| `D:\MDProMod\runtime\card_textures_176x256_q80` | 203.7 MB |
| `D:\MDProMod\runtime\closeup_128` | 77.4 MB |
| `D:\MDProMod\runtime\summon_fx` + `summon_mesh` | 0.6 MB |
| `D:\MDProMod\tools\ygo_tcp_probe\staging`（真正的载荷约 42 MB，其余是历史日志，可清） | 216.3 MB |
| `D:\MDProMod\tools\duel_host\bin\Release\net8.0`（只取 5 个文件） | 74.4 MB |
| `D:\MDProMod\tools\dotnet\host\fxr` + `shared\Microsoft.NETCore.App`（宿主自带运行时，**不是**整个 714 MB 的 dotnet 目录） | 70.5 MB |

这些原料不在时构建**不会失败**，但会打出一个"不带资源"的 jar（`stageBundle` 现在会大声
WARN 提醒）。换机器打包用 `-Pmdpromod.bundleRoot=<原料目录>`。

## 四、这次为"单独运行"做的清理

1. **卡组搜索**：`DuelCommands` 去掉了 `D:\MyCardLibrary\mdpro3\Deck` 这个候选，
   现在只找 `config\mdpromod\decks`（玩家自己的）和当前工作目录。
2. **C# 宿主编译**：把 MDPro3 里的 5 个 DLL（`Mono.Data.Sqlite`、`UnityEngine.CoreModule`、
   `UnityEngine.UI`、`Unity.InputSystem`、`Newtonsoft.Json`，共 3 MB）拷进 `D:\MDProMod\refs\`，
   4 个 csproj 的 `HintPath` 改成相对路径 `$(MSBuildThisFileDirectory)..\..\refs\…`。
   验证：`dotnet build -c Release` 通过（"已成功生成"）——**删掉 19 GB 的 MyCardLibrary 之后宿主照样能编译**。
3. **打包保险**：`stageBundle` 在原料缺失时打 WARN，避免悄悄打出没有资源的包。

## 五、验证记录（2026-10-16）

- 客户端（实机）：解包 26,548 个文件 / 396 MB / 35 秒；宿主 `ready.root` = 解包目录；
  `Loaded 15019 card definitions from cards.json`；卡图正常；一局人机对局正常推进。
- 专用服务器：模组能正常加载（`Done (10.391s)`），开局自检
  `合成配方自检：4 个都在，本模组共 4 条配方（全库 1178 条）`。
- 离线：13 套测试 + `ModBundleSelfTest`（14 断言）+ `RecipeDataTest`（87 断言）全过。

**还没实机验证**：立绘 / 召唤特效（这两类也走解包目录）、卡组导出是否落到
`config\mdpromod\decks`、以及在工作台里真合成一次。
