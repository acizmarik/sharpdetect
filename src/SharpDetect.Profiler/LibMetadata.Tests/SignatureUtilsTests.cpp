// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "doctest.h"
#include "SignatureUtils.h"

#include <vector>

namespace
{
    constexpr BYTE CompressedToken = 0x21;
    using Blob = std::vector<BYTE>;
    
    LibProfiler::TypeArgs ParseTypeSpec(const Blob& blob)
    {
        LibProfiler::TypeArgs typeArgs;
        REQUIRE(LibProfiler::ParseTypeSpecGenericArgs(blob.data(), static_cast<unsigned>(blob.size()), typeArgs));
        return typeArgs;
    }

    LibProfiler::TypeArgs ParseMethodSpec(const Blob& blob)
    {
        LibProfiler::TypeArgs typeArgs;
        REQUIRE(LibProfiler::ParseMethodSpecGenericArgs(blob.data(), static_cast<unsigned>(blob.size()), typeArgs));
        return typeArgs;
    }

    bool IsObjRef(
        const Blob& signature,
        const LibProfiler::TypeArgs& classArgs = {},
        const LibProfiler::TypeArgs& methodArgs = {})
    {
        return LibProfiler::IsSigTypeObjectReference(
            signature.data(), static_cast<unsigned>(signature.size()), classArgs, methodArgs);
    }

    unsigned SkipLength(const Blob& signature)
    {
        return LibProfiler::SkipSigType(signature.data(), static_cast<unsigned>(signature.size()));
    }

    LibProfiler::SigTypeResolution TryResolve(const Blob& signature, const LibProfiler::TypeArgs& typeArgs, Blob& resolved)
    {
        return LibProfiler::ResolveSigType(signature.data(), static_cast<unsigned>(signature.size()), typeArgs, resolved);
    }

    Blob Substitute(const Blob& signature, const LibProfiler::TypeArgs& typeArgs = {})
    {
        Blob resolved;
        REQUIRE(TryResolve(signature, typeArgs, resolved) == LibProfiler::SigTypeResolution::Substituted);
        return resolved;
    }

    bool IsUnchanged(const Blob& signature, const LibProfiler::TypeArgs& typeArgs = {})
    {
        Blob resolved;
        auto const resolution = TryResolve(signature, typeArgs, resolved);
        return resolution == LibProfiler::SigTypeResolution::Unchanged && resolved.empty();
    }

    bool CanResolve(const Blob& signature, const LibProfiler::TypeArgs& typeArgs = {})
    {
        Blob resolved;
        return TryResolve(signature, typeArgs, resolved) != LibProfiler::SigTypeResolution::Failed;
    }
}

TEST_CASE("IsSigTypeObjectReference recognises plain reference types")
{
    CHECK(IsObjRef({ ELEMENT_TYPE_OBJECT }));
    CHECK(IsObjRef({ ELEMENT_TYPE_STRING }));
    CHECK(IsObjRef({ ELEMENT_TYPE_CLASS, CompressedToken }));
    CHECK(IsObjRef({ ELEMENT_TYPE_SZARRAY, ELEMENT_TYPE_I4 }));
    CHECK(IsObjRef({ ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken }));
}

TEST_CASE("IsSigTypeObjectReference rejects value types")
{
    CHECK_FALSE(IsObjRef({ ELEMENT_TYPE_I4 }));
    CHECK_FALSE(IsObjRef({ ELEMENT_TYPE_VALUETYPE, CompressedToken }));
    CHECK_FALSE(IsObjRef({ ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_VALUETYPE, CompressedToken }));
    CHECK_FALSE(IsObjRef({ ELEMENT_TYPE_BYREF, ELEMENT_TYPE_CLASS, CompressedToken }));
    CHECK_FALSE(IsObjRef({}));
}

TEST_CASE("A generic parameter is unresolvable without arguments")
{
    CHECK_FALSE(IsObjRef({ ELEMENT_TYPE_VAR, 0 }));
    CHECK_FALSE(IsObjRef({ ELEMENT_TYPE_MVAR, 0 }));
}

