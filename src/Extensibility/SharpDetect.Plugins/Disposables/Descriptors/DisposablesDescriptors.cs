// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Disposables.Descriptors;

public static class DisposablesDescriptors
{
    private static readonly TypeDescriptor _taskTypeDescriptor;
    private static readonly MethodDescriptor _streamCloseDescriptor;

    static DisposablesDescriptors()
    {
        _taskTypeDescriptor = new TypeDescriptor("System.Threading.Tasks.Task");
        _streamCloseDescriptor = new MethodDescriptor("Close", "System.IO.Stream");
    }

    public static IEnumerable<TypeDescriptor> GetAllTypeIgnores()
    {
        yield return _taskTypeDescriptor;
    }

    public static IEnumerable<MethodDescriptor> GetAllMethodDescriptors()
    {
        yield return _streamCloseDescriptor;
    }
}
