# MDPro3 决斗规则源码分析

本次阅读对象：`D:\MDProMod\MDPro3-main\MDPro3-main`。目标模组：Minecraft Java 1.20.1、Forge 47.4.20、Java 17。分析与参考脚本均保存在 D 盘。

这是基于本地代码的架构与规则阅读、基础卡库/脚本清点和有限原生 DLL 探测，不是官方规则认证，也不是全卡对局测试。未修改原始工程。

## 1. 快照与核对结果

- README 明确说明：从 Moenext 的中文项目镜像而来，镜像维护者计划自行修改。
- `Version.txt` 为 `1.2.4`；此标记不证明对应最新发布版本。本地下载包没有可用于确定提交的 Git 历史。
- `common.h:46`：`CURRENT_RULE = 5`，注释为 Master Rule 2020。
- `Tools/YGO Classes/lua/src/lua.h`：附带 Lua 源码版本为 5.4.6。不能仅据此断言预编译 DLL 的 Lua 版本。
- 基础中文 `cards.cdb` 有 **13,631 条 datas 记录**，包含普通怪兽、异画等，并非 13,631 个独立效果。
- `Data/script.zip` 有 **12,501 个 Lua 文件**，其中 **12,498 个卡号脚本**，其余为 constant、utility、procedure。
- 按 `register_card` 的“非普通怪兽或灵摆需要 initial_effect”判断，并按 `card_data::is_alternative` 映射异画卡号，**12,664 条需要效果初始化的记录均存在对应脚本**。
- 覆盖统计只涵盖基础中文卡库和基础脚本包，不包含额外扩展包，不证明 2026 年最新卡齐全，不证明脚本效果正确。
- 附带原生库位于 `Assets/Plugins/YGO Classes`，不是较新原仓库的 `Assets/Plugins/Windows` 布局。Windows `ocgcore.dll` 的 PE 机器类型为 x64，导出 19 个基础 C 接口。

可重复清点脚本：[audit_mdpro3.py](D:/MDProMod/tools/audit_mdpro3.py)。包含 SHA-256、Lua API 名单与函数行号的结果：[mdpro3-source-audit.json](D:/MDProMod/docs/mdpro3-source-audit.json)。

## 2. 哪些代码真正负责规则

| 层 | 位置 | 职责 |
|---|---|---|
| C++ 决斗核心 | `Tools/YGO Classes/ocgcore` | 状态机、合法行动、事件、连锁、位置和召唤、费用与结算调度 |
| Lua 基础库 | `Data/script.zip` 中的三个公共文件 | 常量、辅助筛选与分组、融合/同调/超量/仪式/灵摆/连接程序 |
| Lua 卡片脚本 | `script/c卡号.lua` | 注册具体效果，定义条件、费用、对象、结算与特殊限制 |
| SQLite 卡库 | `Data/locales/zh-CN/cards.cdb` | 类型、等级、攻击力、种族、属性、字段、名字、描述、效果选项文字 |
| 对局组织与信息分发 | `Tools/YGO Classes/ygoserver`；C# `YGOSharp/Game.cs` 等 | 卡组检查、玩家响应权限、网络会话、隐藏信息过滤、比赛管理 |
| MDPro3 客户端 | `Assets/Scripts/MDPro3/Duel`、`Servants/OcgCore.cs` | 协议解析、选择界面、场面呈现、动画与音效 |

卡片描述文字不会自动变成效果。C++ 核心提供公共裁定能力，Lua 脚本使用这些能力描述每张卡；脚本写错，成熟核心仍会执行错误的卡片行为。

`MasterDuel` 目录中的呈现代码和大量资源不应作为移植规则的主要入口。MDPro3 中名为 `OcgCore.cs` 的大类主要是客户端状态与交互处理；真正的原生核心在上述 C++ 目录。

## 3. 核心如何运行

入口为 `ocgapi.h` / `ocgapi.cpp`：

1. 在创建决斗前注册 script_reader、card_reader、message_handler。
2. `create_duel(seed)` 建立决斗和 Lua 环境；构造过程中即加载 constant、utility、procedure，因此脚本读取回调必须已经可用。
3. `set_player_info` 设置 LP、起始手牌和每回合抽卡数量；`new_card` 装入主卡组与额外卡组。
4. `start_duel(options)` 启动。高 16 位指定规则编号，低 16 位为选项；本原型应显式指定规则 5，而不能依赖所有封装的默认值。
5. `process` 推进处理队列；`get_message` 读取并清空消息。选择操作会进入等待；输入响应后继续推进。
6. 读取 `MSG_WIN` 后由宿主结束该局，并调用 `end_duel` 清理。

