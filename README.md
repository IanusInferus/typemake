# TypeMake

It's never easy to develop a cross-platform C++ project. When there is no prebuilt releases for a software and people need to compile it from source on their own machine, usually it will cost them a lot of time to fight with the build process. Their machines differ in many aspects with the developers' machines, such as different operating systems, different compilers, different thirdparty library paths. This frustrates both developers and users. There are many repositories containing non-buildable build configurations, and there are many repositories existing merely to build a software on a specific platform.

[CMake](https://cmake.org/) is a well-known software to solve this problem. But it features many weaknesses, such as archaic syntax of the scripting language and discrete build script files. It's also quite difficult to figure out what to adjust in scripts knowing which options to adjust in Visual Studio or XCode.

To solve the above problems, and to make the build process of a C++ project debuggable, I propose the TypeMake build system. It is developed fully in C#, making it a typed and debuggable program. Both developers and users can debug a build process easily like a trivial C# project. No more "Please help me on building the project on Windows" issues.

For Usage:
TypeMake --help

Support matrix(Target Operating System vs Building Operating System):
|                                         |   Windows 10 x64   |      Linux x64     |      MacOS x64     |
| :-------------------------------------: | :----------------: | :----------------: | :----------------: |
|             Windows x86/x64             |       VS2017       |                    |                    |
|                Linux x64                |      WSL+cmake     |        cmake       |                    |
|                MacOS x64                |                    |                    |        XCode       |
|  Android x86/x64/armeabi-v7a/arm64-v8a  |  NDK+cmake+gradle  |  NDK+cmake+gradle  |  NDK+cmake+gradle  |
|                   iOS                   |                    |                    |        XCode       |

Notice:
You need to set a key to build Android release APK and iOS applications/dynamic libraries.
You need to set Configuration - Debugger - Debug type to Native/Dual to debug C++ code in Android Studio.
