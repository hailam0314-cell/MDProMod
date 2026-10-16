# Third-party notices

MDProMod itself is licensed **GPL-3.0-or-later** (see `LICENSE`). The components below are **not** covered
by that licence — they carry their own terms, and the ones we redistribute come with their notices here.

## Rules engine, card database and Lua scripts — ygopro / ocgcore

The duel host drives **ocgcore**, the open-source Yu-Gi-Oh! rules engine from the ygopro project
(`ocgcore.dll`, `ygoserver.dll`, `sqlite3.dll`, `cards.cdb`, `lflist.conf`), and loads card scripts from
`Data/script.zip`. Those scripts are **GPL-2.0**; their licence file travels inside the archive
(`script/LICENSE`). These binaries are redistributed as they ship with a MDPro3 release build.

## MDPro3 (GPL-3.0)

Reference implementation, asset sources and the Windows build the host binaries come from:
`https://code.moenext.com/sherry_chaos/MDPro3` (mirrored on GitHub).

The card database (`cards.cdb` / `cards.json`) and the *card art, closeups, sound effects, BGM and the
cut-in Spine animations* used by the released jar originate from this project and from the official game
assets it references. **They are not covered by this repository's licence** — see the "Assets" section of
the README, and support the official release.

`tools/windbot_standalone/` contains WindBot sources extracted from a MDPro3 build (original project:
IceYGO/WindBot). The host glue around them (`tools/duel_host/`) is our own code.

## Spine Runtimes — Esoteric Software LLC

Redistributed inside the mod jar (`com.esotericsoftware.spine.*`) and used to play the cut-in animations.
This notice must accompany any redistribution:

> # Spine Runtimes License Agreement
> Last updated July 28, 2023. Replaces all prior versions.
>
> Copyright (c) 2013-2023, Esoteric Software LLC
>
> Integration of the Spine Runtimes into software or otherwise creating derivative works of the Spine
> Runtimes is permitted under the terms and conditions of Section 2 of the Spine Editor License Agreement:
> http://esotericsoftware.com/spine-editor-license
>
> Otherwise, it is permitted to integrate the Spine Runtimes into software or otherwise create derivative
> works of the Spine Runtimes (collectively, "Products"), provided that each user of the Products must
> obtain their own Spine Editor license and redistribution of the Products in any form must include this
> license and copyright notice.
>
> THE SPINE RUNTIMES ARE PROVIDED BY ESOTERIC SOFTWARE LLC "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
> INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
> PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL ESOTERIC SOFTWARE LLC BE LIABLE FOR ANY DIRECT, INDIRECT,
> INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
> SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED
> AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
> OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THE SPINE RUNTIMES, EVEN IF ADVISED OF THE POSSIBILITY
> OF SUCH DAMAGE.

## libGDX (Apache License 2.0)

Parts of libGDX (`com.badlogic.gdx.*`, with our own `Texture` / `Pixmap` stubs) are bundled in the mod jar.
Licensed under the Apache License, Version 2.0 — `http://www.apache.org/licenses/LICENSE-2.0`. The two
upstream jars live in `mod/libs/`.

## Mono.Data.Sqlite (MIT) and other reference assemblies

`refs/` holds the DLLs needed to compile the C# host: `Mono.Data.Sqlite` (MIT, from the Mono project) and
the Unity assemblies referenced by the WindBot host sources (`UnityEngine.CoreModule`, `UnityEngine.UI`,
`Unity.InputSystem`, `Newtonsoft.Json` — MIT). They are compile-time references only.

## Minecraft Forge / MCP

The mod is built against Forge 47.4.20 using the official Mojang mappings. `mod/LICENSE.txt`,
`mod/CREDITS.txt` and `mod/changelog.txt` are the files that ship with the Forge MDK and describe the
Forge/FML licence (LGPL-2.1) and the MCP data terms.
