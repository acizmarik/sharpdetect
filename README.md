# SharpDetect

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://github.com/acizmarik/sharpdetect/LICENSE.md)
[![GitHub Actions](https://github.com/acizmarik/sharpdetect/actions/workflows/main.yml/badge.svg)](https://github.com/acizmarik/sharpdetect/actions)
[![NuGet Stable Version](https://img.shields.io/nuget/v/SharpDetect)](https://www.nuget.org/packages/SharpDetect)
[![NuGet Preview Version](https://img.shields.io/nuget/vpre/SharpDetect)](https://www.nuget.org/packages/SharpDetect)

## Overview

SharpDetect is a dynamic analysis tool for .NET programs.
It detects concurrency issues - data races and deadlocks.
Monitoring and instrumentation support is implemented using custom profiler.
Analysis is performed by plugins that evaluate runtime properties of the target program.

## Installation

SharpDetect is distributed as a .NET Tool on [NuGet](https://www.nuget.org/packages/SharpDetect):

```bash
dotnet tool install --global SharpDetect # Latest stable release
dotnet tool install --global SharpDetect --prerelease # Latest preview
```

## Quick Start

### 1. Create a Program to Analyze

Create and build a new console .NET application (targeting .NET 8, 9, or 10) with the following code:

```csharp
// Two threads write to a shared field with no synchronization — a data race.
var thread1 = new Thread(() => { Example.Test.Field = 1; });
var thread2 = new Thread(() => { Example.Test.Field = 2; });

thread1.Start();
thread2.Start();
thread1.Join();
thread2.Join();

namespace Example
{
    static class Test { public static int Field; }
}
```

### 2. Create an Analysis Configuration File

Use the `sharpdetect init` command to generate a configuration file:

```bash
sharpdetect init \
  --plugin "FastTrack" \
  --target "<path/to/YourExecutableDotNetAssembly.dll>" \
  --output "AnalysisConfiguration.json"
```

*Replace `<path/to/YourExecutableDotNetAssembly.dll>` with the path to your compiled assembly (e.g., `bin/Debug/net10.0/MyApp.dll`).*

### 3. Run Analysis

Use the `sharpdetect run` command with configuration file from previous step to execute the analysis:

```bash
sharpdetect run AnalysisConfiguration.json
```

### 4. View the Report

When a data race is detected, a log message is emitted:

```text
warn: SharpDetect.Plugins.DataRace.FastTrack.FastTrackPlugin[0]
      [PID=65758] Data race on static field Example.Test.Field
          Current write by thread T3:
              at Program/<>c.<<Main>$>b__0_1:IL_0002
          Previous write by thread T2:
              at Program/<>c.<<Main>$>b__0_0:IL_0002
```

When the target program terminates, the path to the generated report is printed:

```text
Report stored to file: /home/user/Workspace/SharpDetect_Report_20251223_095828.html.
```

Reports are self-contained HTML files.
Each report includes the affected field, the racing threads and their stack frames at the point of the conflicting accesses.

## Analysis Plugins

### Data Race Detection — FastTrack

The `FastTrack` plugin detects data races using the FastTrack algorithm (Flanagan & Freund, 2009).

#### Supported Synchronization Primitives
- `System.Threading.Monitor`
- `System.Threading.Lock`
- `System.Threading.SemaphoreSlim`
- `System.Threading.Volatile` (including `volatile` field modifier)

#### Supported Threading Primitives
- `System.Threading.Thread`
- `System.Threading.Tasks.Task`

#### Supported Memory Accesses
- Static fields (`LDSFLD`, `STSFLD`)
- Instance fields (`LDFLD`, `STFLD`)

#### Usage

```bash
sharpdetect init \
  --plugin "FastTrack" \
  --target "<path/to/YourExecutableDotNetAssembly.dll>" \
  --output "AnalysisConfiguration.json"
```
  
#### Known Limitations
- Memory accesses protected by unsupported primitives may be reported as false positives
- Analysis introduces overhead 

### Deadlock Detection

The `Deadlock` plugin detects deadlocks by tracking lock acquisition order across threads and identifying circular wait conditions.

#### Supported Synchronization Primitives
- `System.Threading.Monitor`
- `System.Threading.Lock`

#### Supported Threading Primitives
- `System.Threading.Thread`

#### Usage

```bash
sharpdetect init \
  --plugin "Deadlock" \
  --target "<path/to/YourExecutableDotNetAssembly.dll>" \
  --output "AnalysisConfiguration.json"
```

#### Known Limitations
- Deadlocks involving unsupported primitives may not be detected
- Analysis introduces overhead

## Building from Source

### Prerequisites

- .NET 10 SDK
- C++20 compiler with CMake
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

SharpDetect supports analysis of programs targeting .NET 8, 9, and 10.
Supported operating systems are Windows and Linux. Supported architecture is x64.

## Acknowledgments

SharpDetect is built with the help of numerous open-source libraries and components.
For detailed licensing information and full copyright notices, please see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## License

This project is licensed under the [Apache-2.0 license](LICENSE).
