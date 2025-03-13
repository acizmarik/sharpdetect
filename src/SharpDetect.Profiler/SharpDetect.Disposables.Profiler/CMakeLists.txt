set(SOURCES
    "CorProfiler.cpp"
    "Configuration.cpp"
    "ConfigurationAdditionalData.cpp"
    "TypeDescriptor.cpp"
    "MethodDescriptor.cpp"
    "dllmain.cpp")

add_library (SharpDetect.Disposables.Profiler SHARED ${SOURCES} "Profiler.def")
target_link_libraries (SharpDetect.Disposables.Profiler PRIVATE LibProfiler LibIPC)