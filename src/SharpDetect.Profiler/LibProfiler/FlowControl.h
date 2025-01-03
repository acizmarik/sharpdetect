// dnlib: See LICENSE.dnlib.txt for more info

#pragma once

namespace LibProfiler
{
    enum class FlowControl
    {
        /// <summary/>
        Branch,
        /// <summary/>
        Break,
        /// <summary/>
        Call,
        /// <summary/>
        Cond_Branch,
        /// <summary/>
        Meta,
        /// <summary/>
        Next,
        /// <summary/>
        Phi,
        /// <summary/>
        Return,
        /// <summary/>
        Throw,
    };
}