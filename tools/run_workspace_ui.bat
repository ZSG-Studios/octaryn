@echo off
setlocal EnableExtensions EnableDelayedExpansion

if /I "%~1"=="--help" goto usage
if /I "%~1"=="-h" goto usage
if /I "%~1"=="help" goto usage

call :find_repo_root "%~dp0"
if errorlevel 1 goto fail

if not exist "%REPO_ROOT%\logs\tools" mkdir "%REPO_ROOT%\logs\tools" >nul 2>nul
set "WORKSPACE_UI_LOG=%REPO_ROOT%\logs\tools\workspace-ui-windows.log"

echo [workspace-ui] log: %WORKSPACE_UI_LOG%
call :ensure_admin %*
if errorlevel 1 goto fail

call :main %* 1>>"%WORKSPACE_UI_LOG%" 2>>&1
set "WORKSPACE_UI_EXIT_CODE=%ERRORLEVEL%"
if not "%WORKSPACE_UI_EXIT_CODE%"=="0" goto fail
exit /b 0

:main
set "IMAGE_NAME=localhost/octaryn-arch-builder:latest"
if not "%OCTARYN_ARCH_BUILDER_IMAGE%"=="" set "IMAGE_NAME=%OCTARYN_ARCH_BUILDER_IMAGE%"
set "BUILDER_VERSION=20260421-2"

echo [workspace-ui] starting one-shot setup from %REPO_ROOT%
call "%REPO_ROOT%\tools\setup\windows_build_environment.bat" --yes
if errorlevel 1 exit /b 1

call :find_python
if errorlevel 1 (
  echo [error] Python 3.10 or newer is required after setup.
  exit /b 1
)

call :check_pyside6
if errorlevel 1 (
  echo [error] PySide6 is not importable by the native Python after setup.
  exit /b 1
)

call :refresh_path
podman info >nul 2>nul
if errorlevel 1 (
  echo [error] Podman is not ready after setup.
  exit /b 1
)

echo [workspace-ui] building or refreshing Arch builder image %IMAGE_NAME%
podman build --build-arg "OCTARYN_ARCH_BUILDER_VERSION=%BUILDER_VERSION%" -t "%IMAGE_NAME%" -f "%REPO_ROOT%\tools\podman\Containerfile.arch-build" "%REPO_ROOT%\tools\podman"
if errorlevel 1 exit /b 1

echo [workspace-ui] validating workspace mount in %IMAGE_NAME%
podman run --rm -v "%REPO_ROOT%:/workspace" --workdir /workspace "%IMAGE_NAME%" bash -lc "test -f CMakePresets.json && test -f tools/ui/workspace_control_app.py && mkdir -p logs/tools && test -w logs/tools"
if errorlevel 1 exit /b 1

echo [workspace-ui] launching workspace control UI
"%PYTHON_CMD%" "%REPO_ROOT%\tools\ui\workspace_control_app.py"
exit /b %ERRORLEVEL%

:usage
echo Usage: run_workspace_ui.bat
echo Installs/repairs required Windows host tools as Administrator, prepares the Podman builder, and launches the workspace UI.
echo A log is written to logs\tools\workspace-ui-windows.log.
exit /b 0

:ensure_admin
if /I "%~1"=="--elevated" exit /b 0
net session >nul 2>nul
if not errorlevel 1 exit /b 0
echo [workspace-ui] requesting Administrator rights for one-shot install/repair...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -Verb RunAs -FilePath 'cmd.exe' -ArgumentList '/k','call ""%~f0"" --elevated'"
exit /b 0

:fail
set "WORKSPACE_UI_EXIT_CODE=%WORKSPACE_UI_EXIT_CODE%"
if "%WORKSPACE_UI_EXIT_CODE%"=="" set "WORKSPACE_UI_EXIT_CODE=%ERRORLEVEL%"
if "%WORKSPACE_UI_EXIT_CODE%"=="0" set "WORKSPACE_UI_EXIT_CODE=1"
echo.
echo [workspace-ui] failed or relaunched with exit code %WORKSPACE_UI_EXIT_CODE%.
echo [workspace-ui] Log: %WORKSPACE_UI_LOG%
echo [workspace-ui] Leaving this window open so you can read the error above.
pause
exit /b %WORKSPACE_UI_EXIT_CODE%

:refresh_path
set "PATH=%PATH%;%LocalAppData%\Microsoft\WindowsApps;%ProgramFiles%\Git\cmd;%ProgramFiles%\RedHat\Podman;%LocalAppData%\Programs\Python\Python313;%LocalAppData%\Programs\Python\Python313\Scripts;%AppData%\Python\Python313\Scripts;%ProgramFiles%\Python313;%ProgramFiles%\Python313\Scripts"
exit /b 0

:find_repo_root
set "CURRENT=%~f1"
if "%CURRENT:~-1%"=="\" set "CURRENT=%CURRENT:~0,-1%"
:find_repo_root_loop
if exist "%CURRENT%\CMakePresets.json" if exist "%CURRENT%\tools\ui\workspace_control_app.py" (
  set "REPO_ROOT=%CURRENT%"
  exit /b 0
)
for %%I in ("%CURRENT%\..") do set "PARENT=%%~fI"
if /I "%PARENT%"=="%CURRENT%" (
  echo [error] could not find Octaryn workspace root from %~f1
  exit /b 1
)
set "CURRENT=%PARENT%"
goto find_repo_root_loop

:find_python
call :refresh_path
set "PYTHON_CMD="
for /f "usebackq delims=" %%P in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$candidates=@($env:LOCALAPPDATA+'\Programs\Python\Python313\python.exe', $env:USERPROFILE+'\AppData\Local\Programs\Python\Python313\python.exe', $env:ProgramFiles+'\Python313\python.exe'); foreach($p in $candidates){ if(Test-Path -LiteralPath $p){ & $p -c 'import sys; raise SystemExit(0 if sys.version_info >= (3,10) else 1)' *> $null; if($LASTEXITCODE -eq 0){ Write-Output $p; exit 0 } } }; $cmd=Get-Command python -ErrorAction SilentlyContinue; if($cmd){ & $cmd.Source -c 'import sys; raise SystemExit(0 if sys.version_info >= (3,10) else 1)' *> $null; if($LASTEXITCODE -eq 0){ Write-Output $cmd.Source; exit 0 } }; exit 1"`) do set "PYTHON_CMD=%%~P"
if defined PYTHON_CMD exit /b 0
exit /b 1

:check_pyside6
"%PYTHON_CMD%" -c "from PySide6 import QtCore, QtGui, QtWidgets" >nul 2>nul
exit /b %ERRORLEVEL%