TEST_CASE("VAR is substituted from the declaring type's arguments")
{
    // Lazy<SomeClass> — GENERICINST CLASS <token> 1 CLASS <token>
    const Blob lazyOfClassBlob = { ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken,
                                   1, ELEMENT_TYPE_CLASS, CompressedToken };
    auto const lazyOfClass = ParseTypeSpec(lazyOfClassBlob);
    REQUIRE(lazyOfClass.size() == 1);
    CHECK(IsObjRef({ ELEMENT_TYPE_VAR, 0 }, lazyOfClass));
    
    const Blob lazyOfIntBlob = { ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken,
                                 1, ELEMENT_TYPE_I4 };
    auto const lazyOfInt = ParseTypeSpec(lazyOfIntBlob);
    REQUIRE(lazyOfInt.size() == 1);
    CHECK_FALSE(IsObjRef({ ELEMENT_TYPE_VAR, 0 }, lazyOfInt));
}

TEST_CASE("VAR indexes the correct argument of a multi-argument instantiation")
{
    // ConcurrentDictionary<int, SomeClass>: !0 is the value-type key, !1 the reference value.
    const Blob dictionaryBlob = { ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken,
                                  2, ELEMENT_TYPE_I4, ELEMENT_TYPE_CLASS, CompressedToken };
    auto const dictionary = ParseTypeSpec(dictionaryBlob);
    REQUIRE(dictionary.size() == 2);

    CHECK_FALSE(IsObjRef({ ELEMENT_TYPE_VAR, 0 }, dictionary));
    CHECK(IsObjRef({ ELEMENT_TYPE_VAR, 1 }, dictionary));
    CHECK_FALSE(IsObjRef({ ELEMENT_TYPE_VAR, 2 }, dictionary));
}

TEST_CASE("VAR and MVAR draw from separate argument lists")
{
    const Blob classBlob = { ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken,
                             1, ELEMENT_TYPE_I4 };
    const Blob methodBlob = { IMAGE_CEE_CS_CALLCONV_GENERICINST, 1, ELEMENT_TYPE_CLASS, CompressedToken };

    auto const classArgs = ParseTypeSpec(classBlob);
    auto const methodArgs = ParseMethodSpec(methodBlob);

    // The class argument is a value type, the method argument a reference type.
    CHECK_FALSE(IsObjRef({ ELEMENT_TYPE_VAR, 0 }, classArgs, methodArgs));
    CHECK(IsObjRef({ ELEMENT_TYPE_MVAR, 0 }, classArgs, methodArgs));
}

TEST_CASE("A nested generic parameter resolves instead of recursing")
{
    const Blob lazyOfVarBlob = { ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken,
                                 1, ELEMENT_TYPE_VAR, 0 };
    auto const lazyOfVar = ParseTypeSpec(lazyOfVarBlob);
    REQUIRE(lazyOfVar.size() == 1);
    CHECK_FALSE(IsObjRef({ ELEMENT_TYPE_VAR, 0 }, lazyOfVar));
}

TEST_CASE("ParseMethodSpecGenericArgs reads an instantiation blob")
{
    const Blob instantiation = { IMAGE_CEE_CS_CALLCONV_GENERICINST, 2,
                                 ELEMENT_TYPE_CLASS, CompressedToken,
                                 ELEMENT_TYPE_I4 };
    auto const typeArgs = ParseMethodSpec(instantiation);

    REQUIRE(typeArgs.size() == 2);
    CHECK(typeArgs[0].first[0] == ELEMENT_TYPE_CLASS);
    CHECK(typeArgs[0].second == 2);
    CHECK(typeArgs[1].first[0] == ELEMENT_TYPE_I4);
    CHECK(typeArgs[1].second == 1);
}