`processor.cpp:35` 是处理器分派入口。`core.units/subunits` 保存带 step 的任务，选择、召唤、破坏、费用、对象、结算分别成为任务。玩家等待与 Lua 协程让多步骤操作暂停、恢复。

注意 `PROCESSOR_END` 表示处理队列结束，不能替代胜负消息。`common.h` 的长度掩码为 `0x0fffffff`，状态掩码为 `0xf0000000`；旧 C# 封装只取低 16 位，Minecraft 桥接应按所选核心的真实定义解析。

## 4. 回合与阶段

`processor.cpp:3659` 的 `process_turn` 负责阶段推进、回合计数、召唤/攻击计数、次数恢复与各类阶段事件。

常规阶段为：抽卡 → 准备 → 主要 1 → 战斗 → 主要 2 → 结束。战斗又拆为开始步骤、战斗步骤、伤害步骤、伤害计算、战斗结束。跳过阶段和第二次战斗阶段存在专门分支，界面不能自行把所有阶段按固定循环推进。

- 规则 >= 3 时，第一回合不进行通常抽卡，参见 `processor.cpp:3779`。
- `DUEL_ATTACK_FIRST_TURN` 未设置时，第一回合不能进入通常战斗阶段，参见主要阶段行动生成。
- 结束阶段默认手牌上限 6，可被 `EFFECT_HAND_LIMIT` 改变；超出后发送选卡请求，并以规则弃牌原因送墓，见 `process_phase_event`。
- 每个阶段的可发动效果与双方放弃响应由核心处理；“玩家点结束”只是提交核心提供的行动。
- 胜负调整见 `processor.cpp:4796` 的 `adjust_step`：LP、无法完成抽卡、特殊胜利等。牌库变为空不立即等于败北；抽卡不足才涉及 overdraw。

## 5. 效果发动生命周期

核心先由 `effect.cpp:242` 的 `is_activateable` 检查类型、位置、控制者、次数、盖放限制、伤害步骤限制等，再由 `effect.cpp:400` 检查脚本的 condition、cost(chk=0)、target(chk=0)。

检查阶段可能重复调用，必须保持无操作副作用。`interpreter::call_function` 设置 no_action，`scriptlib.cpp` 的 `check_action_permission` 会阻止在这种环境中执行多数改变局面的 Duel 操作。

通过检查后，`processor.cpp:3981` 的 `add_chain` 把发动登记到连锁，执行费用，再执行对象函数的实际操作，登记操作信息与事件。随后双方获得合法的追加连锁机会。

`processor.cpp:4361` 的 `solve_chain` 从 `current_chain.rbegin()` 开始，按最后发动先结算执行 operation，处理无效、永续卡关联、效果替代及清理。结算中的玩家选择不是新的连锁发动窗口。

费用不会因为后续效果无效而自动退回。不要把支付费用写进 operation，也不要在 cost(chk=0) 时扣 LP、弃牌或解放。

## 6. 连锁、触发顺序和时点

`process_instant_event` 与 `process_single_event` 收集事件与触发；`processor.cpp:1286` 的 `process_point_event` 组织同时满足条件的诱发效果。

在普通公开区域的同时诱发场景中，代码先处理回合玩家强制，再非回合玩家强制；随后回合玩家任意，再非回合玩家任意。各组中多个可选效果由核心向玩家发出顺序/选择请求。手牌等非公开区域的诱发效果另有特殊检查，不能把这个四组顺序当作所有场景的完整算法。

`processor.cpp:1586` 的 `process_quick_effect` 管理快速效果优先权与双方放弃。`effect.cpp:754` 的 `get_speed` 为起动/诱发、快速效果、速攻魔法、普通陷阱、反击陷阱给出速度；`effect.cpp:584` 的 `is_chainable` 检查速度和连锁限制。

普通魔法不能像快速效果那样任意接连锁，反击陷阱通常需要速度 3 接续；同一事件的誘发效果构筑与非公开区域规则有专门处理。Minecraft 应呈现核心返回的选择，不能只比较速度自行推断所有能发动的卡。

“时……可以”“场合……”和延迟触发涉及事件窗口、最后发生的事件、`EFFECT_FLAG_DELAY`、`BreakEffect` 与效果关联。不能统一给所有诱发效果加 DELAY。`field.cpp:3561` 的 `check_trigger_effect` 在规则 5 下要求仍存在触发关联；进入触发窗口前卡片移动，可能使效果不能发动。

## 7. 对象、选择、关联和操作信息

