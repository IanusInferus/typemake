set -e

command -v xbuild >/dev/null 2>&1 || {
  echo 'xbuild' not found.
  echo you need to install mono-xbuild
  echo openSUSE: sudo zypper install mono-xbuild
  echo CentOS: sudo yum install mono-xbuild
  echo Ubuntu: sudo apt install mono-xbuild
  echo MacOS: sudo brew install mono-xbuild
  exit 1
}
xbuild Src/TypeMake.sln