TEST_CASE("ParseMethodSpecGenericArgs rejects blobs that are not instantiations")
{
    LibProfiler::TypeArgs typeArgs;
    const Blob typeSpec = { ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS,
                            CompressedToken, 1, ELEMENT_TYPE_I4 };
    CHECK_FALSE(LibProfiler::ParseMethodSpecGenericArgs(
        typeSpec.data(), static_cast<unsigned>(typeSpec.size()), typeArgs));

    CHECK_FALSE(LibProfiler::ParseMethodSpecGenericArgs(nullptr, 0, typeArgs));

    // Truncated: the blob promises two arguments but carries one.
    const Blob truncated = { IMAGE_CEE_CS_CALLCONV_GENERICINST, 2, ELEMENT_TYPE_I4 };
    CHECK_FALSE(LibProfiler::ParseMethodSpecGenericArgs(
        truncated.data(), static_cast<unsigned>(truncated.size()), typeArgs));
}

TEST_CASE("SkipSigType measures leaf types")
{
    CHECK(SkipLength({ ELEMENT_TYPE_I4 }) == 1);
    CHECK(SkipLength({ ELEMENT_TYPE_OBJECT }) == 1);
    CHECK(SkipLength({ ELEMENT_TYPE_STRING }) == 1);
    CHECK(SkipLength({ ELEMENT_TYPE_TYPEDBYREF }) == 1);
    CHECK(SkipLength({ ELEMENT_TYPE_CLASS, CompressedToken }) == 2);
    CHECK(SkipLength({ ELEMENT_TYPE_VALUETYPE, CompressedToken }) == 2);
    CHECK(SkipLength({ ELEMENT_TYPE_VAR, 0 }) == 2);
    CHECK(SkipLength({ ELEMENT_TYPE_MVAR, 3 }) == 2);
    CHECK(SkipLength({}) == 0);
}

TEST_CASE("SkipSigType measures single-element prefixes")
{
    CHECK(SkipLength({ ELEMENT_TYPE_SZARRAY, ELEMENT_TYPE_I4 }) == 2);
    CHECK(SkipLength({ ELEMENT_TYPE_PTR, ELEMENT_TYPE_I4 }) == 2);
    CHECK(SkipLength({ ELEMENT_TYPE_PINNED, ELEMENT_TYPE_I4 }) == 2);
    CHECK(SkipLength({ ELEMENT_TYPE_BYREF, ELEMENT_TYPE_CLASS, CompressedToken }) == 3);
    CHECK(SkipLength({ ELEMENT_TYPE_SZARRAY, ELEMENT_TYPE_SZARRAY, ELEMENT_TYPE_I4 }) == 3);
}

TEST_CASE("SkipSigType measures generic instantiations")
{
    // Lazy<int>
    CHECK(SkipLength({ ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 1, ELEMENT_TYPE_I4 }) == 5);
    // ConcurrentDictionary<int, SomeClass>
    CHECK(SkipLength({
        ELEMENT_TYPE_GENERICINST,
        ELEMENT_TYPE_CLASS,
        CompressedToken,
        2,
        ELEMENT_TYPE_I4,
        ELEMENT_TYPE_CLASS,
        CompressedToken }) == 7);
    // Lazy<Lazy<int>>
    CHECK(SkipLength({
        ELEMENT_TYPE_GENERICINST,
        ELEMENT_TYPE_CLASS,
        CompressedToken,
        1,
        ELEMENT_TYPE_GENERICINST,
        ELEMENT_TYPE_CLASS,
        CompressedToken,
        1,
        ELEMENT_TYPE_I4 }) == 9);
}

