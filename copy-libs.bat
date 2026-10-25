@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "GAME_FOLDER=%~1"

:: If game folder not provided as an argument, prompt for it
if "%GAME_FOLDER%"=="" (
    echo [INFO] Game folder path not provided as command line argument.
    echo Usage: %~nx0 ^<path-to-game-folder^>
    echo.
    set /p "GAME_FOLDER=Enter game folder path: "
)

:: Strip quotes if entered with quotes
set "GAME_FOLDER=%GAME_FOLDER:"=%"

if "%GAME_FOLDER%"=="" (
    echo [ERROR] No game folder provided. Aborting.
    exit /b 1
)

if not exist "%GAME_FOLDER%" (
    echo [ERROR] Specified game folder does not exist:
    echo   "%GAME_FOLDER%"
    exit /b 1
)

set "INTEROP_SRC=%GAME_FOLDER%\BepInEx\interop"
if not exist "%INTEROP_SRC%\Assembly-CSharp.dll" (
    echo [ERROR] Missing Assembly-CSharp.dll in:
    echo   "%INTEROP_SRC%"
    echo Please make sure the game has been run at least once with BepInEx so interop assemblies are generated.
    exit /b 1
)

set "PLUGINS_SRC=%GAME_FOLDER%\BepInEx\plugins"
set "DEST_INTEROP=%SCRIPT_DIR%lib\BepInEx\interop"
set "DEST_PLUGINS=%SCRIPT_DIR%lib\BepInEx\plugins"

echo [*] Target directories:
echo     Interop: "%DEST_INTEROP%"
echo     Plugins: "%DEST_PLUGINS%"
echo.

if not exist "%DEST_INTEROP%" mkdir "%DEST_INTEROP%"
if not exist "%DEST_PLUGINS%" mkdir "%DEST_PLUGINS%"

echo [*] Copying interop DLLs (*.dll only, skipping caches)...
copy /y "%INTEROP_SRC%\*.dll" "%DEST_INTEROP%\" >nul
if errorlevel 1 (
    echo [ERROR] Failed to copy interop assemblies.
    exit /b 1
)

if "%PLUGINS_SRC%"=="" (
    echo [WARNING] Could not find SFM_custom_mission.dll in BepInEx\plugins.
    echo           Custom mission mods may fail to build if missing.
) else (
    echo [*] Copying required custom mission plugin DLLs from "%PLUGINS_SRC%"...
    if exist "%PLUGINS_SRC%\SFM_custom_mission.dll" (
        copy /y "%PLUGINS_SRC%\SFM_custom_mission.dll" "%DEST_PLUGINS%\" >nul
        echo     Copied SFM_custom_mission.dll
    )
    if exist "%PLUGINS_SRC%\SFM_custom_mission_v2.dll" (
        copy /y "%PLUGINS_SRC%\SFM_custom_mission_v2.dll" "%DEST_PLUGINS%\" >nul
        echo     Copied SFM_custom_mission_v2.dll
    )
)

echo.
echo [SUCCESS] Required reference files copied to "%SCRIPT_DIR%lib\"!
echo You can now commit the 'lib/' folder to git.
