#!/bin/bash
set -e

pushd tools/TypeMake
echo building TypeMake...
./buildgui.sh --quiet
echo building TypeMake finished.
popd

export SourceDirectory=.
nohup mono --debug tools/TypeMake/Bin/net461/TypeMakeGui.exe >/dev/null 2>/dev/null &
