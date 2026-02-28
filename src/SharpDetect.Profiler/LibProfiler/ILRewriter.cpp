// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unordered_map>
#include "ILRewriter.h"
#include "SignatureUtils.h"

ILRewriter::ILRewriter(ICorProfilerInfo * pICorProfilerInfo, ICorProfilerFunctionControl * pICorProfilerFunctionControl, ModuleID moduleID, mdToken tkMethod)
    : m_pICorProfilerInfo(pICorProfilerInfo), m_pICorProfilerFunctionControl(pICorProfilerFunctionControl),
    m_moduleId(moduleID), m_tkMethod(tkMethod), m_fGenerateTinyHeader(false),
    m_pOffsetToInstr(NULL), m_pOutputBuffer(NULL), m_pIMethodMalloc(NULL),
    m_pMetaDataImport(NULL), m_pMetaDataEmit(NULL)
{
    m_IL.m_pNext = &m_IL;
    m_IL.m_pPrev = &m_IL;
    m_IL.m_objOperandIsObjRef = false;

    m_nInstrs = 0;
}

ILRewriter::~ILRewriter()
{
    ILInstr * p = m_IL.m_pNext;
    while (p != &m_IL)
    {
        ILInstr * t = p->m_pNext;
        delete p;
        p = t;
    }
    delete[] m_pOffsetToInstr;
    delete[] m_pOutputBuffer;

    if (m_pIMethodMalloc)
        m_pIMethodMalloc->Release();
    if (m_pMetaDataImport)
        m_pMetaDataImport->Release();
    if (m_pMetaDataEmit)
        m_pMetaDataEmit->Release();
}

HRESULT ILRewriter::Initialize()
{
    HRESULT hr;
    /*
    IfFailRet(m_pICorProfilerInfo->GetFunctionInfo(
        m_functionId, &m_classId, &m_moduleId, &m_tkMethod));
        */

        // Get metadata interfaces ready

    IfFailRet(m_pICorProfilerInfo->GetModuleMetaData(
        m_moduleId, ofRead | ofWrite, IID_IMetaDataImport, (IUnknown**)&m_pMetaDataImport));

    IfFailRet(m_pMetaDataImport->QueryInterface(IID_IMetaDataEmit, (void **)&m_pMetaDataEmit));

    return S_OK;
}

/////////////////////////////////////////////////////////////////////////////////////////////////
//
// I M P O R T
//
////////////////////////////////////////////////////////////////////////////////////////////////

HRESULT ILRewriter::Import()
{
    HRESULT hr = S_OK;
    LPCBYTE pMethodBytes;

    IfFailRet(m_pICorProfilerInfo->GetILFunctionBody(
        m_moduleId, m_tkMethod, &pMethodBytes, NULL));

    COR_ILMETHOD_DECODER decoder((COR_ILMETHOD*)pMethodBytes);

    // Import the header flags
    m_tkLocalVarSig = decoder.GetLocalVarSigTok();
    m_maxStack = decoder.GetMaxStack();
    m_flags = (decoder.GetFlags() & CorILMethod_InitLocals);

    m_CodeSize = decoder.GetCodeSize();

    IfFailRet(ImportIL(decoder.Code));

    IfFailRet(ImportEH(decoder.EH, decoder.EHCount()));

    IfFailRet(ComputeStackTypes());

    return S_OK;
}

