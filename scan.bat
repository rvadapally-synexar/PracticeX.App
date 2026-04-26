@echo off
REM Run the desktop agent against the Synexar fundraising folder.
REM Usage: scan.bat <connection-id> [extra agent flags]
REM Example: scan.bat 11111111-2222-3333-4444-555555555555 --dry-run

setlocal

if "%~1"=="" (
  echo Usage: scan.bat ^<connection-id^> [extra agent flags]
  echo.
  echo Get a connection id with:
  echo   curl.exe -k https://localhost:7100/api/sources/connections
  exit /b 2
)

set CONN=%~1
shift

set ROOT=C:\Users\harek\SYNEXAR INC\Fundraising
set AGENT=%~dp0src\PracticeX.Agent.Cli\bin\Release\net9.0\publish\practicex-agent.exe

if not exist "%AGENT%" (
  echo Agent not built. Run:
  echo   dotnet publish src\PracticeX.Agent.Cli -c Release
  exit /b 2
)

REM %1..%9 still hold the trailing flags after the shift above.
"%AGENT%" scan --root "%ROOT%" --connection-id %CONN% --insecure %1 %2 %3 %4 %5 %6 %7 %8 %9

endlocal
