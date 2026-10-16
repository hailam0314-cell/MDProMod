# GitHub 发布清单（2026-10-16）

本仓库已经初始化好（`git init -b main`）并按 `.gitignore` 暂存完成：
**734 个文件 / 38.2 MB**。还没提交，也没配远程——下面第四节是命令。

## 一、仓库里放什么

| 进仓库 | 内容 | 体积 |
| --- | --- | --- |
| `mod/` | Java 模组源码、贴图、配方、`sounds.json`、离线测试 | ~9 MB |
| `tools/duel_host/`、`tools/windbot_standalone/src/`、`tools/windbot_host/` | C# 决斗宿主与 WindBot 源码 | ~8 MB |
| `tools/cutin_pipeline/`、`tools/audio_pipeline/`（不含 `deps/`）、`tools/ygo_tcp_probe/`、`tools/headless_*`、`tools/spine_preview/` | 素材流水线与探针 | ~5 MB |
| `assets/` | 决斗盘/卡盒/控制条的 Blockbench 模型与生成脚本（我们自己的素材） | ~11 MB |
| `docs/` | 带日期的设计/验证文档 + 探针结果（不含抓取的官方页面） | ~4 MB |
| `refs/` | 编译宿主用的 5 个参考 DLL | 3 MB |
| 根目录 | `README.md`、`LICENSE`、`THIRD-PARTY-NOTICES.md`、`.gitignore` | — |

| 不进仓库 | 原因 |
| --- | --- |
| `mod/build/`、`mod/run/`、`mod/.gradle/`、`*.log` | 构建产物与本地运行目录（1.9 GB） |
| `mod/src/main/resources/assets/mdpromod/sounds/` | Konami 音频素材，168 MB / 2,443 个 ogg |
| `runtime/`、`analysis/`、`cache/`、`tmp/`、`downloads/`、`tmp_compare/` | 流水线原料与中间产物（合计 3 GB+） |
| `MDPro3-main/`、`MDPro3-main.zip`、`Windows/`、`Android/`、`upstream/` | 第三方游戏安装与源码副本（10 GB+，含 962 MB 的 zip） |
| `tools/dotnet/`、`tools/ilspy*/`、`tools/unitypy/`、`tools/audio_pipeline/deps/` | 第三方工具链大件（900 MB+） |
| `tools/duel_host/publish*/`、`tools/duel_host/*.jsonl` | 发布方式实验的产物与烟测日志 |
| `docs/official-*`、`docs/card-*-community.*` | 抓下来的 Konami 官方页面/文本 |

`mod/src/main/resources/data/mdpromod/cards.json`（6.6 MB 卡名表）**留在仓库里**：没有它，
克隆下来构建出的 jar 连卡名都没有。想更严格就把它也加进 `.gitignore`（README 里已有"素材自备"一节）。

## 二、已经替你做好的文件

| 文件 | 作用 |
| --- | --- |
| `README.md` | 英文主 + 中文简介：功能表、安装、首次解包说明、构建、仓库结构、协议与素材声明 |
| `LICENSE` | GPL-3.0 全文（和 `gradle.properties` 里的 `mod_license=GPL-3.0-or-later` 一致） |
| `THIRD-PARTY-NOTICES.md` | ygopro/ocgcore、MDPro3、WindBot、**Spine Runtimes（全文）**、libGDX、Mono.Data.Sqlite、Forge/MCP |
| `.gitignore` | 上表所有"不进仓库"的规则，每段都有注释说明为什么 |

## 三、Release 放什么

jar 不进 git（566.8 MB），作为 **Release 附件**上传（GitHub 单文件上限 2 GB，够用）。

| 方案 | 内容 | 适合 |
| --- | --- | --- |
| **A. 全量 jar**（就是现在这个） | 卡图、立绘、音效、cut-in、宿主与 .NET 运行时全在里面 | 玩家体验最省事；素材版权风险自己承担 |
| **B. 无素材 jar** | 去掉 `sounds/`、`runtime/cutin_low`、卡图与立绘，只留规则与演出框架 | 上传更安全；玩家需自备素材目录 |

