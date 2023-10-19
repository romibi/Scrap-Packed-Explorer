@echo off

@rem Some sanity checks
where /q msbuild
IF ERRORLEVEL 1 goto :usage

if not exist .\PublishAll.bat goto :usage

@rem Build/Publish calls
@echo Building Scrap Packed Explorer GUI
msbuild ScrapPackedExplorer\ScrapPackedExplorer.csproj -p:Configuration=Release -restore -t:Publish -p:Platform=AnyCPU -p:PublishProfile="ScrapPackedExplorer\Properties\PublishProfiles\ScrapPackedExplorer.exe (x64).pubxml"
msbuild ScrapPackedExplorer\ScrapPackedExplorer.csproj -p:Configuration=Release -restore -t:Publish -p:Platform=x86 -p:PublishProfile="ScrapPackedExplorer\Properties\PublishProfiles\ScrapPackedExplorer.exe (x86).pubxml"
ren "Publish (32bit)\ScrapPackedExplorer.exe" "ScrapPackedExplorer32.exe"

@echo Building Scrap Packed Explorer Combined
msbuild ScrapPackedExplorerCombined\ScrapPackedExplorerCombined.csproj -p:Configuration=Release -restore -t:Publish -p:Platform=AnyCPU -p:PublishProfile="ScrapPackedExplorerCombined\Properties\PublishProfiles\ScrapPackedExplorerCombined.exe (x64).pubxml"
msbuild ScrapPackedExplorerCombined\ScrapPackedExplorerCombined.csproj -p:Configuration=Release -restore -t:Publish -p:Platform=x86 -p:PublishProfile="ScrapPackedExplorerCombined\Properties\PublishProfiles\ScrapPackedExplorerCombined.exe (x86).pubxml"
ren "Publish (32bit)\ScrapPackedExplorerCombined.exe" "ScrapPackedExplorerCombined32.exe"

@echo Building Scrap Packed Explorer CLI
msbuild ScrapPackedExplorerCli\ScrapPackedExplorerCli.csproj -p:Configuration=Release -restore -t:Publish -p:Platform=AnyCPU -p:PublishProfile="ScrapPackedExplorerCli\Properties\PublishProfiles\ScrapPackedExplorerCli.exe (x64).pubxml"
msbuild ScrapPackedExplorerCli\ScrapPackedExplorerCli.csproj -p:Configuration=Release -restore -t:Publish -p:Platform=x86 -p:PublishProfile="ScrapPackedExplorerCli\Properties\PublishProfiles\ScrapPackedExplorerCli.exe (x86).pubxml"
ren "Publish (32bit)\ScrapPackedExplorerCli.exe" "ScrapPackedExplorerCli32.exe"
msbuild ScrapPackedExplorerCli\ScrapPackedExplorerCli.csproj -p:Configuration=Release -restore -t:Publish -p:Platform=AnyCPU -p:PublishProfile="ScrapPackedExplorerCli\Properties\PublishProfiles\ScrapPackedExplorerCli (linux-x86_64).pubxml"

@echo All Publishing done
goto :EOF

:usage
@echo Call this script using VS "Cross Tools Command Prompt" after changing in this projects directory!
