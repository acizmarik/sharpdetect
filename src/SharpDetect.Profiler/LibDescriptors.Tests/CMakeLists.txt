set(SOURCES
	"TestMain.cpp"
	"MethodRewritingDescriptorTests.cpp"
	"${CMAKE_CURRENT_SOURCE_DIR}/../LibDescriptors/MethodRewritingDescriptor.cpp")

add_executable(LibDescriptors.Tests ${SOURCES})

set(INCLUDE_DIRECTORIES
	"${CMAKE_CURRENT_SOURCE_DIR}/../LibDescriptors"
	"${PROFILER_LIB_DIR}/doctest/doctest")
if (UNIX AND NOT APPLE)
	list(APPEND INCLUDE_DIRECTORIES
		"${PROFILER_LIB_DIR}/coreclr/inc"
		"${PROFILER_LIB_DIR}/coreclr/pal/inc"
		"${PROFILER_LIB_DIR}/coreclr/pal/inc/rt"
		"${PROFILER_LIB_DIR}/coreclr/pal/prebuilt/inc")
elseif (WIN32)
	list(APPEND INCLUDE_DIRECTORIES "${PROFILER_LIB_DIR}/coreclr/pal/prebuilt/inc")
	target_compile_definitions(LibDescriptors.Tests PRIVATE WIN32_LEAN_AND_MEAN NOMINMAX)
endif()

target_include_directories(LibDescriptors.Tests PRIVATE ${INCLUDE_DIRECTORIES})
apply_profiler_compile_options(LibDescriptors.Tests)

add_doctest_test(LibDescriptors.Tests)
