@echo off
setlocal
rem Builds all our mods (Release) and packages one Nexus Mods upload zip per
rem mod (<Mod>-<version>.zip, version read from its Info.json) into this
rem Tools folder. Zip layout: <Mod>\{dll, Info.json, Icons if present,
rem README.md if present} - staged from bin\Release, pdb excluded.
rem NOTE: when we create a new mod, add it to MODS below and to deploy-mods.bat.
rem Lives in the repo's Tools\ - work from the repo root.
cd /d "%~dp0.."

set MODS=EddicKingmakerTweaks EddicKingmakerBuffs EddicKingmakerLoot EddicKingmakerRespec

for %%M in (%MODS%) do (
    echo === Building %%M ^(Release^) ===
    dotnet build "%%M\%%M.slnx" -c Release -v q -nologo || goto :error
)

echo === Packaging ===
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference = 'Stop';" ^
  "foreach ($mod in '%MODS%'.Split(' ')) {" ^
  "  $info = Get-Content ($mod + '\' + $mod + '\Info.json') -Raw | ConvertFrom-Json;" ^
  "  $zip = 'Tools\' + $mod + '-' + $info.Version + '.zip';" ^
  "  $root = Join-Path $env:TEMP ($mod + '-package');" ^
  "  $stage = Join-Path $root $mod;" ^
  "  if (Test-Path $root) { Remove-Item $root -Recurse -Force };" ^
  "  New-Item -ItemType Directory -Force $stage | Out-Null;" ^
  "  Copy-Item ($mod + '\' + $mod + '\bin\Release\*') $stage -Recurse -Exclude *.pdb;" ^
  "  if (Test-Path ($mod + '\README.md')) { Copy-Item ($mod + '\README.md') $stage };" ^
  "  Compress-Archive -Path $stage -DestinationPath $zip -Force;" ^
  "  Remove-Item $root -Recurse -Force;" ^
  "  Write-Host ('Created ' + (Resolve-Path $zip));" ^
  "}" || goto :error

exit /b 0

:error
echo.
echo Packaging FAILED.
exit /b 1
