@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..\..") do set "WORKSPACE_ROOT=%%~fI"
set "BUILDER_IMAGE=%OCTARYN_PODMAN_BUILD_IMAGE%"
if "%BUILDER_IMAGE%"=="" set "BUILDER_IMAGE=%OCTARYN_ARCH_BUILDER_IMAGE%"
if "%BUILDER_IMAGE%"=="" set "BUILDER_IMAGE=localhost/octaryn-arch-builder:latest"
set "CONTAINERFILE=%WORKSPACE_ROOT%\tools\podman\Containerfile.arch-build"
set "TARGET_ARCH=%OCTARYN_TARGET_ARCH%"
if "%TARGET_ARCH%"=="" set "TARGET_ARCH=x64"
set "ACTION=%~1"
if "%ACTION%"=="" set "ACTION=status"

if "%ACTION%"=="--help" goto usage
if "%ACTION%"=="-h" goto usage
if "%ACTION%"=="help" goto usage
if "%ACTION%"=="status" goto status
where podman >nul 2>nul
if errorlevel 1 (
  echo [error] missing required tool: podman 1>&2
  exit /b 1
)

call :ensure_builder_image || exit /b 1

if "%ACTION%"=="list-presets" (
  call :run_builder cmake --list-presets=build
  exit /b !errorlevel!
)
if "%ACTION%"=="configure" (
  call :validate_preset "%~2" || exit /b 2
  call :run_builder bash tools/build/cmake_configure.sh "%~2"
  exit /b !errorlevel!
)
if "%ACTION%"=="build" (
  set "PRESET=%~2"
  call :validate_preset "!PRESET!" || exit /b 2
  if "%~3"=="--target" (
    call :run_builder bash tools/build/cmake_build.sh "!PRESET!" --target "%~4"
  ) else (
    call :run_builder bash tools/build/cmake_build.sh "!PRESET!"
  )
  exit /b !errorlevel!
)
if "%ACTION%"=="validate" (
  set "PRESET=%~2"
  call :validate_preset "!PRESET!" || exit /b 2
  call :run_builder bash tools/build/cmake_build.sh "!PRESET!" --target octaryn_validate_all
  exit /b !errorlevel!
)
if "%ACTION%"=="build-all" (
  for %%P in (debug-linux release-linux debug-windows release-windows) do (
    call :run_builder bash tools/build/cmake_configure.sh "%%P" || exit /b !errorlevel!
    call :run_builder bash tools/build/cmake_build.sh "%%P" --target octaryn_all || exit /b !errorlevel!
  )
  exit /b 0
)

goto usage_error

:usage
echo Usage: podman_build.bat ^<command^> [args]
echo Commands: status, list-presets, configure ^<preset^>, build ^<preset^> [args...], validate ^<preset^>, build-all
exit /b 0

:usage_error
call :usage 1>&2
exit /b 2

:status
echo host=windows
echo workspace=%WORKSPACE_ROOT%
echo target_arch=%TARGET_ARCH%
echo builder_image=%BUILDER_IMAGE%
where podman >nul 2>nul
if errorlevel 1 (
  echo podman=missing
  echo builder_image_status=unavailable
  exit /b 0
)
for /f "usebackq delims=" %%P in (`where podman`) do (
  echo podman=%%P
  goto status_image
)
:status_image
podman image exists "%BUILDER_IMAGE%" >nul 2>nul
if errorlevel 1 (
  echo builder_image_status=missing
) else (
  echo builder_image_status=ready
)
exit /b 0

:validate_preset
if "%~1"=="debug-linux" exit /b 0
if "%~1"=="release-linux" exit /b 0
if "%~1"=="debug-windows" exit /b 0
if "%~1"=="release-windows" exit /b 0
echo [error] unsupported preset: %~1 1>&2
echo [error] expected one of: debug-linux release-linux debug-windows release-windows 1>&2
exit /b 2

:ensure_builder_image
if not "%OCTARYN_PODMAN_REBUILD%"=="1" (
  podman image exists "%BUILDER_IMAGE%" >nul 2>nul
  if not errorlevel 1 exit /b 0
)
if not exist "%CONTAINERFILE%" (
  echo [error] missing Podman builder Containerfile: %CONTAINERFILE% 1>&2
  exit /b 1
)
podman build --pull=missing -t "%BUILDER_IMAGE%" -f "%CONTAINERFILE%" "%WORKSPACE_ROOT%\tools\podman"
exit /b %ERRORLEVEL%

:run_builder
podman run --rm ^
  --volume "%WORKSPACE_ROOT%:/workspace" ^
  --workdir /workspace ^
  --env "OCTARYN_WORKSPACE_ROOT=/workspace" ^
  --env "OCTARYN_TARGET_ARCH=%TARGET_ARCH%" ^
  --env "OCTARYN_WINDOWS_CLANG_ROOT=%OCTARYN_WINDOWS_CLANG_ROOT%" ^
  --env "OCTARYN_LINUX_ARM64_SYSROOT=%OCTARYN_LINUX_ARM64_SYSROOT%" ^
  "%BUILDER_IMAGE%" ^
  %*
exit /b %ERRORLEVEL%
