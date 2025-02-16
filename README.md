# SharpDetect

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://github.com/acizmarik/sharpdetect/LICENSE.md)
[![GitHub Actions](https://github.com/acizmarik/sharpdetect/actions/workflows/main.yml/badge.svg)](https://github.com/acizmarik/sharpdetect/actions)

A work-in-progress, experimental dynamic analysis framework for .NET programs.

### Prerequisites

The following list of dependencies is needed to build & run the solution. Alternatively, for Linux, you can use `Dockerfile` from repository root. It creates an environment with necessary dependencies.

#### Windows

* .NET 8 SDK
* Visual Studio 2022 Build Tools

#### Linux

* .NET 8 SDK
* zlib1g-dev
* clang

### Build Steps

#### Windows

```bash
cd src
dotnet tool restore
dotnet cake --rid=win-x64
```

#### Linux

```bash
cd src
dotnet tool restore
dotnet cake --rid=linux-x64
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