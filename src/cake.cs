// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#:sdk Cake.Sdk@6.2.0

//////////////////////////////////////////////////////////////////////
////////////////////////// ARGUMENTS /////////////////////////////////
//////////////////////////////////////////////////////////////////////

var rid = Argument("rid", GetDefaultRuntimeIdentifier());
var libraryExtension = rid.StartsWith("win") ? "dll" : "so";
var target = Argument("target", "Build-Local-Environment");
var configuration = Argument("configuration", "Debug");
var sdk = Argument("sdk", GetTargetFramework());

string GetTargetFramework()
{
    var tfm = XmlPeek("./Directory.Build.props", "/Project/PropertyGroup/TargetFramework/text()");
    return !string.IsNullOrEmpty(tfm) ? tfm : throw new Exception("Could not read <TargetFramework> from Directory.Build.props.");
}

string GetDefaultRuntimeIdentifier()
{
    if (IsRunningOnWindows())
        return "win-x64";
    if (IsRunningOnLinux())
        return "linux-x64";
    
    throw new Exception("Unknown or unsupported platform. Please specify the runtime identifier using --rid parameter.");
}

//////////////////////////////////////////////////////////////////////
////////////////////////// CONFIGURATION /////////////////////////////
//////////////////////////////////////////////////////////////////////

const string artifactsDirectory = "./artifacts";
var nativeArtifactsDirectory = artifactsDirectory + "/Profilers/" + rid + "/";
var profilers = new[] { "SharpDetect.Concurrency.Profiler" };
var warningsAsErrorsSettings = new DotNetMSBuildSettings
{
    TreatAllWarningsAs = MSBuildTreatAllWarningsAs.Error
};

//////////////////////////////////////////////////////////////////////
////////////////////// SETUP / TEARDOWN //////////////////////////////
//////////////////////////////////////////////////////////////////////

Setup(_ =>
{
    Information($"Target:                     {target}");
    Information($"Configuration:              {configuration}");
    Information($"Runtime identifier:         {rid}");
    Information($"Target framework:           {sdk}");
    Information($"Artifacts directory:        {artifactsDirectory}");
    Information($"Native artifacts directory: {nativeArtifactsDirectory}");
});

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

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetRestore("./SharpDetect.slnx");
    DotNetRestore("./Samples/SimpleDataRaceTestsMtp/SimpleDataRaceTestsMtp.csproj");
    DotNetRestore("./Samples/SimpleDataRaceTestsVSTest/SimpleDataRaceTestsVSTest.csproj");
});

