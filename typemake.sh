set -e

[ -f typemake/Bin/TypeMake.exe ] || {
  pushd typemake
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

mono typemake/Bin/TypeMake.exe "$@"
