# 又一次"卡死"：应答格式回退 + 提示去重按形状 → 60 次 MSG_RETRY（2026-10-14 晚）

## 现象

`latest.log`（06:34–06:42 那次）末尾刷屏：

```
Duel Host message: {"function":1,"name":"Retry",...}
Duel Host error: {"type":"error","message":"AI feed failed: Got MSG_RETRY. Last message is UpdateData",...}
Duel Host retry: {"type":"retry","message":"上次应答被核心判为非法，请重新选择"}
…（重复 60 次）…
Duel Host error: {"type":"error","message":"连续应答非法，停止对局"}
Duel Host ended: {"type":"ended"}
```

也就是说：不是画面卡住，而是模组反复发出**非法应答**，host 的兜底
（`_aiRetries > 60` → 停局）把对局结束了。

## 触发点

```
L4430  PROMPT  SelectCard: choose 1..1 from 7 cards
L4431  ANSWER  Select list answered: tawil2 -> 0101          ← 字节格式，被核心接受
       …（中间经过 SelectUnselectCard 流程）…
L4521  PROMPT  SelectCard: choose 1..1 from 7 cards          ← 形状和 L4430 一模一样的新提问
L4522  ANSWER  Select answered as integer: 2                 ← 这次发的是整数
       → MSG_RETRY → 同一条提示重发 → 还是整数 → … 60 次 → 停局
```

## 根因

1. **模组里有个"备用格式"回退**：`answerSelect` 在 `selectIntegerFormat` 为真时直接发下标整数
   （`host.respond(index)`）。但 `ocgcore` 的 `select_card` 只认
   `returns.bvalue[0] = 张数` + `bvalue[1..] = 下标`，也就是字节格式 `[张数, 下标…]`
   （源码 `playerop.cpp:258-280`，非法就 `MSG_RETRY`）。**整数格式对 SelectCard 永远是错的。**
2. **这个标志是怎么亮的**：判据是"新提示的形状签名 == 上一次应答时的签名"，
   而签名只有 `候选数:min:max`。L4521 是**新的一次提问**，只是形状恰好相同，
   于是模组误判成"上次被拒了"，切到整数格式并**一直锁在整数格式**
   （重发的提示签名仍然相同）→ 死循环。
3. **次要问题**：所有界面去重都用"形状签名"，所以同形状的提示重发时不会重新弹界面
   —— 一旦某次应答被拒，玩家可能连界面都看不到（这也是"点了没反应"的一种来源）。

## 改了什么

模组：

- `DuelSessionManager.answerSelect`：**删掉整数格式回退**，`SelectCard` / `SelectTribute` /
  `SelectUnselectCard` 一律发 `[张数, 下标…]`；同时删掉 `selectIntegerFormat` /
  `pendingSelectSignature` 这套推测。
- `DuelHostProcess.promptSeq()` + 所有界面签名前缀 `seq`
  （选卡 / 素材 / 表示 / 排序 / 选项 / 连锁 / 是-否）：**核心重问同一条提示时会重新弹界面**，
  同一提示在等待期间不会重复弹。
- 新增"宿主提示"通道：host 报 `error` / `retry` 时，聊天栏直接说
  （"对局出错：…" / "核心判上次应答非法，会重新问一次"）；retry 提示 1.5 秒节流，避免刷屏。

host：

- 每条 prompt 事件加自增 `seq`（`DuelCore._promptSeq`），供客户端区分"新提示 / 重发的同一提示"。

## 验证

| 验证 | 结果 |
| --- | --- |
| 切分器自检 | `--test-splitter` 9 条真实报文全过 |
| AI 整局（seed 4962） | 3970 行、0 截断警告、0 错误、`MSG_WIN` |
| AI 整局（seed 305419896） | 7110 行、0 截断警告、0 错误、`MSG_WIN` |
| 手动探针 `select_chain_probe.py` | 7533 条消息、303 个连锁窗口 / 950 个候选全部解析正确、0 错误、正常结束；prompt 里已带 `seq` |
| 产物 | `mdpromod-0.1.0.jar` 2,333,247 字节，SHA-256 `8239FEAB…86ACF2`，已复制到客户端 `mods`；旧包备份 `tmp\mdpromod-0.1.0.jar.prev-2026-10-14-0655` |

## 备注

- `_aiRetries > 60` 的兜底**保留**：它是最后一道防线（防止对局真的变成死循环），
  但现在错误会同时显示在聊天栏，不会再"静默结束"。
- 还没有界面的窗口（`SortChain`21 / `SelectCounter`22 / `SelectSum`23 / 宣言 14x）依旧只会提示，
  不会代答；下一步可以做"长时间无界面 → 按最小代价代答"。
