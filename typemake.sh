#!/bin/bash
set -e

pushd "$(dirname "$0")/build-tools/TypeMake"
echo building TypeMake...
./build.sh --quiet
echo building TypeMake finished.
popd

flags=()
if [ "$(uname)" == "Darwin" ]; then
  flags=(--simple)
fi

export SourceDirectory="$(dirname "$0")"
mono --debug "$(dirname "$0")/build/TypeMake/Bin/TypeMake.exe" "${flags[@]}" "$@"
