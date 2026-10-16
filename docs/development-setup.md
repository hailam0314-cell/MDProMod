# MDProMod development setup

- Target: Minecraft Java 1.20.1, Forge 47.4.20, Java 17.
- Project root: D:\MDProMod
- Mod source/build/run: D:\MDProMod\mod
- Downloaded MDPro3 source: D:\MDProMod\MDPro3-main\MDPro3-main
- Extracted upstream references: D:\MDProMod\upstream
- Downloads: D:\MDProMod\downloads
- Gradle cache: D:\MDProMod\cache\gradle
- Maven cache (if used): D:\MDProMod\cache\maven
- Temporary files: D:\MDProMod\tmp
- Existing client: D:\光明MOD服1.12客户端\.minecraft\versions\1.20.1-Forge_47.4.20
- JDK: D:\jdk-17.0.19.10-hotspot

Use process-scoped environment variables for builds: JAVA_HOME, GRADLE_USER_HOME, TEMP, TMP. Also pass -Djava.io.tmpdir=D:\MDProMod\tmp to Java/Gradle. Do not change global environment settings.

Initial implementation should use an isolated development game directory. Deployment into the existing client is a later step.

Upstream: https://code.moenext.com/sherry_chaos/MDPro3
Inspect pinned source, ocgcore, scripts, and cards.cdb together. Large Unity assets, sound, models, and full client downloads are not needed for the initial prototype.

License checks are component-specific: the downloaded mirror has GPL-3.0 at its root and no standalone ocgcore LICENSE. A newer upstream core was observed with MIT licensing; that does not establish the licensing of this mirror's modifications. Other source, card scripts, data, and artwork need separate review before redistribution.

Status: directories prepared; user downloaded the MDPro3 mirror (Version.txt: 1.2.4). Rules analysis, script inventory and limited bundled-DLL probes are saved in docs. No Forge mod implemented or built yet. The script package references a Duel API absent from the bundled core; resolve version compatibility before integration.

Rules analysis: D:\MDProMod\docs\MDPro3决斗规则源码分析.md
Card authoring guide: D:\MDProMod\docs\卡片效果编写指南.md
