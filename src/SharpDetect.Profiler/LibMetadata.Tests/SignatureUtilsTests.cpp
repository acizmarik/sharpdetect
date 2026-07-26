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
