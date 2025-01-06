set(SOURCES
	"../lib/miniutf/miniutf.cpp"
	"../lib/miniutf/miniutf_collation.cpp"
	"CorProfilerBase.cpp" 
	"AssemblyDef.cpp" 
	"ModuleDef.cpp" 
	"Instruction.cpp"
	"MethodBodyHelpers.cpp"
	"OpCodes.cpp"
	"GarbageCollectionContext.cpp"
	"ObjectsTracker.cpp" 
	"Instrumentation.cpp"
	"WString.cpp"
	"PAL.cpp")

set(HEADERS
	"../lib/miniutf/miniutfdata.h"
	"../lib/miniutf/miniutf.hpp"
	"../lib/miniutf/miniutf_collation.hpp"
	"CorProfilerBase.h" 
	"ClassFactory.h" 
	"AssemblyDef.h" 
	"AssemblyRef.h" 
	"ModuleDef.h" 
	"FlowControl.h" 
	"Instruction.h" 
	"OpCode.h"
	"Code.h" 
	"OperandType.h" 
	"OpCodeType.h" 
	"StackBehaviour.h" 
	"Operand.h" 
	"MethodBodyHelpers.h" 
	"OpCodes.h" 
	"GarbageCollectionContext.h" 
	"TrackedObjectId.h" 
	"ObjectsTracker.h"
	"Instrumentation.h"
	"ComPtr.h"
	"WString.h"
	"PAL.h")

add_library (LibProfiler STATIC ${SOURCES} ${HEADERS})
target_link_libraries(LibProfiler PRIVATE loguru::loguru)