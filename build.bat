@echo off
REM Build EcoNotepad (targets .NET Framework 4.8, requires the .NET SDK).
cd /d "%~dp0"
dotnet build -c Release -v m
if errorlevel 1 (
  echo Build FAILED.
  exit /b 1
)
echo.
echo Build OK. Exe: bin\Release\net48\EcoNotepad.exe
echo Run it from bin\Release\net48\ (notes.json and settings.json are stored next to the exe).
