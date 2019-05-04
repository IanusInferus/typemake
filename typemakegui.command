#!/bin/bash
set -e

cd "$(dirname "$0")"

pushd tools/TypeMake
echo building TypeMake...
./buildgui.sh --quiet
echo building TypeMake finished.
popd

export SourceDirectory="$(pwd)"
open tools/TypeMake/Bin/net461/TypeMakeGui.app --args "SourceDirectory=$SourceDirectory"
