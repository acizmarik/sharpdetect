// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors;

public sealed class MethodDescriptorBuilder
{
    private readonly string _declaringTypeFullName;
    private readonly string _methodName;
    private readonly bool _isInstance;
    private readonly List<CapturedArgumentDescriptor> _capturedArguments = [];
    private string _declaringAssemblyName = DeclaringAssemblies.CoreLib;
    private MethodVersionDescriptor? _version;
    private ArgumentTypeDescriptor _returnType = Sig.Void;
    private ArgumentTypeDescriptor[] _parameterTypes = [];
    private byte _genericParametersCount;
    private CapturedValueDescriptor? _capturedReturnValue;
    private RecordedEventType? _enterInterpretation;
    private RecordedEventType? _exitInterpretation;
    private bool _emitExitEvent;
    private bool _injectHooks = true;
    private bool _injectManagedWrapper;

    internal MethodDescriptorBuilder(string declaringTypeFullName, string methodName, bool isInstance)
    {
        _declaringTypeFullName = declaringTypeFullName;
        _methodName = methodName;
        _isInstance = isInstance;
    }

    public MethodDescriptorBuilder InAssembly(string declaringAssemblyName)
    {
        _declaringAssemblyName = declaringAssemblyName;
        return this;
    }

    public MethodDescriptorBuilder OnRuntime(MethodVersionDescriptor version)
    {
        _version = version;
        return this;
    }

    public MethodDescriptorBuilder Returns(ArgumentTypeDescriptor returnType)
    {
        _returnType = returnType;
        return this;
    }

    public MethodDescriptorBuilder Parameters(params ArgumentTypeDescriptor[] parameterTypes)
    {
        _parameterTypes = parameterTypes;
        return this;
    }

    public MethodDescriptorBuilder GenericParameters(byte count)
    {
        _genericParametersCount = count;
        return this;
    }

    public MethodDescriptorBuilder CaptureThis()
    {
        if (!_isInstance)
            throw Invalid("a static method has no 'this' to capture");

        return CaptureArgument(index: 0, Capture.Reference);
    }

    public MethodDescriptorBuilder CaptureParameter(int position, CapturedValueDescriptor value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        return CaptureArgument(_isInstance ? position + 1 : position, value);
    }

    public MethodDescriptorBuilder CaptureReturnValue(CapturedValueDescriptor value)
    {
        _capturedReturnValue = value;
        return this;
    }

    public MethodDescriptorBuilder OnEnter(RecordedEventType interpretation)
    {
        _enterInterpretation = interpretation;
        return this;
    }

    public MethodDescriptorBuilder OnExit(RecordedEventType interpretation)
    {
        _exitInterpretation = interpretation;
        _emitExitEvent = true;
        return this;
    }

    public MethodDescriptorBuilder WithUninterpretedExit()
    {
        _emitExitEvent = true;
        return this;
    }

    public MethodDescriptorBuilder WithManagedWrapper()
    {
        _injectManagedWrapper = true;
        return this;
    }

    public MethodDescriptorBuilder WithoutHooks()
    {
        _injectHooks = false;
        return this;
    }

    public MethodDescriptor Build()
    {
        Validate();

        var signature = _isInstance
            ? MethodSignatureDescriptor.ForInstanceMethod(_returnType, _parameterTypes, _genericParametersCount)
            : MethodSignatureDescriptor.ForStaticMethod(_returnType, _parameterTypes, _genericParametersCount);

        var rewriting = new MethodRewritingDescriptor(
            InjectHooks: _injectHooks,
            InjectManagedWrapper: _injectManagedWrapper,
            Arguments: [.. _capturedArguments],
            ReturnValue: _capturedReturnValue,
            MethodEnterInterpretation: (ushort?)_enterInterpretation,
            MethodExitInterpretation: (ushort?)_exitInterpretation,
            EmitExitEvent: _emitExitEvent);

        return new MethodDescriptor(
            _methodName,
            _declaringTypeFullName,
            _version,
            signature,
            rewriting,
            _declaringAssemblyName);
    }

    private MethodDescriptorBuilder CaptureArgument(int index, CapturedValueDescriptor value)
    {
        _capturedArguments.Add(new CapturedArgumentDescriptor(checked((byte)index), value));
        return this;
    }

    private void Validate()
    {
        var firstParameterIndex = _isInstance ? 1 : 0;
        var seenIndices = new HashSet<byte>();

        foreach (var (index, value) in _capturedArguments)
        {
            if (!seenIndices.Add(index))
                throw Invalid($"argument {index} is captured more than once");

            var position = index - firstParameterIndex;
            if (position < 0)
                continue;

            if (position >= _parameterTypes.Length)
                throw Invalid($"captures parameter {position} but declares only {_parameterTypes.Length} parameter(s)");

            ValidateParameterCapture(position, _parameterTypes[position].ElementTypes[0], value.Flags);
        }

        if (_capturedReturnValue is not { } returnValue)
            return;

        if ((returnValue.Flags & (CapturedValue.Dereference | CapturedValue.ReloadOnExit)) != 0)
            throw Invalid("dereferences the return value");

        if (!_emitExitEvent)
            throw Invalid("captures the return value but reports no exit event");

        if (_returnType.ElementTypes[0] == CorElementType.ELEMENT_TYPE_VOID)
            throw Invalid("captures the return value of a method returning void");
    }

    private void ValidateParameterCapture(int position, CorElementType parameterType, CapturedValue flags)
    {
        var isByRef = parameterType == CorElementType.ELEMENT_TYPE_BYREF;
        var isAddress = isByRef || parameterType == CorElementType.ELEMENT_TYPE_PTR;
        var isDereferenced = flags.HasFlag(CapturedValue.Dereference);

        if (isDereferenced && !isAddress)
            throw Invalid($"parameter {position} is dereferenced but is neither passed by reference nor a pointer");

        if (isAddress && !isDereferenced)
            throw Invalid($"parameter {position} is passed by reference or as a pointer but is not dereferenced");

        if (isDereferenced && flags.HasFlag(CapturedValue.CaptureAsReferenceArray))
            throw Invalid($"parameter {position} is a dereferenced reference array, which cannot be captured");

        if (!flags.HasFlag(CapturedValue.ReloadOnExit))
            return;

        if (!isByRef)
            throw Invalid($"parameter {position} is reloaded on exit but is not passed by reference");

        if (!_emitExitEvent)
            throw Invalid($"parameter {position} is reloaded on exit but no exit event is reported");
    }

    private InvalidOperationException Invalid(string reason)
        => new($"Invalid descriptor for {_declaringTypeFullName}.{_methodName}: {reason}.");
}