HRESULT ILRewriter::ImportIL(LPCBYTE pIL)
{
    m_pOffsetToInstr = new ILInstr*[m_CodeSize + 1];
    IfNullRet(m_pOffsetToInstr);

    memset(m_pOffsetToInstr, 0, m_CodeSize * sizeof(ILInstr*));

    // Set the sentinel instruction
    m_pOffsetToInstr[m_CodeSize] = &m_IL;
    m_IL.m_opcode = -1;

    bool fBranch = false;
    unsigned offset = 0;
    while (offset < m_CodeSize)
    {
        unsigned startOffset = offset;
        unsigned opcode = pIL[offset++];

        if (opcode == CEE_PREFIX1)
        {
            if (offset >= m_CodeSize)
            {
                _ASSERTE(false);
                return COR_E_INVALIDPROGRAM;
            }
            opcode = 0x100 + pIL[offset++];
        }

        if ((CEE_PREFIX7 <= opcode) && (opcode <= CEE_PREFIX2))
        {
            // NOTE: CEE_PREFIX2-7 are currently not supported
            _ASSERTE(false);
            return COR_E_INVALIDPROGRAM;
        }

        if (opcode >= CEE_COUNT)
        {
            _ASSERTE(false);
            return COR_E_INVALIDPROGRAM;
        }

        BYTE flags = s_OpCodeFlags[opcode];

        int size = (flags & OPCODEFLAGS_SizeMask);
        if (offset + size > m_CodeSize)
        {
            _ASSERTE(false);
            return COR_E_INVALIDPROGRAM;
        }

        ILInstr * pInstr = NewILInstr();
        IfNullRet(pInstr);

        pInstr->m_opcode = opcode;

        InsertBefore(&m_IL, pInstr);

        m_pOffsetToInstr[startOffset] = pInstr;

        switch (flags)
        {
        case 0:
            break;
        case 1:
            pInstr->m_Arg8 = *(UNALIGNED INT8 *)&(pIL[offset]);
            break;
        case 2:
            pInstr->m_Arg16 = *(UNALIGNED INT16 *)&(pIL[offset]);
            break;
        case 4:
            pInstr->m_Arg32 = *(UNALIGNED INT32 *)&(pIL[offset]);
            break;
        case 8:
            pInstr->m_Arg64 = *(UNALIGNED INT64 *)&(pIL[offset]);
            break;
        case 1 | OPCODEFLAGS_BranchTarget:
            pInstr->m_Arg32 = offset + 1 + *(UNALIGNED INT8 *)&(pIL[offset]);
            fBranch = true;
            break;
        case 4 | OPCODEFLAGS_BranchTarget:
            pInstr->m_Arg32 = offset + 4 + *(UNALIGNED INT32 *)&(pIL[offset]);
            fBranch = true;
            break;
        case 0 | OPCODEFLAGS_Switch:
        {
            if (offset + sizeof(INT32) > m_CodeSize)
            {
                _ASSERTE(false);
                return COR_E_INVALIDPROGRAM;
            }

            unsigned nTargets = *(UNALIGNED INT32 *)&(pIL[offset]);
            pInstr->m_Arg32 = nTargets;
            offset += sizeof(INT32);

            unsigned base = offset + nTargets * sizeof(INT32);

            for (unsigned iTarget = 0; iTarget < nTargets; iTarget++)
            {
                if (offset + sizeof(INT32) > m_CodeSize)
                {
                    _ASSERTE(false);
                    return COR_E_INVALIDPROGRAM;
                }

                pInstr = NewILInstr();
                IfNullRet(pInstr);

                pInstr->m_opcode = CEE_SWITCH_ARG;

                pInstr->m_Arg32 = base + *(UNALIGNED INT32 *)&(pIL[offset]);
                offset += sizeof(INT32);

                InsertBefore(&m_IL, pInstr);
            }
            fBranch = true;
            break;
        }
        default:
            _ASSERTE(false);
            break;
        }
        offset += size;
    }
    _ASSERTE(offset == m_CodeSize);

    if (fBranch)
    {
        // Go over all control flow instructions and resolve the targets
        for (ILInstr * pInstr = m_IL.m_pNext; pInstr != &m_IL; pInstr = pInstr->m_pNext)
        {
            if (s_OpCodeFlags[pInstr->m_opcode] & OPCODEFLAGS_BranchTarget)
                pInstr->m_pTarget = GetInstrFromOffset(pInstr->m_Arg32);
        }
    }

    return S_OK;
}

HRESULT ILRewriter::ImportEH(const COR_ILMETHOD_SECT_EH* pILEH, unsigned nEH)
{
    _ASSERTE(m_ehClauses.empty());

    if (nEH == 0)
        return S_OK;

    for (unsigned iEH = 0; iEH < nEH; iEH++)
    {
        // If the EH clause is in tiny form, the call to pILEH->EHClause() below will
        // use this as a scratch buffer to expand the EH clause into its fat form.
        COR_ILMETHOD_SECT_EH_CLAUSE_FAT scratch;

        const COR_ILMETHOD_SECT_EH_CLAUSE_FAT* ehInfo;
        ehInfo = (COR_ILMETHOD_SECT_EH_CLAUSE_FAT*)pILEH->EHClause(iEH, &scratch);

        EHClause newEhClause;
        EHClause* clause = &newEhClause;
        clause->m_Flags = ehInfo->GetFlags();

        clause->m_pTryBegin = GetInstrFromOffset(ehInfo->GetTryOffset());
        clause->m_pTryEnd = GetInstrFromOffset(ehInfo->GetTryOffset() + ehInfo->GetTryLength());
        clause->m_pHandlerBegin = GetInstrFromOffset(ehInfo->GetHandlerOffset());
        clause->m_pHandlerEnd = GetInstrFromOffset(ehInfo->GetHandlerOffset() + ehInfo->GetHandlerLength())->m_pPrev;
        if ((clause->m_Flags & COR_ILEXCEPTION_CLAUSE_FILTER) == 0)
            clause->m_ClassToken = ehInfo->GetClassToken();
        else
            clause->m_pFilter = GetInstrFromOffset(ehInfo->GetFilterOffset());

        m_ehClauses.push_back(newEhClause);
    }

    return S_OK;
}

// --------------------------------------------------------------------------
// Single-pass evaluation-stack simulation.
// We only need to answer: "is the top-of-stack an object reference?"
// For verifiable IL all merge points have identical stack states,
// so a single linear pass suffices.
// --------------------------------------------------------------------------

namespace
{
    // Per-slot type: is it an object reference or something else?
    // SlotOther  = definitely NOT an object reference, or unknown (conservative)
    // SlotObjRef = definitely an object reference
    enum StackSlotKind : unsigned char { SlotOther = 0, SlotObjRef = 1 };
    using LibProfiler::SkipSigType;

    // Check if the leading element type of a signature blob represents an object reference.
    // Only inspects the signature bytes — no metadata resolution needed.
    bool IsSigTypeObjRef(const BYTE* sig, unsigned len)
    {
        if (len == 0) return false;
        BYTE elem = sig[0];
        switch (elem)
        {
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_ARRAY:
            return true;
        case ELEMENT_TYPE_GENERICINST:
            if (len >= 2)
                return sig[1] == ELEMENT_TYPE_CLASS;
            return false;
        default:
            return false;
        }
    }


