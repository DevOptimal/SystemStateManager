# DevOptimal.SystemStateManager

[![Continuous Integration](https://github.com/DevOptimal/SystemStateManager/actions/workflows/ci.yml/badge.svg)](https://github.com/DevOptimal/SystemStateManager/actions/workflows/ci.yml)

[![NuGet package](https://img.shields.io/nuget/v/DevOptimal.SystemStateManager.svg?label=DevOptimal.SystemStateManager&logo=nuget)](https://nuget.org/packages/DevOptimal.SystemStateManager)
[![NuGet package](https://img.shields.io/nuget/v/DevOptimal.SystemStateManager.Persistence.svg?label=DevOptimal.SystemStateManager.Persistence&logo=nuget)](https://nuget.org/packages/DevOptimal.SystemStateManager.Persistence)

## Features

- Programatically snapshot and restore the state of various system resources
- Persistence layer ensures you never lose state, even if the process crashes!
- Supported system resources include:
    - Environment variables
    - Directories
    - Files
    - Registry keys
    - Registry values

## Documentation

Documentation can be found [here](https://github.com/DevOptimal/SystemStateManager/blob/main/doc/index.md).

## Target Platforms

- [.NET Framework 4.7.2](https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/versions-and-dependencies#net-framework-472)
- [.NET Standard 2.0](https://docs.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0)