两个可以并存（同一个 tag 挂两个附件，命名 `mdpromod-0.1.0.jar` 与 `mdpromod-0.1.0-lite.jar`）。
另外建议在 Release 说明里附上 jar 的 SHA-256（现在是 `630910A6…`，重新构建后要更新）。

## 四、命令

```powershell
# 0) 先确认提交身份（机器上的全局 git 配置不是你自己的就改掉）
git config user.name  "你的名字"
git config user.email "你的邮箱"

# 1) 提交（暂存已经做好了）
cd D:\MDProMod
git commit -m "MDProMod 0.1.0: full Yu-Gi-Oh! duel in Minecraft"

# 2) 建仓库后接上远程（把 <user> 换成你的 GitHub 用户名）
git remote add origin https://github.com/<user>/mdpromod.git
git push -u origin main

# 3) 打 tag 并发 Release（gh 是 GitHub CLI；不用它就在网页上手动建）
git tag v0.1.0
git push origin v0.1.0
gh release create v0.1.0 "D:\MDProMod\mod\build\libs\mdpromod-0.1.0.jar" `
  --title "MDProMod 0.1.0" --notes-file docs\release-notes-0.1.0.md
```

## 五、Release Notes 草稿（可直接用）

```markdown
# MDProMod 0.1.0 — a full Yu-Gi-Oh! duel inside Minecraft

First public release. Place a **Duel Field**, load a **Card Case** and duel one of 57 WindBot
opponents under real ocgcore rules.

**What's inside**

* 15,019 cards with ocgcore rules and card text, fully offline
* 57 WindBot decks with names and personalities
* Six summon sequences, 497 ace-monster cut-ins, 11,091 closeups, 110 BGM tracks, 2,443 sound effects
* Every decision window (select / place / position / chain / sort / material / option / yes-no)
* Survival crafting: Card Case → Duel Field → AI Deck / Music Settings
* Self-contained: no MDPro3 or ygopro install required

**Requirements**: Minecraft 1.20.1 + Forge 47.4.20 (any 47.x) · Windows x64 · client + server

**First launch** unpacks its assets into `<game dir>/mdpromod/` (~26,500 files / 396 MB / ~35 s).
Progress shows on the action bar; deleting the folder is safe, it gets rebuilt from the jar.

**Known limitations**: AI opponents only (no human-vs-human yet); a few decision windows are chat-only;
Windows x64 only.

**Credits**: rules and card data from ygopro / ocgcore and MDPro3 (GPL-3.0); art and audio from
Konami's Master Duel and community packs (fan project, please support the official release).
Spine Runtimes (Esoteric Software) and libGDX (Apache-2.0) are bundled — see THIRD-PARTY-NOTICES.md.
```

## 六、发布前自查

1. **提交身份**：提交前确认 `git config user.name` / `user.email` 是你自己的（别用机器上遗留的配置）。
2. **README 里的链接**：`Releases` 是相对链接，会自动指向你的仓库；`mods.toml` 里的
   `displayURL` / `issueTrackerURL` 还是注释状态，想加就填上（改完要重新构建才生效）。
3. **素材**：仓库里没有 Konami 的音频（168 MB）与卡图/立绘；`cards.json` 还在。
   对这一点有疑虑的话，最稳的是再把它移出去并在 README 里写清"自备素材"。
4. **第三方声明**：`THIRD-PARTY-NOTICES.md` 已含 Spine 的全文（它的许可要求随产品附带），
   发布 jar 时把它一起放进 Release 说明或仓库里即可。
5. **不要 `git add -f`** 那些被忽略的目录；真要连素材一起开源，把 `.gitignore` 里对应的
   那几行注释掉再 `git add`，同时心里有数：GitHub 上这类项目有被 DMCA 下架的先例。
