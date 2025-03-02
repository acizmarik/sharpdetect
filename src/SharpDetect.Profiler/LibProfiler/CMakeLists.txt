if (WIN32)
    set(ASM_SRC asmHelpers.asm)
elseif (UNIX AND NOT APPLE)
    set(ASM_SRC asmHelpers.S)
endif()
set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -z noexecstack")

set(SOURCES
    ${ASM_SRC}
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

if (UNIX AND NOT APPLE)
    list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/inc")
    list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/pal/inc")
    list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/pal/inc/rt")
    list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/pal/prebuilt/inc")
    list(APPEND SOURCES "../lib/coreclr/pal/prebuilt/idl/corprof_i.cpp")
endif()
list(APPEND INCLUDE_DIRECTORIES "../lib/miniutf")

add_library (LibProfiler STATIC ${SOURCES})
target_include_directories(LibProfiler PUBLIC ${INCLUDE_DIRECTORIES})
if (UNIX AND NOT APPLE)
    # Fix compilation issues with miniutf on Linux
    target_compile_options(LibProfiler PRIVATE -include cstdint)
endif()
target_link_libraries(LibProfiler PRIVATE loguru::loguru)