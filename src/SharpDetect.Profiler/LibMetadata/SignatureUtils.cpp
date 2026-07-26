// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "SignatureUtils.h"

unsigned LibProfiler::SkipSigType(const BYTE* signature, unsigned length)
{
    if (length == 0)
        return 0;

    unsigned position = 0;
    auto const element = signature[position++];

    switch (element)
    {
        case ELEMENT_TYPE_VOID:
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R4:
        case ELEMENT_TYPE_R8:
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_U:
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_TYPEDBYREF:
            return position;

        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_PINNED:
            return position + SkipSigType(signature + position, length - position);

        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_CLASS:
        {
            ULONG token;
            position += CorSigUncompressData(signature + position, &token);
            return position;
        }

        case ELEMENT_TYPE_GENERICINST:
        {
            position += SkipSigType(signature + position, length - position); // skip CLASS/VALUETYPE + token
            ULONG genericArgumentsCount;
            position += CorSigUncompressData(signature + position, &genericArgumentsCount);
            for (ULONG i = 0; i < genericArgumentsCount && position < length; i++)
                position += SkipSigType(signature + position, length - position);
            return position;
        }

        case ELEMENT_TYPE_ARRAY:
        {
            position += SkipSigType(signature + position, length - position); // element type
            ULONG rank;
            position += CorSigUncompressData(signature + position, &rank);
            ULONG numSizes;
            position += CorSigUncompressData(signature + position, &numSizes);
            for (ULONG i = 0; i < numSizes && position < length; i++)
            {
                ULONG s;
                position += CorSigUncompressData(signature + position, &s);
            }
            ULONG numLoBounds;
            position += CorSigUncompressData(signature + position, &numLoBounds);
            for (ULONG i = 0; i < numLoBounds && position < length; i++)
            {
                ULONG s;
                position += CorSigUncompressData(signature + position, &s);
            }
            return position;
        }

        case ELEMENT_TYPE_VAR:
        case ELEMENT_TYPE_MVAR:
        {
            ULONG idx;
            position += CorSigUncompressData(signature + position, &idx);
            return position;
        }

        case ELEMENT_TYPE_FNPTR:
        {
            position++; // skip calling convention
            ULONG parametersCOunt;
            position += CorSigUncompressData(signature + position, &parametersCOunt);
            position += SkipSigType(signature + position, length - position); // return type
            for (ULONG i = 0; i < parametersCOunt && position < length; i++)
                position += SkipSigType(signature + position, length - position);
            return position;
        }

        case ELEMENT_TYPE_CMOD_REQD:
        case ELEMENT_TYPE_CMOD_OPT:
        {
            ULONG token;
            position += CorSigUncompressData(signature + position, &token);
            position += SkipSigType(signature + position, length - position);
            return position;
        }

        default:
            return position;
    }
}

bool LibProfiler::IsSigTypeObjectReference(
    const BYTE* signature,
    const unsigned length,
    const TypeArgs& classArgs,
    const TypeArgs& methodArgs)
{
    static const TypeArgs noTypeArgs;

    if (length == 0)
        return false;

    auto const element = signature[0];
    switch (element)
    {
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_ARRAY:
            return true;

        case ELEMENT_TYPE_GENERICINST:
            return length >= 2 && signature[1] == ELEMENT_TYPE_CLASS;

        case ELEMENT_TYPE_VAR:
        case ELEMENT_TYPE_MVAR:
        {
            if (length < 2)
                return false;

            const TypeArgs& typeArgs = (element == ELEMENT_TYPE_VAR) ? classArgs : methodArgs;
            ULONG index;
            CorSigUncompressData(signature + 1, &index);
            if (index >= typeArgs.size())
                return false;

            // Classify the substituted argument without substituting again
            auto const& [argSignature, argLength] = typeArgs[index];
            return IsSigTypeObjectReference(argSignature, argLength, noTypeArgs, noTypeArgs);
        }

        default:
            return false;
    }
}

bool LibProfiler::ParseTypeSpecGenericArgs(
    const BYTE* typeSpecSignature,
    const unsigned typeSpecSigLength,
    std::vector<std::pair<const BYTE*, unsigned>>& typeArgs)
{
    if (typeSpecSigLength == 0)
        return false;

    unsigned position = 0;
    auto const element = typeSpecSignature[position++];
    if (element != ELEMENT_TYPE_GENERICINST)
        return false;
        
    position += SkipSigType(typeSpecSignature + position, typeSpecSigLength - position);

    ULONG genericArgsCount;
    position += CorSigUncompressData(typeSpecSignature + position, &genericArgsCount);

    typeArgs.clear();
    typeArgs.reserve(genericArgsCount);
    for (ULONG i = 0; i < genericArgsCount && position < typeSpecSigLength; i++)
    {
        const BYTE* argStart = typeSpecSignature + position;
        unsigned argLength = SkipSigType(argStart, typeSpecSigLength - position);
        typeArgs.emplace_back(argStart, argLength);
        position += argLength;
    }

    return typeArgs.size() == genericArgsCount;
}

