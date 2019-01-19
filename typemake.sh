set -e

[ -f tools/TypeMake/Bin/TypeMake.exe ] || {
  pushd tools/TypeMake
  ./build.sh
  popd
}

command -v mono >/dev/null 2>&1 || {
  echo 'mono' not found.
  echo you need to install mono
  echo openSUSE: sudo zypper install mono
  echo CentOS: sudo yum install mono
  echo Ubuntu: sudo apt install mono
  exit 1
}

export SourceDirectory=.
mono tools/TypeMake/Bin/TypeMake.exe "$@"
