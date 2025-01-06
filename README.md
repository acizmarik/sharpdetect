# SharpDetect

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://github.com/acizmarik/sharpdetect/LICENSE.md)
[![GitHub Actions](https://github.com/acizmarik/sharpdetect/actions/workflows/main.yml/badge.svg)](https://github.com/acizmarik/sharpdetect/actions)

A work-in-progress, experimental dynamic analysis framework for .NET programs.

### Prerequisites

* .NET 8 SDK
* Visual Studio 2022 Build Tools

### Build Steps

```bash
cd src
dotnet cake
```

## Available Tools

* **Deadlock Analyzer** - Monitors all lock-related operations on `System.Threading.Monitor`. If a deadlock is detected, user receives a report with details (affected threads, stack traces).

## State of Development

| Runtime   | .NET                          | .NET Framework               |
| --------- | ----------------------------- | ---------------------------- |
| win-x64   | :white_check_mark:            | :x:                          |
| win-x86   | :x:                           | :x:                          |
| linux-x64 | :x:                           | (not applicable)             |
| linux-x86 | :x:                           | (not applicable)             |

## License

This project is licensed under the [Apache-2.0 license](LICENSE), unless specified otherwise in a file header.