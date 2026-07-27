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

const string solution = "./SharpDetect.slnx";
const string artifactsDirectory = "./artifacts";
const string testResultsDirectory = "./TestResults";
const string profilerSourceDirectory = "./SharpDetect.Profiler";
const string profilerArtifactsRootDirectory = profilerSourceDirectory + "/artifacts";
const string ipqProject = "./SharpDetect.InterProcessQueue";
const string cliProject = "./SharpDetect.Cli";

var supportedRuntimeIdentifiers = new[] { "win-x64", "linux-x64" };
var nativeArtifactsDirectory = $"{artifactsDirectory}/Profilers/{rid}/";
var profilerBuildDirectory = $"{profilerArtifactsRootDirectory}/{rid}";
var profilerTestsBuildDirectory = $"{profilerArtifactsRootDirectory}/{rid}-tests";
var profilers = new[] { "SharpDetect.Concurrency.Profiler" };

// Projects that are intentionally excluded from the solution (MTP and VSTest must not be in same solution)
var standaloneProjects = new[]
{
    "./Samples/SimpleDataRaceTestsMtp/SimpleDataRaceTestsMtp.csproj",
    "./Samples/SimpleDataRaceTestsVSTest/SimpleDataRaceTestsVSTest.csproj"
};

var warningsAsErrorsSettings = new DotNetMSBuildSettings
{
    TreatAllWarningsAs = MSBuildTreatAllWarningsAs.Error
};

//////////////////////////////////////////////////////////////////////
//////////////////////////// HELPERS /////////////////////////////////
//////////////////////////////////////////////////////////////////////

void RunTool(string tool, string description, string? workingDirectory, Action<ProcessArgumentBuilder> configureArguments)
{
    var arguments = new ProcessArgumentBuilder();
    configureArguments(arguments);

    var settings = new ProcessSettings { Arguments = arguments };
    if (workingDirectory is not null)
        settings.WorkingDirectory = workingDirectory;

    var exitCode = StartProcess(tool, settings);
    if (exitCode != 0)
        throw new Exception($"{description} failed with exit code: {exitCode}");
}

void CMakeConfigure(string buildDirectory, params string[] options)
{
    EnsureDirectoryExists(buildDirectory);
    RunTool("cmake", $"CMake configure ({buildDirectory})", buildDirectory, arguments =>
    {
        arguments.AppendQuoted(MakeAbsolute(Directory(profilerSourceDirectory)).FullPath);
        arguments.Append($"-DCMAKE_BUILD_TYPE={configuration}");
        foreach (var option in options)
            arguments.AppendQuoted(option);
    });
}

void CMakeBuild(string buildDirectory, string? cmakeTarget = null)
{
    RunTool("cmake", $"CMake build ({cmakeTarget ?? "all"})", buildDirectory, arguments =>
    {
        arguments.Append("--build").Append(".")
                 .Append("--config").Append(configuration)
                 .Append("--parallel");
        if (cmakeTarget is not null)
            arguments.Append("--target").Append(cmakeTarget);
    });
}

DotNetBuildSettings CreateBuildSettings() => new DotNetBuildSettings
{
    Configuration = configuration,
    NoRestore = true,
    MSBuildSettings = warningsAsErrorsSettings
};

DotNetTestSettings CreateTestSettings(TimeSpan timeout, string? filter = null) => new DotNetTestSettings
{
    Configuration = configuration,
    Filter = filter,
    Loggers = ["trx"],
    Collectors = ["XPlat Code Coverage"],
    ResultsDirectory = testResultsDirectory,
    Settings = File("./CodeCoverage.runsettings"),
    NoRestore = true,
    NoBuild = true,
    ToolTimeout = timeout
};

//////////////////////////////////////////////////////////////////////
////////////////////// SETUP / TEARDOWN //////////////////////////////
//////////////////////////////////////////////////////////////////////

Setup(_ =>
{
    if (!supportedRuntimeIdentifiers.Contains(rid))
        throw new Exception($"Unsupported runtime identifier '{rid}'");

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
    DotNetClean(solution, new DotNetCleanSettings
    {
        Configuration = configuration
    });
    CleanDirectory(artifactsDirectory);
    CleanDirectory(profilerArtifactsRootDirectory);
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetRestore(solution);
    foreach (var project in standaloneProjects)
        DotNetRestore(project);
});

Task("Build-Managed")
    .IsDependentOn("Restore")
    .Does(() =>
{
    DotNetBuild(solution, CreateBuildSettings());

    foreach (var project in standaloneProjects)
        DotNetBuild(project, CreateBuildSettings());
});

