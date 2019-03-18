@echo off

setlocal
if "%SUB_NO_PAUSE_SYMBOL%"=="1" set NO_PAUSE_SYMBOL=1
if /I "%COMSPEC%" == %CMDCMDLINE% set NO_PAUSE_SYMBOL=1
set SUB_NO_PAUSE_SYMBOL=1
call :main
set EXIT_CODE=%ERRORLEVEL%
if not "%NO_PAUSE_SYMBOL%"=="1" pause
exit /b %EXIT_CODE%

:main
for %%p in (Enterprise Professional Community BuildTools) do (
  if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\%%p" (
    set VSDir="%ProgramFiles(x86)%\Microsoft Visual Studio\2017\%%p"
  )
)
set VSDir=%VSDir:"=%
echo VSDir=%VSDir%

"%VSDir%\MSBuild\15.0\Bin\MSBuild.exe" TypeMake.sln /t:Rebuild /p:Configuration=Release || exit /b 1
