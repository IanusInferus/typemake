@echo off

setlocal
if "%SUB_NO_PAUSE_SYMBOL%"=="1" set NO_PAUSE_SYMBOL=1
if /I "%COMSPEC%" == %CMDCMDLINE% set NO_PAUSE_SYMBOL=1
set SUB_NO_PAUSE_SYMBOL=1

PATH %ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin;%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin;%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin;%PATH%

set EXIT_CODE=0
MSBuild Src\TypeMake.sln /t:Rebuild /p:Configuration=Release
set /A EXIT_CODE^|=%ERRORLEVEL%

if not "%NO_PAUSE_SYMBOL%"=="1" pause
exit /b %EXIT_CODE%
