if (WIN32)
    set(ASM_SRC asmHelpers.asm)
elseif (UNIX AND NOT APPLE)
    set(ASM_SRC asmHelpers.S)
endif()

set(SOURCES
    "CorProfiler.cpp"
    "dllmain.cpp"
    ${ASM_SRC} "MethodDescriptor.cpp" "MethodRewritingDescriptor.cpp" "MethodSignatureDescriptor.cpp")

set(HEADERS	
    "CorProfiler.h"
    "MethodDescriptor.h"
    "MethodRewritingDescriptor.h"
    "MethodSignatureDescriptor.h"
    "CapturedValueDescriptor.h"
    "CapturedArgumentDescriptor.h"
    "RecordedEventType.h"
    "HashingUtils.h")

add_library (SharpDetect.Profiler SHARED ${SOURCES} ${HEADERS} "Profiler.def" "CapturedValueFlags.h")
target_link_libraries (SharpDetect.Profiler PRIVATE LibProfiler LibIPC)