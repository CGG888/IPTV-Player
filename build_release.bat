@echo off
echo ========================================================
echo   SrcBox Build Script
echo ========================================================

echo.
echo [1/2] Cleaning previous builds...
if exist "bin\Release\net8.0-windows\win-x64\publish" (
    rmdir /s /q "bin\Release\net8.0-windows\win-x64\publish"
)

echo.
echo [2/2] Publishing application (Self-Contained)...
dotnet publish LibmpvIptvClient.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=false

if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Publish failed!
    pause
    exit /b %errorlevel%
)

:: Rename the executable if needed (handled by AssemblyName in csproj)
if not exist "bin\Release\net8.0-windows\win-x64\publish\SrcBox.exe" (
    echo [WARNING] Expected SrcBox.exe not found. Did you update the csproj AssemblyName?
)

setlocal EnableDelayedExpansion
for /f "usebackq delims=" %%i in (`git remote get-url origin 2^>nul`) do set GIT_ORIGIN_URL=%%i
for /f "usebackq delims=" %%i in (`git describe --tags --dirty --always 2^>nul`) do set GIT_DESCRIBE=%%i

set "REPO_WEB=%GIT_ORIGIN_URL%"
if /i "!REPO_WEB:~0,4!"=="git@" (
  set "REPO_WEB=!REPO_WEB:git@github.com:=https://github.com/!"
)
if not "!REPO_WEB!"=="" (
  set "REPO_WEB=!REPO_WEB:.git=!"
)

if not "!REPO_WEB!"=="" (
  set "GIT_REPO_URL=!REPO_WEB!"
  set "GIT_ISSUES_URL=!REPO_WEB!/issues"
  set "GIT_RELEASES_URL=!REPO_WEB!/releases"
)
endlocal & (
  if defined GIT_ORIGIN_URL set "GIT_ORIGIN_URL=%GIT_ORIGIN_URL%"
  if defined GIT_DESCRIBE set "GIT_DESCRIBE=%GIT_DESCRIBE%"
  if defined GIT_REPO_URL set "GIT_REPO_URL=%GIT_REPO_URL%"
  if defined GIT_ISSUES_URL set "GIT_ISSUES_URL=%GIT_ISSUES_URL%"
  if defined GIT_RELEASES_URL set "GIT_RELEASES_URL=%GIT_RELEASES_URL%"
)

echo.
echo ========================================================
echo   Build Successful!
echo ========================================================
echo.
where iscc >nul 2>&1
if %errorlevel%==0 (
  echo Compiling installer via Inno Setup (iscc)...
  iscc setup.iss
  if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Inno Setup compilation failed.
    pause
    exit /b %errorlevel%
  )
  echo Installer compiled successfully.
  echo Output directory: Output
  echo.
  pause
  exit /b 0
) else (
  echo iscc not found on PATH. Please open 'setup.iss' with Inno Setup Compiler and compile manually.
  echo The following environment variables have been prepared for dynamic metadata:
  echo   GIT_ORIGIN_URL=%GIT_ORIGIN_URL%
  echo   GIT_DESCRIBE=%GIT_DESCRIBE%
  echo   GIT_REPO_URL=%GIT_REPO_URL%
  echo   GIT_ISSUES_URL=%GIT_ISSUES_URL%
  echo   GIT_RELEASES_URL=%GIT_RELEASES_URL%
  echo.
  pause
)
