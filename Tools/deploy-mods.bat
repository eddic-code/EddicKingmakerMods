@echo off
setlocal
rem Builds all our mods; each project's DeployToGame target copies
rem dll/pdb/Info.json (and Icons for EddicKingmakerTweaks) into the game's
rem Mods folder automatically after a successful build.
rem NOTE: when we create a new mod, add it to the list below and to package-mods.bat.
rem Lives in the repo's Tools\ - work from the repo root.
cd /d "%~dp0.."

for %%M in (EddicKingmakerTweaks EddicKingmakerBuffs EddicKingmakerLoot EddicKingmakerRespec) do (
    echo === Building and deploying %%M ===
    dotnet build "%%M\%%M.slnx" -v q -nologo || goto :error
)

echo.
echo All mods deployed to the game's Mods folder.
exit /b 0

:error
echo.
echo Build FAILED - deployment aborted.
exit /b 1
