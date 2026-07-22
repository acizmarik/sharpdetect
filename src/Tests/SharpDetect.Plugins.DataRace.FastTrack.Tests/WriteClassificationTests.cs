// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Abstractions;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class WriteClassificationTests
{
    private const int EntrySize = sizeof(ulong) + sizeof(uint);

    private readonly TestMetadata _metadata = new();
    private readonly FastTrackDetector _detector;

    private static readonly ProcessThreadId Writer = new(TestMetadata.ProcessId, new ThreadId(1));
    private static readonly ProcessThreadId Reader = new(TestMetadata.ProcessId, new ThreadId(2));
    private static readonly ProcessTrackedObjectId Instance = new(TestMetadata.ProcessId, new TrackedObjectId(100));

    public WriteClassificationTests()
    {
        _detector = new FastTrackDetector(
            FastTrackPluginConfiguration.Default,
            _metadata,
            TimeProvider.System,
            NullLogger.Instance,
            _ => null);

        _detector.RecordThreadCreated(Writer);
        _detector.RecordThreadCreated(Reader);
    }
    
    private static CapturedStackTrace CreateStack(MdMethodDef top, params MdMethodDef[] callers)
    {
        var topFrame = new CapturedStackFrame(TestMetadata.ModuleId, top);
        if (callers.Length == 0)
            return new CapturedStackTrace(topFrame);

        var frames = new[] { top }.Concat(callers).ToArray();
        var blob = new byte[frames.Length * EntrySize];
        for (var i = 0; i < frames.Length; i++)
        {
            var offset = i * EntrySize;
            MemoryMarshal.Write(blob.AsSpan(offset), (ulong)TestMetadata.ModuleId.Value);
            MemoryMarshal.Write(blob.AsSpan(offset + sizeof(ulong)), (uint)frames[i].Value);
        }

        return new CapturedStackTrace(topFrame, blob);
    }
    
    private bool IsReportedAsRace(MdToken fieldToken, ProcessTrackedObjectId? objectId, CapturedStackTrace writeStack)
    {
        _detector.RecordWrite(Writer, methodOffset: 0, fieldToken, objectId, writeStack);
        var readStack = new CapturedStackTrace(new CapturedStackFrame(TestMetadata.ModuleId, new MdMethodDef(999)));
        return _detector.RecordRead(Reader, methodOffset: 0, fieldToken, objectId, readStack) is not null;
    }

    [Fact]
    public void StaticField_WrittenByDeclaringTypeStaticConstructor_IsNotReported()
    {
        var type = _metadata.AddType("Holder");
        var field = _metadata.AddField(type, "Value", isStatic: true);
        var cctor = _metadata.AddStaticConstructor(type);

        Assert.False(IsReportedAsRace(field, objectId: null, CreateStack(cctor)));
    }

    [Fact]
    public void StaticField_WrittenByUnrelatedTypeStaticConstructor_IsReported()
    {
        var holder = _metadata.AddType("Holder");
        var other = _metadata.AddType("Other");
        var field = _metadata.AddField(holder, "Value", isStatic: true);
        var unrelatedCctor = _metadata.AddStaticConstructor(other);

        Assert.True(IsReportedAsRace(field, objectId: null, CreateStack(unrelatedCctor)));
    }

    [Fact]
    public void StaticField_WrittenByInstanceConstructor_IsReported()
    {
        var type = _metadata.AddType("Holder");
        var field = _metadata.AddField(type, "Value", isStatic: true);
        var ctor = _metadata.AddInstanceConstructor(type);

        Assert.True(IsReportedAsRace(field, objectId: null, CreateStack(ctor)));
    }

    [Fact]
    public void StaticField_WrittenByHelperCalledFromStaticConstructor_WithStack_IsNotReported()
    {
        var type = _metadata.AddType("Holder");
        var field = _metadata.AddField(type, "Value", isStatic: true);
        var helper = _metadata.AddMethod(type, "Initialize");
        var cctor = _metadata.AddStaticConstructor(type);

        Assert.False(IsReportedAsRace(field, objectId: null, CreateStack(helper, cctor)));
    }

    [Fact]
    public void StaticField_WrittenByHelperCalledFromStaticConstructor_WithoutStack_IsReported()
    {
        var type = _metadata.AddType("Holder");
        var field = _metadata.AddField(type, "Value", isStatic: true);
        var helper = _metadata.AddMethod(type, "Initialize");
        _ = _metadata.AddStaticConstructor(type);

        // Documents the known gap: without captured frames the constructor is invisible
        Assert.True(IsReportedAsRace(field, objectId: null, CreateStack(helper)));
    }

    [Fact]
    public void StaticField_StaticConstructorDeeperInStack_IsNotReported()
    {
        var type = _metadata.AddType("Holder");
        var field = _metadata.AddField(type, "Value", isStatic: true);
        var inner = _metadata.AddMethod(type, "Inner");
        var outer = _metadata.AddMethod(type, "Outer");
        var cctor = _metadata.AddStaticConstructor(type);

        Assert.False(IsReportedAsRace(field, objectId: null, CreateStack(inner, outer, cctor)));
    }

    [Fact]
    public void InstanceField_WrittenByConstructor_IsNotReported()
    {
        var type = _metadata.AddType("Holder");
        var field = _metadata.AddField(type, "Value", isStatic: false);
        var ctor = _metadata.AddInstanceConstructor(type);

        Assert.False(IsReportedAsRace(field, Instance, CreateStack(ctor)));
    }

    [Fact]
    public void InstanceField_WrittenByDerivedConstructor_IsNotReported()
    {
        var baseType = _metadata.AddType("Base");
        var derived = _metadata.AddType("Derived", baseType);
        var field = _metadata.AddField(baseType, "Value", isStatic: false);
        var derivedCtor = _metadata.AddInstanceConstructor(derived);

        Assert.False(IsReportedAsRace(field, Instance, CreateStack(derivedCtor)));
    }

    [Fact]
    public void InstanceField_WrittenByHelperCalledFromConstructor_WithStack_IsNotReported()
    {
        var type = _metadata.AddType("Holder");
        var field = _metadata.AddField(type, "Value", isStatic: false);
        var helper = _metadata.AddMethod(type, "Initialize");
        var ctor = _metadata.AddInstanceConstructor(type);

        Assert.False(IsReportedAsRace(field, Instance, CreateStack(helper, ctor)));
    }

    [Fact]
    public void InstanceField_WrittenByHelperCalledFromConstructor_WithoutStack_IsReported()
    {
        var type = _metadata.AddType("Holder");
        var field = _metadata.AddField(type, "Value", isStatic: false);
        var helper = _metadata.AddMethod(type, "Initialize");
        _ = _metadata.AddInstanceConstructor(type);

        Assert.True(IsReportedAsRace(field, Instance, CreateStack(helper)));
    }

    [Fact]
    public void InstanceField_AutoPropertyBackingFieldWrittenBySetter_WithoutStack_IsReported()
    {
        var type = _metadata.AddType("Holder");
        var field = _metadata.AddAutoPropertyBackingField(type, "Value");
        var setter = _metadata.AddMethod(type, "set_Value");

        Assert.True(IsReportedAsRace(field, Instance, CreateStack(setter)));
    }

    [Fact]
    public void InstanceField_AutoPropertyBackingFieldWrittenBySetterCalledFromConstructor_IsNotReported()
    {
        var type = _metadata.AddType("Holder");
        var field = _metadata.AddAutoPropertyBackingField(type, "Value");
        var setter = _metadata.AddMethod(type, "set_Value");
        var ctor = _metadata.AddInstanceConstructor(type);

        Assert.False(IsReportedAsRace(field, Instance, CreateStack(setter, ctor)));
    }

    [Fact]
    public void InstanceField_PlainFieldWrittenOutsideConstructor_IsReported()
    {
        var type = _metadata.AddType("Holder");
        var field = _metadata.AddField(type, "Value", isStatic: false);
        var method = _metadata.AddMethod(type, "Mutate");
        
        Assert.True(IsReportedAsRace(field, Instance, CreateStack(method)));
    }

    [Fact]
    public void InstanceField_ConstructorWriteAfterObjectEscaped_IsReported()
    {
        var type = _metadata.AddType("Holder");
        var field = _metadata.AddField(type, "Value", isStatic: false);
        var ctor = _metadata.AddInstanceConstructor(type);
        var method = _metadata.AddMethod(type, "Mutate");

        var readStack = new CapturedStackTrace(new CapturedStackFrame(TestMetadata.ModuleId, method));
        _detector.RecordRead(Reader, methodOffset: 0, field, Instance, readStack);

        Assert.True(IsReportedAsRace(field, Instance, CreateStack(ctor)));
    }

    [Fact]
    public void InstanceField_EmptyDeeperFramesBlob_FallsBackWithoutFailing()
    {
        var type = _metadata.AddType("Holder");
        var field = _metadata.AddField(type, "Value", isStatic: false);
        var method = _metadata.AddMethod(type, "Mutate");
        var stack = new CapturedStackTrace(
            new CapturedStackFrame(TestMetadata.ModuleId, method),
            deeperFramesBlob: []);

        Assert.True(IsReportedAsRace(field, Instance, stack));
    }
}
