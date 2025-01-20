// dnlib: See LICENSE.dnlib.txt for more info

#include "../lib/optional/include/tl/optional.hpp"


#include "OpCodes.h"

namespace LibProfiler
{
	std::array<tl::optional<LibProfiler::OpCode>, 256> OpCodes::OneByteOpCodes;
	std::array<tl::optional<LibProfiler::OpCode>, 256> OpCodes::TwoByteOpCodes;
}