Lua 中函数名 target 不代表该效果一定“取对象”。它还负责检查能否发动、登记玩家与数值、登记操作信息。

- 死者苏生 `c83764718.lua`：有 `EFFECT_FLAG_CARD_TARGET`，发动时 `SelectTarget`；结算时 `GetFirstTarget` 并检查 `IsRelateToEffect`，然后特殊召唤。
- 雷击 `c12580477.lua`：没有取对象属性，结算时重新取得对方怪兽组并破坏。发动时登记的操作信息不是锁定全部结算卡片。
- 一张卡离开某区域后又回来，不应只凭卡号视作原先的对象。必须使用核心的关联判断。
- `Duel.Destroy`、`SendtoGrave`、`Release`、`Remove` 具有不同原因和事件，不可用一个“删除卡片”操作替代。
- `SetCategory` 与 `SetOperationInfo` 会被其他效果读取。灰流丽脚本直接检查抽卡、检索、卡组送墓、卡组特殊召唤的类别/操作信息；登记错误会影响对手的合法响应。
- 无效发动 `NegateActivation` 与无效效果 `NegateEffect` 是两种不同路径，对事件、次数和誓约有不同影响。

## 8. 召唤、额外区域与战斗

通常召唤/盖放见 `operations.cpp:1459`、`card.cpp:3371`；解放数量见 `card.cpp:3437`。常规等级对应解放需求之外还处理次数增加、减免、替代解放、特殊通常召唤程序及玩家禁止效果。不要在 Java 侧写一个固定等级判断代替核心。

不通过效果结算的特殊召唤程序见 `operations.cpp:2691`，效果进行的特殊召唤见 `special_summon` 系列。融合、同调、超量、仪式、灵摆、连接的材料程序大量位于 `procedure.lua`。通用程序与核心协作筛选材料、检查区域、移动素材、叠放、完成正规召唤。

规则 5 的额外区域见 `field.cpp:826`：通常的里侧额外融合/同调/超量怪兽不必强制依赖连接箭头；连接怪兽与表侧额外灵摆通常仍需额外怪兽区或箭头指向区域。判断仍受区域占用、封锁和卡片效果影响。

`EnableReviveLimit` 与 `STATUS_PROC_COMPLETE` 参与苏生限制，见 `card.cpp:3549`。没有完成正规召唤的相关怪兽，不能当作普通墓地怪兽直接苏生。

攻击、攻击回卷、直接攻击资格、战斗伤害与伤害步骤分别见 `process_battle_command`、战斗相关函数与 `process_damage_step`。伤害步骤与伤害计算的发动范围不同，`EFFECT_FLAG_DAMAGE_STEP` / `DAMAGE_CAL` 不是给所有快速效果都能加的通行证。

## 9. 次数限制与持续效果

`libeffect.cpp:113` 的 `SetCountLimit` 在本快照接收次数和一个整数 count code。`effect.cpp:162` 检查，发动选中时消耗，`process_turn` 恢复；`field.cpp` 区分每回合、每连锁与每决斗的映射。

- `SetCountLimit(1)`：效果实例的次数限制，不能自动表示同名共享限制。
- `SetCountLimit(1,id)`：同玩家共享 count code；用来表达相应的同名限制，卡片各效果是否共享必须按文本设计。
- 本快照的 OATH 等标记通过整数编码组合；某些新版引擎的 table 参数或额外参数不能直接套用。
- 发动被无效时，`solve_chain` 对 OATH count code 有回退分支；“发动一次”与“使用一次”不能混为一谈。
- 永续增攻、禁止、免疫等一般注册非连锁效果，交给范围、value、target 与调整机制计算。临时效果必须设置正确 Reset。

具体编写约定见 [卡片效果编写指南](D:/MDProMod/docs/卡片效果编写指南.md)。

## 10. 移植前已确认的差异与问题

### 10.1 卡片脚本调用了缺失 API

基础包 `c43618262.lua`、`c53323475.lua`、`c94392192.lua` 使用 `Duel.RevealSelectDeckSequence`。本地 C++ API 注册表不含它，公共 Lua 文件没有补充定义，DLL 字节扫描也未发现该名。

进一步用本地 x64 DLL 创建隔离决斗，通过 script_reader 装载探测脚本断言该函数存在，实际收到 `probe: RevealSelectDeckSequence missing`。这证明附带 DLL 的基础 Lua 环境缺少此接口。未执行这三张卡的完整对局；运行到相关调用处预计会发生 nil 函数错误。需查清预期功能、更新配套核心或采用经过验证的兼容实现，不能只把调用删掉或设为空函数。

