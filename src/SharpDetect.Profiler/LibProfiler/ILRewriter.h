// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <vector>
#include "cor.h"
#include "corprof.h"
#include "corhlpr.h"

struct COR_ILMETHOD_SECT_EH;
struct ILInstr
{
    ILInstr *       m_pNext;
    ILInstr *       m_pPrev;

    unsigned        m_opcode;
    unsigned        m_offset;

    // Set during stack-type analysis: true when the object operand on the stack
    // (for LDFLD: top-of-stack; for STFLD: second-from-top) is an object reference.
    // Only meaningful for LDFLD/STFLD instructions.
    bool            m_objOperandIsObjRef;

    union
    {
        ILInstr *   m_pTarget;
        INT8        m_Arg8;
        INT16       m_Arg16;
        INT32       m_Arg32;
        INT64       m_Arg64;
    };
};

struct EHClause
{
    CorExceptionFlag            m_Flags;
    ILInstr *                   m_pTryBegin;
    ILInstr *                   m_pTryEnd;
    ILInstr *                   m_pHandlerBegin;    // First instruction inside the handler
    ILInstr *                   m_pHandlerEnd;      // Last instruction inside the handler
    union
    {
        DWORD                   m_ClassToken;   // use for type-based exception handlers
        ILInstr *               m_pFilter;      // use for filter-based exception handlers (COR_ILEXCEPTION_CLAUSE_FILTER is set)
    };
};

typedef enum
{
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) c,
#include "opcode.def"
#undef OPDEF
    CEE_COUNT,
    CEE_SWITCH_ARG, // special internal instructions
} OPCODE;

#define dimensionof(a) 		(sizeof(a)/sizeof(*(a)))

#define OPCODEFLAGS_SizeMask        0x0F
#define OPCODEFLAGS_BranchTarget    0x10
#define OPCODEFLAGS_Switch          0x20

static const BYTE s_OpCodeFlags[] =
{
#define InlineNone           0
#define ShortInlineVar       1
#define InlineVar            2
#define ShortInlineI         1
#define InlineI              4
#define InlineI8             8
#define ShortInlineR         4
#define InlineR              8
#define ShortInlineBrTarget  1 | OPCODEFLAGS_BranchTarget
#define InlineBrTarget       4 | OPCODEFLAGS_BranchTarget
#define InlineMethod         4
#define InlineField          4
#define InlineType           4
#define InlineString         4
#define InlineSig            4
#define InlineRVA            4
#define InlineTok            4
#define InlineSwitch         0 | OPCODEFLAGS_Switch

#define OPDEF(c,s,pop,push,args,type,l,s1,s2,flow) args,
#include "opcode.def"
#undef OPDEF

#undef InlineNone
#undef ShortInlineVar
#undef InlineVar
#undef ShortInlineI
#undef InlineI
#undef InlineI8
#undef ShortInlineR
#undef InlineR
#undef ShortInlineBrTarget
#undef InlineBrTarget
#undef InlineMethod
#undef InlineField
#undef InlineType
#undef InlineString
#undef InlineSig
#undef InlineRVA
#undef InlineTok
#undef InlineSwitch
    0,                              // CEE_COUNT
    4 | OPCODEFLAGS_BranchTarget,   // CEE_SWITCH_ARG
};

static int k_rgnStackPushes[] = {

#if defined(WIN32) || defined(WIN64)
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) \
    { push },
#else
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) \
    push,
#endif

#define Push0    0
#define Push1    1
#define PushI    1
#define PushI4   1
#define PushR4   1
#define PushI8   1
#define PushR8   1
#define PushRef  1
#define VarPush  1          // Test code doesn't call vararg fcns, so this should not be used

#include "opcode.def"

#undef Push0
#undef Push1
#undef PushI
#undef PushI4
#undef PushR4
#undef PushI8
#undef PushR8
#undef PushRef
#undef VarPush
#undef OPDEF
    0,  // CEE_COUNT
    0   // CEE_SWITCH_ARG
};

// Stack pop counts per opcode.  VarPop is encoded as -1 (needs special handling).
static int k_rgnStackPops[] = {

#if defined(WIN32) || defined(WIN64)
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) \
    { pop },
#else
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) \
    pop,
#endif

#define Pop0     0
#define Pop1     1
#define PopI     1
#define PopI4    1
#define PopI8    1
#define PopR4    1
#define PopR8    1
#define PopRef   1
#define VarPop  -1

