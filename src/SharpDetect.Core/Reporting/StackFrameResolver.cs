// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Core.Reporting;

public static class StackFrameResolver
{
    public static StackFrame ResolveMinimalFrame(
        IMetadataResolver resolver,
        uint processId,
        ModuleId moduleId,
        MdMethodDef methodToken,
        Func<string, string>? methodNameFormatter = null)
    {
        var methodDef = resolver.ResolveMethod(processId, moduleId, methodToken).Value;
        var methodName = methodDef is not null
            ? (methodNameFormatter?.Invoke(methodDef.FullName) ?? methodDef.FullName)
            : "<unable-to-resolve-method>";
        var modulePath = methodDef?.Module?.Location ?? "<unable-to-resolve-module>";

        return new StackFrame(
            MethodName: methodName,
            SourceMapping: modulePath,
            MethodToken: methodToken.Value,
            MethodOffset: null,
            Instruction: null,
            SourceFileName: null,
            SourceLine: null,
            SourceCode: null);
    }
}
