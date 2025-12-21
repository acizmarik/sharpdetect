# SharpDetect

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://github.com/acizmarik/sharpdetect/LICENSE.md)
[![GitHub Actions](https://github.com/acizmarik/sharpdetect/actions/workflows/main.yml/badge.svg)](https://github.com/acizmarik/sharpdetect/actions)
[![NuGet Version](https://img.shields.io/nuget/v/SharpDetect)](https://www.nuget.org/packages/SharpDetect)

A work-in-progress, experimental dynamic analysis for .NET programs.

## Features

### 🔒 Deadlock Detection

.NET programs can be analyzed for deadlocks using `DeadlockPlugin`.
After analysis terminates, it generates a report which contains affected threads, stack traces and more runtime details.

**Monitored synchronization primitives:**
- `System.Threading.Monitor`
  - Supported operations: `Enter`, `TryEnter`, `Exit`, `Wait`, `PulseOne`, `PulseAll`.
- `System.Threading.Lock` (.NET 9+)
  - Supported operations: `Enter`, `TryEnter`, `Exit`, `EnterScope`.
- `System.Threading.Thread`
  - Supported operations: `Join`.

## Installation

SharpDetect is packed and published as a .NET Tool on [NuGet](https://www.nuget.org/packages/SharpDetect).
You can install it using one of the following commands:

```bash
dotnet tool install --global SharpDetect # Latest stable release
dotnet tool install --global SharpDetect --prerelease # Latest preview (prerelease)
```

## Quick Start

### 1. Create an Analysis Descriptor

Create a JSON configuration file (e.g., `analysis.json`) that describes your analysis:

```json
{
    "Target": {
        "Path": "<path/to/YourExecutableDotNetAssembly.dll>",
        "Args": "",
        "Architecture": "X64",
        "RedirectInputOutput": {
            "SingleConsoleMode": true
        }
    },
    "Runtime": {
        "Profiler": {
            "Clsid": "{b2c60596-b36d-460b-902a-3d91f5878529}",
            "Path": {
                "WindowsX64": "Profilers/win-x64/SharpDetect.Concurrency.Profiler.dll",
                "LinuxX64": "Profilers/linux-x64/SharpDetect.Concurrency.Profiler.so"
            }
        }
    },
    "Analysis": {
        "Path": "Plugins/SharpDetect.Plugins.dll",
        "FullTypeName": "SharpDetect.Plugins.Deadlock.DeadlockPlugin",
        "RenderReport": true
    }
}
```

*Note: Make sure to replace `<path/to/YourExecutableDotNetAssembly.dll>` with the actual path to the .NET assembly you want to analyze.*

### 2. Run the Analysis

```bash
sharpdetect run analysis.json
```

## Building from Source

### Prerequisites

- .NET 10 SDK
- C++20 compiler with CMake (clang on Linux, MSVC on Windows)
- Other platform-specific dependencies as specified by [Native AOT deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot)

### Build Instructions

```bash
git clone https://github.com/acizmarik/sharpdetect.git
cd sharpdetect
git submodule update --init --recursive

cd src
dotnet tool restore
dotnet cake
```

## Platform Support

SharpDetect supports modern .NET SDKs that are still under active support.
Right now, only x64 architectures are supported.
The following table summarizes the supported platforms:

| Runtime   | .NET 8 / 9 / 10    |
| --------- |--------------------|
| linux-x64 | :white_check_mark: |
| linux-x86 | :x:                |
| win-x64   | :white_check_mark: |
| win-x86   | :x:                |

## Acknowledgments

SharpDetect is built with the help of numerous open-source libraries and components.
For detailed licensing information and full copyright notices, please see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## License

This project is licensed under the [Apache-2.0 license](LICENSE).
