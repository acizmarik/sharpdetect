set(SOURCES
    "ArgumentCapture.cpp"
    "CorProfiler.cpp"
    "MetadataStore.cpp"
    "MethodDescriptorRegistry.cpp"
    "RewriteRegistry.cpp"
    "TypeInjector.cpp"
    "dllmain.cpp")

add_library (SharpDetect.Concurrency.Profiler SHARED ${SOURCES} "Profiler.def")
target_link_libraries (SharpDetect.Concurrency.Profiler PRIVATE LibProfilerCore LibMetadata LibIL LibDescriptors LibIPC)
apply_profiler_compile_options(SharpDetect.Concurrency.Profiler)
if(UNIX)
    target_link_options(SharpDetect.Concurrency.Profiler PRIVATE
        "LINKER:-z,noexecstack"
        "LINKER:--version-script=${CMAKE_CURRENT_SOURCE_DIR}/Profiler.map"
        -static-libstdc++)
endif()