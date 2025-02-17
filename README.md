# SharpDetect

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://github.com/acizmarik/sharpdetect/LICENSE.md)
[![GitHub Actions](https://github.com/acizmarik/sharpdetect/actions/workflows/main.yml/badge.svg)](https://github.com/acizmarik/sharpdetect/actions)

A work-in-progress, experimental dynamic analysis framework for .NET programs.

### Prerequisites

#### Windows

* .NET 8 SDK
* Visual Studio 2022 Build Tools
* git

#### Linux

* .NET 8 SDK
* zlib1g-dev
* clang
* cmake
* git

### Build Steps

#### Windows Development Build

```bash
git submodule update --init --recursive

cd src
dotnet tool restore
dotnet cake --rid=win-x64
```

#### Linux

##### Alternative: Development Build

```bash
git submodule update --init --recursive

cd src
dotnet tool restore
dotnet cake --rid=linux-x64
```

##### Alternative: Testing Build

```bash
docker build -t sharpdetect/ubuntu-24.04
```

## Available Tools

* **Deadlock Analyzer** - Monitors all lock-related operations on `System.Threading.Monitor`. If a deadlock is detected, user receives a report with details (affected threads, stack traces).

## State of Development

| Runtime   | .NET                          | .NET Framework               |
| --------- | ----------------------------- | ---------------------------- |
| win-x64   | :white_check_mark:            | :x:                          |
| win-x86   | :x:                           | :x:                          |
| linux-x64 | :white_check_mark:            | (not applicable)             |
| linux-x86 | :x:                           | (not applicable)             |

## License

This project is licensed under the [Apache-2.0 license](LICENSE), unless specified otherwise in a file header.