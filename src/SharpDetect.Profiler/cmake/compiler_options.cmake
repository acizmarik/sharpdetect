set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED TRUE)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_BUILD_TYPE Debug)

if (NOT (CMAKE_SIZEOF_VOID_P EQUAL 8))
    message(FATAL_ERROR "Unsupported architecture. Expected 64 bit." )
endif()

if (WIN32)
    enable_language(ASM_MASM)
elseif (UNIX AND NOT APPLE)
    enable_language(ASM)
    add_compile_options(
        -DPAL_STDCPP_COMPAT 
        -DPLATFORM_UNIX 
        -DUNICODE 
        -DBIT64 
        -DHOST_64BIT 
        -DAMD64)
else()
    message(FATAL_ERROR "Unsupported platform. Expected Windows or Linux." )
endif()




