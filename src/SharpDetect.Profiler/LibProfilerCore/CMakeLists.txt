set(SOURCES
    "CorProfilerBase.cpp"
    "StackWalker.cpp"
    "GarbageCollectionContext.cpp"
    "ObjectsTracker.cpp"
    "PAL.cpp")

set(INCLUDE_DIRECTORIES "${CMAKE_CURRENT_SOURCE_DIR}")

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

add_library (LibProfilerCore STATIC ${SOURCES})
target_include_directories(LibProfilerCore PUBLIC ${INCLUDE_DIRECTORIES})
apply_profiler_compile_options(LibProfilerCore)

if (WIN32)
    target_compile_definitions(LibProfilerCore PRIVATE WIN32_LEAN_AND_MEAN)
endif()

# loguru is linked PUBLIC so the metadata / IL layers inherit it.
target_link_libraries(LibProfilerCore PUBLIC loguru::loguru)
