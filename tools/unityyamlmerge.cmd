
@echo off
setlocal

set "UNITY_MERGE=C:\Program Files\Unity\Hub\Editor\6000.1.4f1\Editor\Data\Tools\UnityYAMLMerge.exe"
set "TEMP_DIR=%TEMP%\unityyamlmerge_%RANDOM%_%RANDOM%"

mkdir "%TEMP_DIR%" || exit /b 1

copy /y "%~1" "%TEMP_DIR%\base.unity" >nul || goto :error
copy /y "%~2" "%TEMP_DIR%\local.unity" >nul || goto :error
copy /y "%~3" "%TEMP_DIR%\remote.unity" >nul || goto :error

"%UNITY_MERGE%" merge -p "%TEMP_DIR%\base.unity" "%TEMP_DIR%\remote.unity" "%TEMP_DIR%\local.unity" "%TEMP_DIR%\merged.unity"
if errorlevel 1 goto :error

copy /y "%TEMP_DIR%\merged.unity" "%~4" >nul || goto :error

rmdir /s /q "%TEMP_DIR%"
exit /b 0

:error
rmdir /s /q "%TEMP_DIR%" 2>nul
exit /b 1
