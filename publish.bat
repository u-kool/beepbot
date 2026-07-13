@echo off
cd /d "%~dp0"
taskkill /f /im beepbot.exe 2>nul
timeout /t 1 /nobreak >nul
del "publish\beepbot.exe" 2>nul
dotnet publish -c Release --no-self-contained -r win-x64 -p:PublishSingleFile=true --output publish
del "publish\beepbot.pdb" 2>nul
if not exist "publish\sounds" mkdir "publish\sounds"
xcopy /y /e "sounds\*" "publish\sounds\" >nul
echo.
echo If beepbot.exe doesn't start, install .NET 8 Desktop Runtime:
echo https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-desktop-8.0.18-windows-x64-installer
echo.
pause
