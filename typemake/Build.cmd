@echo off

setlocal
if "%SUB_NO_PAUSE_SYMBOL%"=="1" set NO_PAUSE_SYMBOL=1
if /I "%COMSPEC%" == %CMDCMDLINE% set NO_PAUSE_SYMBOL=1
set SUB_NO_PAUSE_SYMBOL=1

for %%p in (Enterprise Professional Community BuildTools) do (
  if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\%%p" (
    set VSDir="%ProgramFiles(x86)%\Microsoft Visual Studio\2017\%%p"
  )
)
set VSDir=%VSDir:"=%
echo VSDir=%VSDir%

set EXIT_CODE=0
"%VSDir%\MSBuild\15.0\Bin\MSBuild.exe" Src\TypeMake.sln /t:Rebuild /p:Configuration=Release
set /A EXIT_CODE^|=%ERRORLEVEL%

if not "%NO_PAUSE_SYMBOL%"=="1" pause
exit /b %EXIT_CODE%
