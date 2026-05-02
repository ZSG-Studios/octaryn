@echo off
setlocal EnableExtensions EnableDelayedExpansion

call :find_repo_root "%~dp0"
if errorlevel 1 exit /b 1

set "IMAGE_NAME=localhost/octaryn-arch-builder:latest"
if not "%OCTARYN_ARCH_BUILDER_IMAGE%"=="" set "IMAGE_NAME=%OCTARYN_ARCH_BUILDER_IMAGE%"
set "BUILDER_VERSION=20260421-2"

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

podman info >nul 2>nul
if errorlevel 1 (
  echo [error] Podman is not ready after setup.
  exit /b 1
)

set "CURRENT_BUILDER_VERSION="
for /f "delims=" %%V in ('podman image inspect "%IMAGE_NAME%" --format "{{ index .Config.Labels \"org.octaryn.arch-builder.version\" }}" 2^>nul') do set "CURRENT_BUILDER_VERSION=%%V"
if not "%CURRENT_BUILDER_VERSION%"=="%BUILDER_VERSION%" (
  echo [workspace-ui] building Arch builder image %IMAGE_NAME%
  podman build --build-arg "OCTARYN_ARCH_BUILDER_VERSION=%BUILDER_VERSION%" -t "%IMAGE_NAME%" -f "%REPO_ROOT%\tools\podman\Containerfile.arch-build" "%REPO_ROOT%\tools\podman"
  if errorlevel 1 exit /b 1
) else (
  echo [workspace-ui] Arch builder image ready: %IMAGE_NAME%
)

if not exist "%REPO_ROOT%\logs\tools" mkdir "%REPO_ROOT%\logs\tools"
echo [workspace-ui] validating workspace mount in %IMAGE_NAME%
podman run --rm -v "%REPO_ROOT%:/workspace" --workdir /workspace "%IMAGE_NAME%" bash -lc "test -f CMakePresets.json && test -f tools/ui/workspace_control_app.py && mkdir -p logs/tools && test -w logs/tools"
if errorlevel 1 exit /b 1

echo [workspace-ui] launching workspace control UI
%PYTHON_CMD% "%REPO_ROOT%\tools\ui\workspace_control_app.py"
exit /b %ERRORLEVEL%

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
where py >nul 2>nul
if not errorlevel 1 (
  py -3 -c "import sys; raise SystemExit(0 if sys.version_info >= (3, 10) else 1)" >nul 2>nul
  if not errorlevel 1 (
    set "PYTHON_CMD=py -3"
    exit /b 0
  )
)
where python >nul 2>nul
if not errorlevel 1 (
  python -c "import sys; raise SystemExit(0 if sys.version_info >= (3, 10) else 1)" >nul 2>nul
  if not errorlevel 1 (
    set "PYTHON_CMD=python"
    exit /b 0
  )
)
exit /b 1

:check_pyside6
%PYTHON_CMD% -c "from PySide6 import QtCore, QtGui, QtWidgets" >nul 2>nul
exit /b %ERRORLEVEL%