Task("Build-IPQ")
    .IsDependentOn("Restore")
    .Does(() =>
{
    DotNetPublish(ipqProject, new DotNetPublishSettings
    {
        Configuration = configuration,
        Runtime = rid
    });
});

Task("Build-Profiler")
    .IsDependentOn("Clean")
    .Does(() =>
{
    CMakeConfigure(profilerBuildDirectory);
    CMakeBuild(profilerBuildDirectory);
});

Task("Copy-Native-Artifacts")
    .IsDependentOn("Build-IPQ")
    .IsDependentOn("Build-Profiler")
    .Does(() =>
{
    EnsureDirectoryExists(artifactsDirectory);
    EnsureDirectoryExists(nativeArtifactsDirectory);

    var ipqLibrary = $"{ipqProject}/bin/{configuration}/{sdk}/{rid}/native/SharpDetect.InterProcessQueue.{libraryExtension}";
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
    var baseDirectory = $"{profilerBuildDirectory}/{profilerName}";
    return rid.StartsWith("win")
        ? $"{baseDirectory}/{configuration}/{profilerName}.{libraryExtension}"
        : $"{baseDirectory}/{profilerName}.{libraryExtension}";
}

Task("Build-Local-Environment")
    .IsDependentOn("Build-Managed")
    .IsDependentOn("Copy-Native-Artifacts");

Task("Test-Unit-Managed")
    .IsDependentOn("Build-Managed")
    .Does(() =>
{
    DotNetTest(solution, CreateTestSettings(
        timeout: TimeSpan.FromMinutes(10),
        filter: "FullyQualifiedName!~SharpDetect.E2ETests"));
});

Task("Test-Unit-Native")
    .IsDependentOn("Clean")
    .Does(() =>
{
    EnsureDirectoryExists(testResultsDirectory);
    DeleteFiles($"{testResultsDirectory}/native-tests-*.xml");

    CMakeConfigure(profilerTestsBuildDirectory,
        "-DSHARPDETECT_BUILD_TESTS=ON",
        $"-DSHARPDETECT_TEST_RESULTS_DIR={MakeAbsolute(Directory(testResultsDirectory)).FullPath}");
    CMakeBuild(profilerTestsBuildDirectory, "SharpDetect.NativeTests");

    RunTool("ctest", $"Native tests (reports in {testResultsDirectory})", profilerTestsBuildDirectory, arguments =>
    {
        arguments.Append("--output-on-failure")
                 .Append("--build-config").Append(configuration);
    });
});

Task("Test-Unit")
    .IsDependentOn("Test-Unit-Managed")
    .IsDependentOn("Test-Unit-Native");

Task("Test-E2E")
    .IsDependentOn("Build-Local-Environment")
    .Does(() =>
{
    DotNetTest("./Tests/SharpDetect.E2ETests/SharpDetect.E2ETests.csproj", CreateTestSettings(timeout: TimeSpan.FromMinutes(20)));
});

Task("Tests")
    .IsDependentOn("Test-Unit")
    .IsDependentOn("Test-E2E");

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
    const string reportDirectory = testResultsDirectory + "/CoverageReport";
    EnsureDirectoryExists(reportDirectory);

    RunTool("dotnet", "ReportGenerator", workingDirectory: null, arguments =>
    {
        arguments.Append("tool")
                 .Append("run")
                 .Append("reportgenerator")
                 .Append($"-reports:{testResultsDirectory}/**/coverage.cobertura.xml")
                 .Append($"-targetdir:{reportDirectory}")
                 .Append("-reporttypes:Html;MarkdownSummaryGithub");
    });

    Information($"Coverage report generated in: {reportDirectory}");
});

Task("CI-Prepare-Managed")
    .Does(() =>
{
    DotNetPublish(cliProject, new DotNetPublishSettings
    {
        Configuration = configuration
    });
});

Task("CI-Pack")
    .IsDependentOn("CI-Prepare-Managed")
    .Does(() =>
{
    EnsureDirectoryExists(artifactsDirectory);

    DotNetPack(cliProject, new DotNetPackSettings
    {
        Configuration = configuration,
        OutputDirectory = artifactsDirectory
    });

    Information($"Package created in: {artifactsDirectory}");
    var packages = GetFiles($"{artifactsDirectory}/*.nupkg");
    foreach (var package in packages)
        Information($"  - {package.GetFilename()}");
});

await RunTargetAsync(target);
