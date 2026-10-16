# 禁卡表配置文件（2026-10-15）

## 之前为什么乱

卡盒的禁限判定写死在 `DeckRules` 里——一张只有 6 条的 `LIMITS` 表（强欲之壶 / 死者苏生 /
羽毛扫 = 1，雷击之类 = 2），而运行时根目录里那份**完整的 `lflist.conf`（104 张卡表、
676 KB）根本没被读过**。于是"卡表"这件事既不是真的、也没法调整。

## 现在的结构：一份数据 + 一份开关

**开关文件在游戏的 config 目录里**（`config/mdpromod/banlist.conf`，和别的模组配置放一起）；
卡表数据默认还在运行时根目录（`mdpromod.runtimeRoot`，默认 `D:\MDProMod\tools\ygo_tcp_probe\staging`），
但也可以自己在 config 目录里放一份覆盖它：

| 文件 | 位置 | 作用 | 谁写 |
| --- | --- | --- | --- |
| `banlist.conf` | `<游戏目录>/config/mdpromod/` | 卡表**开关** | 模组首次运行自动生成，之后由玩家编辑 |
| `lflist.conf` | 同上（可选） | 卡表**数据**替代品：放了就用它 | 玩家自带 |
| `lflist.conf` | 运行时根目录 | 卡表**数据**（ygopro 格式，104 张表） | 只读，不碰 |

路径怎么来的：游戏里用 Forge 的 `FMLPaths.CONFIGDIR`（`MDProMod` 构造函数里登记进
`ModPaths.useGameConfigDir`），所以线上就是**这个实例自己的 `config/mdpromod/`**
（版本隔离的实例是 `...\.minecraft\versions\<版本>\config\mdpromod\`）。
离线测试用系统属性 `mdpromod.configDir` 覆盖；两个都没有时退回"运行时根目录下的 `config/`"。

**旧位置会自动搬**：如果 `config/mdpromod/banlist.conf` 不存在、而运行时根目录里有一份，
就把它搬过去（内容是玩家的编辑，不能丢）；搬不动（文件被占用）才继续就地读。

`banlist.conf` 长这样（生成时把所有卡表都列出来，只有一张是 `on`）：

```
# MDProMod 禁卡表开关
# on = 启用，off = 关掉。可以同时开多个：同一张卡按最严的那条算。
# 名字要和 lflist.conf 里的 !名字 一致（大小写随意）；这里没列出来的卡表一律算关闭。
# 改完存盘即生效：卡盒和开局校验最多 2 秒后重新读一次，不用重启游戏。
# lflist.conf 里一共 104 个卡表：

on  2026.10
off 2026.9 TCG
off 2026.7
...
```

写法很宽松：开关认 `on/off`、`1/0`、`true/false`、`启用/关闭`；行尾可以写 `#` 注释；
名字可以带引号；**整行只写卡表名**（不写开关）当作打开。名字拼错不会误开别的表
（匹配不上就等于没开，卡盒标题上的状态行会如实显示生效了哪几张）。

### 判定规则

- **同时启用多张时取最严的那条**：2026.10 说禁止、2026.9 TCG 说准限制 → 按禁止算。
- 一张都没开 = 只校验张数（主 40..60 / 额外 15 / 副 15），不限制任何卡。
- `$whitelist` 模式的表（"只列出来的卡能用"）也照办：开了这种表，没列出来的卡一律 0 张。
- **alias 记账**：同一张卡的不同版本（`cards.json` 里的 `alias`，共 580 张）共用额度，
  和 MD 一样——拿别的版本凑张数也算超。

### 什么时候重新读

每 2 秒查一次两个文件的**修改时间 + 大小**，变了才重新解析（676 KB 的 lflist 解析约 83 ms，
不查签名的话每帧解析会卡）。所以改完存盘，游戏里最多 2 秒后生效，**不用重启**。

## 管到哪些地方

判定收敛在 `DeckRules`（`limitOf` / `copies` / `problems`），所以这些地方一起生效：

