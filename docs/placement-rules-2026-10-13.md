# 卡片放置规则与交互设计

日期：2026-10-13

## 结论

“自动寻找最近空位”已经废弃。正确流程是：

1. 玩家选择一张卡。
2. 玩家选择具体区域。
3. 规则引擎判断该区域是否合法。
4. 只有合法区域才能提交。

最终裁定必须来自 ocgcore 的 `SelectPlace` 消息，而不是客户端自己猜测。`SelectPlace` 会给出一个合法区域位掩码，客户端只负责高亮这些区域并把玩家点击的区域回传。

## 区域与引擎编号

| 区域 | 显示位置 | ocgcore 位置 | 说明 |
| --- | --- | --- | --- |
| 主怪兽区 | 每方 5 格 | location 4，sequence 0-4 | 通常怪兽、效果怪兽、仪式怪兽、融合、同步、超量 |
| 魔陷区 | 每方 5 格 | location 8，sequence 0-4 | 魔法、陷阱、灵摆刻度 |
| 场地区 | 每方 1 格 | location 8，sequence 5 | 场地魔法 |
| 额外怪兽区 | 双方中间 2 格 | location 4，sequence 5-6 | 双方共享 |
| 墓地 | 每方 1 格 | location 16 | 不用于场上放置 |
| 除外区 | 每方 1 格 | location 32 | 不用于场上放置 |
| 额外卡组 | 每方 1 格 | location 64 | 不用于场上放置 |
| 主卡组 | 每方 1 格 | location 1 | 不用于场上放置 |

## 额外怪兽区规则

- Extra Monster Zone 是双方共享的两个物理位置。
- 融合、同步、超量怪兽在 Master Rule 2020 下可以特殊召唤到主怪兽区，也可以使用额外怪兽区。
- Link 怪兽必须召唤到额外怪兽区，或者召唤到 Link 箭头指向的主怪兽区。
- Link 箭头、灵摆召唤、连接素材、卡片效果造成的特殊召唤，全部以 ocgcore 返回的 `SelectPlace` 掩码为准。

## 灵摆区规则

- 灵摆区兼用最左和最右的魔陷区，不额外占用两个物理格子。
- 灵摆怪兽作为刻度放置时使用魔陷区 sequence 0 和 4。
- 灵摆召唤时，怪兽进入主怪兽区。

## 当前两种交互路径

### 1. 沙盒预览

手持卡片右键场地，必须指向具体区域。模组会用本地近似规则检查：

- 通常/效果/仪式怪兽：本方主怪兽区。
- 融合/同步/超量：本方主怪兽区或空闲额外怪兽区。
- Link：保守地只允许空闲额外怪兽区，Link 箭头规则等待引擎掩码接管。
- 魔法/陷阱：本方魔陷区。
- 场地魔法：本方场地区。
- 灵摆怪兽：本方主怪兽区，或最左/最右魔陷区。

这层检查只用于没有运行决斗引擎时的预览和测试，不是最终裁定。

### 2. 规则引擎驱动

当 Duel Host 发出 `SelectPlace` 提示时：

```text
/mdpromod duel prompt
/mdpromod duel places
/mdpromod duel place <index>
```

- `prompt` 显示当前选择类型。
- `places` 列出 ocgcore 允许的具体区域。
- `place <index>` 把对应区域 `[player, location, sequence]` 作为 `responseBytes` 发回 Duel Host。

这条路径是规则准确的。下一步把客户端场地高亮接到 `places` 列表，玩家点击高亮区域时直接发送对应的 `place` 索引。

## 实测结果

使用只包含 3 星通常怪兽 `32864` 的测试卡组，手动响应首个 `SelectIdleCmd` 后，Duel Host 输出：

```text
SelectIdleCmd: summon=5, set=0, activate=0, battle=no, end=index 7
SelectPlace: choose 1 zones (23 available)
```

`SelectPlace` 的 payload：

```text
12 00 01 E0 FF FF FF
```

其中 `places` 数组包含 23 个 `[player, location, sequence]` 组合。这个列表就是规则引擎当前允许的区域集合，客户端必须原样使用。不同卡片效果可能允许跨玩家区域、魔陷区、场地区或额外怪兽区，所以不能假设“怪兽只能放在自己主怪兽区”。

## 尚未完成

1. 客户端还没有把 `places` 列表渲染成高亮区域。
2. 世界点击还没有自动映射到 `place <index>`。
3. 多区域选择（一次选择多个区域）还需要多索引命令。
4. Link 箭头、灵摆召唤和特殊召唤的完整判定仍由 Duel Host 的 `SelectPlace` 掩码负责，沙盒本地检查不会覆盖它。
