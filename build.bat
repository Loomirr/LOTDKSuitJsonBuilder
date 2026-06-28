@echo off
setlocal
cd /d "%~dp0"
dotnet build BatmanSuitJsonBuilder.sln -c Debug
endlocal
