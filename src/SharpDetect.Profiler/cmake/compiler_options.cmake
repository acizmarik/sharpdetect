set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED TRUE)
set(CMAKE_CXX_EXTENSIONS OFF)

if (NOT (CMAKE_SIZEOF_VOID_P EQUAL 8))
    message(FATAL_ERROR "Unsupported architecture. Expected 64 bit.")
endif()

function(apply_profiler_compile_options target_name)
    if (UNIX AND NOT APPLE)
        target_compile_options(${target_name} PRIVATE
            -g
            -fPIC
            -fms-extensions
            -Wno-pragma-pack)
        target_compile_definitions(${target_name} PRIVATE
            HOST_AMD64
            HOST_64BIT
            PLATFORM_UNIX
            PAL_STDCPP_COMPAT)
    endif()
endfunction()

if (WIN32)
    message("Windows x64 ${CMAKE_BUILD_TYPE} build")
    enable_language(ASM_MASM)
elseif (UNIX AND NOT APPLE)
    message("Linux x64 ${CMAKE_BUILD_TYPE} build")
    enable_language(ASM)
    set(CMAKE_STATIC_LIBRARY_PREFIX "")
    set(CMAKE_SHARED_LIBRARY_PREFIX "")
else()
    message(FATAL_ERROR "Unsupported platform. Expected Windows or Linux.")
endif()