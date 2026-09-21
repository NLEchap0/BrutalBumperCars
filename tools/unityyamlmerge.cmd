@echo off

"C:\Program Files\Unity\Hub\Editor\6000.1.4f1\Editor\Data\Tools\UnityYAMLMerge.exe" merge -p "%~1" "%~3" "%~2" "%~2"

exit /b %ERRORLEVEL%