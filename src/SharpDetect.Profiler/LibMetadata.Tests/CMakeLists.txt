set(SOURCES
	"TestMain.cpp"
	"SignatureUtilsTests.cpp"
	"${CMAKE_CURRENT_SOURCE_DIR}/../LibMetadata/SignatureUtils.cpp")

add_executable(LibMetadata.Tests ${SOURCES})

set(INCLUDE_DIRECTORIES
	"${CMAKE_CURRENT_SOURCE_DIR}/../LibMetadata"
	"${PROFILER_LIB_DIR}/doctest/doctest")
if (UNIX AND NOT APPLE)
	list(APPEND INCLUDE_DIRECTORIES
		"${PROFILER_LIB_DIR}/coreclr/inc"
		"${PROFILER_LIB_DIR}/coreclr/pal/inc"
		"${PROFILER_LIB_DIR}/coreclr/pal/inc/rt"
		"${PROFILER_LIB_DIR}/coreclr/pal/prebuilt/inc")
elseif (WIN32)
	list(APPEND INCLUDE_DIRECTORIES "${PROFILER_LIB_DIR}/coreclr/pal/prebuilt/inc")
	target_compile_definitions(LibMetadata.Tests PRIVATE WIN32_LEAN_AND_MEAN NOMINMAX)
endif()

target_include_directories(LibMetadata.Tests PRIVATE ${INCLUDE_DIRECTORIES})
apply_profiler_compile_options(LibMetadata.Tests)

add_doctest_test(LibMetadata.Tests)
