#!/bin/bash
set -e

pushd tools/TypeMake
echo building TypeMake...
./buildgui.sh --quiet
echo building TypeMake finished.
popd

export SourceDirectory=.
nohup mono tools/TypeMake/Bin/net461/TypeMakeGui.Desktop.exe >/dev/null 2>/dev/null &
