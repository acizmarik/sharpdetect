set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED TRUE)
set(CMAKE_CXX_EXTENSIONS OFF)

if (NOT (CMAKE_SIZEOF_VOID_P EQUAL 8))
    message(FATAL_ERROR "Unsupported architecture. Expected 64 bit.")
endif()

option(SHARPDETECT_KEEP_SYMBOLS "Keep symbols in Release builds" OFF)

function(apply_profiler_compile_options target_name)
    if (UNIX AND NOT APPLE)
        target_compile_options(${target_name} PRIVATE
            $<$<OR:$<NOT:$<CONFIG:Release>>,$<BOOL:${SHARPDETECT_KEEP_SYMBOLS}>>:-g>
            -fPIC
            -fms-extensions
            -Wno-pragma-pack)
        target_compile_definitions(${target_name} PRIVATE
            HOST_AMD64
            HOST_64BIT
            PLATFORM_UNIX
            PAL_STDCPP_COMPAT)
        if (NOT SHARPDETECT_KEEP_SYMBOLS)
            target_link_options(${target_name} PRIVATE $<$<CONFIG:Release>:LINKER:-s>)
        endif()
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