# Configuring Analysis

Before running analysis, we must configure the following:

1) **Target** - what are we going to analyze? We need to describe target executable assembly.
2) **Runtime** - what is the host for target? We need to describe runtime environment for our target.
3) **Analysis** - how to analyze collected information? We need to describe plugins that will be used to collect and evaluate runtime events.

## Paths

Whenever user specifies a path within configuration file, they have the following options:
1) **Absolute path**
2) **Relative path** - such paths are always relative to the configuration file

### Environment Variables

Users can utilize environment variables when specifying paths. Syntax for environment variables is the following: `%MY_VARIABLE%`. Such construct will expand contents of `MY_VARIABLE` environment variable.
Along with user-defined environment variables, there is always defined also `SHARPDETECT_ROOT`, which points to the folder where SharpDetect is executed from.

## Example

SharpDetect expects these configurations as JSON files. An example of such configuration can be seen in the example below:

```json
{
    "Target": 
    {
        "Path": "TargetProgram/ExecutableAssembly.dll",
        "Architecture": "X64"
    },
    "Runtime": 
    {
        "Profiler":
        {
            "Clsid": "{b2c60596-b36d-460b-902a-3d91f5878529}",
            "Path": {
                "WindowsX64": "%SHARPDETECT_ROOT%/Profilers/win-x64/SharpDetect.Concurrency.Profiler.dll",
                "LinuxX64": "%SHARPDETECT_ROOT%/Profilers/linux-x64/SharpDetect.Concurrency.Profiler.so"
            }
        }
    },
    "Analysis": 
    {
        "Path": "%SHARPDETECT_ROOT%/Plugins/SharpDetect.Plugins.dll",
        "FullTypeName": "SharpDetect.Plugins.Deadlock.DeadlockPlugin"
    }
}
```

Configuration above specifies the following:
* Target application path is relative to the JSON on path `TargetProgram/ExecutableAssembly.dll`
* Target application architecture is x64 (that is 64 bit version of x86 architecture)
* Runtime path was not specified (in this case, we assume that the `dotnet` executable should be used)
* Runtime profiler ID is set to `{b2c60596-b36d-460b-902a-3d91f5878529}` (this is the ID of `SharpDetect.Concurrency.Profiler`)
* Runtime profiler paths are pointing to paths where profilers is installed
* Analysis path points to the assembly that contains plugins
* Analysis full type name specifies which plugin should be used for analysis (in this case it is `SharpDetect.Plugins.Deadlock.DeadlockPlugin`)