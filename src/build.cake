var rid = Argument<string>("rid");
var libraryExtension = rid.StartsWith("win") ? "dll" : "so";
var target = Argument("target", "Build-Local-Environment");
var configuration = Argument("configuration", "Debug");

Task("Clean")
    .WithCriteria(c => HasArgument("rebuild"))
    .Does(() =>
{
    CleanDirectory($"./SharpDetect.Cli/bin/{configuration}");
    CleanDirectory($"./SharpDetect.Core/bin/{configuration}");
    CleanDirectory($"./SharpDetect.InterProcessQueue/bin/{configuration}");
    CleanDirectory($"./SharpDetect.Loader/bin/{configuration}");
    CleanDirectory($"./SharpDetect.Metadata/bin/{configuration}");
    CleanDirectory($"./SharpDetect.Reporting/bin/{configuration}");
    CleanDirectory($"./SharpDetect.Serialization/bin/{configuration}");
    CleanDirectory($"./Extensibility/SharpDetect.PluginHost/bin/{configuration}");
    CleanDirectory($"./Extensibility/SharpDetect.Plugins/bin/{configuration}");
    CleanDirectory($"./Samples/SimpleDeadlock/bin/{configuration}");
    CleanDirectory($"./Tests/SharpDetect.E2ETests/bin/{configuration}");
    CleanDirectory($"./Tests/SharpDetect.E2ETests.Subject/bin/{configuration}");
    CleanDirectory($"./SharpDetect.Profiler/artifacts");
    CleanDirectory($"./artifacts");
    Information("Cleanup finished.");
});

Task("Build-Managed")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetBuild("./SharpDetect.sln", new DotNetBuildSettings
    {
        Configuration = configuration
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
    .Does(() =>
{
    var artifactsDirectory = $"./SharpDetect.Profiler/artifacts/{rid}";
    if (!System.IO.Directory.Exists(artifactsDirectory))
    {
        System.IO.Directory.CreateDirectory(artifactsDirectory);
        Information($"Created directory: {artifactsDirectory}.");
    }

    var exitCode = StartProcess("cmake", new ProcessSettings
    {
        Arguments = new ProcessArgumentBuilder()
            .Append("../..")
            .Append($"-DCMAKE_BUILD_TYPE={configuration}")
            .Append("-DMSGPACK_USE_BOOST=off"),
        WorkingDirectory = artifactsDirectory
    });

    if (exitCode != 0)
        throw new Exception($"Failure during CMake configure. Exit code: {exitCode}.");

    exitCode = StartProcess("cmake", new ProcessSettings
    {
        Arguments = new ProcessArgumentBuilder().Append("--build ."),
        WorkingDirectory = artifactsDirectory
    });

    if (exitCode != 0)
        throw new Exception($"Failure during build. Exit code: {exitCode}.");
});

Task("Build-Local-Environment")
    .IsDependentOn("Build-IPQ")
    .IsDependentOn("Build-Profiler")
    .Does(() =>
{
    var artifactsDirectory = "./artifacts/";
    if (!System.IO.Directory.Exists(artifactsDirectory))
    {
        System.IO.Directory.CreateDirectory(artifactsDirectory);
        Information($"Created directory: {artifactsDirectory}.");
    }

    var ipqLibrary = $"./SharpDetect.InterProcessQueue/bin/{configuration}/net8.0/{rid}/native/SharpDetect.InterProcessQueue.{libraryExtension}";
    CopyFileToDirectory(ipqLibrary, artifactsDirectory);
    Information($"Copied IPQ native library to {artifactsDirectory}.");

    var profilerLibrary = (rid.StartsWith("win"))
        ? $"./SharpDetect.Profiler/artifacts/{rid}/SharpDetect.Profiler/{configuration}/SharpDetect.Profiler.{libraryExtension}"
        : $"./SharpDetect.Profiler/artifacts/{rid}/SharpDetect.Profiler/SharpDetect.Profiler.{libraryExtension}";
    CopyFileToDirectory(profilerLibrary, artifactsDirectory);
    Information($"Copied profiler native library to {artifactsDirectory}.");
});

Task("Tests")
    .IsDependentOn("Build-Local-Environment")
    .Does(() =>
{
    DotNetTest("./SharpDetect.sln", new DotNetTestSettings
    {
        Configuration = configuration,
    });
});

RunTarget(target);