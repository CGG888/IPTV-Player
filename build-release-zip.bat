@echo off
setlocal enabledelayedexpansion

:: ============================================
:: SrcBox Release Build Script
:: Builds a clean, redistributable ZIP package
:: ============================================

:: Get script directory (handles spaces in path)
set "SCRIPT_DIR=%~dp0"
set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"

:: Project paths
set "PROJECT_FILE=%SCRIPT_DIR%\LibmpvIptvClient.csproj"
set "OUTPUT_DIR=%SCRIPT_DIR%\Output"
set "PUBLISH_DIR=%OUTPUT_DIR%\publish"
set "STAGE_DIR=%OUTPUT_DIR%\stage"

:: Extract version from csproj using PowerShell
echo Extracting version from project file...
for /f "usebackq tokens=*" %%i in (`powershell -noprofile -command "(Select-String -Path '%SCRIPT_DIR%\LibmpvIptvClient.csproj' -Pattern '<Version>(.*)</Version>' | Select-Object -First 1).Matches.Groups[1].Value"`) do set "VERSION=%%i"

:: Validate version
if not defined VERSION (
    echo ERROR: Could not extract version from csproj!
    echo Please check that LibmpvIptvClient.csproj contains a <Version> tag.
    exit /b 1
)

echo Version detected: %VERSION%

:: Package name
set "ZIP_NAME=SrcBox-v%VERSION%.zip"
set "ZIP_PATH=%OUTPUT_DIR%\%ZIP_NAME%"

echo ============================================
echo SrcBox Release Build Script
echo Version: %VERSION%
echo ============================================
echo.

:: Step 1: Clean previous build
echo [1/6] Cleaning previous build...
if exist "%PUBLISH_DIR%" (
    rmdir /s /q "%PUBLISH_DIR%" 2>nul
)
if exist "%STAGE_DIR%" (
    rmdir /s /q "%STAGE_DIR%" 2>nul
)
if exist "%ZIP_PATH%" (
    del /q "%ZIP_PATH%" 2>nul
)
echo Done.
echo.

:: Step 2: Create output directories
echo [2/6] Creating output directories...
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
if not exist "%STAGE_DIR%" mkdir "%STAGE_DIR%"
echo Done.
echo.

:: Step 3: Build and publish
echo [3/6] Building and publishing (Release configuration)...
pushd "%SCRIPT_DIR%"
dotnet publish LibmpvIptvClient.csproj -c Release -o "%PUBLISH_DIR%" --no-self-contained
if errorlevel 1 (
    echo ERROR: Build failed!
    popd
    goto :error
)
popd
echo Done.
echo.

:: Step 4: Copy files to staging directory
echo [4/6] Copying files to staging directory...
if not exist "%STAGE_DIR%\SrcBox" mkdir "%STAGE_DIR%\SrcBox"

:: Use xcopy for reliable copying with junctions handling
xcopy /e /i /y "%PUBLISH_DIR%\*" "%STAGE_DIR%\SrcBox\" >nul 2>&1

if errorlevel 1 (
    echo ERROR: File copy failed!
    goto :error
)
echo Done.
echo.

:: Step 5: Clean debug files
echo [5/6] Cleaning debug files (.pdb, .xml, etc.)...

:: Clean .pdb files
for /r "%STAGE_DIR%\SrcBox" %%f in (*.pdb) do (
    if exist "%%f" del /q "%%f" 2>nul
)

:: Clean .xml files
for /r "%STAGE_DIR%\SrcBox" %%f in (*.xml) do (
    if exist "%%f" del /q "%%f" 2>nul
)

:: Clean .gitignore
if exist "%STAGE_DIR%\SrcBox\.gitignore" del /q "%STAGE_DIR%\SrcBox\.gitignore" 2>nul

:: Clean .gitattributes
if exist "%STAGE_DIR%\SrcBox\.gitattributes" del /q "%STAGE_DIR%\SrcBox\.gitattributes" 2>nul

:: Clean VS user files
for /r "%STAGE_DIR%\SrcBox" %%f in (*.user) do (
    if exist "%%f" del /q "%%f" 2>nul
)
for /r "%STAGE_DIR%\SrcBox" %%f in (*.suo) do (
    if exist "%%f" del /q "%%f" 2>nul
)

:: Clean Thumbs.db
for /r "%STAGE_DIR%\SrcBox" %%f in (Thumbs.db) do (
    if exist "%%f" del /q "%%f" 2>nul
)

:: Clean thumbs.db (case insensitive on some systems)
for /r "%STAGE_DIR%\SrcBox" %%f in (thumbs.db) do (
    if exist "%%f" del /q "%%f" 2>nul
)

echo Done.
echo.

:: Step 6: Create ZIP package
echo [6/6] Creating ZIP package...

:: Use PowerShell to create ZIP (available on all modern Windows)
powershell -command "Compress-Archive -Path '%STAGE_DIR%\SrcBox\*' -DestinationPath '%ZIP_PATH%' -Force"

if errorlevel 1 (
    echo ERROR: ZIP creation failed!
    goto :error
)

if not exist "%ZIP_PATH%" (
    echo ERROR: ZIP file was not created!
    goto :error
)
echo Done.
echo.

:: Cleanup staging and publish directories
echo Cleaning temporary files...
if exist "%STAGE_DIR%" rmdir /s /q "%STAGE_DIR%" 2>nul
if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%" 2>nul

echo.
echo ============================================
echo Build completed successfully!
echo Output: %ZIP_PATH%
echo ============================================
goto :end

:error
echo.
echo ============================================
echo Build failed! Please check the errors above.
echo ============================================
exit /b 1

:end
endlocal
