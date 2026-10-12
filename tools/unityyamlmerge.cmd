@echo off

"C:\Program Files\Unity\Hub\Editor\6000.1.4f1\Editor\Data\Tools\UnityYAMLMerge.exe" merge -p "%~1" "%~3" "%~2" "%~4"

exit /b %ERRORLEVEL%