set(SOURCES
    "ArgumentTypeDescriptor.cpp"
    "Configuration.cpp"
    "MethodDescriptor.cpp"
    "MethodInjectionDescriptor.cpp"
    "MethodRewritingDescriptor.cpp"
    "MethodSignatureDescriptor.cpp"
    "MethodVersionDescriptor.cpp"
    "TypeInjectionDescriptor.cpp")

add_library (LibDescriptors STATIC ${SOURCES})
target_include_directories(LibDescriptors PUBLIC "${CMAKE_CURRENT_SOURCE_DIR}")
target_link_libraries(LibDescriptors PUBLIC LibProfilerCore LibIPC)
apply_profiler_compile_options(LibDescriptors)

if (WIN32)
    target_compile_definitions(LibDescriptors PRIVATE WIN32_LEAN_AND_MEAN)
endif()
