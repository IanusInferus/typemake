set -e

command -v xbuild >/dev/null 2>&1 || {
  echo 'xbuild' not found.
  echo you need to install mono-devel
  echo openSUSE: sudo zypper install mono-devel
  echo CentOS: sudo yum install mono-devel
  echo Ubuntu: sudo apt install mono-devel
  echo MacOS: brew install mono
  exit 1
}
xbuild TypeMake.sln
