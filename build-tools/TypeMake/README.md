# TypeMake

It's never easy to develop a cross-platform C++ project. If no prebuilt releases for a software is provided and people need to compile it from source on their own machine, it will usually cost a lot of time to fight against the build process. Their machines differ in many aspects from the developers' machines, with different operating systems, different compilers and different third-party library paths. This frustrates both developers and users. There are many repositories that not build on all supported platforms claimed and provide broken build configurations, and there are many repositories existing merely for the purpose to build a software on a specific platform.

[CMake](https://cmake.org/) is a well-known software that tries to solve this problem. But it features many weaknesses, such as archaic syntax of the scripting language and discrete build script files. It's also quite difficult to figure out what to change in scripts, given the options to adjust in Visual Studio or XCode.

To solve the above problems, and to make the build process of a C++ project debuggable, here I propose the TypeMake build system. It is developed fully in C#, making it a typed and debuggable program. Both developers and users can debug a build process easily as in any trivial C# project. No more "Please help me in building the project on Windows" issues.

The objective of TypeMake is not to implement a general-purpose C++ build tool, but to function as an example for you to experiment on, modify and integrate into your project. Realizing the fact that every project is distinct from others in some aspects, doing all the build actions for a project through a single tool is not attempted. Instead, calling and to be called from external programs are supported to ease build system integration with other tools.

## Usage

Windows: typemake.cmd

Linux/Mac: ./typemake.sh

## Support matrix

Target Operating System vs Building Operating System

|                                |   Windows 10 x64   |      Linux x64     |      MacOS x64     |   Android arm64    |
| :----------------------------: | :----------------: | :----------------: | :----------------: | :----------------: |
|  Windows x86/x64/armv7a/arm64  |         VS         |                    |                    |                    |
|         Windows x86/x64        |     VS+clang-cl    |                    |                    |                    |
|         Windows x86/x64        |     Ninja+clang    |                    |                    |                    |
|   WinRT x86/x64/armv7a/arm64   |         VS         |                    |                    |                    |
|             Linux *            | WSL+Ninja+clang/gcc|   Ninja+clang/gcc  |                    |                    |
|            Linux x64           |  WSL+VS+Ninja+gcc  |                    |                    |                    |
|         MacOS x64/arm64        |                    |                    |        XCode       |                    |
|            MacOS x64           |                    |                    |    Ninja+clang     |                    |
|  Android x86/x64/armv7a/arm64  |     NDK+Ninja      |     NDK+Ninja      |     NDK+Ninja      |                    |
|  Android x86/x64/armv7a/arm64  |  NDK+Ninja+Gradle  |  NDK+Ninja+Gradle  |  NDK+Ninja+Gradle  |                    |
|          Android arm64         |                    |                    |                    | Termux+Ninja+clang |
|          iOS arm64/x64         |                    |                    |        XCode       |                    |
|     iOS Simulator x64/arm64    |                    |                    |        XCode       |                    |

Win32 and WinRT targets are both supported, but WinRT only supports libraries and not applications.

Different OSs use different ABIs on the same CPU architecture.

\* Supported CPU architectures for Linux are x86/x64/armv7a/arm64/riscv64. For non-x64 CPU architectures, it is neccessary to specify toolchain commands and flags.

## Dependencies

Windows: VS2022 .Net(4.8) \[LLVM(12.0.0-win64)\] \[[C++ Clang-cl for v143 build tools](https://docs.microsoft.com/en-us/cpp/build/clang-support-msbuild)\]

Linux: mono-devel(6.x)

Linux(openSUSE 15.3) with clang: clang(11.0.1) libc++-devel(11.0.1) llvm(11.0.1) lld(11.0.1) [gcc10(10.3.0, x64-only)] (Compiler=clang)

Linux(openSUSE 15.3) with gcc: gcc10-c++(10.3.0) \[gcc10-c++-32bit(10.3.0)\] (CC=gcc-10 CXX=g++-10)

Linux(Ubuntu 20.04) with clang: clang(10.0.0) libc++-dev(10.0.0) libc++abi-dev(10.0.0) llvm(10.0.0) lld(10.0.0) [gcc10(10.3.0, x64-only)] (Compiler=clang)

Linux(Ubuntu 20.04) with gcc: g++-10(10.3.0) \[g++-10-multilib(10.3.0)\] (CC=gcc-10 CXX=g++-10)

Linux with musl and clang(x86/x64/armv7a/arm64/riscv64): musl libc toolchain (Compiler=clang CLibraryForm=Static EnableCustomSysroot=True) (See `lib/sysroot/HowToGetSysroot.md` for cross-compilation)

Linux with musl and gcc(x86/x64/armv7a/arm64/riscv64): musl-cross-make (CppLibraryForm=Static CC=xxx-linux-musl-gcc CXX=xxx-linux-musl-g++ AR=xxx-linux-musl-ar)

Mac: XCode(13.0) mono(6.x)

iOS: XCode(13.0) mono(6.x)

Android: JDK(11.x) AndroidSDK("build-tools;31.0.0" "platforms;android-28" "ndk;23.0.7599858") \[Android Gradle plugin(7.0.0)\]

Android on Linux(openSUSE 15.3): java-11-openjdk-devel

Android on Linux(Ubuntu 20.04): openjdk-11-jdk

Android on Android: ninja mono(https://github.com/IanusInferus/termux-mono) {OpenJDK(11.0.1), AndroidSDK, AndroidNDK(r23)}(https://github.com/Lzhiyong/termux-ndk/releases, only Android 10.0 is tested)

## Notice

To use this repo in a project, just copy 'tools' directory to the project repo.

You may need to customize some code to cope with your project, mainly in directory 'Make' and 'Templates'.

Static libc++ need to be built manually on most Linux distributions. You need to download the source files according to `lib/libcxx/generic/version.txt` to use it.

## Android

You need to set a key to build Android release APK and iOS applications/dynamic libraries.

With Android Ninja toolchain, you can generate a debug key if you don't already have one.

    mkdir ~/.android
    ${JAVA_HOME}/bin/keytool -genkeypair -v -alias androiddebugkey -keyalg RSA -keysize 2048 -dname "C=US, O=Android, CN=Android Debug" -validity 10000 -keypass android -keystore ~/.android/debug.keystore -storepass android

You need to set Configuration - Debugger - Debug type to Native/Dual to debug C++ code in Android Studio.

Android host support is [Termux](https://github.com/termux/termux-app)-only and is limited.

## Linux glibc dependency

glibc dependency is notorious in the Linux community. Unlike kernel32.dll of Windows, glibc provides both an operating system interface(POSIX) and a C standard library(C99, C11, etc) and it is backward-compatible but not forward-compatible, meaning that a program compiled on an old system will run on a new system but not vice versa.

One may think that we can link glibc statically to solve this problem, but glibc is [not usable when linked statically](https://stackoverflow.com/questions/57476533/why-is-statically-linking-glibc-discouraged).

To link a C library statically on Linux (and Android), you need [libmusl](https://www.musl-libc.org/). You can build [musl-cross-make](https://github.com/richfelker/musl-cross-make) with ([GCC_CONFIG += --enable-default-pie](https://github.com/richfelker/musl-cross-make/issues/47)) and then build your program for Linux with its gcc. Another version [musl libc toolchain](https://musl.cc/) is prebuilt and can also be used as a sysroot for clang(x86_64-linux-musl-native.tgz). But this method does not apply to dynamic libraries as libmusl does not have a dynamic linker in its static library.

An alternative solution for Linux is to create a custom sysroot from an old Linux distribution with desired old version glibc and use clang to compile against it. You can also try gcc, but newer gcc and libstdc++ [depend on newer glibc](https://gcc.gnu.org/onlinedocs/libstdc++/faq.html#faq.linux_glibc), which breaks easily and you need to compile everything. For clang, you only need to compile libc++, libc++abi and other libraries you use, not the compiler and related tools. This method is what Google uses for Android NDK, except for that they use bionic rather than glibc.

Yet another solution for Linux is to use [glibc_version_header](https://github.com/wheybags/glibc_version_header), which forces code to use older glibc symbols. This method works for small projects, but when you reference third-party libraries from the system package manager, newer glibc symbols will slip in.

As a last resort, one can build programs on a machine with a distribution with an older version of glibc, say CentOS 6.x. The problem is that it will not be very portable. You may need to carry it around or open a virtual machine to do a build.

By the way, docker is not a solution to this problem, as glibc depends on kernel version tightly, and we have to [run a Docker image that was based on the same or older kernel version as the host](https://github.com/boostorg/filesystem/issues/164), in which case, we can run our program directly on the host.
