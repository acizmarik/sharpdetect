var rid = Argument("rid", "win-x64");
var target = Argument("target", "Build-Local-Environment");
var configuration = Argument("configuration", "Debug");

Task("Clean")
    .WithCriteria(c => HasArgument("rebuild"))
    .Does(() =>
{
    CleanDirectory($"./SharpDetect.Cli/bin/{configuration}");
    CleanDirectory($"./SharpDetect.Events/bin/{configuration}");
    CleanDirectory($"./SharpDetect.InterProcessQueue/bin/{configuration}");
    CleanDirectory($"./SharpDetect.Loader/bin/{configuration}");
    CleanDirectory($"./SharpDetect.Metadata/bin/{configuration}");
    CleanDirectory($"./SharpDetect.Reporting/bin/{configuration}");
    CleanDirectory($"./SharpDetect.Serialization/bin/{configuration}");
    CleanDirectory($"./Extensibility/SharpDetect.Extensibility/bin/{configuration}");
    CleanDirectory($"./Extensibility/SharpDetect.Extensibility.Abstractions/bin/{configuration}");
    CleanDirectory($"./Extensibility/SharpDetect.MethodDescriptors/bin/{configuration}");
    CleanDirectory($"./Extensibility/SharpDetect.Plugins/bin/{configuration}");
    CleanDirectory($"./Samples/SimpleDeadlock/bin/{configuration}");
    CleanDirectory($"./Tests/SharpDetect.E2ETests/bin/{configuration}");
    CleanDirectory($"./Tests/SharpDetect.E2ETests.Subject/bin/{configuration}");
    CleanDirectory($"./SharpDetect.Profiler/artifacts");
    CleanDirectory($"./artifacts");
    Information("Cleanup finished.");
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetRestore("./SharpDetect.sln");
});

Task("Build-Managed")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    DotNetBuild("./SharpDetect.sln", new DotNetBuildSettings
    {
        Configuration = configuration,
        NoRestore = true,
    });
});

Task("Build-IPQ")
    .IsDependentOn("Build-Managed")
    .Does(() =>
{
    DotNetPublish("./SharpDetect.InterProcessQueue", new DotNetPublishSettings
    {
        Configuration = configuration,
        Runtime = rid
    });
});

Task("Build-Profiler")
    .IsDependentOn("Build-IPQ")
    .Does(() =>
{
    var artifactsDirectory = "./SharpDetect.Profiler/artifacts";
    if (!System.IO.Directory.Exists(artifactsDirectory))
    {
        System.IO.Directory.CreateDirectory(artifactsDirectory);
        Information($"Created directory: {artifactsDirectory}.");
    }

    StartProcess("cmake", new ProcessSettings
    {
        Arguments = new ProcessArgumentBuilder()
            .Append("..")
            .Append($"-DCMAKE_BUILD_TYPE={configuration}")
            .Append("-DMSGPACK_USE_BOOST=off"),
        WorkingDirectory = artifactsDirectory
    });

    StartProcess("cmake", new ProcessSettings
    {
        Arguments = new ProcessArgumentBuilder().Append("--build ."),
        WorkingDirectory = artifactsDirectory
    });
});

Task("Build-Local-Environment")
    .IsDependentOn("Build-Profiler")
    .Does(() =>
{
    var artifactsDirectory = "./artifacts";
    if (!System.IO.Directory.Exists(artifactsDirectory))
    {
        System.IO.Directory.CreateDirectory(artifactsDirectory);
        Information($"Created directory: {artifactsDirectory}.");
    }

    var ipqLibrary = $"./SharpDetect.InterProcessQueue/bin/{configuration}/net8.0/win-x64/native/SharpDetect.InterProcessQueue.dll";
    CopyFileToDirectory(ipqLibrary, artifactsDirectory);
    Information($"Copied IPQ native library to {artifactsDirectory}.");

    var profilerLibrary = $"./SharpDetect.Profiler/artifacts/SharpDetect.Profiler/{configuration}/SharpDetect.Profiler.dll";
    CopyFileToDirectory(profilerLibrary, artifactsDirectory);
    Information($"Copied profiler native library to {artifactsDirectory}.");
});

RunTarget(target);