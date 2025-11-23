set(SOURCES
    "ArgumentTypeDescriptor.cpp"
    "CorProfiler.cpp"
    "Configuration.cpp"
    "dllmain.cpp"
    "MethodDescriptor.cpp"
    "MethodRewritingDescriptor.cpp"
    "MethodSignatureDescriptor.cpp")

add_library (SharpDetect.Concurrency.Profiler SHARED ${SOURCES} "Profiler.def")
target_link_libraries (SharpDetect.Concurrency.Profiler PRIVATE LibProfiler LibIPC)
apply_profiler_compile_options(SharpDetect.Concurrency.Profiler)
if(UNIX)
    target_link_options(SharpDetect.Concurrency.Profiler PRIVATE "LINKER:-z,noexecstack")
endif()