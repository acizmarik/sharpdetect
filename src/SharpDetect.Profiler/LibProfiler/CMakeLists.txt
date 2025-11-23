if (WIN32)
    set(ASM_SRC asmHelpers.asm)
elseif (UNIX AND NOT APPLE)
    set(ASM_SRC asmHelpers.S)
endif()

set(SOURCES
    ${ASM_SRC}
    "${PROFILER_LIB_DIR}/miniutf/miniutf.cpp"
    "${PROFILER_LIB_DIR}/miniutf/miniutf_collation.cpp"
    "CorProfilerBase.cpp"
    "AssemblyDef.cpp" 
    "ModuleDef.cpp" 
    "Instruction.cpp"
    "MethodBodyHelpers.cpp"
    "OpCodes.cpp"
    "GarbageCollectionContext.cpp"
    "ObjectsTracker.cpp" 
    "Instrumentation.cpp"
    "StackWalker.cpp"
    "WString.cpp"
    "PAL.cpp")

set(INCLUDE_DIRECTORIES "")

if (UNIX AND NOT APPLE)
    list(APPEND INCLUDE_DIRECTORIES
        "${PROFILER_LIB_DIR}/coreclr/inc"
        "${PROFILER_LIB_DIR}/coreclr/pal/inc"
        "${PROFILER_LIB_DIR}/coreclr/pal/inc/rt"
        "${PROFILER_LIB_DIR}/coreclr/pal/prebuilt/inc")
    list(APPEND SOURCES "${PROFILER_LIB_DIR}/coreclr/pal/prebuilt/idl/corprof_i.cpp")
elseif (WIN32)
    list(APPEND INCLUDE_DIRECTORIES "${PROFILER_LIB_DIR}/coreclr/pal/prebuilt/inc")
endif()

list(APPEND INCLUDE_DIRECTORIES "${PROFILER_LIB_DIR}/miniutf")

add_library (LibProfiler STATIC ${SOURCES})
target_include_directories(LibProfiler PUBLIC ${INCLUDE_DIRECTORIES})
apply_profiler_compile_options(LibProfiler)

if (UNIX AND NOT APPLE)
    # Fix compilation issues with miniutf on Linux
    target_compile_options(LibProfiler PRIVATE $<$<COMPILE_LANGUAGE:CXX>:-include cstdint>)
endif()

if (WIN32)
    target_compile_definitions(LibProfiler PRIVATE WIN32_LEAN_AND_MEAN)
endif()

target_link_libraries(LibProfiler PRIVATE loguru::loguru)