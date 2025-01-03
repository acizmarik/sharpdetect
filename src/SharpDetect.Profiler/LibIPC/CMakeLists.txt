set(SOURCES
	"Client.cpp"
	"Messages.cpp")

set(HEADERS
	"Client.h"
	"Messages.h")

add_library (LibIPC STATIC ${SOURCES} ${HEADERS})
target_link_libraries(LibIPC PRIVATE msgpack-cxx)