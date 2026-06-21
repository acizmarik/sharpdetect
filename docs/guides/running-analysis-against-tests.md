# Running Analysis Against Unit Tests

Command `sharpdetect run` can launch analysis of a test assembly.
Pass `--test`, and the target process becomes the test runner with your assembly loaded into it.

Two runners are supported via `--runner`:
- `Mtp` (default) — `Microsoft.Testing.Platform`-based projects (e.g. TUnit, NUnit/xUnit with MTP enabled)
- `VSTest` — classic `VSTest`-based projects (xUnit/NUnit/MSTest with `Microsoft.NET.Test.Sdk`)

## Example: Microsoft.Testing.Platform (TUnit)

```bash
sharpdetect run \
  --target "bin/Debug/net10.0/MyTests.dll" \
  --plugin "FastTrack" \
  --test \
  --filter "/*/*/*/RaceFact"
```

See `src/Samples/SimpleDataRaceTestsMtp` for a complete, runnable project using this form.

## Example: VSTest (xUnit)

```bash
sharpdetect run \
  --target "bin/Debug/net10.0/MyTests.dll" \
  --plugin "FastTrack" \
  --test \
  --runner VSTest \
  --filter "FullyQualifiedName~RaceFact"
```

See `src/Samples/SimpleDataRaceTestsVSTest` for a complete, runnable project using this form.

## Filtering Tests

Filter syntax depends on the runner: MTP uses [`/*/*/*/<test>`](https://github.com/Microsoft/vstest-docs/blob/main/docs/filter.md)-style hierarchical filters, VSTest uses [`--filter` expressions](https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests) like `FullyQualifiedName~<test>`.
If no filter is provided, then all tests from given assembly will be run.