# Third-party notices

> **Escopo do PeakCraft Local:** esta árvore preserva abaixo os avisos da base SkyCraft. O pacote local não inclui o bundle Prism, o runtime Java, Skyrim ou os jogos. Seus mods compilados ficam em `binarios/`; as licenças e os avisos específicos do Co-op estão em [`referencia-coop/`](referencia-coop/). Java, BepInEx e bibliotecas oficiais são obtidos separadamente pela preparação, com suas respectivas licenças.

SkyCraft is MIT-licensed (see `LICENSE`). A release also contains, or is built from, the following.

## In the Skyrim plugin (`SkyCraft.dll`)

| Component | License | Source |
|---|---|---|
| CommonLibSSE-NG | MIT | https://github.com/alandtse/CommonLibVR/tree/ng |
| spdlog | MIT | https://github.com/gabime/spdlog |
| {fmt} | MIT | https://github.com/fmtlib/fmt |
| xbyak | BSD-3-Clause | https://github.com/herumi/xbyak |
| SimpleIni | MIT | https://github.com/brofield/simpleini |

## In the bundled Minecraft (`SkyCraft-Minecraft.zip`)

| Component | License | Source |
|---|---|---|
| Prism Launcher (unmodified portable Windows build) | GPL-3.0 | https://github.com/PrismLauncher/PrismLauncher (the version is in the bundle's `THIRD-PARTY.txt`) |
| Fabric API | Apache-2.0 | https://github.com/FabricMC/fabric |
| e4mc | MIT | https://github.com/vgskye/e4mc-minecraft-architectury |

The bundle carries Prism Launcher's full license text as `Prism/LICENSE-PrismLauncher.txt`.

## Not included

Minecraft, Java and Fabric Loader aren't included. Prism Launcher downloads them from Mojang,
the Java vendor and FabricMC after the player signs in with a Microsoft account that owns
Minecraft: Java Edition. Skyrim, SKSE and the Address Library aren't included either.

SkyCraft isn't affiliated with or endorsed by Mojang, Microsoft, Bethesda or ZeniMax.