    // Check if the base type token has name "System.ValueType" or "System.Enum".
    // Works for mdTypeDef (same module) and mdTypeRef (just reads the name, no resolution).
    bool IsBaseTokenValueType(IMetaDataImport* pImport, mdToken baseToken)
    {
        if (IsNilToken(baseToken))
            return false;

        WCHAR name[64];
        ULONG nameLen = 0;
        auto tt = TypeFromToken(baseToken);
        if (tt == mdtTypeDef)
            pImport->GetTypeDefProps(baseToken, name, 64, &nameLen, nullptr, nullptr);
        else if (tt == mdtTypeRef)
            pImport->GetTypeRefProps(baseToken, nullptr, name, 64, &nameLen);
        else
            return false;

        // Compare against known value-type base names.
        // On Linux WCHAR is char16_t, so use a char-array literal for portability.
        if (nameLen == 0) return false;
        const WCHAR sysValueType[] = { 'S','y','s','t','e','m','.','V','a','l','u','e','T','y','p','e',0 };
        const WCHAR sysEnum[]      = { 'S','y','s','t','e','m','.','E','n','u','m',0 };

        auto wstrEq = [](const WCHAR* a, const WCHAR* b) {
            while (*a && *a == *b) { a++; b++; }
            return *a == *b;
        };
        return wstrEq(name, sysValueType) || wstrEq(name, sysEnum);
    }

    // Determine the StackSlotKind for 'this' (arg 0) of the method being analyzed.
    // m_tkMethod is always mdtMethodDef. We get its declaring mdTypeDef, then check
    // whether the base type is System.ValueType or System.Enum (by reading the name).
    // No mdTypeRef resolution is needed — we just read the name string.
    StackSlotKind GetThisArgKind(IMetaDataImport* pImport, mdToken tkMethod)
    {
        mdTypeDef declaringType = mdTypeDefNil;
        pImport->GetMethodProps(tkMethod, &declaringType, nullptr, 0, nullptr,
                                nullptr, nullptr, nullptr, nullptr, nullptr);
        if (IsNilToken(declaringType))
            return SlotOther; // cannot determine — conservative

        mdToken baseToken = mdTokenNil;
        pImport->GetTypeDefProps(declaringType, nullptr, 0, nullptr, nullptr, &baseToken);

        if (IsBaseTokenValueType(pImport, baseToken))
            return SlotOther; // value type: 'this' is a managed pointer

        return SlotObjRef; // reference type: 'this' is an object reference
    }

