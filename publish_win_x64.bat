@echo off
setlocal
cd /d "%~dp0"

echo Publishing Batman Suit JSON Builder as a Windows x64 single-file EXE...
dotnet publish src\BatmanSuitJsonBuilder\BatmanSuitJsonBuilder.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -p:PublishReadyToRun=false ^
  -o publish\win-x64-single

echo.
echo Done. Release EXE is in: publish\win-x64-single
endlocal
