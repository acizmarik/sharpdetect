set(SOURCES
	"../lib/miniutf/miniutf.cpp"
	"../lib/miniutf/miniutf_collation.cpp"
	"CorProfilerBase.cpp" 
	"AssemblyDef.cpp" 
	"ModuleDef.cpp" 
	"Instruction.cpp"
	"MethodBodyHelpers.cpp"
	"OpCodes.cpp"
	"GarbageCollectionContext.cpp"
	"ObjectsTracker.cpp" 
	"Instrumentation.cpp"
	"WString.cpp"
	"PAL.cpp")

add_library (LibProfiler STATIC ${SOURCES} )
if (UNIX AND NOT APPLE)
	list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/inc")
	list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/pal/inc")
	list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/pal/inc/rt")
	list(APPEND INCLUDE_DIRECTORIES "../lib/coreclr/pal/prebuilt/inc")
endif()
list(APPEND INCLUDE_DIRECTORIES "../lib/miniutf")
target_include_directories(LibProfiler PUBLIC ${INCLUDE_DIRECTORIES})
if (UNIX AND NOT APPLE)
	# Fix compilation issues with miniutf on Linux
	target_compile_options(LibProfiler PRIVATE -include cstdint)
endif()
target_link_libraries(LibProfiler PRIVATE loguru::loguru)