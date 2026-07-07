# Running Analysis With a Configuration File

Command `sharpdetect run` accepts a configuration file as its positional argument instead of inline `--plugin`/`--target` options.
This is useful once you need options that don't have a dedicated CLI flag (working directory, extra environment variables, custom report locations, plugin-specific configuration, ...).

A configuration file and inline options are mutually exclusive — you can't pass both.

## Generating a Configuration File

Command `sharpdetect init` writes a starter file using the same options as `sharpdetect run`:

```bash
sharpdetect init \
  --target "bin/Debug/net10.0/MyApp.dll" \
  --plugin "FastTrack" \
  --output AnalysisDescriptor.json
```

This produces `AnalysisDescriptor.json` with following content:

```json
{
  "Target": {
    "Path": "bin/Debug/net10.0/MyApp.dll",
    "RedirectInputOutput": {
      "SingleConsoleMode": true
    }
  },
  "Analysis": {
    "Configuration": {
      "SkipInstrumentationForAssemblies": [ "System.", "Microsoft." ]
    },
    "PluginName": "FastTrack",
    "RenderReport": true,
    "LogLevel": "Warning"
  },
  "Runtime": {
    "Profiler": {
      "LogLevel": "Warning"
    }
  }
}
```

Command `init` also accepts `--test`, `--runner`, `--filter` and `--instrument-system-libraries`, mirroring command `run`.
Edit the generated file as required, then run it:

```bash
sharpdetect run AnalysisDescriptor.json
```

## File Structure

### `Target` (required)

#### Properties

- `Path` (required)
  - Target assembly path
- `Args`
  - Command-line arguments for the target process
- `WorkingDirectory`
  - Working directory for the target process
- `AdditionalEnvironmentVariables`
  - Array of `{ "Key": ..., "Value": ... }` pairs merged into the target's environment.
- `RedirectInputOutput`
  - Properties:
    - `SingleConsoleMode`
      - Wires the target's stdin/stdout/stderr straight through to the console
      - Either `true` or `false`
    - `StdinFilePath`
      - Path to redirect standard input.
    - `StdoutFilePath`
      - Path to redirect standard output.
    - `StderrFilePath`
      - Path to redirect standard error.
- `Kind`
    - Either `Executable` (default) or `TestAssembly`
- `Test` (required when `Kind` is `TestAssembly`)
  - Properties:
    - `Runner`
      - Either `Mtp` (default) or `VsTest`
    - `Filter`
      - See [Running Analysis Against Unit Tests](running-analysis-against-tests.md) for filter syntax
    - `AdditionalRunnerArgs`
      - Additional arguments to pass to test runner

### `Analysis` (required)

- `PluginName` or `PluginFullTypeName` **(one of them is required)**
  - Short name from `[PluginMetadata]` (e.g. `FastTrack`, `Deadlock`)
  - Or a fully-qualified plugin type name
- `Path`
  - Plugin assembly to load
  - Defaults to `%SHARPDETECT_ROOT%/SharpDetect.Plugins.dll`
- `Configuration`
  - Plugin-specific options object
  - See [Data Race Plugin Options](#data-race-plugin-options) for the `FastTrack` plugin configuration
- `RenderReport`
  - Determines whether to write an HTML report after the `run` command
- `LogLevel`
  - `Trace` | `Debug` | `Information` | `Warning` | `Error` | `Critical` | `None`
  - Defaults to `Warning`
- `ReportsFolder`
  - Folder for generated reports
- `ReportFileName`
  - Name for the generated report file
- `TemporaryFilesFolder`
  - Path where temporary files are stored

#### Data Race Plugin Options

Data race analysis accepts the following `Analysis.Configuration` properties.
- `EnableFieldsAccessInstrumentation`
  - Instrument field reads and writes
  - Either `true` (default) or `false`
- `SkipInstrumentationForAssemblies`
  - Array of assembly-name prefixes that are excluded from instrumentation
  - Defaults to `[ "System.", "Microsoft." ]`
- `EnableStackTraceCollection`
  - Capture stack traces for conflicting accesses instead of the single frame
  - Either `true` or `false` (default)
  - When disabled, reports show only the frame at the point of the access
- `StackTraceCollectionMaxDepth`
  - Maximum number of frames captured per access when `EnableStackTraceCollection` is `true`
  - Defaults to `8`
- `StackTraceCollectionForFields`
  - Array of fully-qualified field names (`Namespace.Type.Field`) to restrict stack-trace collection to
  - Matching is by prefix, so a value ending in `.` matches every field of a type (e.g. `MyApp.Test.` matches `MyApp.Test.Field` and `MyApp.Test.AnotherField`)
  - Defaults to `[]`, which collects stack traces for all instrumented fields

Stack-trace collection walks the target thread's call stack at each analyzed access, so enabling it for a broad set of fields adds overhead. Use `StackTraceCollectionForFields` to limit collection to the fields you are investigating.

##### Example

```json
{
  "Analysis": {
    "PluginName": "FastTrack",
    "RenderReport": true,
    "Configuration": {
      "EnableStackTraceCollection": true,
      "StackTraceCollectionMaxDepth": 8,
      "StackTraceCollectionForFields": [ "MyApp.Test.Field" ]
    }
  }
}
```

### `Runtime` (optional)

- `Host`
  - Properties:
    - `Path`
      - Path to dotnet host
      - Default is `dotnet`
    - `Args`
      - Arguments for dotnet host
    - `AdditionalEnvironmentVariables`
      - Array of `{ "Key": ..., "Value": ... }` pairs merged into the target's environment.
- `Profiler`
  - Properties:
    - `PathWindowsX64`
      - Path for native Windows x86_64 profiler
      - Defaults to built-in profiler in package
    - `PathLinuxX64`
      - Path for native Linux x86_64 profiler
      - Defaults to built-in profiler in package
    - `Clsid`
      - Class identifier for profiler
      - Defaults to built-in profiler in package
    - `LogLevel`
      - `Information` | `Warning` | `Error`
      - Defaults to `Warning`

## Environment Variable Expansion

All path-like fields (`Target.Path`, `Analysis.Path`, `Runtime.Profiler.Path*`, ...) expand environment variables, including two that SharpDetect sets automatically before launch:
- `SHARPDETECT_ROOT`: directory containing the `sharpdetect` tool.
- `SHARPDETECT_PROFILERS`: directory containing the native profiler libraries.

The syntax for environment variable expansion is `%SHARPDETECT_ROOT%`.
