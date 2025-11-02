set(SOURCES
	"Client.cpp"
	"Messages.cpp")

add_library (LibIPC STATIC ${SOURCES})
if (UNIX AND NOT APPLE)
	list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/inc")
	list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/pal/inc")
	list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/pal/inc/rt")
	list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/pal/prebuilt/inc")
elseif (WIN32)
	add_definitions(-DWIN32_LEAN_AND_MEAN)
	set(INCLUDE_DIRECTORIES "../lib/coreclr/pal/prebuilt/inc" ${INCLUDE_DIRECTORIES})
endif()

target_include_directories(LibIPC PUBLIC ${INCLUDE_DIRECTORIES})
target_link_libraries(LibIPC PRIVATE msgpack-cxx)