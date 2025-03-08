set(SOURCES
    "CorProfiler.cpp"
    "dllmain.cpp")

add_library (SharpDetect.Disposables.Profiler SHARED ${SOURCES} "Profiler.def")
target_link_libraries (SharpDetect.Disposables.Profiler PRIVATE LibProfiler LibIPC)