TEST_CASE("SkipSigType measures multidimensional arrays, function pointers and custom modifiers")
{
    // Arrays
    CHECK(SkipLength({ ELEMENT_TYPE_ARRAY, ELEMENT_TYPE_I4, 2, 1, 3, 1, 0 }) == 7);
    CHECK(SkipLength({ ELEMENT_TYPE_ARRAY, ELEMENT_TYPE_I4, 1, 0, 0 }) == 5);
    // Function pointers
    CHECK(SkipLength({ ELEMENT_TYPE_FNPTR, IMAGE_CEE_CS_CALLCONV_DEFAULT, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4 }) == 5);
    // Modifiers
    CHECK(SkipLength({ ELEMENT_TYPE_CMOD_OPT, CompressedToken, ELEMENT_TYPE_I4 }) == 3);
    CHECK(SkipLength({ ELEMENT_TYPE_CMOD_REQD, CompressedToken, ELEMENT_TYPE_I4 }) == 3);
}

TEST_CASE("ResolveSigType leaves closed signatures untouched")
{
    CHECK(IsUnchanged({ ELEMENT_TYPE_I4 }));
    CHECK(IsUnchanged({ ELEMENT_TYPE_CLASS, CompressedToken }));
    CHECK(IsUnchanged({ ELEMENT_TYPE_SZARRAY, ELEMENT_TYPE_OBJECT }));
    CHECK(IsUnchanged({ ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 1, ELEMENT_TYPE_I4 }));
    CHECK(IsUnchanged({ ELEMENT_TYPE_ARRAY, ELEMENT_TYPE_I4, 2, 1, 3, 1, 0 }));
    CHECK(IsUnchanged({ ELEMENT_TYPE_CMOD_OPT, CompressedToken, ELEMENT_TYPE_I4 }));
    CHECK_FALSE(CanResolve({}));
}

TEST_CASE("ResolveSigType substitutes VAR from the declaring type's arguments")
{
    const Blob closedOverClass = { ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 1, ELEMENT_TYPE_CLASS, CompressedToken };
    auto const classArg = ParseTypeSpec(closedOverClass);
    CHECK(Substitute({ ELEMENT_TYPE_VAR, 0 }, classArg) == Blob{ ELEMENT_TYPE_CLASS, CompressedToken });

    const Blob closedOverInt = { ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 1, ELEMENT_TYPE_I4 };
    auto const intArg = ParseTypeSpec(closedOverInt);
    CHECK(Substitute({ ELEMENT_TYPE_VAR, 0 }, intArg) == Blob{ ELEMENT_TYPE_I4 });
}

TEST_CASE("ResolveSigType substitutes a compound type argument as a whole")
{
    // ConcurrentDictionary<int, Lazy<string>> — !1 stands for the whole instantiation
    const Blob typeSpec = {
        ELEMENT_TYPE_GENERICINST,
        ELEMENT_TYPE_CLASS,
        CompressedToken,
        2,
        ELEMENT_TYPE_I4,
        ELEMENT_TYPE_GENERICINST,
        ELEMENT_TYPE_CLASS,
        CompressedToken,
        1,
        ELEMENT_TYPE_STRING };
    auto const typeArgs = ParseTypeSpec(typeSpec);
    REQUIRE(typeArgs.size() == 2);

    CHECK(
        Substitute({ ELEMENT_TYPE_VAR, 1 }, typeArgs) ==
        Blob{ ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 1, ELEMENT_TYPE_STRING });
}

TEST_CASE("ResolveSigType fails when VAR has no matching argument")
{
    const Blob oneArgument = { ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 1, ELEMENT_TYPE_I4 };
    auto const typeArgs = ParseTypeSpec(oneArgument);

    CHECK_FALSE(CanResolve({ ELEMENT_TYPE_VAR, 1 }, typeArgs));
    CHECK_FALSE(CanResolve({ ELEMENT_TYPE_VAR, 0 }));
    CHECK_FALSE(CanResolve({ ELEMENT_TYPE_SZARRAY, ELEMENT_TYPE_VAR, 0 }));
}

