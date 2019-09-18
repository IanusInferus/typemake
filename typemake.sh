#!/bin/bash
set -e

pushd "$(dirname "$0")/tools/TypeMake"
echo building TypeMake...
./build.sh --quiet
echo building TypeMake finished.
popd

export SourceDirectory="$(dirname "$0")"
mono --debug "$(dirname "$0")/tools/TypeMake/Bin/TypeMake.exe" "$@"
