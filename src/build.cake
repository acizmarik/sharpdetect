//////////////////////////////////////////////////////////////////////
////////////////////////// ARGUMENTS /////////////////////////////////
//////////////////////////////////////////////////////////////////////

var rid = Argument<string>("rid", GetDefaultRuntimeIdentifier());
var libraryExtension = rid.StartsWith("win") ? "dll" : "so";
var target = Argument("target", "Build-Local-Environment");
var configuration = Argument("configuration", "Debug");
var sdk = Argument("sdk", "net10.0");

string GetDefaultRuntimeIdentifier()
{
    if (IsRunningOnWindows())
        return "win-x64";
    if (IsRunningOnLinux())
        return "linux-x64";
    
    throw new Exception("Unknown or unsupported platform. Please specify the runtime identifier using --rid parameter.");
}

Information($"Executing target: {target}.");
Information($"Using runtime identifier: {rid}.");

//////////////////////////////////////////////////////////////////////
////////////////////////// CONFIGURATION /////////////////////////////
//////////////////////////////////////////////////////////////////////

var artifactsDirectory = "./artifacts";
var nativeArtifactsDirectory = artifactsDirectory + "/Profilers/" + rid + "/";
var profilers = new string[] { "SharpDetect.Concurrency.Profiler" };

//////////////////////////////////////////////////////////////////////
////////////////////////////// TASKS /////////////////////////////////
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .WithCriteria(c => HasArgument("rebuild"))
    .Does(() =>
{
    var directoriesToClean = new[]
    {
        $"./SharpDetect.Cli/bin/{configuration}",
        $"./SharpDetect.Core/bin/{configuration}",
        $"./SharpDetect.InterProcessQueue/bin/{configuration}",
        $"./SharpDetect.Loader/bin/{configuration}",
        $"./SharpDetect.Metadata/bin/{configuration}",
        $"./SharpDetect.Reporting/bin/{configuration}",
        $"./SharpDetect.Serialization/bin/{configuration}",
        $"./SharpDetect.Communication/bin/{configuration}",
        $"./SharpDetect.Worker/bin/{configuration}",
        $"./Extensibility/SharpDetect.PluginHost/bin/{configuration}",
        $"./Extensibility/SharpDetect.Plugins/bin/{configuration}",
        $"./Samples/SimpleDeadlock/bin/{configuration}",
        $"./Tests/SharpDetect.E2ETests/bin/{configuration}",
        $"./Tests/SharpDetect.E2ETests.Subject/bin/{configuration}",
        $"./Tests/SharpDetect.InterProcessQueue.Tests/bin/{configuration}",
        $"./SharpDetect.Profiler/artifacts",
        $"./artifacts"
    };

    foreach (var dir in directoriesToClean)
    {
        if (DirectoryExists(dir))
            CleanDirectory(dir);
    }
});

Task("Build-Managed")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetBuild("./SharpDetect.sln", new DotNetBuildSettings
    {
        Configuration = configuration
    });
    
    EnsureDirectoryExists("artifacts/Plugins");
    DotNetPublish("./Extensibility/SharpDetect.Plugins", new DotNetPublishSettings
    {
        Configuration = configuration
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
    var profilerArtifactsDirectory = $"./SharpDetect.Profiler/artifacts/{rid}";
    EnsureDirectoryExists(profilerArtifactsDirectory);

    var exitCode = StartProcess("cmake", new ProcessSettings
    {
        Arguments = new ProcessArgumentBuilder()
            .Append("../..")
            .Append($"-DCMAKE_BUILD_TYPE={configuration}")
            .Append("-DMSGPACK_USE_BOOST=off"),
        WorkingDirectory = profilerArtifactsDirectory
    });

    if (exitCode != 0)
        throw new Exception($"CMake configure failed with exit code: {exitCode}");

    exitCode = StartProcess("cmake", new ProcessSettings
    {
        Arguments = new ProcessArgumentBuilder()
            .Append("--build")
            .Append(".")
            .Append("--config")
            .Append(configuration),
        WorkingDirectory = profilerArtifactsDirectory
    });

    if (exitCode != 0)
        throw new Exception($"CMake build failed with exit code: {exitCode}");
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
    EnsureDirectoryExists(artifactsDirectory);
    EnsureDirectoryExists(nativeArtifactsDirectory);

    var ipqLibrary = $"./SharpDetect.InterProcessQueue/bin/{configuration}/{sdk}/{rid}/native/SharpDetect.InterProcessQueue.{libraryExtension}";
    if (!System.IO.File.Exists(ipqLibrary))
        throw new Exception($"IPQ native library not found at: {ipqLibrary}");    
    CopyFileToDirectory(ipqLibrary, nativeArtifactsDirectory);

    foreach (var profilerName in profilers)
    {
        var profilerLibrary = (rid.StartsWith("win"))
            ? $"./SharpDetect.Profiler/artifacts/{rid}/{profilerName}/{configuration}/{profilerName}.{libraryExtension}"
            : $"./SharpDetect.Profiler/artifacts/{rid}/{profilerName}/{profilerName}.{libraryExtension}";
        
        if (!System.IO.File.Exists(profilerLibrary))
            throw new Exception($"Profiler library not found at: {profilerLibrary}");
        
        CopyFileToDirectory(profilerLibrary, nativeArtifactsDirectory);
    }
});

Task("Tests")
    .IsDependentOn("Build-Local-Environment")
    .Does(() =>
{
    DotNetTest("./SharpDetect.sln", new DotNetTestSettings
    {
        Configuration = configuration
    });
});

Task("CI-Prepare-Managed")
    .Does(() =>
{
    DotNetPublish("./SharpDetect.Cli", new DotNetPublishSettings
    {
        Configuration = configuration
    });
    
    EnsureDirectoryExists("artifacts/Plugins");
    DotNetPublish("./Extensibility/SharpDetect.Plugins", new DotNetPublishSettings
    {
        Configuration = configuration
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
            Information($"Stripping symbols from: {file.GetFilename()}");
            var exitCode = StartProcess("strip", new ProcessSettings
            {
                Arguments = new ProcessArgumentBuilder()
                    .Append("-s")
                    .Append(file.FullPath)
            });
            
            if (exitCode != 0)
                Warning($"Failed to strip {file.GetFilename()}, exit code: {exitCode}");
        }
    }
});

Task("CI-Pack")
    .IsDependentOn("CI-Prepare-Managed")
    .Does(() =>
{
    var outputDirectory = $"{artifactsDirectory}";
    EnsureDirectoryExists(outputDirectory);
    
    DotNetPack("./SharpDetect.Cli", new DotNetPackSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        NoBuild = false
    });
    
    Information($"Package created in: {outputDirectory}");
    var packages = GetFiles($"{outputDirectory}/*.nupkg");
    foreach (var package in packages)
    {
        Information($"  - {package.GetFilename()}");
    }
});

RunTarget(target);