Task("Build-Managed")
    .IsDependentOn("Restore")
    .Does(() =>
{
    DotNetBuild("./SharpDetect.slnx", new DotNetBuildSettings
    {
        Configuration = configuration,
        NoRestore = true,
        MSBuildSettings = warningsAsErrorsSettings
    });

    DotNetBuild("./Samples/SimpleDataRaceTestsMtp/SimpleDataRaceTestsMtp.csproj", new DotNetBuildSettings
    {
        Configuration = configuration,
        NoRestore = true,
        MSBuildSettings = warningsAsErrorsSettings
    });
    DotNetBuild("./Samples/SimpleDataRaceTestsVSTest/SimpleDataRaceTestsVSTest.csproj", new DotNetBuildSettings
    {
        Configuration = configuration,
        NoRestore = true,
        MSBuildSettings = warningsAsErrorsSettings
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
            .Append(configuration)
            .Append("--parallel"),
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

Task("Test-Unit")
    .IsDependentOn("Build-Managed")
    .Does(() =>
{
    DotNetTest("./SharpDetect.slnx", new DotNetTestSettings
    {
        Configuration = configuration,
        Filter = "FullyQualifiedName!~SharpDetect.E2ETests",
        Loggers = [ "trx" ],
        Collectors = [ "XPlat Code Coverage" ],
        ResultsDirectory = "./TestResults",
        Settings = File("./CodeCoverage.runsettings"),
        NoRestore = true,
        NoBuild = true,
        ToolTimeout = TimeSpan.FromMinutes(10)
    });
});

Task("Test-E2E")
    .IsDependentOn("Build-Local-Environment")
    .Does(() =>
{
    DotNetTest("./Tests/SharpDetect.E2ETests/SharpDetect.E2ETests.csproj", new DotNetTestSettings
    {
        Configuration = configuration,
        Loggers = [ "trx" ],
        Collectors = [ "XPlat Code Coverage" ],
        ResultsDirectory = "./TestResults",
        Settings = File("./CodeCoverage.runsettings"),
        NoRestore = true,
        NoBuild = true,
        ToolTimeout = TimeSpan.FromMinutes(20)
    });
});

Task("Tests")
    .IsDependentOn("Test-Unit")
    .IsDependentOn("Test-E2E");

Task("Test-Native")
    .Does(() =>
{
    var testBuildDirectory = $"./SharpDetect.Profiler/artifacts/{rid}-tests";
    EnsureDirectoryExists(testBuildDirectory);

    var exitCode = StartProcess("cmake", new ProcessSettings
    {
        Arguments = new ProcessArgumentBuilder()
            .Append("../..")
            .Append($"-DCMAKE_BUILD_TYPE={configuration}")
            .Append("-DSHARPDETECT_BUILD_TESTS=ON"),
        WorkingDirectory = testBuildDirectory
    });
    if (exitCode != 0)
        throw new Exception($"CMake configure (tests) failed with exit code: {exitCode}");

    exitCode = StartProcess("cmake", new ProcessSettings
    {
        Arguments = new ProcessArgumentBuilder()
            .Append("--build").Append(".")
            .Append("--config").Append(configuration)
            .Append("--target").Append("LibIPC.Tests")
            .Append("--parallel"),
        WorkingDirectory = testBuildDirectory
    });
    if (exitCode != 0)
        throw new Exception($"CMake build (tests) failed with exit code: {exitCode}");

    exitCode = StartProcess("ctest", new ProcessSettings
    {
        Arguments = new ProcessArgumentBuilder()
            .Append("--output-on-failure")
            .Append("--build-config").Append(configuration),
        WorkingDirectory = testBuildDirectory
    });
    if (exitCode != 0)
        throw new Exception($"Native tests failed with exit code: {exitCode}");
});

Task("Validate-Benchmark-Configuration")
    .Does(() =>
{
    if (!configuration.Equals("Release", StringComparison.OrdinalIgnoreCase) && !HasArgument("allow-debug-benchmark"))
        throw new Exception("Benchmark baselines must be measured with --configuration=Release (pass --allow-debug-benchmark to override).");
});

Task("Benchmark")
    .IsDependentOn("Validate-Benchmark-Configuration")
    .IsDependentOn("Build-Local-Environment")
    .Does(() =>
{
    var benchmarkArguments = new ProcessArgumentBuilder()
        .Append("--workload")
        .AppendQuoted(MakeAbsolute(File($"./Samples/PerfWorkload/bin/{configuration}/{sdk}/PerfWorkload.dll")).FullPath);

    foreach (var name in new[] { "iterations", "threads", "warmup", "runs", "output" })
    {
        if (HasArgument(name))
            benchmarkArguments.Append($"--{name}").AppendQuoted(Argument<string>(name));
    }

    DotNetRun("./Tools/SharpDetect.Benchmarks/SharpDetect.Benchmarks.csproj",
        benchmarkArguments,
        new DotNetRunSettings
        {
            Configuration = configuration,
            NoBuild = true
        });
});

Task("Coverage-Report")
    .Does(() =>
{
    const string reportDirectory = "./TestResults/CoverageReport";
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
    const string outputDirectory = $"{artifactsDirectory}";
    EnsureDirectoryExists(outputDirectory);
    
    DotNetPack("./SharpDetect.Cli", new DotNetPackSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory
    });
    
    Information($"Package created in: {outputDirectory}");
    var packages = GetFiles($"{outputDirectory}/*.nupkg");
    foreach (var package in packages)
        Information($"  - {package.GetFilename()}");
});

await RunTargetAsync(target);
