// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using OperationResult;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;
using static OperationResult.Helpers;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;

internal sealed class TestMetadata : IMetadataContext, IMetadataResolver
{
    public const uint ProcessId = 1;
    public static readonly ModuleId ModuleId = new(1);

    private readonly ModuleDefUser _module;
    private readonly Dictionary<int, MethodDef> _methods = [];
    private readonly Dictionary<int, FieldDef> _fields = [];
    private int _nextToken = 1;

    public TestMetadata()
    {
        _module = new ModuleDefUser("TestModule");
        var assembly = new AssemblyDefUser("TestAssembly", new Version(1, 0, 0, 0));
        assembly.Modules.Add(_module);
    }

    public TypeDefUser AddType(string name, TypeDefUser? baseType = null)
    {
        var type = new TypeDefUser(
            "TestNamespace",
            name,
            baseType ?? _module.CorLibTypes.Object.TypeDefOrRef);
        type.Attributes = TypeAttributes.Public | TypeAttributes.Class;
        _module.Types.Add(type);
        return type;
    }

    public MdMethodDef AddInstanceConstructor(TypeDefUser type)
        => AddMethod(type, ".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, isStatic: false);

    public MdMethodDef AddStaticConstructor(TypeDefUser type)
        => AddMethod(type, ".cctor", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, isStatic: true);

    public MdMethodDef AddMethod(TypeDefUser type, string name)
        => AddMethod(type, name, MethodAttributes.Public, isStatic: false);

    private MdMethodDef AddMethod(TypeDefUser type, string name, MethodAttributes attributes, bool isStatic)
    {
        var signature = isStatic
            ? MethodSig.CreateStatic(_module.CorLibTypes.Void)
            : MethodSig.CreateInstance(_module.CorLibTypes.Void);
        var method = new MethodDefUser(name, signature, MethodImplAttributes.IL, attributes);
        type.Methods.Add(method);

        var token = _nextToken++;
        _methods.Add(token, method);
        return new MdMethodDef(token);
    }

    public MdToken AddField(TypeDefUser type, string name, bool isStatic)
    {
        var attributes = isStatic
            ? FieldAttributes.Public | FieldAttributes.Static
            : FieldAttributes.Public;
        var field = new FieldDefUser(name, new FieldSig(_module.CorLibTypes.Int32), attributes);
        type.Fields.Add(field);

        var token = _nextToken++;
        _fields.Add(token, field);
        return new MdToken(token);
    }
    
    public MdToken AddAutoPropertyBackingField(TypeDefUser type, string propertyName)
    {
        var token = AddField(type, $"<{propertyName}>k__BackingField", isStatic: false);
        var field = _fields[token.Value];

        var attributeTypeRef = _module.CorLibTypes.GetTypeRef(
            "System.Runtime.CompilerServices",
            "CompilerGeneratedAttribute");
        var attributeCtor = new MemberRefUser(
            _module,
            ".ctor",
            MethodSig.CreateInstance(_module.CorLibTypes.Void),
            attributeTypeRef);
        field.CustomAttributes.Add(new CustomAttribute(attributeCtor));

        return token;
    }

    public IMetadataEmitter GetEmitter(uint processId)
        => throw new NotSupportedException();

    public IMetadataResolver GetResolver(uint processId) => this;

    public Result<ModuleDef, MetadataResolverErrorType> ResolveModule(RecordedEventMetadata metadata, ModuleId moduleId)
        => ResolveModule(metadata.Pid, moduleId);

    public Result<ModuleDef, MetadataResolverErrorType> ResolveModule(uint pid, ModuleId moduleId)
        => Ok<ModuleDef>(_module);

    public Result<TypeDef, MetadataResolverErrorType> ResolveType(RecordedEventMetadata metadata, ModuleId moduleId, MdTypeDef typeToken)
        => ResolveType(metadata.Pid, moduleId, typeToken);

    public Result<TypeDef, MetadataResolverErrorType> ResolveType(uint pid, ModuleId moduleId, MdTypeDef typeToken)
        => Error(MetadataResolverErrorType.TypeNotFound);

    public Result<MethodDef, MetadataResolverErrorType> ResolveMethod(RecordedEventMetadata metadata, ModuleId moduleId, MdMethodDef methodToken)
        => ResolveMethod(metadata.Pid, moduleId, methodToken);

    public Result<MethodDef, MetadataResolverErrorType> ResolveMethod(uint pid, ModuleId moduleId, MdMethodDef methodToken)
    {
        if (!_methods.TryGetValue(methodToken.Value, out var method))
            return Error(MetadataResolverErrorType.MethodNotFound);

        return Ok(method);
    }

    public Result<FieldDef, MetadataResolverErrorType> ResolveField(RecordedEventMetadata metadata, ModuleId moduleId, MdToken fieldToken)
        => ResolveField(metadata.Pid, moduleId, fieldToken);

    public Result<FieldDef, MetadataResolverErrorType> ResolveField(uint pid, ModuleId moduleId, MdToken fieldToken)
    {
        if (!_fields.TryGetValue(fieldToken.Value, out var field))
            return Error(MetadataResolverErrorType.FieldNotFound);

        return Ok(field);
    }
}
