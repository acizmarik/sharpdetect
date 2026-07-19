find_package(Threads REQUIRED)

set(SOURCES
	"TestMain.cpp"
	"EventLaneTests.cpp"
	"EventDispatcherTests.cpp"
	"${CMAKE_CURRENT_SOURCE_DIR}/../LibIPC/EventDispatcher.cpp"
	"${CMAKE_CURRENT_SOURCE_DIR}/../LibIPC/LaneRegistry.cpp"
	"${CMAKE_CURRENT_SOURCE_DIR}/../LibIPC/OverflowBuffer.cpp")

add_executable(LibIPC.Tests ${SOURCES})

set(INCLUDE_DIRECTORIES
	"${CMAKE_CURRENT_SOURCE_DIR}/../LibIPC"
	"${PROFILER_LIB_DIR}/doctest/doctest")
if (UNIX AND NOT APPLE)
	list(APPEND INCLUDE_DIRECTORIES
		"${PROFILER_LIB_DIR}/coreclr/inc"
		"${PROFILER_LIB_DIR}/coreclr/pal/inc"
		"${PROFILER_LIB_DIR}/coreclr/pal/inc/rt"
		"${PROFILER_LIB_DIR}/coreclr/pal/prebuilt/inc")
elseif (WIN32)
	list(APPEND INCLUDE_DIRECTORIES "${PROFILER_LIB_DIR}/coreclr/pal/prebuilt/inc")
	target_compile_definitions(LibIPC.Tests PRIVATE WIN32_LEAN_AND_MEAN NOMINMAX)
endif()

target_include_directories(LibIPC.Tests PRIVATE ${INCLUDE_DIRECTORIES})
target_link_libraries(LibIPC.Tests PRIVATE loguru Threads::Threads)
apply_profiler_compile_options(LibIPC.Tests)

add_test(NAME LibIPC.Tests COMMAND LibIPC.Tests)
