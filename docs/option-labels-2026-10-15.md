# 「选择选项」窗口文字修复（2026-10-15）

## 现象

`MSG_SELECT_OPTION` 的弹窗只有占位文字：

```
选择选项
  Option 0 (description 78857041)
  Option 1 (description 78857042)
```

## 根因

**宿主从没解析过选项描述**——`tools/duel_host/Program.cs` 里直接拼的占位串：

```csharp
["label"] = $"Option {i} (description {description})",
```

而选项描述本来就是**一个字符串 id**，要按规则翻译。口径在 MDPro3 的
`MDPro3\Helper\StringHelper.cs` 的 `Get(int description)` 里写得明明白白：

```csharp
if (description < 10000) return Get("system", description);   // strings.conf 的 !system 段
var code = description >> 4;                                  // 高位 = 卡号
var index = description & 0xf;                                // 低 4 位 = 第几条（0 起）
return CardsManager.Get(code).Str[index];                     // 卡片自己的 16 条字符串
```

对应到本机数据：卡片字符串在 **`Data/locales/zh-CN/cards.cdb` 的 `texts` 表**
（`id`, `name`, `desc`, `str1..str16`），系统词在 **`Data/locales/zh-CN/strings.conf` 的 `!system` 段**
——两者都躺在宿主的 `--root`（`tools/ygo_tcp_probe/staging`）里。

### 核对（截图里那两个 id）

| 描述 id | `>>4` | `&0xf` | 结果 |
| --- | --- | --- | --- |
| 78857041 | 4928565（珠泪哀歌族型俱舍怒威族） | 1 | `str2` =「从自己卡组上面把3张卡送去墓地」 |
| 78857042 | 同上 | 2 | `str3` =「从对方卡组上面把3张卡送去墓地」 |

也就是这张卡的两个分支，和截图完全对得上。

## 改动

### 宿主（`tools/duel_host/Program.cs`）

1. 新增两张**进程级只读表**（不每局重读）：
   - `_cardTexts`：`SELECT id,str1..str16 FROM texts`，本机 15,019 张卡；
   - `_systemStrings`：`strings.conf` 的 `!system <id> <text>`，本机 535 条。
2. 新增 `DescribeOption(description, index)`，逐字照抄原版判断；查不到时退回
   「选项 N（描述 …）」占位（至少还能核对），而不是原来那句英文。
3. `case 14` 的 `label` 改用 `DescribeOption`，并把原始 `description` 一并留给前端。
4. **新增自检入口** `--describe-option <描述号…>`：不用开一局就能核对翻译结果，例如

```
dotnet tools\duel_host\bin\Release\net8.0\duel_host.dll --root tools\ygo_tcp_probe\staging ^
       --describe-option 78857041 78857042 1 1003
文字表：卡片 15019 张，system 535 条
78857041 -> 从自己卡组上面把3张卡送去墓地
78857042 -> 从对方卡组上面把3张卡送去墓地
1 -> 通常召唤
1003 -> 魔法陷阱区
```

### 模组（`OptionScreen`）

- 窗口宽度按**最长的一条选项**自适应（下限 320），避免长句溢出按钮；
- 标签为空时给「选项 N」兜底，不再出现空白按钮。

## 没动的部分

`SelectEffectYN`(12) / `SelectYesNo`(13) 的 `description` 仍然只是数字透传：模组那边
本来就不显示这个数字（弹窗显示的是"是否发动〈卡名〉"，卡名由 code/summary 得到），
所以没有可见问题。要以后想在那儿显示"发动哪条效果"，同一套表可以用现成的。

## 验证

- 宿主 `dotnet build -c Release` 通过；
- `--describe-option` 用真实 root 跑出上表结果；
- 模组 5 套离线测试全过；JAR 243,116,846 字节，
  SHA-256 `9EDE6B1C16276010781EC78FE887CD74401602B57EB1512ACED2F5ED14F35C7B`，已部署（哈希一致）。

## 备注

宿主是模组**直接按路径启动**的（`DuelSessionManager` 默认
`tools\duel_host\bin\Release\net8.0\duel_host.dll`），所以改完宿主**不需要重打模组 jar**，
但要重新 `dotnet build -c Release`，否则游戏里跑的还是旧 dll。
