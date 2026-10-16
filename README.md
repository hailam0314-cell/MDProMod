# MDProMod

**Play a full Yu-Gi-Oh! duel inside Minecraft.** Place a Duel Field, load a Card Case, press the green
button — and duel one of 57 WindBot opponents under the real ocgcore rules, with Master Duel's own summon
sequences, ace-monster cut-ins, monster closeups and music.

* Minecraft **1.20.1** · **Forge 47.4.20** (any 47.x)
* **Windows x64** — the bundled duel host is a Windows/.NET program (it ships with its own runtime)
* Ready-to-play jar: see **[Releases](../../releases)** — fully self-contained, no MDPro3 or ygopro install

## What's in it

| | |
| --- | --- |
| **Cards** | 15,019 cards with the ocgcore rules and card text, fully offline |
| **Opponents** | 57 WindBot decks with names and personalities |
| **Duel field** | 16×14 blocks, 32 zones, real phase buttons, live LP, victory/defeat states |
| **Summons** | Six sequences (Fusion / Synchro / Xyz / Link / Ritual / Pendulum) using Master Duel's own meshes and measured timings |
| **Presentation** | 497 ace-monster cut-ins (real Spine animations), 11,091 closeups, 110 BGM tracks, 2,443 sound effects |
| **Prompts** | Every decision window — select, place, position, chain, sort, material, option, yes/no — as Minecraft-styled panels with card art |
| **Survival** | Crafting recipes for Card Case, Duel Field, AI Deck and Music Settings |

## Install

1. Install Minecraft 1.20.1 with Forge 47.4.20 — on the **client** *and* the **server** (singleplayer
   counts; the duel runs on the server side).
2. Drop `mdpromod-0.1.0.jar` into `mods/`.
3. First launch unpacks its assets into `<game dir>/mdpromod/` — about 26,500 files / 396 MB / ~35 seconds.
   A progress readout scrolls on the action bar and chat says so when it is done. Deleting that folder is
   safe: it is rebuilt from the jar.

## How to duel

1. Right-click the ground with the **Duel Field** — the field appears.
2. Right-click with the **Card Case** to build a deck (craft one first in survival).
3. Sneak-right-click to place the Card Case on your half and an **AI Deck** on the far half.
4. Press the green button → Ready → Start.

## Building from source

```powershell
# Java mod — JDK 17, Gradle wrapper included
cd mod
.\gradlew.bat build                       # -> mod/build/libs/mdpromod-0.1.0.jar

# C# duel host — any .NET 8 SDK (a local copy lives in tools/dotnet)
.\..\tools\dotnet\dotnet.exe publish ..\tools\duel_host\duel_host.csproj -c Release
```

**Assets are not in this repository.** A fresh clone builds a mod without audio, cut-ins and card art; the
[Releases](../../releases) jar has everything pre-bundled. The bundling step (`stageBundle` in
`mod/build.gradle`) collects art, closeups and summon effects from `runtime/`, the host runtime from
`tools/ygo_tcp_probe/staging`, and the host binary from `tools/duel_host/bin/Release/net8.0` — point it at
your own copies with `-Pmdpromod.bundleRoot=<dir>`. How that works, and how to rebuild the asset pipelines
from your own game install, is documented in
[docs/bundled-runtime-2026-10-16.md](docs/bundled-runtime-2026-10-16.md).

## Repository layout

| Path | What it is |
| --- | --- |
| `mod/` | The Java/Forge mod (sources, textures, recipes, sound index) |
| `tools/duel_host/` | The C# host that drives ocgcore and talks to the mod over JSON |
| `tools/windbot_standalone/` | WindBot sources used by the host |
| `tools/cutin_pipeline/`, `tools/audio_pipeline/` | Asset extraction and compression pipelines |
| `tools/ygo_tcp_probe/`, `tools/headless_probe/`, `tools/headless_smoke/` | Protocol probes and headless tests |
| `docs/` | Dated design and verification notes — start with `docs/session-digest.md` |
| `refs/` | Third-party reference DLLs needed to compile the C# host |

## License

GPL-3.0-or-later — see [LICENSE](LICENSE) and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

Yu-Gi-Oh! and its assets belong to Konami. This is a fan project for study and personal use: art, audio
and card data come from Master Duel and community card databases and are **not** covered by this
repository's licence. Please support the official release.

## 中文简介

把《游戏王》的完整对战搬进 Minecraft：摆下决斗盘、放上卡盒，和 57 套 WindBot 人机用真正的 ocgcore
规则打一场。召唤法阵、王牌登场、怪兽立绘、110 首 BGM 全部照搬 Master Duel，而且一个 jar 全自带——
不需要安装 MDPro3 或任何外部程序（Windows x64，客户端与服务端都要装）。玩法、构建与素材说明见上，
详细设计文档在 `docs/`。
