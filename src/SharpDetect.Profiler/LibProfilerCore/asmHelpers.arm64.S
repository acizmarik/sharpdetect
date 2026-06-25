# Copyright (c) .NET Foundation and contributors. All rights reserved.

.section .text

.globl EnterNaked
.globl LeaveNaked
.globl TailcallNaked

EnterNaked:

    stp x0, x1, [sp, #-288]!
    stp x2, x3, [sp, #16]
    stp x4, x5, [sp, #32]
    stp x6, x7, [sp, #48]
    stp x8, x9, [sp, #64]
    stp x10, x11, [sp, #80]
    stp x12, x13, [sp, #96]
    stp x14, x15, [sp, #112]
    stp x16, x17, [sp, #128]
    stp x18, x30, [sp, #144]
    stp q0, q1, [sp, #160]
    stp q2, q3, [sp, #192]
    stp q4, q5, [sp, #224]
    stp q6, q7, [sp, #256]

    bl EnterStub

    ldp q6, q7, [sp, #256]
    ldp q4, q5, [sp, #224]
    ldp q2, q3, [sp, #192]
    ldp q0, q1, [sp, #160]
    ldp x18, x30, [sp, #144]
    ldp x16, x17, [sp, #128]
    ldp x14, x15, [sp, #112]
    ldp x12, x13, [sp, #96]
    ldp x10, x11, [sp, #80]
    ldp x8, x9, [sp, #64]
    ldp x6, x7, [sp, #48]
    ldp x4, x5, [sp, #32]
    ldp x2, x3, [sp, #16]
    ldp x0, x1, [sp], #288
    ret

LeaveNaked:

    stp x0, x1, [sp, #-288]!
    stp x2, x3, [sp, #16]
    stp x4, x5, [sp, #32]
    stp x6, x7, [sp, #48]
    stp x8, x9, [sp, #64]
    stp x10, x11, [sp, #80]
    stp x12, x13, [sp, #96]
    stp x14, x15, [sp, #112]
    stp x16, x17, [sp, #128]
    stp x18, x30, [sp, #144]
    stp q0, q1, [sp, #160]
    stp q2, q3, [sp, #192]
    stp q4, q5, [sp, #224]
    stp q6, q7, [sp, #256]

    bl LeaveStub

    ldp q6, q7, [sp, #256]
    ldp q4, q5, [sp, #224]
    ldp q2, q3, [sp, #192]
    ldp q0, q1, [sp, #160]
    ldp x18, x30, [sp, #144]
    ldp x16, x17, [sp, #128]
    ldp x14, x15, [sp, #112]
    ldp x12, x13, [sp, #96]
    ldp x10, x11, [sp, #80]
    ldp x8, x9, [sp, #64]
    ldp x6, x7, [sp, #48]
    ldp x4, x5, [sp, #32]
    ldp x2, x3, [sp, #16]
    ldp x0, x1, [sp], #288
    ret

TailcallNaked:

    stp x0, x1, [sp, #-288]!
    stp x2, x3, [sp, #16]
    stp x4, x5, [sp, #32]
    stp x6, x7, [sp, #48]
    stp x8, x9, [sp, #64]
    stp x10, x11, [sp, #80]
    stp x12, x13, [sp, #96]
    stp x14, x15, [sp, #112]
    stp x16, x17, [sp, #128]
    stp x18, x30, [sp, #144]
    stp q0, q1, [sp, #160]
    stp q2, q3, [sp, #192]
    stp q4, q5, [sp, #224]
    stp q6, q7, [sp, #256]

    bl TailcallStub

    ldp q6, q7, [sp, #256]
    ldp q4, q5, [sp, #224]
    ldp q2, q3, [sp, #192]
    ldp q0, q1, [sp, #160]
    ldp x18, x30, [sp, #144]
    ldp x16, x17, [sp, #128]
    ldp x14, x15, [sp, #112]
    ldp x12, x13, [sp, #96]
    ldp x10, x11, [sp, #80]
    ldp x8, x9, [sp, #64]
    ldp x6, x7, [sp, #48]
    ldp x4, x5, [sp, #32]
    ldp x2, x3, [sp, #16]
    ldp x0, x1, [sp], #288
    ret

.section .note.GNU-stack,"",@progbits
