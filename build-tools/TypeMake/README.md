# TypeMake

It's never easy to develop a cross-platform C++ project. If no prebuilt releases for a software is provided and people need to compile it from source on their own machine, it will usually cost a lot of time to fight against the build process. Their machines differ in many aspects from the developers' machines, with different operating systems, different compilers and different third-party library paths. This frustrates both developers and users. There are many repositories that not build on all supported platforms claimed and provide broken build configurations, and there are many repositories existing merely for the purpose to build a software on a specific platform.

[CMake](https://cmake.org/) is a well-known software that tries to solve this problem. But it features many weaknesses, such as archaic syntax of the scripting language and discrete build script files. It's also quite difficult to figure out what to change in scripts, given the options to adjust in Visual Studio or XCode.

To solve the above problems, and to make the build process of a C++ project debuggable, here I propose the TypeMake build system. It is developed fully in C#, making it a typed and debuggable program. Both developers and users can debug a build process easily as in any trivial C# project. No more "Please help me in building the project on Windows" issues.

The objective of TypeMake is not to implement a general-purpose C++ build tool, but to function as an example for you to experiment on, modify and integrate into your project. Realizing the fact that every project is distinct from others in some aspects, doing all the build actions for a project through a single tool is not attempted. Instead, calling and to be called from external programs are supported to ease build system integration with other tools.

## Usage

Windows: typemake.cmd

Windows GUI: typemakegui.cmd

Linux/Mac: ./typemake.sh

Linux GUI: ./typemakegui.sh

Mac GUI: ./typemakegui.command

## Support matrix

Target Operating System vs Building Operating System

|                                |   Windows 10 x64   |      Linux x64     |      MacOS x64     |   Android arm64    |
| :----------------------------: | :----------------: | :----------------: | :----------------: | :----------------: |
|  Windows x86/x64/armv7a/arm64  |         VS         |                    |                    |                    |
|         Windows x86/x64        |     VS+clang-cl    |                    |                    |                    |
|         Windows x86/x64        |     Ninja+clang    |                    |                    |                    |
|   WinRT x86/x64/armv7a/arm64   |         VS         |                    |                    |                    |
|          Linux x86/x64         |    WSL+Ninja+gcc   |      Ninja+gcc     |                    |                    |
|            Linux x64           |   WSL+Ninja+clang  |     Ninja+clang    |                    |                    |
|            Linux x64           |  WSL+VS+Ninja+gcc  |                    |                    |                    |
|          Linux x86/x64         |   WSL+CMake+gcc *  |     CMake+gcc *    |                    |                    |
|       Linux armv7a/arm64       |    WSL+Ninja+gcc   |      Ninja+gcc     |                    |                    |
|         Linux Unknown **       |    WSL+Ninja+gcc   |      Ninja+gcc     |                    |                    |
|         MacOS x64/arm64        |                    |                    |        XCode       |                    |
|            MacOS x64           |                    |                    |    Ninja+clang     |                    |
|            MacOS x64           |                    |                    |   CMake+clang *    |                    |
|  Android x86/x64/armv7a/arm64  |     NDK+Ninja      |     NDK+Ninja      |     NDK+Ninja      |                    |
|  Android x86/x64/armv7a/arm64  |  NDK+Ninja+Gradle  |  NDK+Ninja+Gradle  |  NDK+Ninja+Gradle  |                    |
|  Android x86/x64/armv7a/arm64  | NDK+CMake+Gradle * | NDK+CMake+Gradle * | NDK+CMake+Gradle * |                    |
|          Android arm64         |                    |                    |                    | Termux+Ninja+clang |
|          iOS arm64/x64         |                    |                    |        XCode       |                    |

Win32 and WinRT targets are both supported, but WinRT only supports libraries and not applications.

Different OSs use different ABIs on the same CPU architecture.

\* CMake is supported as a compatibility solution. (Ex. Integration with other projects.)

\** Linux for other CPU architectures are supported by specifying toolchain commands and flags.

## Dependencies

Windows: VS2019 \[LLVM(10.0.0-win64)\] \[[C++ Clang-cl for v142 build tools](https://docs.microsoft.com/en-us/cpp/build/clang-support-msbuild?view=vs-2019)\]

Linux(openSUSE 15.1): \[cmake(>=3.3.2)\] gcc-c++(7.3.1) \[gcc-c++-32bit(7.3.1)\] mono-devel(5.x) glibc(runtime 2.14)

Linux(Ubuntu 18.04): \[cmake(>=3.3.2)\] g++(7.3.0) \[g++-multilib(7.3.0)\] mono-devel(5.x) glibc(runtime 2.14)

Linux(Ubuntu 18.04) with clang: clang-7 libc++-7-dev libc++abi-7-dev llvm-7-tools (CC=clang-7 CXX=clang++-7 AR=llvm-ar-7)

Linux(Ubuntu 18.04) with musl(x86/x64/armv7a/arm64): musl-cross-make (CC=xxx-linux-musl-gcc CXX=xxx-linux-musl-g++ AR=xxx-linux-musl-ar)

Linux(Alpine 3.9): g++(8.2.0) bash mono-devel(5.x, in edge/testing repo) ninja

Linux GUI: GTK

Mac: XCode(11.0) mono(5.x)

iOS: XCode(11.0) mono(5.x)

Android: JDK(8.x) AndroidSDK(build-tools:30.0.3 platforms:android-28) AndroidNDK(r22) \[Android Gradle plugin(4.1.0)\] \[CMake(>=3.3.2)\]

Android on Linux(openSUSE 15.1): java-1_8_0-openjdk-devel

Android on Linux(Ubuntu 18.04): openjdk-8-jdk

Android on Linux(Alpine 3.9): openjdk8

Android on Linux(CentOS 7.7): java-1.8.0-openjdk-devel

Android on Android: ninja mono(https://github.com/IanusInferus/termux-mono) {OpenJDK(11.0.1), AndroidSDK, AndroidNDK(r21)}(https://github.com/Lzhiyong/termux-ndk/releases, only Android 10.0 is tested)

## Notice

You need to set a key to build Android release APK and iOS applications/dynamic libraries.

With Android Ninja toolchain, you can generate a debug key if you don't already have one.

    mkdir ~/.android
    ${JAVA_HOME}/bin/keytool -genkeypair -v -alias androiddebugkey -keyalg RSA -keysize 2048 -dname "C=US, O=Android, CN=Android Debug" -validity 10000 -keypass android -keystore ~/.android/debug.keystore -storepass android

You need to set Configuration - Debugger - Debug type to Native/Dual to debug C++ code in Android Studio.

To use this repo in a project, just copy 'tools' directory to the project repo.

You may need to customize some code to cope with your project, mainly in directory 'Make' and 'Templates'.

To build a program statically for Linux/Android on x64/arm64/... without libc(glibc/bionic) dependency, you can build [musl-cross-make](https://github.com/richfelker/musl-cross-make) with ([GCC_CONFIG += --enable-default-pie](https://github.com/richfelker/musl-cross-make/issues/47)) and then build your program for Linux with static options(CLibrary=Static).

Android host support is [Termux](https://github.com/termux/termux-app)-only and is limited.
