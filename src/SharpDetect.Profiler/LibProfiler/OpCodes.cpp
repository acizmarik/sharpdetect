// dnlib: See LICENSE.dnlib.txt for more info

#include "OpCodes.h"

namespace LibProfiler
{
	std::array<std::optional<LibProfiler::OpCode>, 256> OpCodes::OneByteOpCodes;
	std::array< std::optional<LibProfiler::OpCode>, 256> OpCodes::TwoByteOpCodes;
}