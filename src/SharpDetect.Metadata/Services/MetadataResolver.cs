// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using OperationResult;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Loader;
using SharpDetect.Core.Metadata;
using static OperationResult.Helpers;

namespace SharpDetect.Metadata.Services;

internal class MetadataResolver : IMetadataResolver
{
    public readonly uint ProcessId;
    private readonly IModuleBindContext _moduleBindContext;
    private readonly InjectedData _state;
    private readonly ILogger<MetadataResolver> _logger;

    public MetadataResolver(
        uint processId,
        IModuleBindContext moduleBindContext,
        InjectedData state,
        ILogger<MetadataResolver> logger)
    {
        ProcessId = processId;
        _moduleBindContext = moduleBindContext;
        _state = state;
        _logger = logger;
    }

    public Result<ModuleDef, MetadataResolverErrorType> ResolveModule(RecordedEventMetadata metadata, ModuleId moduleId)
        => ResolveModule(metadata.Pid, moduleId);

    public Result<ModuleDef, MetadataResolverErrorType> ResolveModule(uint pid, ModuleId moduleId)
    {
        var moduleGetResult = _moduleBindContext.TryGetModule(pid, moduleId);
        if (moduleGetResult.IsError)
        {
            _logger.LogError("Could not resolve module with identifier={Id}", moduleId.Value);
            return Error(MetadataResolverErrorType.ModuleNotFound);
        }

        return Ok(moduleGetResult.Value);
    }

    public Result<TypeDef, MetadataResolverErrorType> ResolveType(RecordedEventMetadata metadata, ModuleId moduleId, MdTypeDef typeToken)
        => ResolveType(metadata.Pid, moduleId, typeToken);

    public Result<TypeDef, MetadataResolverErrorType> ResolveType(uint pid, ModuleId moduleId, MdTypeDef typeToken)
    {
        // Resolve module
        var moduleResolveResult = ResolveModule(pid, moduleId);
        if (moduleResolveResult.IsError)
        {
            _logger.LogError("Could not resolve type with token={Tok} because module was not resolved", typeToken.Value);
            return Error(moduleResolveResult.Error);
        }

        // Resolve type
        var moduleDef = moduleResolveResult.Value;
        if (moduleDef.ResolveToken(typeToken.Value) is not TypeDef typeDef)
        {
            _logger.LogError("Could not resolve type with token={Tok} on module {Module}", typeToken.Value, moduleDef.FullName);
            return Error(MetadataResolverErrorType.TypeNotFound);
        }

        return Ok(typeDef);
    }

    public Result<MethodDef, MetadataResolverErrorType> ResolveMethod(RecordedEventMetadata metadata, ModuleId moduleId, MdMethodDef methodToken)
        => ResolveMethod(metadata.Pid, moduleId, methodToken);

    public Result<MethodDef, MetadataResolverErrorType> ResolveMethod(uint pid, ModuleId moduleId, MdMethodDef methodToken)
    {
        // Resolve wrappers
        if (_state.TryGetWrappedMethod(moduleId, methodToken, out var wrappedMethod))
            methodToken = wrappedMethod;

        // Resolve module
        var moduleResolveResult = ResolveModule(pid, moduleId);
        if (moduleResolveResult.IsError)
        {
            _logger.LogError("Could not resolve method with token={Tok} because module was not resolved", methodToken.Value);
            return Error(moduleResolveResult.Error);
        }

        // Resolve method
        var moduleDef = moduleResolveResult.Value;
        if (moduleDef.ResolveToken(methodToken.Value) is not MethodDef methodDef)
        {
            _logger.LogError("Could not resolve method with token={Tok} on module {Module}", methodToken.Value, moduleDef.FullName);
            return Error(MetadataResolverErrorType.MethodNotFound);
        }

        return Ok(methodDef);
    }

    public Result<FieldDef, MetadataResolverErrorType> ResolveField(RecordedEventMetadata metadata, ModuleId moduleId, MdToken fieldToken)
        => ResolveField(metadata.Pid, moduleId, fieldToken);

    public Result<FieldDef, MetadataResolverErrorType> ResolveField(uint pid, ModuleId moduleId, MdToken fieldToken)
    {
        // Resolve module
        var moduleResolveResult = ResolveModule(pid, moduleId);
        if (moduleResolveResult.IsError)
        {
            _logger.LogError("Could not resolve field with token={Tok} because module was not resolved", fieldToken.Value);
            return Error(moduleResolveResult.Error);
        }

        // Resolve field
        var tokenProvider = moduleResolveResult.Value.ResolveToken(fieldToken.Value);
        switch (tokenProvider)
        {
            case FieldDef fieldDef:
                return Ok(fieldDef);
            case MemberRef { IsFieldRef: true } fieldRef:
            {
                var resolvedFieldDef = fieldRef.ResolveFieldDef();
                if (resolvedFieldDef is not null)
                    return Ok(resolvedFieldDef);
                _logger.LogError("Could not resolve field definition for field reference with token={Tok} on module {Module}", fieldToken.Value, moduleResolveResult.Value.FullName);
                return Error(MetadataResolverErrorType.FieldNotFound);
            }
            default:
                _logger.LogError("Could not resolve field with token={Tok} on module {Module}", fieldToken.Value, moduleResolveResult.Value.FullName);
                return Error(MetadataResolverErrorType.FieldNotFound);
        }
    }
}
