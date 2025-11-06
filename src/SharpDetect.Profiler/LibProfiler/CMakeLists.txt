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
    "StackWalker.cpp"
    "WString.cpp"
    "PAL.cpp")

if (UNIX AND NOT APPLE)
    list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/inc")
    list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/pal/inc")
    list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/pal/inc/rt")
    list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/pal/prebuilt/inc")
    list(APPEND SOURCES "../lib/coreclr/pal/prebuilt/idl/corprof_i.cpp")
elseif (WIN32)
    add_definitions(-DWIN32_LEAN_AND_MEAN)
    set(INCLUDE_DIRECTORIES "../lib/coreclr/pal/prebuilt/inc" ${INCLUDE_DIRECTORIES})
endif()

list(APPEND INCLUDE_DIRECTORIES "../lib/miniutf")

add_library (LibProfiler STATIC ${SOURCES})
target_include_directories(LibProfiler PUBLIC ${INCLUDE_DIRECTORIES})
if (UNIX AND NOT APPLE)
    # Fix compilation issues with miniutf on Linux
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -include cstdint")
endif()
target_link_libraries(LibProfiler PRIVATE loguru::loguru)