#include "opcode.def"

#undef Pop0
#undef Pop1
#undef PopI
#undef PopI4
#undef PopI8
#undef PopR4
#undef PopR8
#undef PopRef
#undef VarPop
#undef OPDEF
    0,  // CEE_COUNT
    0   // CEE_SWITCH_ARG
};

// Whether the opcode pushes an object reference.
//   0 = definitely NOT an object reference (PushI, PushI4, PushI8, PushR4, PushR8, Push1)
//   1 = definitely an object reference (PushRef)
//  -1 = VarPush (needs method-signature inspection)
// Note: Push1 is mapped to 0 here. ComputeStackTypes() resolves Push1 opcodes
// (ldarg/ldloc/ldfld) explicitly from signature blobs without resolving mdTypeRef tokens.
static int k_rgnPushIsRef[] = {

#if defined(WIN32) || defined(WIN64)
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) \
    { push },
#else
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) \
    push,
#endif

#define Push0    0
#define Push1    0
#define PushI    0
#define PushI4   0
#define PushR4   0
#define PushI8   0
#define PushR8   0
#define PushRef  1
#define VarPush -1

#include "opcode.def"

#undef Push0
#undef Push1
#undef PushI
#undef PushI4
#undef PushR4
#undef PushI8
#undef PushR8
#undef PushRef
#undef VarPush
#undef OPDEF
    0,  // CEE_COUNT
    0   // CEE_SWITCH_ARG
};


class ILRewriter
{
private:
    ICorProfilerInfo * m_pICorProfilerInfo;
    ICorProfilerFunctionControl * m_pICorProfilerFunctionControl;

    ModuleID    m_moduleId;
    mdToken     m_tkMethod;

    mdToken     m_tkLocalVarSig;
    unsigned    m_maxStack;
    unsigned    m_flags;
    bool        m_fGenerateTinyHeader;

    ILInstr m_IL; // Double linked list of all il instructions

    std::vector<EHClause>  m_ehClauses;

    // Helper table for importing.  Sparse array that maps BYTE offset of beginning of an
    // instruction to that instruction's ILInstr*.  BYTE offsets that don't correspond
    // to the beginning of an instruction are mapped to NULL.
    ILInstr **  m_pOffsetToInstr;
    unsigned    m_CodeSize;

    unsigned    m_nInstrs;

    BYTE *      m_pOutputBuffer;

    IMethodMalloc * m_pIMethodMalloc;

    IMetaDataImport * m_pMetaDataImport;
    IMetaDataEmit * m_pMetaDataEmit;

public:
    ILRewriter(ICorProfilerInfo * pICorProfilerInfo, ICorProfilerFunctionControl * pICorProfilerFunctionControl, ModuleID moduleID, mdToken tkMethod);
    ~ILRewriter();
    HRESULT Initialize();

    [[nodiscard]] mdToken GetLocalVarSigToken() const { return m_tkLocalVarSig; }
    void SetLocalVarSigToken(const mdToken tkLocalVarSig) { m_tkLocalVarSig = tkLocalVarSig; }
    [[nodiscard]] mdToken GetMethodSigToken() const { return m_tkMethod; }

    /////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // I M P O R T
    //
    ////////////////////////////////////////////////////////////////////////////////////////////////
    HRESULT Import();
    HRESULT ImportIL(LPCBYTE pIL);
    HRESULT ImportEH(const COR_ILMETHOD_SECT_EH* pILEH, unsigned nEH);
    HRESULT ComputeStackTypes();
    ILInstr* NewILInstr();
    ILInstr* GetInstrFromOffset(unsigned offset);
    void InsertBefore(ILInstr * pWhere, ILInstr * pWhat);
    void InsertAfter(ILInstr * pWhere, ILInstr * pWhat);
    void InsertTryCatch(ILInstr * pTryStart, ILInstr * pCatchStart, ILInstr * pCatchEnd, mdToken filterClassToken);
    void AdjustState(ILInstr * pNewInstr);
    ILInstr * GetILList();

    /////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // E X P O R T
    //
    ////////////////////////////////////////////////////////////////////////////////////////////////
    HRESULT Export();
    HRESULT SetILFunctionBody(unsigned size, LPBYTE pBody);
    LPBYTE AllocateILMemory(unsigned size);
    void DeallocateILMemory(LPBYTE pBody);
};