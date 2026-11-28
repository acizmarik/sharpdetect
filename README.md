![SharpDetect](https://raw.githubusercontent.com/acizmarik/sharpdetect/main/docs/assets/logo/sharpdetect-logo.svg)

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://github.com/acizmarik/sharpdetect/blob/main/LICENSE)
[![GitHub Actions](https://github.com/acizmarik/sharpdetect/actions/workflows/main.yml/badge.svg)](https://github.com/acizmarik/sharpdetect/actions)
[![NuGet Stable Version](https://img.shields.io/nuget/v/SharpDetect)](https://www.nuget.org/packages/SharpDetect)
[![NuGet Preview Version](https://img.shields.io/nuget/vpre/SharpDetect)](https://www.nuget.org/packages/SharpDetect)

## Overview

SharpDetect is a dynamic analysis tool for .NET applications that detects concurrency issues such as data races and deadlocks.
It attaches a native CoreCLR profiler to the target process and routes runtime events through analysis plugins.

## Installation

SharpDetect is distributed as a .NET Tool on [NuGet](https://www.nuget.org/packages/SharpDetect):

```bash
dotnet tool install --global SharpDetect # Latest stable release
dotnet tool install --global SharpDetect --prerelease # Latest preview
```

## Quick Start

### 1. Create a Program to Analyze

Create and build a new console .NET application with the following code:

```csharp
// Two threads write to a shared field with no synchronization — a data race.
var t1 = new Thread(() => { Example.Test.Field = 1; });
var t2 = new Thread(() => { Example.Test.Field = 2; });

t1.Start();
t2.Start();
t1.Join();
t2.Join();

namespace Example
{
    static class Test { public static int Field; }
}
```

### 2. Run Analysis

Use the `sharpdetect run` command to run the analysis:

```bash
sharpdetect run \
  --target "bin/Debug/net10.0/TestApp.dll" \
  --plugin "FastTrack"
```

The command above launches target application, attaches profiler and configures analysis pipeline with the FastTrack plugin for data race detection.

### 3. View the Report

Each racy field is printed as one line while the target runs.
When the target exits, a summary of the detected races and the path to the generated report are printed:

```text
data race on Example.Test.Field: write by T2 at Program.cs:2 ↔ write by T3 at Program.cs:3

FastTrack: 1 data race in 1 field
  1. Example.Test.Field  (1 race)
     - T2  Write Program.<>c.<<Main>$>b__0_0()  Program.cs:2
       T3  Write Program.<>c.<<Main>$>b__0_1()  Program.cs:3
       1 occurrence · static field
Report: /home/user/TestApp/SharpDetect_Report_20261117_164559_335.html
Issues found.
```

The exit code is `5` when issues were found and `0` when none were.

Reports are self-contained HTML files. Each report includes:
- The affected field
- The participating threads
- Stack traces of the conflicting accesses (or a single frame when [stack-trace collection](https://github.com/acizmarik/sharpdetect/blob/main/docs/guides/running-analysis-against-executables.md#stack-traces) is disabled)

The same stack traces are printed to the console with `-v detailed`:

```text
[PID=64385] Data race on static field Example.Test.Field
    Earlier write by thread T2:
        at Program.<>c.<<Main>$>b__0_0() in /home/user/TestApp/Program.cs:2
        at System.Threading.Thread.StartHelper.RunWorker() (+2 more)
    Later write by thread T3:
        at Program.<>c.<<Main>$>b__0_1() in /home/user/TestApp/Program.cs:3
        at System.Threading.Thread.StartHelper.RunWorker() (+2 more)
```

## Documentation

More details for running and configuring analysis is described in `docs` folder.
- [Running analysis against regular .NET assembly](https://github.com/acizmarik/sharpdetect/blob/main/docs/guides/running-analysis-against-executables.md)
- [Running analysis against .NET test assembly](https://github.com/acizmarik/sharpdetect/blob/main/docs/guides/running-analysis-against-tests.md)
- [Advanced analysis configuration](https://github.com/acizmarik/sharpdetect/blob/main/docs/guides/running-analysis-with-configuration-file.md)
- [Supported synchronization primitives](https://github.com/acizmarik/sharpdetect/blob/main/docs/guides/supported-primitives.md)
- [Limitations / known issues](https://github.com/acizmarik/sharpdetect/blob/main/docs/guides/limitations.md)

## Analysis Plugins

### Data Race Detection — FastTrack

The `FastTrack` plugin detects data races using the FastTrack algorithm (Flanagan & Freund, 2009).
See [supported synchronization primitives](https://github.com/acizmarik/sharpdetect/blob/main/docs/guides/supported-primitives.md#data-race-detection--fasttrack) and [limitations](https://github.com/acizmarik/sharpdetect/blob/main/docs/guides/limitations.md#data-race-detection).

#### Configuration

To customize instrumentation scope, generate an analysis configuration file:

```bash
sharpdetect init \
  --plugin "FastTrack" \
  --target "<path/to/YourExecutableDotNetAssembly.dll>" \
  --output "AnalysisConfiguration.json"
```

### Deadlock Detection

The `Deadlock` plugin detects deadlocks by tracking lock acquisition order across threads and identifying circular wait conditions.
See [supported synchronization primitives](https://github.com/acizmarik/sharpdetect/blob/main/docs/guides/supported-primitives.md#deadlock-detection) and [limitations](https://github.com/acizmarik/sharpdetect/blob/main/docs/guides/limitations.md#deadlock-detection).

#### Configuration

To customize instrumentation scope, generate an analysis configuration file:

```bash
sharpdetect init \
  --plugin "Deadlock" \
  --target "<path/to/YourExecutableDotNetAssembly.dll>" \
  --output "AnalysisConfiguration.json"
```

## Building from Source

### Prerequisites

- .NET 11 SDK (and the .NET 8, 9 and 10 runtimes to run the end-to-end tests)
- C++20 compiler with CMake
- Platform-specific dependencies for [Native AOT deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot)

### Build Instructions

```bash
git clone https://github.com/acizmarik/sharpdetect.git
cd sharpdetect
git submodule update --init --recursive

cd src
dotnet cake.cs
```

## Platform Support

SharpDetect supports analysis of programs targeting .NET 8, 9, 10, and 11.
Supported operating systems are Windows and Linux (glibc 2.34 or newer). Supported architecture is x64.

## Acknowledgments

SharpDetect is built with the help of numerous open-source libraries and components.
For detailed licensing information and full copyright notices, please see [THIRD-PARTY-NOTICES.md](https://github.com/acizmarik/sharpdetect/blob/main/THIRD-PARTY-NOTICES.md).

## License

This project is licensed under the [Apache-2.0 license](https://github.com/acizmarik/sharpdetect/blob/main/LICENSE).
