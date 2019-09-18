#!/bin/bash
set -e

pushd "$(dirname "$0")/tools/TypeMake"
echo building TypeMake...
./buildgui.sh --quiet
echo building TypeMake finished.
popd

export SourceDirectory="$(dirname "$0")"
open "$(dirname "$0")/tools/TypeMake/Bin/net461/TypeMakeGui.app" --args "SourceDirectory=$SourceDirectory"
