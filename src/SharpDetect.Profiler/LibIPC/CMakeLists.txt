set(SOURCES
	"Client.cpp"
	"Messages.cpp")

add_library (LibIPC STATIC ${SOURCES})
if (UNIX AND NOT APPLE)
	list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/inc")
	list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/pal/inc")
	list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/pal/inc/rt")
	list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/pal/prebuilt/inc")
endif()
target_include_directories(LibIPC PUBLIC ${INCLUDE_DIRECTORIES})
target_link_libraries(LibIPC PRIVATE msgpack-cxx)