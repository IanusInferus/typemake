# TypeMake

It's never easy to develop a cross-platform C++ project. If no prebuilt releases for a software is provided and people need to compile it from source on their own machine, it will usually cost a lot of time to fight against the build process. Their machines differ in many aspects from the developers' machines, with different operating systems, different compilers and different third-party library paths. This frustrates both developers and users. There are many repositories that not build on all supported platforms claimed and provide broken build configurations, and there are many repositories existing merely for the purpose to build a software on a specific platform.

[CMake](https://cmake.org/) is a well-known software that tries to solve this problem. But it features many weaknesses, such as archaic syntax of the scripting language and discrete build script files. It's also quite difficult to figure out what to change in scripts, given the options to adjust in Visual Studio or XCode.

To solve the above problems, and to make the build process of a C++ project debuggable, here I propose the TypeMake build system. It is developed fully in C#, making it a typed and debuggable program. Both developers and users can debug a build process easily as in any trivial C# project. No more "Please help me in building the project on Windows" issues.

The objective of TypeMake is not to implement a general-purpose C++ build tool, but to function as an example for you to experiment on, modify and integrate into your project. Realizing the fact that every project is distinct from others in some aspects, doing all the build actions for a project through a single tool is not attempted. Instead, calling and to be called from external programs are supported to ease build system integration with other tools.

## Usage

TypeMake --help

## Support matrix

Target Operating System vs Building Operating System

|                                |   Windows 10 x64   |      Linux x64     |      MacOS x64     |
| :----------------------------: | :----------------: | :----------------: | :----------------: |
|  Windows x86/x64/armv7a/arm64  |       VS2017       |                    |                    |
|          Linux x86/x64         |    WSL+Ninja+gcc   |      Ninja+gcc     |                    |
|            Linux x64           |   WSL+Ninja+clang  |     Ninja+clang    |                    |
|          Linux x86/x64         |    WSL+CMake+gcc   |      CMake+gcc     |                    |
|       Linux armv7a/arm64       |    WSL+Ninja+gcc   |      Ninja+gcc     |                    |
|            MacOS x64           |                    |                    |        XCode       |
|  Android x86/x64/armv7a/arm64  |     NDK+Ninja      |     NDK+Ninja      |     NDK+Ninja      |
|  Android x86/x64/armv7a/arm64  |  NDK+Ninja+Gradle  |  NDK+Ninja+Gradle  |  NDK+Ninja+Gradle  |
|  Android x86/x64/armv7a/arm64  |  NDK+CMake+Gradle  |  NDK+CMake+Gradle  |  NDK+CMake+Gradle  |
|        iOS armv7a/arm64        |                    |                    |        XCode       |

Different OSs use different ABIs on the same CPU architecture.

## Dependencies

Windows: VS2017

Linux(openSUSE 15): \[cmake(>=3.3.2)\] gcc-c++(7.3.1) \[gcc-c++-32bit(7.3.1)\] mono-devel(5.x) glibc(develop 2.18/runtime 2.14)

Linux(Ubuntu 18.04): \[cmake(>=3.3.2)\] g++(7.3.0) \[g++-multilib(7.3.0)\] mono-devel(5.x) glibc(develop 2.18/runtime 2.14)

Linux(Ubuntu 18.04) for armv7a/arm64: g++-arm-linux-gnueabihf(7.3.0) g++-aarch64-linux-gnu(7.3.0)

Linux(Ubuntu 18.04) with clang: clang-7 libc++-7-dev libc++abi-7-dev llvm-7-tools (CC=clang-7 CXX=clang++-7 AR=llvm-ar-7)

Mac: XCode(10.0) mono(5.x)

Android: JDK(8.x) AndroidSDK(build-tools:28.0.3 platforms:android-27) AndroidNDK(r19c) \[Gradle(4.4)\] \[CMake(>=3.3.2)\]

iOS: XCode(10.0)

## Notice

You need to set a key to build Android release APK and iOS applications/dynamic libraries.

You need to set Configuration - Debugger - Debug type to Native/Dual to debug C++ code in Android Studio.

To use this repo in a project, just copy 'tools' directory to the project repo.

You may need to customize some code to cope with your project, mainly in directory 'Make' and 'Templates'.
