@echo off

setlocal
if "%SUB_NO_PAUSE_SYMBOL%"=="1" (
    set NO_PAUSE_SYMBOL=1
) else (
    set SUB_NO_PAUSE_SYMBOL=1
)

PATH %ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin;%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin;%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin;%ProgramFiles(x86)%\MSBuild\14.0\Bin;%SystemRoot%\Microsoft.NET\Framework\v4.0.30319;%PATH%

set EXIT_CODE=0
MSBuild /t:Rebuild /p:Configuration=Release
set /A EXIT_CODE^|=%ERRORLEVEL%

if not "%NO_PAUSE_SYMBOL%"=="1" pause
exit /b %EXIT_CODE%
