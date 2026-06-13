set(SOURCES
    "ILRewriter.cpp"
    "Instrumentation.cpp"
    "MetadataAnalysis.cpp")

add_library (LibIL STATIC ${SOURCES})
target_include_directories(LibIL PUBLIC "${CMAKE_CURRENT_SOURCE_DIR}")
target_link_libraries(LibIL PUBLIC LibMetadata LibIPC)
apply_profiler_compile_options(LibIL)

if (WIN32)
    target_compile_definitions(LibIL PRIVATE WIN32_LEAN_AND_MEAN)
endif()
