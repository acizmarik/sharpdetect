//////////////////////////////////////////////////////////////////////
////////////////////////// ARGUMENTS /////////////////////////////////
//////////////////////////////////////////////////////////////////////

var rid = Argument<string>("rid", GetDefaultRuntimeIdentifier());
var libraryExtension = rid.StartsWith("win") ? "dll" : "so";
var target = Argument("target", "Build-Local-Environment");
var configuration = Argument("configuration", "Debug");
var sdk = Argument("sdk", GetTargetFramework());

string GetTargetFramework()
{
    var tfm = XmlPeek("./Directory.Build.props", "/Project/PropertyGroup/TargetFramework/text()");
    if (string.IsNullOrEmpty(tfm))
        throw new Exception("Could not read <TargetFramework> from Directory.Build.props.");
    return tfm;
}

string GetDefaultRuntimeIdentifier()
{
    if (IsRunningOnWindows())
        return "win-x64";
    if (IsRunningOnLinux())
        return System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64
            ? "linux-arm64"
            : "linux-x64";

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
    DotNetClean("./SharpDetect.slnx", new DotNetCleanSettings
    {
        Configuration = configuration
    });
    CleanDirectory("./artifacts");
    CleanDirectory("./SharpDetect.Profiler/artifacts");
});

Task("Build-Managed")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetBuild("./SharpDetect.slnx", new DotNetBuildSettings
    {
        Configuration = configuration
    });

    // Some samples are intentionally left out of solution (cannot mix VSTest and MTP projects in a single solution)
    DotNetBuild("./Samples/SimpleDataRaceTestsMtp/SimpleDataRaceTestsMtp.csproj", new DotNetBuildSettings
    {
        Configuration = configuration
    });
    DotNetBuild("./Samples/SimpleDataRaceTestsVSTest/SimpleDataRaceTestsVSTest.csproj", new DotNetBuildSettings
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
    .IsDependentOn("Build-IPQ")
    .Does(() =>
{
    var profilerArtifactsDirectory = $"./SharpDetect.Profiler/artifacts/{rid}";
    EnsureDirectoryExists(profilerArtifactsDirectory);

    var exitCode = StartProcess("cmake", new ProcessSettings
    {
        Arguments = new ProcessArgumentBuilder()
            .Append("../..")
            .Append($"-DCMAKE_BUILD_TYPE={configuration}"),
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
        var profilerLibrary = GetProfilerLibraryPath(profilerName);
        if (!System.IO.File.Exists(profilerLibrary))
            throw new Exception($"Profiler library not found at: {profilerLibrary}");
        
        CopyFileToDirectory(profilerLibrary, nativeArtifactsDirectory);
    }
});

string GetProfilerLibraryPath(string profilerName)
{
    var baseDirectory = $"./SharpDetect.Profiler/artifacts/{rid}/{profilerName}";
    return rid.StartsWith("win")
        ? $"{baseDirectory}/{configuration}/{profilerName}.{libraryExtension}"
        : $"{baseDirectory}/{profilerName}.{libraryExtension}";
}

Task("Tests")
    .IsDependentOn("Build-Local-Environment")
    .Does(() =>
{
    DotNetTest("./SharpDetect.slnx", new DotNetTestSettings
    {
        Configuration = configuration,
        Loggers = new[] { "trx" },
        Collectors = new[] { "XPlat Code Coverage" },
        ResultsDirectory = "./TestResults",
        Settings = File("./CodeCoverage.runsettings")
    });
});

Task("Coverage-Report")
    .Does(() =>
{
    var reportDirectory = "./TestResults/CoverageReport";
    EnsureDirectoryExists(reportDirectory);

    var exitCode = StartProcess("dotnet", new ProcessSettings
    {
        Arguments = new ProcessArgumentBuilder()
            .Append("tool")
            .Append("run")
            .Append("reportgenerator")
            .Append("-reports:./TestResults/**/coverage.cobertura.xml")
            .Append($"-targetdir:{reportDirectory}")
            .Append("-reporttypes:Html;MarkdownSummaryGithub")
    });

    if (exitCode != 0)
        throw new Exception($"ReportGenerator failed with exit code: {exitCode}");

    Information($"Coverage report generated in: {reportDirectory}");
});

Task("CI-Prepare-Managed")
    .Does(() =>
{
    DotNetPublish("./SharpDetect.Cli", new DotNetPublishSettings
    {
        Configuration = configuration
    });
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
        OutputDirectory = outputDirectory
    });
    
    Information($"Package created in: {outputDirectory}");
    var packages = GetFiles($"{outputDirectory}/*.nupkg");
    foreach (var package in packages)
    {
        Information($"  - {package.GetFilename()}");
    }
});

RunTarget(target);