# LibMetadata — CLR metadata read/emit.
# Assembly/module metadata wrappers, signature parsing and wide-string helpers
# (miniutf). Builds on LibProfilerCore (PAL, COM helpers).

set(SOURCES
    "${PROFILER_LIB_DIR}/miniutf/miniutf.cpp"
    "${PROFILER_LIB_DIR}/miniutf/miniutf_collation.cpp"
    "AssemblyDef.cpp"
    "ModuleDef.cpp"
    "SignatureUtils.cpp"
    "TypeClassification.cpp"
    "WString.cpp")

add_library (LibMetadata STATIC ${SOURCES})
target_include_directories(LibMetadata
    PUBLIC "${CMAKE_CURRENT_SOURCE_DIR}"
    PRIVATE "${PROFILER_LIB_DIR}/miniutf")
target_link_libraries(LibMetadata PUBLIC LibProfilerCore)
apply_profiler_compile_options(LibMetadata)

if (UNIX AND NOT APPLE)
    # Fix compilation issues with miniutf on Linux
    target_compile_options(LibMetadata PRIVATE $<$<COMPILE_LANGUAGE:CXX>:-include cstdint>)
endif()

if (WIN32)
    target_compile_definitions(LibMetadata PRIVATE WIN32_LEAN_AND_MEAN)
endif()
