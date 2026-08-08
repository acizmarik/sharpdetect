// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Reporting.Formatters;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Core.Reporting;

public static class StackFrameResolver
{
    public static StackFrame ResolveMinimalFrame(
        IMetadataResolver resolver,
        uint processId,
        ModuleId moduleId,
        MdMethodDef methodToken)
    {
        var methodDef = resolver.ResolveMethod(processId, moduleId, methodToken).Value;
        var methodName = methodDef is not null
            ? MethodFormatter.ToDisplayName(methodDef.FullName)
            : "<unable-to-resolve-method>";
        var modulePath = methodDef?.Module?.Location ?? "<unable-to-resolve-module>";

        return new StackFrame(
            MethodName: methodName,
            ModulePath: modulePath,
            MethodToken: methodToken,
            Il: null,
            Source: null);
    }
}
