cmake_minimum_required(VERSION 3.21 FATAL_ERROR)

# Cake passes <Version> from src/Directory.Build.props
set(SHARPDETECT_VERSION "0.0.0" CACHE STRING "SharpDetect version, numeric part only")
project ("SharpDetect.Profiler" VERSION ${SHARPDETECT_VERSION})

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

option(SHARPDETECT_BUILD_TESTS "Build native unit tests" OFF)
if (SHARPDETECT_BUILD_TESTS)
	enable_testing()
	include(${PROFILER_ROOT_DIR}/cmake/doctest_test.cmake)
	add_subdirectory("LibIPC.Tests")
	add_subdirectory("LibProfilerCore.Tests")
	add_subdirectory("LibMetadata.Tests")
	add_subdirectory("LibDescriptors.Tests")

	add_custom_target(SharpDetect.NativeTests)
	add_dependencies(SharpDetect.NativeTests LibIPC.Tests LibProfilerCore.Tests LibMetadata.Tests LibDescriptors.Tests)
endif()
