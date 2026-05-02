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

call :refresh_path
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

"%PYTHON_CMD%" -c "from PySide6 import QtCore, QtGui, QtWidgets" >nul 2>nul
if errorlevel 1 (
  echo [error] PySide6 is not importable by the native Python.
  exit /b 1
)

echo [setup] Windows host build environment ready for %REPO_ROOT%
exit /b 0

:usage
echo Usage: windows_build_environment.bat [--yes]
echo Installs/repairs host tools for the Podman-first Octaryn build environment on Windows.
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

:find_winget
where winget >nul 2>nul
if not errorlevel 1 (
  set "WINGET_CMD=winget"
  exit /b 0
)
if exist "%LocalAppData%\Microsoft\WindowsApps\winget.exe" (
  set "WINGET_CMD=%LocalAppData%\Microsoft\WindowsApps\winget.exe"
  exit /b 0
)
exit /b 1

:refresh_path
set "PATH=%PATH%;%LocalAppData%\Microsoft\WindowsApps;%ProgramFiles%\Git\cmd;%ProgramFiles%\RedHat\Podman;%LocalAppData%\Programs\Python\Python313;%LocalAppData%\Programs\Python\Python313\Scripts;%AppData%\Python\Python313\Scripts;%ProgramFiles%\Python313;%ProgramFiles%\Python313\Scripts"
exit /b 0

:winget_install
call :find_winget
if errorlevel 1 (
  echo [error] %~2 is missing and winget is not available to install %~1.
  exit /b 1
)
echo [setup] installing %~1 with winget
if /I "%INSTALL_MODE%"=="yes" (
  "%WINGET_CMD%" install --id %~1 --exact --source winget --silent --force --accept-package-agreements --accept-source-agreements
) else (
  "%WINGET_CMD%" install --id %~1 --exact --source winget --force --accept-package-agreements --accept-source-agreements
)
set "WINGET_RESULT=%ERRORLEVEL%"
call :refresh_path
exit /b %WINGET_RESULT%

:repair_winget_package
call :find_winget
if errorlevel 1 exit /b 1
echo [setup] repairing stale package registration for %~1
"%WINGET_CMD%" uninstall --id %~1 --silent --accept-source-agreements >nul 2>nul
"%WINGET_CMD%" install --id %~1 --exact --source winget --silent --force --accept-package-agreements --accept-source-agreements
set "REPAIR_RESULT=%ERRORLEVEL%"
call :refresh_path
exit /b %REPAIR_RESULT%

:ensure_tool
call :refresh_path
where %~2 >nul 2>nul
if not errorlevel 1 exit /b 0
call :winget_install %~1 %~2
if errorlevel 1 call :repair_winget_package %~1 %~2
if errorlevel 1 exit /b 1
call :refresh_path
where %~2 >nul 2>nul
if not errorlevel 1 exit /b 0
echo [setup] %~2 is still missing after install; forcing repair.
call :repair_winget_package %~1 %~2
if errorlevel 1 exit /b 1
call :refresh_path
where %~2 >nul 2>nul
if errorlevel 1 (
  echo [error] %~2 is still missing after repair.
  exit /b 1
)
exit /b 0

:ensure_python
call :refresh_path
call :find_python
if not errorlevel 1 exit /b 0
call :winget_install Python.Python.3.13 python
if errorlevel 1 call :repair_winget_package Python.Python.3.13 python
if errorlevel 1 exit /b 1
call :find_python
if not errorlevel 1 exit /b 0
echo [setup] Python is still missing after install; forcing repair.
call :repair_winget_package Python.Python.3.13 python
if errorlevel 1 exit /b 1
call :find_python
if errorlevel 1 (
  echo [error] Python is still missing after repair.
  exit /b 1
)
exit /b 0

:find_python
call :refresh_path
set "PYTHON_CMD="
for /f "usebackq delims=" %%P in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$candidates=@($env:LOCALAPPDATA+'\Programs\Python\Python313\python.exe', $env:USERPROFILE+'\AppData\Local\Programs\Python\Python313\python.exe', $env:ProgramFiles+'\Python313\python.exe'); foreach($p in $candidates){ if(Test-Path -LiteralPath $p){ & $p -c 'import sys; raise SystemExit(0 if sys.version_info >= (3,10) else 1)' *> $null; if($LASTEXITCODE -eq 0){ Write-Output $p; exit 0 } } }; $cmd=Get-Command python -ErrorAction SilentlyContinue; if($cmd){ & $cmd.Source -c 'import sys; raise SystemExit(0 if sys.version_info >= (3,10) else 1)' *> $null; if($LASTEXITCODE -eq 0){ Write-Output $cmd.Source; exit 0 } }; exit 1"`) do set "PYTHON_CMD=%%~P"
if defined PYTHON_CMD exit /b 0
exit /b 1

:ensure_pyside6
"%PYTHON_CMD%" -c "from PySide6 import QtCore, QtGui, QtWidgets" >nul 2>nul
if not errorlevel 1 exit /b 0
echo [setup] installing PySide6 for native Python user site
"%PYTHON_CMD%" -m ensurepip --upgrade >nul 2>nul
"%PYTHON_CMD%" -m pip install --user --upgrade PySide6
if errorlevel 1 exit /b 1
"%PYTHON_CMD%" -c "from PySide6 import QtCore, QtGui, QtWidgets" >nul 2>nul
exit /b %ERRORLEVEL%

:ensure_podman_machine
call :refresh_path
podman machine inspect >nul 2>nul
if errorlevel 1 (
  echo [setup] initializing Podman machine
  podman machine init
  if errorlevel 1 exit /b 1
)
echo [setup] starting Podman machine
podman machine start >nul 2>nul
for /L %%I in (1,1,30) do (
  podman info >nul 2>nul
  if not errorlevel 1 exit /b 0
  timeout /t 2 /nobreak >nul
)
echo [error] Podman machine did not start cleanly.
exit /b 1
