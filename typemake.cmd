@echo off

setlocal
if "%SUB_NO_PAUSE_SYMBOL%"=="1" set NO_PAUSE_SYMBOL=1
if /I "%COMSPEC%" == %CMDCMDLINE% set NO_PAUSE_SYMBOL=1
set SUB_NO_PAUSE_SYMBOL=1

set EXIT_CODE=0
if not exist tools\TypeMake\Bin\TypeMake.exe (
  pushd tools\TypeMake
  call Build.cmd
  set /A EXIT_CODE^|=%ERRORLEVEL%
  popd
)

set SourceDirectory=.
tools\TypeMake\Bin\TypeMake.exe %*

if not "%NO_PAUSE_SYMBOL%"=="1" pause
exit /b %EXIT_CODE%
