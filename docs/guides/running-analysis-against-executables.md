# Running Analysis Against an Executable

Command `sharpdetect run` can launch analysis of a regular .NET executable assembly.
Pass `--target` pointing at the assembly's `.dll`, and optionally `--plugin` naming the analysis to run (`FastTrack` when omitted).
When running analysis, the target becomes a normal child process with the profiler attached.
Events emitted by the target will be analyzed by the specified plugin.

## Example: Data Race Detection

```bash
sharpdetect run \
  --target "bin/Debug/net10.0/MyApp.dll" \
  --plugin "FastTrack"
```

See `src/Samples/SimpleDataRace` for a complete, runnable project using this form.

## Example: Deadlock Detection

```bash
sharpdetect run \
  --target "bin/Debug/net10.0/MyApp.dll" \
  --plugin "Deadlock"
```

See `src/Samples/SimpleDeadlock` for a complete, runnable project using this form.

## Plugins

`--plugin` accepts either the short name registered via `PluginMetadataAttribute` (e.g. `FastTrack`, `Deadlock`) or a fully-qualified type name if you're loading a custom plugin assembly.
When `--plugin` is omitted, `FastTrack` (data race detection) is used.
Run `sharpdetect plugins` to list the available plugins with their descriptions (`--plugin-assembly <path>` inspects a custom plugin assembly).

## Target Arguments

Everything after `--` is passed to the target unchanged, one argument per token:

```bash
sharpdetect run \
  --target "bin/Debug/net10.0/MyApp.dll" \
  -- --port 5000 "two words"
```

## Standard Input/Output

By default, the target's stdin/stdout/stderr are wired straight through to the console (`SingleConsoleMode`), so the target behaves the same as if it had been launched with `dotnet MyApp.dll` directly.

## Instrumenting System Libraries

By default, `System.*`/`Microsoft.*` assemblies are excluded from instrumentation.
Pass `--instrument-system-libraries` to also analyze events raised from inside the BCL.

```bash
sharpdetect run \
  --target "bin/Debug/net10.0/MyApp.dll" \
  --plugin "FastTrack" \
  --instrument-system-libraries
```

## Stack Traces

By default, data race reports show a single frame for each conflicting access.
Pass `--stack-traces` to collect a stack trace for every analyzed access, and `--stack-trace-depth <n>` to change the maximum number of frames (8 by default).
Stack-trace collection adds overhead; to restrict it to specific fields, use `StackTraceCollectionForFields` in a [configuration file](running-analysis-with-configuration-file.md#data-race-plugin-options).

```bash
sharpdetect run \
  --target "bin/Debug/net10.0/MyApp.dll" \
  --stack-traces \
  --stack-trace-depth 16
```

## Exit Code and Reports

An HTML report is written after every run (see the `Report stored to file:` line printed to the console).
`sharpdetect run` exits with one of these codes:

| Code | Meaning |
| --- | --- |
| 0 | The analysis completed and found no issues. |
| 1 | The command line could not be parsed. |
| 2 | The analysis failed due to an internal error. |
| 3 | The configuration is invalid, or the plugin failed to load. |
| 4 | The analysis was cancelled. |
| 5 | The analysis completed and found one or more issues. |
| 6 | The analysis finished, but some events or processes are missing from it. The report is still written. |