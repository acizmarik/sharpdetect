# SharpDetect

This is an experimental dynamic analysis framework for .NET programs

## Build

The following steps describe all dependencies and steps needed to perform in order to fully build SharpDetect, both its managed (.NET) and its unmanaged (.NET AOT-compiled) modules. In case something is not clear, also checkout pipeline `.github/main.yml`. All the steps mentioned below are implemented by the pipeline too.

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

## Test

Project contains either unit tests (.NET only), or end-2-end tests. Both test categories are implemented in .NET solutions. Keep in mind that end-2-end tests can take some time, as there is a lot of IO going on and many processes are being spawned as well.

```bash
# Unit tests
dotnet test src/Tests/SharpDetect.UnitTests/SharpDetect.UnitTests.csproj

# End-2-end tests
dotnet test src/Tests/SharpDetect.E2ETests/SharpDetect.E2ETests.csproj
```

## Custom Analysis

The following steps describes how to run custom analysis using SharpDetect.

### Global SharpDetect Configuration

This configuration is needed to perform only once. Ensure that you updated the `appsettings.yaml` that should be located next to the `SharpDetect.Console` executable. Provide at least, the missing absolute paths for the `PluginsFolder` and the `ProfilerPath` properties. 

```bash
# Folder with plugins (will also traverse all its subfolders)
PluginsPath: <sharpdetect-installation-folder>/Plugins
# Path to the profiler native library
ProfilerPath: <sharpdetect-installation-folder>/SharpDetect.Profiler.dll
```

### Subject Program

Create a standard .NET console application as a subject for analysis.

```bash
mkdir TestSubject
cd TestSubject

# This will create and build a "Hello-World" style project
dotnet new console
dotnet build
```

### Subject Program Analysis Configuration

Create a new file that describes the analysis of the subject program. You can use the template below. Basically any setting from the global configuration can be overriden as well. Additional parameters to plugins can be provided too.

```yaml
---
TargetAssembly: <subject-program-absolute-path>/SubjectProgram.dll
# Optional: override working directory (provide full path)
WorkingDirectory: <working-directory-absolute-path>

# Settings for the instrumentation and IL inspecting module
Rewriting:
  Enabled: true
  Strategy: OnlyPatterns
  # Provide instrumentation patterns (for example namespaces to be analyzed)
  Patterns:
    - { Pattern: "TestNamespace", Target: Method }
    - { Pattern: "TestNamespace", Target: Field }

# Optional plugin settings
Plugins:
  Echo:
    Setting1: 123
  Eraser:
    SomeOtherSetting: false
```

### Running Analysis

In order to run an analysis, you need to run `SharpDetect.Console` with verb `analyze` and provide it a configuration for the subject program and plugin names. Invoking multiple plugins is possible by delimiting their names with the pipe character.

```bash
dotnet SharpDetect.Console.dll analyze <absolute-path-to-configuration> Echo|Eraser
```

## Plugins

Currently, the following plugins can be used:
* `Nop` - **N**o **o**peration **p**lugin that does, as the name suggests :), nothing
* `Echo` - Logs events to console on the `Information` level. It is very verbose.
* `Eraser` - Basic implementation of the lock-set data-race detector
* `FastTrack` - Basic implementation of the happens-before data-race detector
