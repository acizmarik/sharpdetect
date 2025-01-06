// dnlib: See LICENSE.dnlib.txt for more info

#pragma once

#include "cor.h"

namespace LibProfiler
{
    union Operand
    {
        INT8 Arg8;
        INT16 Arg16;
        INT32 Arg32;
        INT64 Arg64;
        FLOAT Single;
        DOUBLE Real;
    };
}