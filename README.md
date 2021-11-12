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
|          Linux x86/x64         |    WSL+Ninja+gcc   |      Ninja+gcc     |                    |                    |
|            Linux x64           |   WSL+Ninja+clang  |     Ninja+clang    |                    |                    |
|            Linux x64           |  WSL+VS+Ninja+gcc  |                    |                    |                    |
|       Linux armv7a/arm64       |    WSL+Ninja+gcc   |      Ninja+gcc     |                    |                    |
|         Linux Unknown *        |    WSL+Ninja+gcc   |      Ninja+gcc     |                    |                    |
|         MacOS x64/arm64        |                    |                    |        XCode       |                    |
|            MacOS x64           |                    |                    |    Ninja+clang     |                    |
|  Android x86/x64/armv7a/arm64  |     NDK+Ninja      |     NDK+Ninja      |     NDK+Ninja      |                    |
|  Android x86/x64/armv7a/arm64  |  NDK+Ninja+Gradle  |  NDK+Ninja+Gradle  |  NDK+Ninja+Gradle  |                    |
|          Android arm64         |                    |                    |                    | Termux+Ninja+clang |
|          iOS arm64/x64         |                    |                    |        XCode       |                    |
|     iOS Simulator x64/arm64    |                    |                    |        XCode       |                    |

Win32 and WinRT targets are both supported, but WinRT only supports libraries and not applications.

Different OSs use different ABIs on the same CPU architecture.

\* Linux for other CPU architectures are supported by specifying toolchain commands and flags.

## Dependencies

Windows: VS2019/VS2022 \[LLVM(10.0.0-win64/12.0.0-win64)\] \[[C++ Clang-cl for v142/v143 build tools](https://docs.microsoft.com/en-us/cpp/build/clang-support-msbuild)\]

Linux: mono-devel(6.x)

Linux(openSUSE 15.3): gcc10-c++(10.3.0) \[gcc10-c++-32bit(10.3.0)\] (CC=gcc-10 CXX=g++-10)

Linux(openSUSE 15.3) with clang: clang(11.0.1) libc++-devel(11.0.1) llvm(11.0.1) (Compiler=clang)

Linux(Ubuntu 20.04): g++-10(10.3.0) \[g++-10-multilib(10.3.0)\] (CC=gcc-10 CXX=g++-10)

Linux(Ubuntu 20.04) with clang: clang(10.0.0) libc++-dev(10.0.0) libc++abi-dev(10.0.0) llvm(10.0.0) (Compiler=clang)

Linux with musl(x86/x64/armv7a/arm64): musl-cross-make (CppLibraryForm=Static CC=xxx-linux-musl-gcc CXX=xxx-linux-musl-g++ AR=xxx-linux-musl-ar)

Mac: XCode(12.0) mono(6.x)

iOS: XCode(12.0) mono(6.x)

Android: JDK(8.x) AndroidSDK("build-tools;30.0.3" "platforms;android-28" "ndk;23.0.7599858") \[Android Gradle plugin(4.1.0)\]

Android on Linux(openSUSE 15.3): java-1_8_0-openjdk-devel

Android on Linux(Ubuntu 20.04): openjdk-8-jdk

Android on Android: ninja mono(https://github.com/IanusInferus/termux-mono) {OpenJDK(11.0.1), AndroidSDK, AndroidNDK(r23)}(https://github.com/Lzhiyong/termux-ndk/releases, only Android 10.0 is tested)

## Notice

You need to set a key to build Android release APK and iOS applications/dynamic libraries.

With Android Ninja toolchain, you can generate a debug key if you don't already have one.

    mkdir ~/.android
    ${JAVA_HOME}/bin/keytool -genkeypair -v -alias androiddebugkey -keyalg RSA -keysize 2048 -dname "C=US, O=Android, CN=Android Debug" -validity 10000 -keypass android -keystore ~/.android/debug.keystore -storepass android

You need to set Configuration - Debugger - Debug type to Native/Dual to debug C++ code in Android Studio.

To use this repo in a project, just copy 'tools' directory to the project repo.

You may need to customize some code to cope with your project, mainly in directory 'Make' and 'Templates'.

To build a program statically for Linux/Android on x64/arm64/... without libc(glibc/bionic) dependency, you can build [musl-cross-make](https://github.com/richfelker/musl-cross-make) with ([GCC_CONFIG += --enable-default-pie](https://github.com/richfelker/musl-cross-make/issues/47)) and then build your program for Linux with static options(CLibraryForm=Static).

Android host support is [Termux](https://github.com/termux/termux-app)-only and is limited.
