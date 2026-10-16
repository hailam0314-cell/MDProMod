# D:\MyCardLibrary\mdpro3 与旧 MDPro3 源码比较

核查日期：2026-10-11。仅读取文件；没有修改安装目录或源码。

## 成品与数据

`D:\MyCardLibrary\mdpro3` 是 Unity 6.0.0.24f1 Windows x64 成品，主程序和 Managed 程序集时间为 2026-09-26。原有源码快照的 `Version.txt` 是 `1.2.4`，数据主要停留在 2024.10。

| 项目 | 成品 | 旧源码快照 |
|---|---:|---:|
| zh-CN `cards.cdb` 记录 | 15,019 | 13,631 |
| `script.zip` 中卡片脚本 | 13,572 | 12,498 |
| `script.zip` 总条目 | 13,577 | 12,503 |
| `lflist.conf` 最新 OCG 标题 | 2026.10 | 2024.10 |

成品脚本 ID 集合包含旧快照全部 12,498 个脚本，并新增 1,074 个。成品中文卡库相对旧快照新增 1,388 个 ID，没有删除旧 ID。成品 `lflist.conf` 同时包含 OCG 2026.10、TCG 2026.5 及 Genesys 等格式入口。

## 核心 DLL

成品路径：`D:\MyCardLibrary\mdpro3\MDPro3_Data\Plugins\x86_64`。

- `ocgcore.dll`：1,687,040 bytes，SHA-256 `90981efa9176b042eb07790269555ff98d522b9aa926ec464ecfb7cb55035300`
- `ygoserver.dll`：3,396,096 bytes，SHA-256 `315ffc32ce322ac9f482f37884cca29b9a4eef3bf3ac1442d70bc009bbef9750`

旧源码随附 DLL 位于 `Assets\Plugins\YGO Classes`：ocgcore 2,190,848 bytes，ygoserver 2,117,120 bytes，二者均与成品不同。

成品 `ocgcore` 导出/字符串显示已经存在 `create_duel_v2`、`create_duel_v2_ex`、`query_field_card`、`query_field_count`、`query_field_info`、`set_card_reader`、`set_player_info`、registry API、`new_card`/`new_tag_card` 等新接口；旧 DLL 主要只有基础 `create_duel`、`start_duel`、`get_message` 等接口。

用旧 Python ctypes 探针改指向成品 DLL、成品 `cards.cdb` 和 `script.zip` 后，CardData 仍为 76 bytes，十张代表性卡片初始化成功，Lua helper/API 断言通过且没有错误日志。该探针仍不是完整决斗测试。

## 新规则和卡片支持

成品 `script/constant.lua` 增加了 `EFFECT_REVEAL_ONFIELD=376`、超量最低素材数、扩展同调/仪式等级、双重超量素材、增加/删除/改变卡片种类等常量。

`script/c34298391.lua` 是「心を見通す眼」：

- `EFFECT_PUBLIC` 持续公开对手手牌；
- `EFFECT_REVEAL_ONFIELD` 允许确认对手场上的里侧表示卡；
- 另含新卡自身的连锁效果。

成品脚本中 `Duel.RevealSelectDeckSequence` 已有 5 个引用，旧 DLL/脚本快照对此 API 的缺口结论不能套用到成品。

## 单人、AI 与网络流程

旧源码中的可复用流程已经很清楚：`Solo.StartAIForSolo` 调用 `Launch`，由 `YgoServer.StartServer(args)` 启动独立 `ygoserver.dll`，然后 `TcpHelper.LinkStart("127.0.0.1",...)` 加入本地房间，再启动 `WindBot.Program.Main(args)`。`StartAIForRoom` 则把 WindBot 直接连到已经加入的房间。

成品程序集仍包含 `MDPro3.Net.YgoServer`、`MDPro3.PercyOCG`、`MDPro3.Servant.OcgCore`、`TcpHelper`、`AsyncYGOClient`、`AsyncBinaryClient`、`AsyncNetworkServer`，以及大量 `WindBot.Game.AI.*Executor` 和 AutoDuel 类型。

联网仍是 YGOPro 风格协议：2 字节 little-endian 长度，随后 Ctos/Stoc 消息；成品 `hosts2.conf` 给出了 OCG、TCG、Genesys、MD 等房间入口和模式字段。

## 对 Minecraft 模组的结论

开发基线应改用成品的核心、脚本、卡库和禁限表的独立镜像，而不是旧源码 `Data`。建议复制到 `D:\MDProMod\data\mdpro3-latest-2026-09-26`，保留安装目录只读。优先按“最新版核心 headless 单人 → 本地 ygoserver + WindBot → Forge 世界实体投影 → Minecraft 双人服务器 → 外部房间兼容”顺序实现。

