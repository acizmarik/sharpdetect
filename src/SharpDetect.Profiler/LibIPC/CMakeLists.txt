set(SOURCES
	"Client.cpp"
	"Messages.cpp")

add_library (LibIPC STATIC ${SOURCES})

set(INCLUDE_DIRECTORIES "")
if (UNIX AND NOT APPLE)
	list(APPEND INCLUDE_DIRECTORIES
		"${PROFILER_LIB_DIR}/coreclr/inc"
		"${PROFILER_LIB_DIR}/coreclr/pal/inc"
		"${PROFILER_LIB_DIR}/coreclr/pal/inc/rt"
		"${PROFILER_LIB_DIR}/coreclr/pal/prebuilt/inc")
elseif (WIN32)
	list(APPEND INCLUDE_DIRECTORIES "${PROFILER_LIB_DIR}/coreclr/pal/prebuilt/inc")
	target_compile_definitions(LibIPC PRIVATE WIN32_LEAN_AND_MEAN)
endif()

target_include_directories(LibIPC PUBLIC ${INCLUDE_DIRECTORIES})
target_link_libraries(LibIPC PRIVATE msgpack-cxx)
apply_profiler_compile_options(LibIPC)
