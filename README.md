# SharpDetect

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

## Documentation

* [Running your first analysis](docs/running-analysis.md)

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for a guide on how to contribute to this repository.

## License

This project is licensed under the Apache-2.0 license, unless specified otherwise in a file header.