; Copyright (c) .NET Foundation and contributors. All rights reserved.
; Licensed under the MIT license. See LICENSE.coreclr.txt file in the project root for full license information.

EXTERN EnterStub:PROC
EXTERN LeaveStub:PROC
EXTERN TailcallStub:PROC

_text SEGMENT PARA 'CODE'

ALIGN 16
PUBLIC EnterNaked

EnterNaked PROC FRAME

    PUSH RAX
    .PUSHREG RAX
    PUSH RCX
    .PUSHREG RCX
    PUSH RDX
    .PUSHREG RDX
    PUSH R8
    .PUSHREG R8
    PUSH R9
    .PUSHREG R9
    PUSH R10
    .PUSHREG R10
    PUSH R11
    .PUSHREG R11

    SUB RSP, 20H
    .ALLOCSTACK 20H

    .ENDPROLOG

    CALL EnterStub

    ADD RSP, 20H

    POP R11
    POP R10
    POP R9
    POP R8
    POP RDX
    POP RCX
    POP RAX

    RET

EnterNaked ENDP

ALIGN 16
PUBLIC LeaveNaked

LeaveNaked PROC FRAME

    PUSH RAX
    .PUSHREG RAX
    PUSH RCX
    .PUSHREG RCX
    PUSH RDX
    .PUSHREG RDX
    PUSH R8
    .PUSHREG R8
    PUSH R9
    .PUSHREG R9
    PUSH R10
    .PUSHREG R10
    PUSH R11
    .PUSHREG R11

    SUB RSP, 20H
    .ALLOCSTACK 20H

    .ENDPROLOG

    CALL LeaveStub

    ADD RSP, 20H

    POP R11
    POP R10
    POP R9
    POP R8
    POP RDX
    POP RCX
    POP RAX

    RET

LeaveNaked ENDP

ALIGN 16
PUBLIC TailcallNaked

TailcallNaked PROC FRAME

    PUSH RAX
    .PUSHREG RAX
    PUSH RCX
    .PUSHREG RCX
    PUSH RDX
    .PUSHREG RDX
    PUSH R8
    .PUSHREG R8
    PUSH R9
    .PUSHREG R9
    PUSH R10
    .PUSHREG R10
    PUSH R11
    .PUSHREG R11

    SUB RSP, 20H
    .ALLOCSTACK 20H

    .ENDPROLOG

    CALL TailcallStub

    ADD RSP, 20H

    POP R11
    POP R10
    POP R9
    POP R8
    POP RDX
    POP RCX
    POP RAX

    RET

TailcallNaked ENDP

_text ENDS

END