    // Build a lookup of argument index → StackSlotKind from the method signature.
    // For instance methods, argTypes[0] = kind of 'this'.
    // Only reads signature blobs and type names — no mdTypeRef resolution.
    void BuildArgTypes(IMetaDataImport* pImport, mdToken tkMethod,
                       std::vector<StackSlotKind>& argTypes)
    {
        argTypes.clear();
        PCCOR_SIGNATURE sig = nullptr;
        ULONG sigLen = 0;
        pImport->GetMethodProps(tkMethod, nullptr, nullptr, 0, nullptr,
                                nullptr, &sig, &sigLen, nullptr, nullptr);
        if (sig == nullptr || sigLen == 0) return;

        const BYTE* ptr = sig;
        BYTE callingConv = *ptr++;
        bool hasThis = (callingConv & IMAGE_CEE_CS_CALLCONV_HASTHIS) != 0;

        if (callingConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
        {
            ULONG g;
            ptr += CorSigUncompressData(ptr, &g);
        }

        ULONG paramCount;
        ptr += CorSigUncompressData(ptr, &paramCount);

        if (hasThis)
            argTypes.push_back(GetThisArgKind(pImport, tkMethod));

        // Skip return type
        unsigned rem = static_cast<unsigned>(sigLen - (ptr - sig));
        ptr += SkipSigType(ptr, rem);

        // Parse each parameter type
        for (ULONG i = 0; i < paramCount; i++)
        {
            rem = static_cast<unsigned>(sigLen - (ptr - sig));
            if (rem == 0) break;
            argTypes.push_back(IsSigTypeObjRef(ptr, rem) ? SlotObjRef : SlotOther);
            ptr += SkipSigType(ptr, rem);
        }
    }

    // Build a lookup of local index → StackSlotKind from the local variable signature.
    // Only reads signature blobs — no metadata resolution.
    void BuildLocalTypes(IMetaDataImport* pImport, mdToken tkLocalVarSig,
                         std::vector<StackSlotKind>& localTypes)
    {
        localTypes.clear();
        if (IsNilToken(tkLocalVarSig) || tkLocalVarSig == 0) return;

        PCCOR_SIGNATURE sig = nullptr;
        ULONG sigLen = 0;
        pImport->GetSigFromToken(tkLocalVarSig, &sig, &sigLen);
        if (sig == nullptr || sigLen < 2) return;

        const BYTE* ptr = sig;
        if (*ptr++ != IMAGE_CEE_CS_CALLCONV_LOCAL_SIG) return;

        ULONG localCount;
        ptr += CorSigUncompressData(ptr, &localCount);

        for (ULONG i = 0; i < localCount; i++)
        {
            unsigned rem = static_cast<unsigned>(sigLen - (ptr - sig));
            if (rem == 0) break;
            const BYTE* typeStart = ptr;
            // Skip ELEMENT_TYPE_PINNED if present
            if (*ptr == ELEMENT_TYPE_PINNED) { ptr++; rem--; typeStart = ptr; }
            localTypes.push_back(IsSigTypeObjRef(typeStart, rem) ? SlotObjRef : SlotOther);
            ptr += SkipSigType(typeStart, rem);
        }
    }

    // Determine the kind for a field load (ldfld pushes the field value).
    // Reads the field signature blob — no type resolution.
    StackSlotKind GetFieldLoadKind(IMetaDataImport* pImport, mdToken fieldToken)
    {
        PCCOR_SIGNATURE sig = nullptr;
        ULONG sigLen = 0;
        auto tt = TypeFromToken(fieldToken);
        if (tt == mdtFieldDef)
            pImport->GetFieldProps(fieldToken, nullptr, nullptr, 0, nullptr,
                                   nullptr, &sig, &sigLen, nullptr, nullptr, nullptr);
        else if (tt == mdtMemberRef)
            pImport->GetMemberRefProps(fieldToken, nullptr, nullptr, 0, nullptr, &sig, &sigLen);

        if (sig == nullptr || sigLen < 2) return SlotOther;
        const BYTE* ptr = sig;
        if (*ptr == IMAGE_CEE_CS_CALLCONV_FIELD) ptr++;
        unsigned rem = static_cast<unsigned>(sigLen - (ptr - sig));
        return IsSigTypeObjRef(ptr, rem) ? SlotObjRef : SlotOther;
    }

    // Get the argument index for ldarg-family opcodes. Returns -1 if not ldarg.
    int GetArgIndex(unsigned opcode, INT32 arg)
    {
        switch (opcode)
        {
        case CEE_LDARG_0: return 0;
        case CEE_LDARG_1: return 1;
        case CEE_LDARG_2: return 2;
        case CEE_LDARG_3: return 3;
        case CEE_LDARG_S: return static_cast<int>(static_cast<unsigned char>(arg));
        case CEE_LDARG:   return static_cast<int>(static_cast<unsigned short>(arg));
        default: return -1;
        }
    }

    // Get the local index for ldloc-family opcodes. Returns -1 if not ldloc.
    int GetLocIndex(unsigned opcode, INT32 arg)
    {
        switch (opcode)
        {
        case CEE_LDLOC_0: return 0;
        case CEE_LDLOC_1: return 1;
        case CEE_LDLOC_2: return 2;
        case CEE_LDLOC_3: return 3;
        case CEE_LDLOC_S: return static_cast<int>(static_cast<unsigned char>(arg));
        case CEE_LDLOC:   return static_cast<int>(static_cast<unsigned short>(arg));
        default: return -1;
        }
    }

    // Return the number of parameters a method pops (excluding the return value push).
    // Works for mdMethodDef, mdMemberRef.  Returns -1 on failure.
    int GetMethodParamCount(IMetaDataImport* pImport, mdToken methodToken, bool* hasThis, bool* returnsVoid)
    {
        PCCOR_SIGNATURE sig = nullptr;
        ULONG sigLen = 0;

        auto tokenType = TypeFromToken(methodToken);
        if (tokenType == mdtMethodDef)
        {
            pImport->GetMethodProps(methodToken, nullptr, nullptr, 0, nullptr,
                                    nullptr, &sig, &sigLen, nullptr, nullptr);
        }
        else if (tokenType == mdtMemberRef)
        {
            pImport->GetMemberRefProps(methodToken, nullptr, nullptr, 0, nullptr, &sig, &sigLen);
        }

        if (sig == nullptr || sigLen == 0)
            return -1;

        // Byte 0: calling convention
        BYTE callingConv = sig[0];
        *hasThis = (callingConv & IMAGE_CEE_CS_CALLCONV_HASTHIS) != 0;
        const BYTE* ptr = sig + 1;

        // Skip generic param count if GENERIC
        if (callingConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
        {
            ULONG genParamCount;
            ptr += CorSigUncompressData(ptr, &genParamCount);
        }

        // Parameter count
        ULONG paramCount;
        ptr += CorSigUncompressData(ptr, &paramCount);

        // Check return type – is it void?
        *returnsVoid = (*ptr == ELEMENT_TYPE_VOID);

        return static_cast<int>(paramCount);
    }

    // Determine whether a method's return type is an object reference.
    // Conservatively returns false when we cannot tell.
    bool MethodReturnsObjRef(IMetaDataImport* pImport, mdToken methodToken)
    {
        PCCOR_SIGNATURE sig = nullptr;
        ULONG sigLen = 0;

        auto tokenType = TypeFromToken(methodToken);
        if (tokenType == mdtMethodDef)
            pImport->GetMethodProps(methodToken, nullptr, nullptr, 0, nullptr,
                                    nullptr, &sig, &sigLen, nullptr, nullptr);
        else if (tokenType == mdtMemberRef)
            pImport->GetMemberRefProps(methodToken, nullptr, nullptr, 0, nullptr, &sig, &sigLen);

        if (sig == nullptr || sigLen < 2)
            return false;

        const BYTE* ptr = sig;
        BYTE callingConv = *ptr++;

        // Skip generic param count
        if (callingConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
        {
            ULONG g;
            ptr += CorSigUncompressData(ptr, &g);
        }

        // Skip param count
        ULONG paramCount;
        ptr += CorSigUncompressData(ptr, &paramCount);

        unsigned remaining = static_cast<unsigned>(sigLen - (ptr - sig));
        return IsSigTypeObjRef(ptr, remaining);
    }
}

HRESULT ILRewriter::ComputeStackTypes()
{
    // We need IMetaDataImport to resolve VarPop/VarPush for call instructions.
    // m_pMetaDataImport may not be set yet (Initialize() might not have been called).
    // In that case, acquire a temporary one.
    IMetaDataImport* pImport = m_pMetaDataImport;
    bool ownedImport = false;
    if (pImport == nullptr)
    {
        HRESULT hr = m_pICorProfilerInfo->GetModuleMetaData(
            m_moduleId, ofRead, IID_IMetaDataImport, (IUnknown**)&pImport);
        if (FAILED(hr) || pImport == nullptr)
        {
            // Cannot get metadata – skip stack analysis, all annotations remain false
            return S_OK;
        }
        ownedImport = true;
    }

    // Stack is a vector of SlotOther/SlotObjRef.
    std::vector<StackSlotKind> stack;
    stack.reserve(m_maxStack);

    // Pre-parse method argument types and local variable types from signature blobs.
    std::vector<StackSlotKind> argTypes;
    BuildArgTypes(pImport, m_tkMethod, argTypes);

    std::vector<StackSlotKind> localTypes;
    BuildLocalTypes(pImport, m_tkLocalVarSig, localTypes);

    // Seed catch/filter handler entry points with one ObjRef element.
    // We'll handle this by checking if an instruction is a handler begin.
    // Build a set of handler-begin instructions for fast lookup.
    std::unordered_map<ILInstr*, StackSlotKind> handlerEntryKind;
    for (auto& eh : m_ehClauses)
    {
        // Catch handlers push the exception object
        if ((eh.m_Flags & COR_ILEXCEPTION_CLAUSE_FILTER) == 0 &&
            (eh.m_Flags & COR_ILEXCEPTION_CLAUSE_FINALLY) == 0 &&
            (eh.m_Flags & COR_ILEXCEPTION_CLAUSE_FAULT) == 0)
        {
            handlerEntryKind[eh.m_pHandlerBegin] = SlotObjRef;
        }
        // Filter handlers also start with the exception object
        if (eh.m_Flags & COR_ILEXCEPTION_CLAUSE_FILTER)
        {
            handlerEntryKind[eh.m_pFilter] = SlotObjRef;
            handlerEntryKind[eh.m_pHandlerBegin] = SlotObjRef;
        }
    }

    for (ILInstr* pInstr = m_IL.m_pNext; pInstr != &m_IL; pInstr = pInstr->m_pNext)
    {
        unsigned opcode = pInstr->m_opcode;

        // Skip internal pseudo-instructions
        if (opcode == CEE_SWITCH_ARG)
            continue;

        // Safety: skip opcodes outside the known range
        if (opcode >= CEE_COUNT)
            continue;

        // Check if this is a handler entry point – reset stack
        auto handlerIt = handlerEntryKind.find(pInstr);
        if (handlerIt != handlerEntryKind.end())
        {
            stack.clear();
            stack.push_back(handlerIt->second);
        }

        // Instructions that empty the stack (leave/leave.s, endfinally, throw, rethrow, endfilter)
        if (opcode == CEE_LEAVE || opcode == CEE_LEAVE_S ||
            opcode == CEE_ENDFINALLY || opcode == CEE_THROW ||
            opcode == CEE_RETHROW)
        {
            stack.clear();
            continue;
        }

        // Annotate LDFLD/STFLD: is the object operand an object reference?
        pInstr->m_objOperandIsObjRef = false;
        if (opcode == CEE_LDFLD || opcode == CEE_LDFLDA)
        {
            // Stack: [..., obj] — object operand is top
            pInstr->m_objOperandIsObjRef = !stack.empty() && stack.back() == SlotObjRef;
        }
        else if (opcode == CEE_STFLD)
        {
            // Stack: [..., obj, value] — object operand is second from top
            if (stack.size() >= 2)
                pInstr->m_objOperandIsObjRef = stack[stack.size() - 2] == SlotObjRef;
        }

        // --- Pop ---
        int popCount = k_rgnStackPops[opcode];
        if (popCount == -1) // VarPop
        {
            // call, callvirt, calli, ret, newobj
            if (opcode == CEE_RET)
            {
                stack.clear();
                continue;
            }
            else if (opcode == CEE_CALL || opcode == CEE_CALLVIRT || opcode == CEE_NEWOBJ)
            {
                mdToken methodToken = static_cast<mdToken>(pInstr->m_Arg32);
                bool hasThis = false;
                bool returnsVoid = true;
                int paramCount = GetMethodParamCount(pImport, methodToken, &hasThis, &returnsVoid);
                if (paramCount >= 0)
                {
                    int totalPop = paramCount;
                    // For call/callvirt, add 1 for 'this' if instance method
                    // For newobj, 'this' is not on the stack (it is created by the instruction)
                    if (opcode != CEE_NEWOBJ && hasThis)
                        totalPop += 1;
                    for (int i = 0; i < totalPop && !stack.empty(); i++)
                        stack.pop_back();
                }
                else
                {
                    // Cannot resolve – conservatively clear
                    stack.clear();
                }

                // Push return value
                if (opcode == CEE_NEWOBJ)
                {
                    stack.push_back(SlotObjRef);
                }
                else if (!returnsVoid)
                {
                    // Check if return type is an object reference
                    bool isRef = MethodReturnsObjRef(pImport, methodToken);
                    stack.push_back(isRef ? SlotObjRef : SlotOther);
                }
                continue;
            }
            else if (opcode == CEE_CALLI)
            {
                // Cannot easily resolve calli signature; conservatively clear
                stack.clear();
                // VarPush: signature-dependent
                // Push one unknown slot to be safe
                stack.push_back(SlotOther);
                continue;
            }
            else
            {
                // Unknown VarPop – clear
                stack.clear();
                continue;
            }
        }
        else
        {
            for (int i = 0; i < popCount && !stack.empty(); i++)
                stack.pop_back();
        }

        // --- Push ---
        int pushCount = k_rgnStackPushes[opcode];

        // Handle DUP specially: it duplicates the top, preserving type
        if (opcode == CEE_DUP)
        {
            if (!stack.empty())
            {
                StackSlotKind top = stack.back();
                stack.push_back(top);
            }
            else
            {
                stack.push_back(SlotOther);
            }
            continue;
        }

        if (pushCount > 0)
        {
            StackSlotKind kind;
            int isRef = k_rgnPushIsRef[opcode];
            if (isRef == 1)
            {
                // PushRef: definitely an object reference
                kind = SlotObjRef;
            }
            else
            {
                // Push1 or PushI/PushI4/etc. — default to SlotOther,
                // then try to resolve for ldarg/ldloc/ldfld via signature blobs.
                kind = SlotOther;

                // ldarg family: look up pre-parsed argument type
                int argIdx = GetArgIndex(opcode, pInstr->m_Arg32);
                if (argIdx >= 0 && argIdx < static_cast<int>(argTypes.size()))
                {
                    kind = argTypes[argIdx];
                }
                else
                {
                    // ldloc family: look up pre-parsed local type
                    int locIdx = GetLocIndex(opcode, pInstr->m_Arg32);
                    if (locIdx >= 0 && locIdx < static_cast<int>(localTypes.size()))
                    {
                        kind = localTypes[locIdx];
                    }
                    // ldfld: check field signature blob for the pushed type
                    else if (opcode == CEE_LDFLD)
                    {
                        kind = GetFieldLoadKind(pImport, static_cast<mdToken>(pInstr->m_Arg32));
                    }
                }
            }

            for (int i = 0; i < pushCount; i++)
                stack.push_back(kind);
        }

        // Unconditional branches: for verifiable IL, we trust the merge.
        // After unconditional branch, stack state is undefined until next target.
        if (opcode == CEE_BR || opcode == CEE_BR_S)
        {
            stack.clear();
        }
    }

    if (ownedImport)
        pImport->Release();

    return S_OK;
}

ILInstr* ILRewriter::NewILInstr()
{
    m_nInstrs++;
    return new ILInstr { };
}

ILInstr* ILRewriter::GetInstrFromOffset(unsigned offset)
{
    ILInstr * pInstr = NULL;

    if (offset <= m_CodeSize)
        pInstr = m_pOffsetToInstr[offset];

    _ASSERTE(pInstr != NULL);
    return pInstr;
}

void ILRewriter::InsertTryCatch(ILInstr * pTryStart, ILInstr * pCatchStart, ILInstr * pCatchEnd, mdToken filterClassToken)
{
    EHClause clause = {};
    clause.m_Flags = CorExceptionFlag::COR_ILEXCEPTION_CLAUSE_NONE;
    clause.m_ClassToken = filterClassToken;

    clause.m_pTryBegin = pTryStart;
    clause.m_pTryEnd = pCatchStart;
    clause.m_pHandlerBegin = pCatchStart;
    clause.m_pHandlerEnd = pCatchEnd;

    m_ehClauses.push_back(clause);
}

void ILRewriter::InsertBefore(ILInstr * pWhere, ILInstr * pWhat)
{
    pWhat->m_pNext = pWhere;
    pWhat->m_pPrev = pWhere->m_pPrev;

    pWhat->m_pNext->m_pPrev = pWhat;
    pWhat->m_pPrev->m_pNext = pWhat;

    AdjustState(pWhat);
}

void ILRewriter::InsertAfter(ILInstr * pWhere, ILInstr * pWhat)
{
    pWhat->m_pNext = pWhere->m_pNext;
    pWhat->m_pPrev = pWhere;

    pWhat->m_pNext->m_pPrev = pWhat;
    pWhat->m_pPrev->m_pNext = pWhat;

    AdjustState(pWhat);
}

void ILRewriter::AdjustState(ILInstr * pNewInstr)
{
    m_maxStack += k_rgnStackPushes[pNewInstr->m_opcode];
}


ILInstr * ILRewriter::GetILList()
{
    return &m_IL;
}

/////////////////////////////////////////////////////////////////////////////////////////////////
//
// E X P O R T
//
////////////////////////////////////////////////////////////////////////////////////////////////



// ILRewriter::Export intentionally does a comparison by casting a variable (delta) down
// to an INT8, with data loss being expected and handled. This pragma is required because
// this is compiled with RTC on, and without the pragma, the above cast will generate a
// run-time check on whether we lose data, and cause an unhandled exception (look up
// RTC_Check_4_to_1).
#if TARGET_WINDOWS
#pragma runtime_checks( "c", off )
#pragma warning ( push )
#pragma warning( disable: 4244 )
#endif
// SectEH_Emit (in corhlpr.cpp) also triggers the potential data loss warning, however this function is not used so we can safely ignore it.
#include <corhlpr.cpp>

HRESULT ILRewriter::Export()
{
    HRESULT hr = S_OK;
    // One instruction produces 6 bytes in the worst case
    unsigned maxSize = m_nInstrs * 9;

    m_pOutputBuffer = new BYTE[maxSize];
    IfNullRet(m_pOutputBuffer);

    if (m_ehClauses.size() > UINT32_MAX)
    {
        return E_UNEXPECTED;
    }
    unsigned m_nEH = static_cast<unsigned>(m_ehClauses.size());

again:
    // TODO [DAVBR]: Why separate pointer pIL?  Doesn't look like either pIL or
    // m_pOutputBuffer is moved.
    BYTE* pIL = m_pOutputBuffer;

    bool fBranch = false;
    unsigned offset = 0;

    // Go over all instructions and produce code for them
    for (ILInstr * pInstr = m_IL.m_pNext; pInstr != &m_IL; pInstr = pInstr->m_pNext)
    {
        pInstr->m_offset = offset;

        unsigned opcode = pInstr->m_opcode;
        if (opcode < CEE_COUNT)
        {
            // CEE_PREFIX1 refers not to instruction prefixes (like tail.), but to
            // the lead byte of multi-byte opcodes. For now, the only lead byte
            // supported is CEE_PREFIX1 = 0xFE.
            if (opcode >= 0x100)
                m_pOutputBuffer[offset++] = CEE_PREFIX1;

            // TODO: [DAVBR]: This appears to depend on an implicit conversion from
            // unsigned opcode down to BYTE, to deliberately lose data and have
            // opcode >= 0x100 wrap around to 0.
            m_pOutputBuffer[offset++] = (opcode & 0xFF);
        }

        _ASSERTE(pInstr->m_opcode < dimensionof(s_OpCodeFlags));
        BYTE flags = s_OpCodeFlags[pInstr->m_opcode];
        switch (flags)
        {
        case 0:
            break;
        case 1:
            *(UNALIGNED INT8 *)&(pIL[offset]) = pInstr->m_Arg8;
            break;
        case 2:
            *(UNALIGNED INT16 *)&(pIL[offset]) = pInstr->m_Arg16;
            break;
        case 4:
            *(UNALIGNED INT32 *)&(pIL[offset]) = pInstr->m_Arg32;
            break;
        case 8:
            *(UNALIGNED INT64 *)&(pIL[offset]) = pInstr->m_Arg64;
            break;
        case 1 | OPCODEFLAGS_BranchTarget:
            fBranch = true;
            break;
        case 4 | OPCODEFLAGS_BranchTarget:
            fBranch = true;
            break;
        case 0 | OPCODEFLAGS_Switch:
            *(UNALIGNED INT32 *)&(pIL[offset]) = pInstr->m_Arg32;
            offset += sizeof(INT32);
            break;
        default:
            _ASSERTE(false);
            break;
        }
        offset += (flags & OPCODEFLAGS_SizeMask);
    }
    m_IL.m_offset = offset;

    if (fBranch)
    {
        bool fTryAgain = false;
        unsigned switchBase = 0;

        // Go over all control flow instructions and resolve the targets
        for (ILInstr * pInstr = m_IL.m_pNext; pInstr != &m_IL; pInstr = pInstr->m_pNext)
        {
            unsigned opcode = pInstr->m_opcode;

            if (pInstr->m_opcode == CEE_SWITCH)
            {
                switchBase = pInstr->m_offset + 1 + sizeof(INT32) * (pInstr->m_Arg32 + 1);
                continue;
            }
            if (opcode == CEE_SWITCH_ARG)
            {
                // Switch args are special
                *(UNALIGNED INT32 *)&(pIL[pInstr->m_offset]) = pInstr->m_pTarget->m_offset - switchBase;
                continue;
            }

            BYTE flags = s_OpCodeFlags[pInstr->m_opcode];

            if (flags & OPCODEFLAGS_BranchTarget)
            {
                int delta = pInstr->m_pTarget->m_offset - pInstr->m_pNext->m_offset;

                switch (flags)
                {
                case 1 | OPCODEFLAGS_BranchTarget:
                    // Check if delta is too big to fit into an INT8.
                    //
                    // (see #pragma at top of file)
                    if ((INT8)delta != delta)
                    {
                        if (opcode == CEE_LEAVE_S)
                        {
                            pInstr->m_opcode = CEE_LEAVE;
                        }
                        else
                        {
                            _ASSERTE(opcode >= CEE_BR_S && opcode <= CEE_BLT_UN_S);
                            pInstr->m_opcode = opcode - CEE_BR_S + CEE_BR;
                            _ASSERTE(pInstr->m_opcode >= CEE_BR && pInstr->m_opcode <= CEE_BLT_UN);
                        }
                        fTryAgain = true;
                        continue;
                    }
                    *(UNALIGNED INT8 *)&(pIL[pInstr->m_pNext->m_offset - sizeof(INT8)]) = delta;
                    break;
                case 4 | OPCODEFLAGS_BranchTarget:
                    *(UNALIGNED INT32 *)&(pIL[pInstr->m_pNext->m_offset - sizeof(INT32)]) = delta;
                    break;
                default:
                    _ASSERTE(false);
                    break;
                }
            }
        }

        // Do the whole thing again if we changed the size of some branch targets
        if (fTryAgain)
            goto again;
    }

    unsigned codeSize = offset;
    unsigned totalSize;
    LPBYTE pBody = NULL;
    if (m_fGenerateTinyHeader)
    {
        // Make sure we can fit in a tiny header
        if (codeSize >= 64)
            return E_FAIL;

        totalSize = sizeof(IMAGE_COR_ILMETHOD_TINY) + codeSize;
        pBody = AllocateILMemory(totalSize);
        IfNullRet(pBody);

        BYTE * pCurrent = pBody;

        // Here's the tiny header
        *pCurrent = (BYTE)(CorILMethod_TinyFormat | (codeSize << 2));
        pCurrent += sizeof(IMAGE_COR_ILMETHOD_TINY);

        // And the body
        memcpy(pCurrent, m_pOutputBuffer, codeSize);
    }
    else
    {
        // Use FAT header

        unsigned alignedCodeSize = (offset + 3) & ~3;

        totalSize = sizeof(IMAGE_COR_ILMETHOD_FAT) + alignedCodeSize +
            (m_nEH ? (sizeof(IMAGE_COR_ILMETHOD_SECT_FAT) + sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT) * m_nEH) : 0);

        pBody = AllocateILMemory(totalSize);
        IfNullRet(pBody);

        BYTE * pCurrent = pBody;

        IMAGE_COR_ILMETHOD_FAT *pHeader = (IMAGE_COR_ILMETHOD_FAT *)pCurrent;
        pHeader->Flags = m_flags | (m_nEH ? CorILMethod_MoreSects : 0) | CorILMethod_FatFormat;
        if (m_tkLocalVarSig != 0)
            pHeader->Flags |= CorILMethod_InitLocals;
        pHeader->Size = sizeof(IMAGE_COR_ILMETHOD_FAT) / sizeof(DWORD);
        pHeader->MaxStack = m_maxStack;
        pHeader->CodeSize = offset;
        pHeader->LocalVarSigTok = m_tkLocalVarSig;

        pCurrent = (BYTE*)(pHeader + 1);

        memcpy(pCurrent, m_pOutputBuffer, codeSize);
        pCurrent += alignedCodeSize;

        if (m_nEH != 0)
        {
            IMAGE_COR_ILMETHOD_SECT_FAT *pEH = (IMAGE_COR_ILMETHOD_SECT_FAT *)pCurrent;
            pEH->Kind = CorILMethod_Sect_EHTable | CorILMethod_Sect_FatFormat;
            pEH->DataSize = (unsigned)(sizeof(IMAGE_COR_ILMETHOD_SECT_FAT) + sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT) * m_nEH);

            pCurrent = (BYTE*)(pEH + 1);

            for (unsigned iEH = 0; iEH < m_nEH; iEH++)
            {
                EHClause *pSrc = &(m_ehClauses.at(iEH));
                IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT * pDst = (IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT *)pCurrent;

                pDst->Flags = pSrc->m_Flags;
                pDst->TryOffset = pSrc->m_pTryBegin->m_offset;
                pDst->TryLength = pSrc->m_pTryEnd->m_offset - pSrc->m_pTryBegin->m_offset;
                pDst->HandlerOffset = pSrc->m_pHandlerBegin->m_offset;
                pDst->HandlerLength = pSrc->m_pHandlerEnd->m_pNext->m_offset - pSrc->m_pHandlerBegin->m_offset;
                if ((pSrc->m_Flags & COR_ILEXCEPTION_CLAUSE_FILTER) == 0)
                    pDst->ClassToken = pSrc->m_ClassToken;
                else
                    pDst->FilterOffset = pSrc->m_pFilter->m_offset;

                pCurrent = (BYTE*)(pDst + 1);
            }
        }
    }

    IfFailRet(SetILFunctionBody(totalSize, pBody));
    DeallocateILMemory(pBody);

    return S_OK;
}
#if TARGET_WINDOWS
#pragma warning( pop )
#pragma runtime_checks( "", restore )
#endif

HRESULT ILRewriter::SetILFunctionBody(unsigned size, LPBYTE pBody)
{
    HRESULT hr = S_OK;
    if (m_pICorProfilerFunctionControl != NULL)
    {
        // We're supplying IL for a rejit, so use the rejit mechanism
        IfFailRet(m_pICorProfilerFunctionControl->SetILFunctionBody(size, pBody));
    }
    else
    {
        // "classic-style" instrumentation on first JIT, so use old mechanism
        IfFailRet(m_pICorProfilerInfo->SetILFunctionBody(m_moduleId, m_tkMethod, pBody));
    }

    return S_OK;
}

LPBYTE ILRewriter::AllocateILMemory(unsigned size)
{
    if (m_pICorProfilerFunctionControl != NULL)
    {
        // We're supplying IL for a rejit, so we can just allocate from
        // the heap
        return new BYTE[size];
    }

    // Else, this is "classic-style" instrumentation on first JIT, and
    // need to use the CLR's IL allocator

    if (FAILED(m_pICorProfilerInfo->GetILFunctionBodyAllocator(m_moduleId, &m_pIMethodMalloc)))
        return NULL;

    return (LPBYTE)m_pIMethodMalloc->Alloc(size);
}

void ILRewriter::DeallocateILMemory(LPBYTE pBody)
{
    if (m_pICorProfilerFunctionControl == NULL)
    {
        // Old-style instrumentation does not provide a way to free up bytes
        return;
    }

    delete[] pBody;
}