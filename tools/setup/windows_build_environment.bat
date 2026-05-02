@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "INSTALL_MODE=ask"
if /I "%~1"=="--yes" set "INSTALL_MODE=yes"
if /I "%~1"=="-y" set "INSTALL_MODE=yes"
if /I "%~1"=="--help" goto usage
if /I "%~1"=="-h" goto usage

call :find_repo_root "%~dp0\..\.."
if errorlevel 1 exit /b 1

call :ensure_tool "Git.Git" "git"
if errorlevel 1 exit /b 1

call :ensure_python
if errorlevel 1 exit /b 1

call :ensure_tool "RedHat.Podman" "podman"
if errorlevel 1 exit /b 1

call :ensure_pyside6
if errorlevel 1 exit /b 1

call :ensure_podman_machine
if errorlevel 1 exit /b 1

git --version >nul 2>nul
if errorlevel 1 (
  echo [error] Git is not available after setup.
  exit /b 1
)

podman info >nul 2>nul
if errorlevel 1 (
  echo [error] Podman is not ready after machine setup.
  exit /b 1
)

%PYTHON_CMD% -c "from PySide6 import QtCore, QtGui, QtWidgets" >nul 2>nul
if errorlevel 1 (
  echo [error] PySide6 is not importable by the native Python.
  exit /b 1
)

echo [setup] Windows host build environment ready for %REPO_ROOT%
exit /b 0

:usage
echo Usage: windows_build_environment.bat [--yes]
echo Installs host tools for the Podman-first Octaryn build environment on Windows.
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

:winget_install
where winget >nul 2>nul
if errorlevel 1 (
  echo [error] %~2 is missing and winget is not available to install %~1.
  exit /b 1
)
echo [setup] installing %~1 with winget
if /I "%INSTALL_MODE%"=="yes" (
  winget install --id %~1 --exact --source winget --silent --accept-package-agreements --accept-source-agreements
) else (
  winget install --id %~1 --exact --source winget --accept-package-agreements --accept-source-agreements
)
exit /b %ERRORLEVEL%

:ensure_tool
where %~2 >nul 2>nul
if not errorlevel 1 exit /b 0
call :winget_install %~1 %~2
if errorlevel 1 exit /b 1
where %~2 >nul 2>nul
if errorlevel 1 (
  echo [error] %~2 is still missing. Open a new terminal if winget updated PATH, then rerun this script.
  exit /b 1
)
exit /b 0

:ensure_python
call :find_python
if not errorlevel 1 exit /b 0
call :winget_install Python.Python.3.13 python
if errorlevel 1 exit /b 1
call :find_python
if errorlevel 1 (
  echo [error] Python is still missing. Open a new terminal if winget updated PATH, then rerun this script.
  exit /b 1
)
exit /b 0

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
for %%P in ("%LocalAppData%\Programs\Python\Python313\python.exe" "%ProgramFiles%\Python313\python.exe") do (
  if exist "%%~P" (
    "%%~P" -c "import sys; raise SystemExit(0 if sys.version_info >= (3, 10) else 1)" >nul 2>nul
    if not errorlevel 1 (
      set "PYTHON_CMD="%%~P""
      exit /b 0
    )
  )
)
exit /b 1

:ensure_pyside6
%PYTHON_CMD% -c "from PySide6 import QtCore, QtGui, QtWidgets" >nul 2>nul
if not errorlevel 1 exit /b 0
echo [setup] installing PySide6 for native Python user site
%PYTHON_CMD% -m ensurepip --upgrade >nul 2>nul
%PYTHON_CMD% -m pip install --user --upgrade PySide6
if errorlevel 1 exit /b 1
%PYTHON_CMD% -c "from PySide6 import QtCore, QtGui, QtWidgets" >nul 2>nul
exit /b %ERRORLEVEL%

:ensure_podman_machine
podman machine inspect >nul 2>nul
if errorlevel 1 (
  echo [setup] initializing Podman machine
  podman machine init
  if errorlevel 1 exit /b 1
)
podman machine start >nul 2>nul
podman info >nul 2>nul
if errorlevel 1 (
  echo [error] Podman machine did not start cleanly.
  exit /b 1
)
exit /b 0
