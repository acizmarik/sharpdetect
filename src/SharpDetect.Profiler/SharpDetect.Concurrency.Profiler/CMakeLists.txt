set(SOURCES
    "CorProfiler.cpp"
    "MetadataStore.cpp"
    "MethodDescriptorRegistry.cpp"
    "dllmain.cpp")

add_library (SharpDetect.Concurrency.Profiler SHARED ${SOURCES} "Profiler.def")
target_link_libraries (SharpDetect.Concurrency.Profiler PRIVATE LibProfilerCore LibMetadata LibIL LibDescriptors LibIPC)
apply_profiler_compile_options(SharpDetect.Concurrency.Profiler)
if(UNIX)
    target_link_options(SharpDetect.Concurrency.Profiler PRIVATE "LINKER:-z,noexecstack")
endif()