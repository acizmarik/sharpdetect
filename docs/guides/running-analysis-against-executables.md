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

## Console Output

SharpDetect writes its own output to **standard error**, so the target's standard output stays clean (`sharpdetect run ... > app.out` captures only the target).

While the target runs, the first race on each field is printed as one line:

```
data race on Test.Field: write by T2 at Program.cs:8 ↔ write by T3 at Program.cs:13
```

At the end of the run a summary lists the findings in the same order and with the same counts as report (the top 5 fields, up to 3 races each), followed by the report path:

```
FastTrack: 1 data race in 1 field
  1. Test.Field  (1 race)
     - T2  Write Program.<>c.<<Main>$>b__0_0()  Program.cs:8
       T3  Write Program.<>c.<<Main>$>b__0_1()  Program.cs:13
       1 occurrence · static field
Report: /home/user/MyApp/SharpDetect_Report_20261105_134154_123.html
```

Each race starts with `-` and has one row per racing access: the thread, the access and the method that made it, and its source location (omitted when no symbols are available).
When a field has more races than are listed, or more fields raced than are listed, a line says how many more races are in the report.

The `Deadlock` plugin prints the same way. While the target runs, each deadlock is one line:

```
deadlock: T3 (Subject 2) waits for lock #10 held by T2 (Subject 1), T2 (Subject 1) waits for lock #11 held by T3 (Subject 2)
```

The summary lists, for each thread in the cycle, what it waits for and the first method outside system libraries on its stack:

```
Deadlock: 1 deadlock
  1. Deadlock between T3 (Subject 2) and T2 (Subject 1)
       T3 (Subject 2)  Waits for lock #10 held by T2 (Subject 1)  Program.<>c__DisplayClass0_0.<<Main>$>b__1()
       T2 (Subject 1)  Waits for lock #11 held by T3 (Subject 2)  Program.<>c__DisplayClass0_0.<<Main>$>b__0()
       2 threads are blocked in a cycle.
```

With `-v detailed`, the first race on each field is printed with the full stack traces of both accesses instead of one line, and each deadlock with one line per thread.
With `-v quiet`, nothing is printed while the target runs, and the summary keeps only the "Analysis incomplete" line (when the analysis is incomplete) and the report path.

## Reports

An HTML report is written after every run (see the `Report:` line at the end of the summary).
By default, it is named `SharpDetect_Report_<local date>_<local time with milliseconds>.html` and written to the current directory; a generated name never replaces an existing report (a `_2`, `_3`, ... suffix is added instead).

- `--report <path>` writes the report to a file when the path ends in `.html` (an existing file is replaced), and to a folder otherwise.
- `--no-report` skips the report.
- `-v|--verbosity quiet|normal|detailed|diagnostic` controls console output (`normal` by default). `quiet` prints only errors, `detailed` and `diagnostic` add analysis and profiler logs for troubleshooting.

These output options can also be combined with a configuration file (`sharpdetect run config.json -v quiet --report out.html`), where they override the values from the file.

## Exit Code

`sharpdetect run` exits with one of these codes:

| Code | Meaning |
| --- | --- |
| 0 | The analysis completed and found no issues. |
| 1 | The command line could not be parsed. |
| 2 | The analysis failed due to an internal error. |
| 3 | The configuration is invalid, or the plugin failed to load. |
| 4 | The analysis was cancelled. |
| 5 | The analysis completed and found one or more issues. |
| 6 | The analysis finished without finding issues, but some events or processes are missing from it. The report is still written. |
| 7 | The analysis found one or more issues, but some events or processes are missing from it (for example, the target crashed). The report is still written. |
