// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;
using System.Text;

namespace SharpDetect.Core.Plugins.Models;

public readonly record struct CallStackSnapshot(uint Pid, ThreadId Tid, Stack<(ModuleId ModuleId, MdMethodDef MethodToken)> CallStack)
{
    public string Dump(IMetadataResolver metadataResolver)
    {
        var sb = new StringBuilder();
        foreach (var stackFrame in CallStack.Reverse())
        {
            var methodResolveResult = metadataResolver.ResolveMethod(Pid, stackFrame.ModuleId, stackFrame.MethodToken);
            var methodFullName = methodResolveResult.IsSuccess ? methodResolveResult.Value.FullName : "<unknown>";
            sb.AppendLine($"at {methodFullName}");
        }

        return sb.ToString();
    }
}
