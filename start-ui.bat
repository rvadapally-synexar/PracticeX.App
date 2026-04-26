@echo off
REM Launch the PracticeX Facility Discovery Agent UI.
REM Re-publishes if missing so a fresh checkout works on first run.

setlocal
set UI=%~dp0src\PracticeX.Agent.Ui\bin\Release\net9.0-windows\publish\practicex-agent-ui.exe

if not exist "%UI%" (
  echo Building UI...
  pushd "%~dp0"
  dotnet publish src\PracticeX.Agent.Ui -c Release
  popd
)

start "" "%UI%"
endlocal
