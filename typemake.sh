#!/bin/bash
set -e

pushd tools/TypeMake
echo building TypeMake...
./build.sh --quiet
echo building TypeMake finished.
popd

export SourceDirectory=.
mono --debug tools/TypeMake/Bin/TypeMake.exe "$@"
