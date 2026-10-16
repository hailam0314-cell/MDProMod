# MDPro3 native headless smoke tool

Standalone, read-only smoke test for the installed MDPro3 native rule core. It does not modify `D:\MyCardLibrary\mdpro3`.

The program loads the installed `ocgcore.dll`, reads the installed `cards.cdb` through the bundled SQLite DLL, reads `Data/script.zip`, creates a duel with two deterministic decks, and auto-answers core selection messages. It is intentionally a smoke test, not a full AI and not a network server.

Build/run from PowerShell:

```powershell
$env:DOTNET_CLI_HOME='D:\MDProMod\cache\dotnet-headless'
& D:\MDProMod\tools\dotnet\dotnet.exe run --project D:\MDProMod\tools\headless_smoke\headless_smoke.csproj --no-restore
```

Output is JSON. A successful start message, phase messages and bounded end condition confirm that the installed native core can be driven headlessly.
