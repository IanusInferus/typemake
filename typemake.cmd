@echo off

setlocal
if "%SUB_NO_PAUSE_SYMBOL%"=="1" set NO_PAUSE_SYMBOL=1
if /I "%COMSPEC%" == %CMDCMDLINE% set NO_PAUSE_SYMBOL=1
set SUB_NO_PAUSE_SYMBOL=1
call :main %*
set EXIT_CODE=%ERRORLEVEL%
if not "%NO_PAUSE_SYMBOL%"=="1" pause
exit /b %EXIT_CODE%

:main
pushd tools\TypeMake || exit /b 1
echo building TypeMake...
call Build.cmd --quiet || exit /b 1
echo building TypeMake finished.
popd

set SourceDirectory=.
tools\TypeMake\Bin\TypeMake.exe %* || exit /b 1