bool LibProfiler::ParseMethodSpecGenericArgs(
    const BYTE* methodSpecSignature,
    const unsigned methodSpecSigLength,
    std::vector<std::pair<const BYTE*, unsigned>>& typeArgs)
{
    if (methodSpecSigLength == 0)
        return false;

    unsigned position = 0;
    if (methodSpecSignature[position++] != IMAGE_CEE_CS_CALLCONV_GENERICINST)
        return false;

    ULONG genericArgsCount;
    position += CorSigUncompressData(methodSpecSignature + position, &genericArgsCount);

    typeArgs.clear();
    typeArgs.reserve(genericArgsCount);
    for (ULONG i = 0; i < genericArgsCount && position < methodSpecSigLength; i++)
    {
        const BYTE* argStart = methodSpecSignature + position;
        unsigned argLength = SkipSigType(argStart, methodSpecSigLength - position);
        typeArgs.emplace_back(argStart, argLength);
        position += argLength;
    }

    return typeArgs.size() == genericArgsCount;
}

namespace
{
    using LibProfiler::SigTypeResolution;

    SigTypeResolution RewindUnlessSubstituted(
        const SigTypeResolution resolution,
        const size_t mark,
        std::vector<BYTE>& resolved)
    {
        if (resolution != SigTypeResolution::Substituted)
            resolved.resize(mark);

        return resolution;
    }
}

LibProfiler::SigTypeResolution LibProfiler::ResolveSigType(
    const BYTE* typeSignature,
    const unsigned typeSignatureLength,
    const TypeArgs& typeArgs,
    std::vector<BYTE>& resolved)
{
    if (typeSignatureLength == 0)
        return SigTypeResolution::Failed;

    unsigned position = 0;
    auto const element = typeSignature[position++];

    switch (element)
    {
        case ELEMENT_TYPE_VAR:
        {
            ULONG index;
            CorSigUncompressData(typeSignature + position, &index);
            if (index >= typeArgs.size())
                return SigTypeResolution::Failed;

            // Substitute with the concrete type argument
            auto const& [argSignature, argLength] = typeArgs[index];
            resolved.insert(resolved.end(), argSignature, argSignature + argLength);
            return SigTypeResolution::Substituted;
        }

        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_PINNED:
        {
            auto const mark = resolved.size();
            resolved.push_back(element);
            auto const inner = ResolveSigType(
                typeSignature + position, typeSignatureLength - position, typeArgs, resolved);
            return RewindUnlessSubstituted(inner, mark, resolved);
        }

        case ELEMENT_TYPE_CMOD_REQD:
        case ELEMENT_TYPE_CMOD_OPT:
        {
            ULONG token;
            position += CorSigUncompressData(typeSignature + position, &token);

            auto const mark = resolved.size();
            resolved.insert(resolved.end(), typeSignature, typeSignature + position);
            auto const inner = ResolveSigType(
                typeSignature + position, typeSignatureLength - position, typeArgs, resolved);
            return RewindUnlessSubstituted(inner, mark, resolved);
        }

        case ELEMENT_TYPE_GENERICINST:
        {
            auto const mark = resolved.size();
            // The generic type definition (CLASS or VALUETYPE plus its token) is never a parameter
            position += SkipSigType(typeSignature + position, typeSignatureLength - position);
            ULONG genericArgsCount;
            position += CorSigUncompressData(typeSignature + position, &genericArgsCount);
            resolved.insert(resolved.end(), typeSignature, typeSignature + position);

            auto result = SigTypeResolution::Unchanged;
            for (ULONG i = 0; i < genericArgsCount && position < typeSignatureLength; i++)
            {
                auto const argLength = SkipSigType(typeSignature + position, typeSignatureLength - position);
                auto const argument = ResolveSigType(
                    typeSignature + position, typeSignatureLength - position, typeArgs, resolved);
                if (argument == SigTypeResolution::Failed)
                    return SigTypeResolution::Failed;

                if (argument == SigTypeResolution::Unchanged)
                    resolved.insert(resolved.end(), typeSignature + position, typeSignature + position + argLength);
                else
                    result = SigTypeResolution::Substituted;

                position += argLength;
            }

            return RewindUnlessSubstituted(result, mark, resolved);
        }

        case ELEMENT_TYPE_ARRAY:
        {
            auto const mark = resolved.size();
            resolved.push_back(element);
            auto const elementTypeLength = SkipSigType(typeSignature + position, typeSignatureLength - position);
            auto const inner = ResolveSigType(
                typeSignature + position, typeSignatureLength - position, typeArgs, resolved);
            if (inner != SigTypeResolution::Substituted)
                return RewindUnlessSubstituted(inner, mark, resolved);

            // Preserve the array shape (rank, sizes and lower bounds) trailing the element type
            position += elementTypeLength;
            auto const totalLength = SkipSigType(typeSignature, typeSignatureLength);
            resolved.insert(resolved.end(), typeSignature + position, typeSignature + totalLength);
            return SigTypeResolution::Substituted;
        }

        default:
            // A leaf (primitive, CLASS, VALUETYPE), function pointer or generic that cannot be closed
            return SigTypeResolution::Unchanged;
    }
}
