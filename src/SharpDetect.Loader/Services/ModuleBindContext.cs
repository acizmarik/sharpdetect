// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using OperationResult;
using SharpDetect.Events;
using SharpDetect.Events.Descriptors.Profiler;
using SharpDetect.Loaders;
using System.Collections.Concurrent;
using static OperationResult.Helpers;

namespace SharpDetect.Loader;

internal class ModuleBindContext : IModuleBindContext
{
    private readonly IAssemblyLoadContext _assemblyLoadContext;
    private readonly ConcurrentDictionary<ModuleEntry, ModuleDef> _modules;
    private readonly ConcurrentDictionary<uint, ModuleDef> _coreLibraries;
    private readonly ILogger<ModuleBindContext> _logger;

    public ModuleBindContext(
        IAssemblyLoadContext assemblyLoadContext, 
        ILogger<ModuleBindContext> logger)
    {
        _assemblyLoadContext = assemblyLoadContext;
        _logger = logger;
        _modules = new();
        _coreLibraries = new();
    }

    public Result<ModuleDef, ModuleLoadErrorType> TryGetModule(RecordedEventMetadata metadata, ModuleId moduleId)
        => TryGetModule(metadata.Pid, moduleId);

    public Result<ModuleDef, ModuleLoadErrorType> TryGetModule(uint pid, ModuleId moduleId)
    {
        var moduleEntry = new ModuleEntry(pid, moduleId);
        if (_modules.TryGetValue(moduleEntry, out var module))
            return Ok(module);

        return Error(ModuleLoadErrorType.ModuleNotLoaded);
    }

    public Result<ModuleDef, ModuleLoadErrorType> LoadModule(RecordedEventMetadata metadata, ModuleId moduleId, string path)
    {
        // Check if the module is already loaded to cache
        var getModuleResult = TryGetModule(metadata, moduleId);
        if (getModuleResult.IsSuccess)
            return Ok(getModuleResult.Value);

        // Check if its corresponding assembly can be loaded
        var assemblyLoadResult = _assemblyLoadContext.LoadAssemblyFromPath(path);
        if (assemblyLoadResult.IsError)
        {
            if (assemblyLoadResult.Error == AssemblyLoadErrorType.InvalidPath)
            {
                // Dynamically generated assembly (this is not supported)
                // There is not an easy way to obtain a dynamically emitted assembly
                _logger.LogWarning("[PID={Pid}] Could not load assembly \"{Path}\" for module with identifier={Id}. It was probably generated during runtime.", 
                    metadata.Pid, path, moduleId.Value);
            }
            else
            {
                _logger.LogError("[PID={Pid}] Could not load assembly from \"{Path}\" for module with identifier={Id}. Error: \"{Error}\".",
                    metadata.Pid, path, moduleId.Value, assemblyLoadResult.Error);
            }

            return Error(ModuleLoadErrorType.ErrorDuringLoading);
        }

        // If this is the first module for a process, it is the core module
        var assemblyDef = assemblyLoadResult.Value;
        if (!_coreLibraries.TryGetValue(metadata.Pid, out _))
        {
            var coreModuleDef = assemblyDef.ManifestModule;
            _coreLibraries.TryAdd(metadata.Pid, coreModuleDef);
        }

        // FIXME: this approach does not work for multi-module assemblies
        var moduleDef = assemblyDef.ManifestModule;
        var moduleEntry = new ModuleEntry(metadata.Pid, moduleId);
        _modules.TryAdd(moduleEntry, moduleDef);
        return Ok(moduleDef);
    }

    public Result<ModuleDef, ModuleLoadErrorType> LoadModule(RecordedEventMetadata metadata, ModuleId moduleId, Stream stream, string virtualPath)
    {
        // Check if the module is already loaded to cache
        var getModuleResult = TryGetModule(metadata, moduleId);
        if (getModuleResult.IsSuccess)
            return Ok(getModuleResult.Value);

        // Check if its corresponding assembly can be loaded
        var assemblyLoadResult = _assemblyLoadContext.LoadAssemblyFromStream(stream, virtualPath);
        if (assemblyLoadResult.IsError)
        {
            _logger.LogError("[PID={Pid}] Could not load assembly from \"{Path}\" for module with identifier={Id}", metadata.Pid, virtualPath, moduleId.Value);
            return Error(ModuleLoadErrorType.ErrorDuringLoading);
        }

        var assemblyDef = assemblyLoadResult.Value;
        // FIXME: this approach does not work for multi-module assemblies
        var moduleDef = assemblyDef.ManifestModule;
        var moduleEntry = new ModuleEntry(metadata.Pid, moduleId);
        _modules.TryAdd(moduleEntry, moduleDef);
        return Ok(moduleDef);
    }
}
