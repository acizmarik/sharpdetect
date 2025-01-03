cmake_minimum_required(VERSION 3.23 FATAL_ERROR)

project ("Profiler" VERSION 2.0.0)

include(${CMAKE_SOURCE_DIR}/cmake/compiler_options.cmake)
include(${CMAKE_SOURCE_DIR}/cmake/msgpack.cmake)
include(${CMAKE_SOURCE_DIR}/cmake/loguru.cmake)

add_subdirectory("LibIPC")
add_subdirectory("LibProfiler")
add_subdirectory("Profiler")