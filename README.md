# SharpDetect

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://github.com/acizmarik/sharpdetect/LICENSE.md)
[![GitHub Actions](https://github.com/acizmarik/sharpdetect/actions/workflows/main.yml/badge.svg)](https://github.com/acizmarik/sharpdetect/actions)

A work-in-progress, experimental dynamic analysis framework for .NET programs.

## Getting Started

The following steps describe all dependencies and steps to build SharpDetect, both its managed (.NET) and its unmanaged (.NET AOT-compiled) modules.

### Prerequisites

* .NET 7 SDK
* Visual Studio 2022 Build Tools

### Steps

```bash
# Managed part
cd src/SharpDetect.sln
dotnet build

# Unmanaged part
cd src/SharpDetect.Profiler/Scripts
./publish-win-x64-aot-debug.ps1
```

## Running Tests

Project contains either unit tests (.NET only), or end-2-end tests. Both test categories are implemented in .NET solutions. Keep in mind that end-2-end tests can take some time, as there is a lot of IO going on and many processes are being spawned as well.

```bash
# Unit tests
dotnet test src/Tests/SharpDetect.UnitTests/SharpDetect.UnitTests.csproj

# End-2-end tests
dotnet test src/Tests/SharpDetect.E2ETests/SharpDetect.E2ETests.csproj
```

## State of Development

SharpDetect is still in development and not production ready. The following runtimes and .NET implementations are supported:

| Runtime   | .NET Core/5+                  | .NET Framework               |
| --------- | ----------------------------- | ---------------------------- |
| win-x64   | :white_check_mark:            | :construction: (in-progress) |
| win-x86   | :x:                           | :x:                          |
| linux-x64 | :construction: (in-progress)  | (not applicable)             |
| linux-x86 | :x:                           | (not applicable)             |
| osx-x64   | :x:                           | (not applicable)             |

## Documentation

* [Running your first analysis](docs/running-analysis.md)

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for a guide on how to contribute to this repository.

## License

This project is licensed under the [Apache-2.0 license](LICENSE), unless specified otherwise in a file header.
