#!/bin/bash
set -e

command -v msbuild >/dev/null 2>&1 || {
  echo 'msbuild' not found.
  echo you need to install mono-devel
  echo openSUSE: sudo zypper install mono-devel
  echo CentOS: sudo yum install mono-devel
  echo Ubuntu: sudo apt install mono-devel
  echo Apine: su; apk add mono \(add edge/testing in /etc/apk/repositories\)
  echo MacOS: brew install mono
  exit 1
}
if [ "$1" == "--quiet" ]
then
  msbuild TypeMakeGui.sln /restore /t:Build /p:Configuration=Release /nologo /consoleloggerparameters:ErrorsOnly
else
  msbuild TypeMakeGui.sln /restore /t:Rebuild /p:Configuration=Release
fi
