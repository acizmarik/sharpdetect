set(SOURCES
	"TestMain.cpp"
	"ExceptionUnwindTrackerTests.cpp"
	"ThreadIncarnationsTests.cpp"
	"${CMAKE_CURRENT_SOURCE_DIR}/../LibProfilerCore/ExceptionUnwindTracker.cpp"
	"${CMAKE_CURRENT_SOURCE_DIR}/../LibProfilerCore/ThreadIncarnations.cpp")

add_executable(LibProfilerCore.Tests ${SOURCES})

set(INCLUDE_DIRECTORIES
	"${CMAKE_CURRENT_SOURCE_DIR}/../LibProfilerCore"
	"${PROFILER_LIB_DIR}/doctest/doctest")
if (UNIX AND NOT APPLE)
	list(APPEND INCLUDE_DIRECTORIES
		"${PROFILER_LIB_DIR}/coreclr/inc"
		"${PROFILER_LIB_DIR}/coreclr/pal/inc"
		"${PROFILER_LIB_DIR}/coreclr/pal/inc/rt"
		"${PROFILER_LIB_DIR}/coreclr/pal/prebuilt/inc")
elseif (WIN32)
	list(APPEND INCLUDE_DIRECTORIES
		"${PROFILER_LIB_DIR}/coreclr/inc"
		"${PROFILER_LIB_DIR}/coreclr/pal/prebuilt/inc")
	target_compile_definitions(LibProfilerCore.Tests PRIVATE WIN32_LEAN_AND_MEAN NOMINMAX)
endif()

target_include_directories(LibProfilerCore.Tests PRIVATE ${INCLUDE_DIRECTORIES})
apply_profiler_compile_options(LibProfilerCore.Tests)

add_doctest_test(LibProfilerCore.Tests)
