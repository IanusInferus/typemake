# TypeMake

It's never easy to develop a cross-platform C++ project. When there is no prebuilt releases for a software and people need to compile it from source on their own machine, usually they will cost a lot of time to fight with the build process. Their machine differs in many aspects with the developers' machines, such as different operating systems, different compilers, different thirdparty library paths. This frustrates both developers and users. There are many repositories containing not buildable build configurations, and there are many repositories existing merely to build a software on a specific platform.

[CMake](https://cmake.org/) is a well-known software to solve this problem. But it features many weaknesses, such as archaic syntax of the scripting language and discrete build script files. It's also quite difficult to figure out what to adjust in scripts knowing what options to adjust in Visual Studio or XCode.

To solve the above problems, and to make the build process of a C++ project debuggable, I propose the TypeMake build system. It is developed fully in C#, making it a typed and debuggable program. Both developers and users can debug a build process easily like a trivial C# project. No more "Please help me on building the project on Windows" issues.
