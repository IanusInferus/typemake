#!/bin/bash
set -e

pushd tools/TypeMake
./build.sh --quiet
popd

export SourceDirectory=.
mono tools/TypeMake/Bin/TypeMake.exe "$@"
