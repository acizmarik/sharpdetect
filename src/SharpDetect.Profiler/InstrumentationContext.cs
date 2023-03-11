// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Messages;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace SharpDetect.Profiler;

internal record InstrumentationContext
{
    public record struct WrappedMethodInfo(string Name, Module Module, MdTypeDef TypeDef, MdMethodDef MethodDef, COR_SIGNATURE[] Signature, ushort Parameters);
    public record struct InjectedHelperMethodInfo(string Name, MethodType Type, MdMethodDef Token, COR_SIGNATURE[] Signature);

    public IReadOnlyDictionary<Module, ConcurrentDictionary<string, ImmutableList<WrappedMethodInfo>>> WrappedMethods => wrapperMethods;
    public IReadOnlyList<InjectedHelperMethodInfo> HelperMethods => helperMethods;

    public readonly ICorProfilerInfo CorProfilerInfo;
    public readonly Assembly CoreAssembly;
    public readonly Module CoreModule;
    private readonly List<InjectedHelperMethodInfo> helperMethods;
    private readonly ConcurrentDictionary<Module, ConcurrentDictionary<string, ImmutableList<WrappedMethodInfo>>> wrapperMethods;

    public MdTypeDef EventDispatcherTypeDef { get; init; }

    public InstrumentationContext(ICorProfilerInfo corProfilerInfo, Assembly coreAssembly, Module coreModule)
    {
        CorProfilerInfo = corProfilerInfo;
        CoreAssembly = coreAssembly;
        CoreModule = coreModule;
        helperMethods = new();
        wrapperMethods = new();
    }

    public void AddHelperMethod(InjectedHelperMethodInfo helperMethodInfo)
    {
        helperMethods.Add(helperMethodInfo);
    }

    public void AddWrapperMethod(Module module, string typeName, WrappedMethodInfo methodInfo)
    {
        var moduleInfo = wrapperMethods.GetOrAdd(module, module => new());
        moduleInfo.AddOrUpdate(
            typeName, 
            _ => ImmutableList.Create(methodInfo), 
            (_, list) => list.Add(methodInfo));
    }
}
