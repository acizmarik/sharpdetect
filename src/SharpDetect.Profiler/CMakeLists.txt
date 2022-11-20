cmake_minimum_required(VERSION 3.23)
project(SharpDetect.Profiler)

# Common compilation options
add_compile_options(-DBIT64 -DHOST_64BIT -DAMD64)
add_compile_options(-DPAL_STDCPP_COMPAT -DUNICODE)
set(CMAKE_CXX_STANDARD 20)

# Include files built by conan
include(${CMAKE_BINARY_DIR}/conanbuildinfo.cmake)
conan_basic_setup()

if (WIN32)
	message("Target platform: WINDOWS")
	# Enable assembler	
	enable_language(ASM_MASM)
	set(ASM_SRC "src/GlobalHooks.Windows.x64.asm")

	# Additional compilation options
	add_compile_options(-DWIN32)
endif(WIN32)

if (UNIX)
	message("Target platform: LINUX")
	# Enable assembler	
	enable_language(ASM)	
	set(ASM_SRC "src/GlobalHooks.Linux.x64.S")

	# Additional compilation options
	add_compile_options(-fPIC -fms-extensions -Wno-pragmas)
	add_compile_options(-DPLATFORM_UNIX -DUNIX)

	# Additional includes
	include_directories(
		"$ENV{CORECLR_SOURCE}/pal/inc/rt"
		"$ENV{CORECLR_SOURCE}/pal/inc"
		"$ENV{CORECLR_SOURCE}/pal/prebuilt/inc"
		"$ENV{CORECLR_SOURCE}/inc"
	)
endif(UNIX)

# Regenerate protocol buffer definitions
find_package(Protobuf REQUIRED)
protobuf_generate_cpp(PROTO_NTF_SRC PROTO_NTF_HDR "../SharpDetect.ProtocolBuffers/profiler_notifications.proto")
protobuf_generate_cpp(PROTO_REQ_SRC PROTO_REQ_HDR "../SharpDetect.ProtocolBuffers/profiler_requests.proto")

# Common includes
include_directories(
	"src"
	"build"
)

# Create a dynamically-linked library
add_library(${PROJECT_NAME} SHARED
	"src/ClrProfiler.def"
	"src/dllmain.cpp"

	"src/ClassFactory.cpp"
	"src/CorProfiler.cpp"
	"src/CorProfilerBase.cpp"
	"src/Client.cpp"
	"src/CorProfiler.cpp"
	"src/EasyLogging.cpp"
	"src/ILGeneration.cpp"
	"src/InstrumentationContext.cpp"
	"src/InstructionFactory.cpp"
	"src/MessageFactory.cpp"
	"src/ModuleMetadata.cpp"
	"src/TinyMethodUser.cpp"
	"src/wstring.cpp"
	"src/PAL.cpp"

	${PROTO_NTF_SRC}
	${PROTO_REQ_SRC}
	${ASM_SRC}
)

# Link external libraries
target_link_libraries(${PROJECT_NAME} ${CONAN_LIBS})
