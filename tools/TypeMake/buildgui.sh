#!/bin/bash
set -e

MSBUILD=$(command -v msbuild) || MSBUILD=$(command -v xbuild) || {
  echo 'msbuild' or 'xbuild' not found.
  echo you need to install mono-devel
  echo MacOS/Ubuntu/CentOS: see https://www.mono-project.com/download/stable
  echo openSUSE: sudo zypper install mono-devel
  echo Apine: su\; apk add mono \(add edge/testing in /etc/apk/repositories\)
  exit 1
}
if [ "$1" == "--quiet" ]
then
  $MSBUILD TypeMakeGui.sln /restore /t:Build /p:Configuration\=Release /nologo /consoleloggerparameters:ErrorsOnly
else
  $MSBUILD TypeMakeGui.sln /restore /t:Rebuild /p:Configuration\=Release
fi
