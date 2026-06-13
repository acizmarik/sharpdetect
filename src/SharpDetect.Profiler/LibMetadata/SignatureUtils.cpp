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

bool LibProfiler::SigTypeContainsGenericParam(const BYTE* signature, unsigned length)
{
    if (length == 0)
        return false;

    unsigned position = 0;
    auto const element = signature[position++];

    switch (element)
    {
        case ELEMENT_TYPE_VAR:
        case ELEMENT_TYPE_MVAR:
            return true;

        case ELEMENT_TYPE_VOID:
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I1: case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2: case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_I4: case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_I8: case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R4: case ELEMENT_TYPE_R8:
        case ELEMENT_TYPE_I:  case ELEMENT_TYPE_U:
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_TYPEDBYREF:
            return false;

        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_PINNED:
            return SigTypeContainsGenericParam(signature + position, length - position);

        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_CLASS:
        {
            // Compressed token follows — skip it, no generic params possible here
            ULONG token;
            position += CorSigUncompressData(signature + position, &token);
            return false;
        }

        case ELEMENT_TYPE_GENERICINST:
        {
            position++;
            ULONG token;
            position += CorSigUncompressData(signature + position, &token);
            ULONG genericArgumentsCount;
            position += CorSigUncompressData(signature + position, &genericArgumentsCount);
            for (ULONG i = 0; i < genericArgumentsCount && position < length; i++)
            {
                if (SigTypeContainsGenericParam(signature + position, length - position))
                    return true;
                position += SkipSigType(signature + position, length - position);
            }
            return false;
        }

        case ELEMENT_TYPE_ARRAY:
        {
            return SigTypeContainsGenericParam(signature + position, length - position);
        }

        case ELEMENT_TYPE_FNPTR:
        {
            position++;
            ULONG paramCount;
            position += CorSigUncompressData(signature + position, &paramCount);
            if (SigTypeContainsGenericParam(signature + position, length - position))
                return true;
            return false;
        }

        case ELEMENT_TYPE_CMOD_REQD:
        case ELEMENT_TYPE_CMOD_OPT:
        {
            ULONG token;
            position += CorSigUncompressData(signature + position, &token);
            return SigTypeContainsGenericParam(signature + position, length - position);
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

bool LibProfiler::ResolveSigType(
    const BYTE* typeSignature,
    const unsigned typeSignatureLength,
    const std::vector<std::pair<const BYTE*, unsigned>>& typeArgs,
    std::vector<BYTE>& resolved)
{
    if (typeSignatureLength == 0)
        return false;

    unsigned position = 0;
    auto const element = typeSignature[position++];

    switch (element)
    {
        case ELEMENT_TYPE_VAR:
        {
            ULONG index;
            CorSigUncompressData(typeSignature + position, &index);
            if (index >= typeArgs.size())
                return false;

            // Substitute with the concrete type argument
            auto const& [argPtr, argLength] = typeArgs[index];
            resolved.insert(resolved.end(), argPtr, argPtr + argLength);
            return true;
        }

        case ELEMENT_TYPE_GENERICINST:
        {
            resolved.push_back(element);
            // Copy CLASS/VALUETYPE + token
            const auto innerElement = typeSignature[position];
            resolved.push_back(innerElement);
            position++;
            ULONG token;
            auto compressedLength = CorSigUncompressData(typeSignature + position, &token);
            resolved.insert(resolved.end(), typeSignature + position, typeSignature + position + compressedLength);
            position += compressedLength;
            // Generic arguments count
            ULONG genericArgsCount;
            compressedLength = CorSigUncompressData(typeSignature + position, &genericArgsCount);
            resolved.insert(resolved.end(), typeSignature + position, typeSignature + position + compressedLength);
            position += compressedLength;
            // Resolve each argument
            for (ULONG i = 0; i < genericArgsCount && position < typeSignatureLength; i++)
            {
                if (!ResolveSigType(typeSignature + position, typeSignatureLength - position, typeArgs, resolved))
                    return false;
                position += SkipSigType(typeSignature + position, typeSignatureLength - position);
            }
            return true;
        }

        case ELEMENT_TYPE_ARRAY:
        {
            resolved.push_back(element);
            unsigned const totalLength = SkipSigType(typeSignature, typeSignatureLength);
            if (!ResolveSigType(typeSignature + position, typeSignatureLength - position, typeArgs, resolved))
                return false;

            position += SkipSigType(typeSignature + position, typeSignatureLength - position);
            resolved.insert(resolved.end(), typeSignature + position, typeSignature + totalLength);
            return true;
        }

        case ELEMENT_TYPE_FNPTR:
        {
            const unsigned totalLength = SkipSigType(typeSignature, typeSignatureLength);
            resolved.insert(resolved.end(), typeSignature, typeSignature + totalLength);
            return true;
        }

        case ELEMENT_TYPE_CMOD_REQD:
        case ELEMENT_TYPE_CMOD_OPT:
        {
            resolved.push_back(element);
            ULONG token;
            const auto compressedLength = CorSigUncompressData(typeSignature + position, &token);
            resolved.insert(resolved.end(), typeSignature + position, typeSignature + position + compressedLength);
            position += compressedLength;
            return ResolveSigType(typeSignature + position, typeSignatureLength - position, typeArgs, resolved);
        }

        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_PINNED:
        {
            resolved.push_back(element);
            // Recursively resolve inner signature element
            return ResolveSigType(typeSignature + position, typeSignatureLength - position, typeArgs, resolved);
        }

        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_MVAR:
        {
            // Note: ELEMENT_TYPE_MVAR is intentionally skipped and not resolved
            // Copy element and compressed token as-is
            resolved.push_back(element);
            ULONG data;
            const auto compressedLength = CorSigUncompressData(typeSignature + position, &data);
            resolved.insert(resolved.end(), typeSignature + position, typeSignature + position + compressedLength);
            return true;
        }

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
        default:
            // Primitive element type or unknown — copy as-is
            resolved.push_back(element);
            return true;
    }
}