TEST_CASE("ResolveSigType substitutes underneath prefixes and custom modifiers")
{
    const Blob closedOverClass = { ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 1, ELEMENT_TYPE_CLASS, CompressedToken };
    auto const typeArgs = ParseTypeSpec(closedOverClass);

    CHECK(
        Substitute({ ELEMENT_TYPE_SZARRAY, ELEMENT_TYPE_VAR, 0 }, typeArgs) ==
        Blob{ ELEMENT_TYPE_SZARRAY, ELEMENT_TYPE_CLASS, CompressedToken });
    CHECK(
        Substitute({ ELEMENT_TYPE_BYREF, ELEMENT_TYPE_VAR, 0 }, typeArgs) ==
        Blob{ ELEMENT_TYPE_BYREF, ELEMENT_TYPE_CLASS, CompressedToken });
    CHECK(
        Substitute({ ELEMENT_TYPE_PTR, ELEMENT_TYPE_VAR, 0 }, typeArgs) ==
        Blob{ ELEMENT_TYPE_PTR, ELEMENT_TYPE_CLASS, CompressedToken });
    CHECK(
        Substitute({ ELEMENT_TYPE_PINNED, ELEMENT_TYPE_VAR, 0 }, typeArgs) ==
        Blob{ ELEMENT_TYPE_PINNED, ELEMENT_TYPE_CLASS, CompressedToken });
    CHECK(
        Substitute({ ELEMENT_TYPE_CMOD_OPT, CompressedToken, ELEMENT_TYPE_VAR, 0 }, typeArgs) ==
        Blob{ ELEMENT_TYPE_CMOD_OPT, CompressedToken, ELEMENT_TYPE_CLASS, CompressedToken });
}

TEST_CASE("ResolveSigType substitutes generic arguments in place")
{
    const Blob closedOverInt = { ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 1, ELEMENT_TYPE_I4 };
    auto const typeArgs = ParseTypeSpec(closedOverInt);

    // List<!0> becomes List<int>
    CHECK(
        Substitute({ ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 1, ELEMENT_TYPE_VAR, 0 }, typeArgs) ==
        Blob{ ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 1, ELEMENT_TYPE_I4 });

    // Only the generic parameter is rewritten, the sibling argument is preserved
    CHECK(
        Substitute({ ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 2, ELEMENT_TYPE_VAR, 0, ELEMENT_TYPE_STRING }, typeArgs) ==
        Blob{ ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 2, ELEMENT_TYPE_I4, ELEMENT_TYPE_STRING });

    // Nested instantiations are substituted at any depth
    CHECK(
        Substitute({ ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 1, ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 1, ELEMENT_TYPE_VAR, 0 }, typeArgs) ==
        Blob{ ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 1, ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 1, ELEMENT_TYPE_I4 });
}

TEST_CASE("ResolveSigType preserves an array shape while substituting its element type")
{
    const Blob closedOverInt = { ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 1, ELEMENT_TYPE_I4 };
    auto const typeArgs = ParseTypeSpec(closedOverInt);

    // !0[2,] keeps its rank, sizes and lower bounds
    CHECK(
        Substitute({ ELEMENT_TYPE_ARRAY, ELEMENT_TYPE_VAR, 0, 2, 1, 3, 1, 0 }, typeArgs) ==
        Blob{ ELEMENT_TYPE_ARRAY, ELEMENT_TYPE_I4, 2, 1, 3, 1, 0 });
}

TEST_CASE("ResolveSigType leaves MVAR and function pointers alone")
{
    const Blob closedOverInt = { ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, CompressedToken, 1, ELEMENT_TYPE_I4 };
    auto const typeArgs = ParseTypeSpec(closedOverInt);

    // Method generic parameters are not substituted from the declaring type's arguments
    CHECK(IsUnchanged({ ELEMENT_TYPE_MVAR, 0 }, typeArgs));
    // A function pointer's own signature is left alone, generic parameters included
    const Blob functionPointer = { ELEMENT_TYPE_FNPTR, IMAGE_CEE_CS_CALLCONV_DEFAULT, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_VAR, 0 };
    CHECK(IsUnchanged(functionPointer, typeArgs));
}
