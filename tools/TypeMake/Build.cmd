@echo off

setlocal EnableDelayedExpansion
if "%SUB_NO_PAUSE_SYMBOL%"=="1" set NO_PAUSE_SYMBOL=1
if /I "%COMSPEC%" == %CMDCMDLINE% set NO_PAUSE_SYMBOL=1
set SUB_NO_PAUSE_SYMBOL=1
call :main %*
set EXIT_CODE=%ERRORLEVEL%
if not "%NO_PAUSE_SYMBOL%"=="1" pause
exit /b %EXIT_CODE%

:main
for %%v in (2019 2017) do (
  for %%p in (Enterprise Professional Community BuildTools) do (
    for %%b in (Current 15.0) do (
      if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\%%v\%%p\MSBuild\%%b\Bin\MSBuild.exe" (
        set MSBuild="%ProgramFiles(x86)%\Microsoft Visual Studio\%%v\%%p\MSBuild\%%b\Bin\MSBuild.exe"
        goto MSBuild_Found
      )
    )
  )
)
:MSBuild_Found

if "%1"=="--quiet" (
  !MSBuild! TypeMake.sln /t:Build /p:Configuration=Release /m /nologo /consoleloggerparameters:ErrorsOnly || exit /b 1
) else (
  echo !MSBuild!
  !MSBuild! TypeMake.sln /t:Rebuild /p:Configuration=Release /m || exit /b 1
)
