// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using OperationResult;
using SharpDetect.Core.Loader;
using static OperationResult.Helpers;

namespace SharpDetect.Loader.Services;

internal class AssemblyLoadContext : IAssemblyLoadContext
{
    private readonly Dictionary<string, AssemblyDef> _assemblies;
    private readonly AssemblyResolver _assemblyResolver;
    private readonly ModuleCreationOptions _moduleCreationOptions;
    private readonly ILogger<AssemblyLoadContext> _logger;

    public AssemblyLoadContext(ILogger<AssemblyLoadContext> logger)
    {
        _logger = logger;
        _assemblies = new();
        var moduleContext = ModuleDef.CreateModuleContext();
        _moduleCreationOptions = new ModuleCreationOptions()
        {
            Context = moduleContext,
            TryToLoadPdbFromDisk = false
        };
        _assemblyResolver = (AssemblyResolver)moduleContext.AssemblyResolver;
        _assemblyResolver.UseGAC = false;
        _assemblyResolver.EnableFrameworkRedirect = false;
        _assemblyResolver.FindExactMatch = false;
    }

    public Result<AssemblyDef, AssemblyLoadErrorType> LoadAssemblyFromPath(string path)
    {
        if (_assemblies.TryGetValue(path, out var assembly))
            return Ok(assembly);

        try
        {
            if (!Path.IsPathFullyQualified(path))
                return Error(AssemblyLoadErrorType.InvalidPath);

            assembly = AssemblyDef.Load(path, _moduleCreationOptions);
            _assemblies.TryAdd(path, assembly);
            _assemblyResolver.AddToCache(assembly);
            return Ok(assembly);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during loading assembly from path: {Path}", path);
            return Error(AssemblyLoadErrorType.ErrorDuringLoading);
        }
    }

    public Result<AssemblyDef, AssemblyLoadErrorType> LoadAssemblyFromStream(Stream stream, string virtualPath)
    {
        if (_assemblies.TryGetValue(virtualPath, out var assembly))
            return Ok(assembly);

        try
        {
            assembly = AssemblyDef.Load(stream, _moduleCreationOptions);
            _assemblyResolver.AddToCache(assembly);
            return Ok(assembly);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during loading assembly from stream with virtual path: {Path}", virtualPath);
            return Error(AssemblyLoadErrorType.ErrorDuringLoading);
        }
    }
}