公共脚本加载成功；强欲之壶、死者苏生、灰流丽、成金哥布林、圣防、雷击、星尘龙、希望皇霍普、青眼究极龙、连接蜘蛛共十个脚本初始化未产生日志错误。这只验证加载与 initial_effect，不代表费用、条件和完整结算已经通过测试。

探测代码：[probe_mdpro3_core.py](D:/MDProMod/tools/probe_mdpro3_core.py)，结果：[mdpro3-native-probe.json](D:/MDProMod/docs/mdpro3-native-probe.json)。

### 10.2 两套封装不能混用

旧 `Assets/Scripts/YGOSharp/YGOSharp.OCGWrapper/Card.cs` 使用一个 long Setcode；当前 C++ `card_data.h` 使用 `uint16_t setcode[16]`。MDPro3 实際 `Duel/CoreWrapper.cs:229` 使用相匹配的 16 项结构。Windows 常规布局的这个结构为 76 字节，有限探测按此布局成功读取并初始化样例卡。桥接必须验证字段偏移、大小和 ABI，不能以旧封装直接调用当前库。

`CoreWrapper.cs` 还声明了若干 AI 的额外导出，本地 DLL 的导出名单不含它们。声明不等于可用能力；PvP 原型使用已确认的 19 个基础接口即可。

### 10.3 函数签名与内部实现需逐项核实

`preload_script` 的头文件参数看似 script+len，当前实现却调用 `load_script(script)`，实际把第二个参数视作读取路径，len 未使用。探测已按“回调提供路径对应内容”方式成功工作。

旧 C# 及 MDPro3 CoreWrapper 的消息接收分配固定 4096 字节，而核心消息缓冲是可增长 vector；`SIZE_MESSAGE_BUFFER=0x2000` 只是初始 reserve。固定 4096 不能作为一般容量保证。读取长度之前，还必须确保原生 get_message 的写入目标足够，建议桥接提供容量受控的 C 包装，查询接口也一样。

`ygoserver/data_manager.cpp:228` 的 setcode 拷贝把 `SIZE_SETCODE`（16 个元素）直接作为字节数，按头文件应拷贝 32 字节。通常 CDB 打包最多四项，不代表所有扩展数据都安全。这是明确代码差异，尚未构造完整场景复现。独立 card_reader 应按正确元素大小转换。

### 10.4 单局之外还有规则

卡组数量、禁限、同名合计、OCG/TCG 可用范围与换备牌检查在宿主层，参见 YGOSharp 的 Deck/Game 和 C++ 服务端的相应模块。不能以“核心允许装入”替代卡组合法性检查。`Data/lflist.conf` 开头列表为 2024.10，该事实也说明这不是直接可用于当前禁限表的最新数据。

`Player.OnResponse` 校验应答状态；`Game.WritePublicCards` 等按接受者过滤场面。原始 query 消息包含隐藏信息，必须在服务端过滤。不要把整个决斗状态发到所有 Minecraft 客户端后再靠界面隐藏。

### 10.5 授权来源需要纠正

本地根目录 LICENSE 是 GPL-3.0，**本地 ocgcore 目录没有独立 LICENSE**。此前提到的 MIT LICENSE 来自较新的原仓库核心目录，不能据此把本地镜像的所有核心修改直接认定为 MIT。应补齐本地组件的来源与许可记录后再决定如何分发。

## 11. 对 Minecraft 实现的约束

1. 服务端拥有权威决斗状态；客户端只提交当前请求允许的选择，附加 session、玩家身份、request id 等校验。
2. 核心提供合法行动，Java 适配层负责协议和会话，客户端负责选择与展示。
3. 起初用独立工作进程/串行调度：此版全局回调、全局 duel_set、共享读脚本缓冲等没有明显同步保障，不能直接认定不同 duel 可随意多线程并发。
4. 支持 yes/no、选项、卡组/手牌选卡、区域、表示、解放、材料求和、计数器、排序、宣言等请求。未支持的请求应明确终止/报错，不能随意自动选答案继续。
5. 核心推进不占用 Minecraft tick 主线程；设置执行预算与超时，进程隔离为脚本卡死提供退出路径。
6. 固定并记录 core+DLL+卡库+公共脚本+卡片脚本+禁限表的一组版本。更新其中任何一项都要重新做兼容与规则回归。
7. 初期先验证完整 PvP 协议、费用/对象/无效、正规召唤与隐藏信息，再做抽卡包、实体卡片掉落、三维投影。

本轮完成了关键规则路径阅读与上述清点、初始化探测。尚未编译核心、实现 Forge 模组、完成完整对局、测试全部卡片或验证与最新官方裁定的一致性。
