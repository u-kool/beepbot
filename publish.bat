@echo off
cd /d "%~dp0"
taskkill /f /im beepbot.exe 2>nul
timeout /t 1 /nobreak >nul
del "publish\beepbot.exe" 2>nul
dotnet publish -c Release --no-self-contained -r win-x64 -p:PublishSingleFile=true --output publish
if not exist "publish\sounds" mkdir "publish\sounds"
xcopy /y /e "sounds\*" "publish\sounds\" >nul
pause