| 地方 | 表现 |
| --- | --- |
| 卡盒加卡 | 超上限直接拦下并说明原因：「「X」是禁止卡（2026.10 禁止）」「最多放 1 张（2026.10 限制）」；同组卡会补一句「和「Y」算同一张卡」 |
| 卡图角标 | 禁止 = 深红「禁」、限制 = 红「1」、准限制 = 橙「2」 |
| 悬停提示 | 「卡组内 N 张 · 限制 1（2026.10 限制）」 |
| 卡盒标题 | 右边显示「禁卡表：2026.10」；没生效时灰色「禁卡表：未启用」 |
| 检索筛选 | "只看限卡"勾上后按当前生效表过滤 |
| 卡组合法性（`problems`） | 存卡组、放上决斗盘、开局校验全走这一条，非法就报「「X」是禁止卡（2026.10 禁止），当前 1 张」 |

不替换：**人机卡组不校验**（Windbot 的 57 副卡组是固定资源，禁卡表挡它们会让对局开不起来）。

## 默认行为（重要）

第一次运行时生成的 `banlist.conf` **默认打开最新的那张表**（当前是 `2026.10`，禁止 94 张 /
限制 95 张）。这和之前的"内置 6 条"相比严格得多，老卡组可能一开就报不合法。
想回到"完全不管"：把那一行改成 `off 2026.10` 就行。

## 代码

| 文件 | 作用 |
| --- | --- |
| `Banlists`（新，纯函数） | lflist 解析 / 开关解析 / 生成模板 / 合并成"生效限制"（`Active.limitOf`、`reason`、`summary`） |
| `BanlistFile`（新） | 找文件、读文件、按签名缓存、生成默认配置；离线也能跑（没有 slf4j 时日志静默） |
| `ModPaths`（新） | 运行时根目录 + config 目录（`DuelSessionManager.runtimeRoot()` 改成转调它，好让不依赖 MC 的部分也能取路径；config 目录由 `MDProMod` 登记 `FMLPaths.CONFIGDIR`） |
| `ModLog`（新） | 日志出口，懒拿 slf4j |
| `DeckRules`（改） | 删掉写死的 `LIMITS`；按 alias 分组数张数；说明文案带上是哪张表管的 |
| `CardCaseScreen`（改） | 标题状态行、角标、悬停、加卡反馈 |
| `CardRegistry`（改） | 日志改走 `ModLog`，卡库载入失败只试一次并记下原因（离线测试没有 gson 也不会炸） |

## 验证

`BanlistTest`（67 断言，`src/test/java/net/mdpromod/BanlistTest.java`）：

- lflist 解析：表名/顺序、0/1/2 三种上限、注释行与 `--` 尾巴、`$whitelist` 只影响自己那段、空数据不炸；
- 开关解析：`on/1/启用`、`off`、行尾注释、带引号、名字里有空格、纯名字行、拼错不误开、重复只算一次；
- 模板：所有卡片都在、只有启用那张是 `on`、生成的文件再读回来一致；
- 合并：多张表取最严、只在一张表里的卡也认、没开的表不影响、白名单模式没列出来就是 0；
- 端到端（临时目录当运行时根 + config 目录）：改 `banlist.conf` → `DeckRules.limitOf/limitText/
  banlistStatus` 和 `problems` 立刻跟着变；配置不存在时自动生成并默认打开第一张表；
  旧位置的开关文件自动搬进 config 目录（内容不丢）；config 目录里自带 `lflist.conf` 时以它为准；
- alias：拿真卡库找一对 alias，验证"不同版本算同一张卡"（需要 gson，见下）；
- 真 lflist：运行时根目录里那份 104 张表的文件解析得动、第一张表（2026.10）能生效、
  能说清是哪张表管的，并打印解析耗时。

跑法（main 风格；`BanlistTest` 的 alias 断言需要 gson，其它测试不用）：

```
java -cp "build\libs\mdpromod-0.1.0.jar;build\classes\java\test;<gson-2.10.jar>" net.mdpromod.BanlistTest
```

另外 9 套测试全过（DuelCardMarks 23、DuelTiming 20、DuelLocation 12、DuelVisualState 71、
DuelVisualStateAudio 526、DuelAudioRules 117、DuelBgmRules、DuelAudioPerspective、AiDeckCatalog 177）。

产物：JAR 243,179,125 字节，SHA-256
`C960943DE50952C0B2A7866B5A3E52F1DC94350BA5F02C4E560E1BA217AD8D59`，已部署（哈希一致）。
`banlist.conf` 已放在 `<实例>\config\mdpromod\`（1 行 on + 103 行 off；运行时根目录那份已移过去）。
游戏日志里读（含改动）时也会打一行「生效的禁卡表：…（开关 <路径>，数据 <路径>）」方便找文件。
**实机未验证**。
