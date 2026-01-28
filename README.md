# SharpDetect

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://github.com/acizmarik/sharpdetect/LICENSE.md)
[![GitHub Actions](https://github.com/acizmarik/sharpdetect/actions/workflows/main.yml/badge.svg)](https://github.com/acizmarik/sharpdetect/actions)
[![NuGet Version](https://img.shields.io/nuget/v/SharpDetect)](https://www.nuget.org/packages/SharpDetect)

## Overview

SharpDetect is a dynamic analysis framework for .NET programs.
Monitoring and instrumentation support is implemented using the [Profiling API](https://learn.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/profiling-overview).
Analysis is performed by plugins that can be developed to evaluate various runtime properties of the target program.

## Installation

SharpDetect is distributed as a .NET Tool on [NuGet](https://www.nuget.org/packages/SharpDetect).
You can install it using one of the following commands:

```bash
dotnet tool install --global SharpDetect # Latest stable release
dotnet tool install --global SharpDetect --prerelease # Latest preview (prerelease)
```

## Quick Start

### 1. Create Program to Analyze

Create and build a new console .NET application (targeting .NET 8, 9, or 10) with the following code:

```csharp
// Note: When executed, this program should eventually deadlock

var a = new object();
var b = new object();

new Thread(() => { while (true) lock (a) lock (b) { } }) { IsBackground = true }.Start();
new Thread(() => { while (true) lock (b) lock (a) { } }) { IsBackground = true }.Start();

Thread.Sleep(5000);
```

### 2. Create Analysis Configuration File

Create a configuration file that describes the analysis to be performed.
The easiest way to create this file is to use the `sharpdetect init` command:

```bash
sharpdetect init \
  --plugin "SharpDetect.Plugins.Deadlock.DeadlockPlugin" \
  --target "<path/to/YourExecutableDotNetAssembly.dll>" \
  --output "AnalysisConfiguration.json"
```

*Note: Replace `<path/to/YourExecutableDotNetAssembly.dll>` with the actual path to your compiled .NET assembly (e.g., `bin/Debug/net9.0/MyApp.dll`).*

### 3. Run Analysis

When running analysis, SharpDetect will use the configuration specified in the file from previous step.
Start analysis using the `sharpdetect run` command:

```bash
sharpdetect run AnalysisConfiguration.json
```

### 4. View Report

Shortly after the deadlock occurs, you should see a log message similar to this:
```text
warn: SharpDetect.Core.Plugins.PluginBase[0]
     [PID=20611] Deadlock detected (affects 2 threads).
```

When the target program terminates, you should see a confirmation that the report has been generated:
```text
Report stored to file: /home/user/Workspace/SharpDetect_Report_20251223_095828.html.
```

Reports are self-contained HTML files that can be opened in any modern web browser.

## Analysis Plugins

### 🔒 Deadlock Detection Plugin

The `DeadlockPlugin` analyzes .NET programs for deadlocks.
When analysis completes, it generates a comprehensive HTML report containing affected threads, stack traces and other runtime details.

#### Supported Synchronization Primitives

- `System.Threading.Monitor`
    - Supported operations: `Enter`, `TryEnter`, `Exit`, `Wait`, `Pulse`, `PulseAll`
- `System.Threading.Lock` (.NET 9+)
    - Supported operations: `Enter`, `TryEnter`, `Exit`, `EnterScope`
- `System.Threading.Thread`
    - Supported operations: `Join`

#### Known Limitations

- Deadlocks involving `async`/`await` are currently not detected
- Other synchronization primitives (e.g., `SemaphoreSlim`, `Mutex`, `ReaderWriterLock`) are not supported yet
- The plugin introduces performance overhead during analysis

## Building from Source

### Prerequisites

- .NET 10 SDK
- C++20 compiler with CMake (clang on Linux, MSVC on Windows)
- Platform-specific dependencies for [Native AOT deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot)

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

SharpDetect supports analysis of programs that are targeting .NET 8, 9, and 10.
Supported operating systems are Windows and Linux. Supported architecture is x64.

## Roadmap

Plans for next releases include:
- Improvements to the user interface of generated reports.
- Support for additional synchronization primitives in deadlock detection.
- Implementation of additional analysis plugins (e.g., data race detection plugin).

## Acknowledgments

SharpDetect is built with the help of numerous open-source libraries and components.
For detailed licensing information and full copyright notices, please see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## License

This project is licensed under the [Apache-2.0 license](LICENSE).
