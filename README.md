# TypeMake

It's never easy to develop a cross-platform C++ project. If no prebuilt releases for a software is provided and people need to compile it from source on their own machine, it will usually cost a lot of time to pass through the build process. Their machines differ in many aspects from the developers' machines, such as different operating systems, different compilers and different third-party library paths. This frustrates both developers and users. There are many repositories that only build on a specific platform or even provides a broken build configuration.

[CMake](https://cmake.org/) is a well-known software that solves this problem. But its script-based features have many weaknesses, such as archaic syntax of the scripting language and discrete build script files. It's also quite difficult to figure out what to adjust in scripts knowing which options to adjust in Visual Studio or XCode.

To solve the above problems and to make the build process of a C++ project debuggable, I created the TypeMake build system. It is developed fully in C#, making it a typed and debuggable program. Both developers and users can debug a build process easily like a simple C# project. No more "Please help me in building the project on Windows" requests.

## Usage

TypeMake --help

## Support matrix

Target Operating System vs Building Operating System

|                                         |   Windows 10 x64   |      Linux x64     |      MacOS x64     |
| :-------------------------------------: | :----------------: | :----------------: | :----------------: |
|  Windows x86/x64/armeabi-v7a/arm64-v8a  |       VS2017       |                    |                    |
|                Linux x64                |      WSL+cmake     |        cmake       |                    |
|                MacOS x64                |                    |                    |        XCode       |
|  Android x86/x64/armeabi-v7a/arm64-v8a  |  NDK+cmake+gradle  |  NDK+cmake+gradle  |  NDK+cmake+gradle  |
|        iOS armeabi-v7a/arm64-v8a        |                    |                    |        XCode       |

## Notice

You need to set a key to build Android release APK and iOS applications/dynamic libraries.

You need to set Configuration - Debugger - Debug type to Native/Dual to debug C++ code in Android Studio.

To use this repo in a project, just copy 'tools' directory to the project repo.

You may need to customize some code to cope with your project, mainly in directory 'Make' and 'Templates'.
