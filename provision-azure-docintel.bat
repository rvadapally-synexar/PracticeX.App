@echo off
setlocal enabledelayedexpansion

REM ============================================================================
REM PracticeX - Azure Document Intelligence provisioning
REM ============================================================================
REM
REM Creates a resource group + Document Intelligence resource in eastus
REM (HIPAA-eligible region, S0 tier required for HIPAA workloads). Saves the
REM endpoint and key1 to data\azure-docintel-credentials.txt (gitignored) so
REM you can paste them to Claude for the user-secrets wire-up.
REM
REM Safe to re-run. If resources already exist, the script just fetches the
REM keys and writes them to the output file again.
REM
REM Prereqs:
REM   - Azure CLI installed: https://aka.ms/installazurecliwindows
REM   - rvadapally@practicex.ai signed in via az login
REM   - Pay-As-You-Go subscription active (Free Trial blocks S0 tier)
REM ============================================================================

REM ===== Edit these only if you want different names =====
set RG_NAME=rg-practicex-prod
set LOCATION=eastus
set DOCINTEL_NAME=practicex-docintel
REM ========================================================

echo.
echo === PracticeX Azure Document Intelligence provisioning ===
echo.
echo Resource group:  %RG_NAME%
echo Location:        %LOCATION%
echo Resource name:   %DOCINTEL_NAME%
echo.

REM --- Step 1/5: az cli check ---
where az >nul 2>nul
if errorlevel 1 (
    echo [ERROR] Azure CLI not found on PATH.
    echo Install: https://aka.ms/installazurecliwindows
    echo Then restart your terminal and run this script again.
    exit /b 1
)

REM --- Step 2/5: login if needed ---
echo [1/5] Verifying Azure login...
az account show >nul 2>nul
if errorlevel 1 (
    echo Not logged in. Opening browser - sign in as rvadapally@practicex.ai
    az login --only-show-errors
    if errorlevel 1 (
        echo [ERROR] az login failed.
        exit /b 1
    )
)

echo.
echo [2/5] Active subscription:
az account show --output table
echo.
echo If this is the wrong account, press Ctrl+C now and run:
echo     az account set --subscription "^<subscription-id-or-name^>"
echo.
echo Otherwise press any key to continue...
pause >nul

REM --- Step 3/5: resource group (idempotent) ---
echo.
echo [3/5] Creating resource group %RG_NAME% in %LOCATION%...
az group create ^
    --name %RG_NAME% ^
    --location %LOCATION% ^
    --output none ^
    --only-show-errors
if errorlevel 1 (
    echo [ERROR] Failed to create resource group %RG_NAME%.
    exit /b 1
)
echo Resource group ready.

REM --- Step 4/5: Doc Intel resource (idempotent) ---
echo.
echo [4/5] Provisioning Document Intelligence resource %DOCINTEL_NAME%...
az cognitiveservices account show ^
    --name %DOCINTEL_NAME% ^
    --resource-group %RG_NAME% ^
    --output none ^
    --only-show-errors >nul 2>nul
if errorlevel 1 (
    echo Resource does not exist yet. Creating ^(takes ~30 seconds^)...
    az cognitiveservices account create ^
        --name %DOCINTEL_NAME% ^
        --resource-group %RG_NAME% ^
        --kind FormRecognizer ^
        --sku S0 ^
        --location %LOCATION% ^
        --custom-domain %DOCINTEL_NAME% ^
        --yes ^
        --output none ^
        --only-show-errors
    if errorlevel 1 (
        echo.
        echo [ERROR] Doc Intel resource creation failed.
        echo Common causes:
        echo   - Resource name "%DOCINTEL_NAME%" is taken globally.
        echo     Edit DOCINTEL_NAME at the top of this file and re-run.
        echo   - Subscription is Free Trial. S0 tier requires Pay-As-You-Go.
        echo     Upgrade at https://portal.azure.com/#blade/Microsoft_Azure_Billing/BillingMenuBlade/Overview
        echo   - Region quota for Cognitive Services exhausted in eastus.
        echo     Try eastus2, westus2, or southcentralus ^(also HIPAA-eligible^).
        exit /b 1
    )
    echo Doc Intel resource created.
) else (
    echo Resource already exists, fetching keys.
)

REM --- Step 5/5: capture endpoint + key1, write to gitignored file ---
echo.
echo [5/5] Fetching endpoint + key1...

set ENDPOINT=
set KEY1=

for /f "usebackq delims=" %%i in (`az cognitiveservices account show --name %DOCINTEL_NAME% --resource-group %RG_NAME% --query "properties.endpoint" --output tsv 2^>nul`) do set ENDPOINT=%%i
for /f "usebackq delims=" %%i in (`az cognitiveservices account keys list --name %DOCINTEL_NAME% --resource-group %RG_NAME% --query "key1" --output tsv 2^>nul`) do set KEY1=%%i

if "!ENDPOINT!"=="" (
    echo [ERROR] Could not fetch endpoint.
    exit /b 1
)
if "!KEY1!"=="" (
    echo [ERROR] Could not fetch key1.
    exit /b 1
)

if not exist "data" mkdir data
set OUT_FILE=data\azure-docintel-credentials.txt

(
    echo PracticeX - Azure Document Intelligence credentials
    echo Generated: %DATE% %TIME%
    echo.
    echo Endpoint: !ENDPOINT!
    echo Key1:     !KEY1!
    echo.
    echo Resource group: %RG_NAME%
    echo Location:       %LOCATION%
    echo Resource:       %DOCINTEL_NAME%
    echo.
    echo To activate locally, run from project root:
    echo   cd src\PracticeX.Api
    echo   dotnet user-secrets set "DocumentIntelligence:Endpoint" "!ENDPOINT!"
    echo   dotnet user-secrets set "DocumentIntelligence:ApiKey"   "!KEY1!"
    echo   dotnet user-secrets set "DocumentIntelligence:Enabled"  "true"
    echo.
    echo Then add the Eagle GI tenant Guid to DocumentIntelligence:AllowedTenantIds.
) > "%OUT_FILE%"

echo.
echo ============================================================
echo === Provisioning complete ===
echo ============================================================
echo.
echo Endpoint: !ENDPOINT!
echo Key1:     !KEY1!
echo.
echo Saved to: %OUT_FILE%
echo (this file is in data\ which is gitignored - never commit)
echo.
echo Next: paste the endpoint + key1 above into chat with Claude
echo so the user-secrets get configured and Doc Intel goes live.
echo ============================================================
echo.

endlocal
