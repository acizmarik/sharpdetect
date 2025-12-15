var rid = Argument<string>("rid");
var libraryExtension = rid.StartsWith("win") ? "dll" : "so";
var target = Argument("target", "Build-Local-Environment");
var configuration = Argument("configuration", "Debug");
var sdk = Argument("sdk", "net10.0");

var artifactsDirectory = "./artifacts";
var nativeArtifactsDirectory = artifactsDirectory + "/Profilers/" + rid + "/";
var profilers = new string[]
{
    "SharpDetect.Concurrency.Profiler",
};

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

    DotNetPublish("./Extensibility/SharpDetect.Plugins", new DotNetPublishSettings
    {
        Configuration = configuration,
        OutputDirectory = "artifacts/Plugins"
    });
});

Task("Build-IPQ")
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
        Arguments = new ProcessArgumentBuilder().Append($"--build . --config {configuration}"),
        WorkingDirectory = artifactsDirectory
    });

    if (exitCode != 0)
        throw new Exception($"Failure during build. Exit code: {exitCode}.");
});

Task("Build-Local-Environment")
    .IsDependentOn("Build-Managed")
    .IsDependentOn("Build-IPQ")
    .IsDependentOn("Build-Profiler")
    .Does(() =>
{
    RunTarget("Copy-Native-Artifacts");
});

Task("Copy-Native-Artifacts")
    .Does(() =>
{
    if (!System.IO.Directory.Exists(artifactsDirectory))
    {
        System.IO.Directory.CreateDirectory(artifactsDirectory);
        Information($"Created directory: {artifactsDirectory}.");
    }

    if (!System.IO.Directory.Exists(nativeArtifactsDirectory))
    {
        System.IO.Directory.CreateDirectory(nativeArtifactsDirectory);
        Information($"Created directory: {nativeArtifactsDirectory}.");
    }

    var ipqLibrary = $"./SharpDetect.InterProcessQueue/bin/{configuration}/{sdk}/{rid}/native/SharpDetect.InterProcessQueue.{libraryExtension}";
    CopyFileToDirectory(ipqLibrary, nativeArtifactsDirectory);
    Information($"Copied IPQ native library to {nativeArtifactsDirectory}.");

    foreach (var profilerName in profilers)
    {
        var profilerLibrary = (rid.StartsWith("win"))
            ? $"./SharpDetect.Profiler/artifacts/{rid}/{profilerName}/{configuration}/{profilerName}.{libraryExtension}"
            : $"./SharpDetect.Profiler/artifacts/{rid}/{profilerName}/{profilerName}.{libraryExtension}";
        CopyFileToDirectory(profilerLibrary, $"{nativeArtifactsDirectory}");
    }

    Information($"Copied profiler native libraries to {nativeArtifactsDirectory}.");
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

Task("CI-Prepare-Managed")
    .Does(() =>
{
    DotNetPublish("./SharpDetect.Cli", new DotNetPublishSettings
    {
        Configuration = configuration
    });
    DotNetPublish("./Extensibility/SharpDetect.Plugins", new DotNetPublishSettings
    {
        Configuration = configuration,
        OutputDirectory = "artifacts/Plugins"
    });
});

Task("CI-Prepare-Native-Libs")
    .IsDependentOn("Build-IPQ")
    .IsDependentOn("Build-Profiler")
    .IsDependentOn("Copy-Native-Artifacts")
    .Does(() =>
{
    var files = GetFiles($"{nativeArtifactsDirectory}*");
    foreach (var file in files)
    {
        if (rid.StartsWith("linux"))
        {
            StartProcess("strip", new ProcessSettings
            {
                Arguments = new ProcessArgumentBuilder()
                    .Append("-s")
                    .Append(file.FullPath),
                WorkingDirectory = nativeArtifactsDirectory
            });
        }

        Information(file.FullPath);
    }
});

RunTarget(target);