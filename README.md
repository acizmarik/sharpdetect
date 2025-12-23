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

### 1. Prepare Program to Analyze

Create and build a new console .NET application with the following code:

```csharp
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

*Note: Make sure to replace `<path/to/YourExecutableDotNetAssembly.dll>` with the actual path to the .NET assembly that you want to analyze.*

### 3. Run Analysis

When running analysis, SharpDetect will use the configuration specified in the file from previous step.
Start analysis using the `sharpdetect run` command:

```bash
sharpdetect run AnalysisConfiguration.json
```

Shortly after the deadlock occurs, you should see a log message similar to this:
```text
warn: SharpDetect.Core.Plugins.PluginBase[0]
     [PID=20611] Deadlock detected (affects 2 threads).
```

Finally when target program terminates, you should see a log message indicating that the report has been generated:
```text
Report stored to file: /home/user/Workspace/SharpDetect_Report_2025-12-23T09:58:28.5087901.html.
```

Reports are self-contained HTML files that can be opened in any modern web browser.

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

SharpDetect supports modern .NET SDKs that are still under active support by .NET team (versions 8, 9 and 10).
Supported platforms are Windows and Linux on x64 architecture.

## Acknowledgments

SharpDetect is built with the help of numerous open-source libraries and components.
For detailed licensing information and full copyright notices, please see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## License

This project is licensed under the [Apache-2.0 license](LICENSE).
