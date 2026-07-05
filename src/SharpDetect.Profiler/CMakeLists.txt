cmake_minimum_required(VERSION 3.21 FATAL_ERROR)

project ("SharpDetect.Profiler" VERSION 2.1.4)

set(PROFILER_ROOT_DIR ${CMAKE_CURRENT_SOURCE_DIR})
set(PROFILER_LIB_DIR ${PROFILER_ROOT_DIR}/lib)

include(${PROFILER_ROOT_DIR}/cmake/compiler_options.cmake)
include(${PROFILER_ROOT_DIR}/cmake/msgpack.cmake)
include(${PROFILER_ROOT_DIR}/cmake/loguru.cmake)

add_subdirectory("LibIPC")
add_subdirectory("LibProfilerCore")
add_subdirectory("LibMetadata")
add_subdirectory("LibIL")
add_subdirectory("LibDescriptors")
add_subdirectory("SharpDetect.Concurrency.Profiler")