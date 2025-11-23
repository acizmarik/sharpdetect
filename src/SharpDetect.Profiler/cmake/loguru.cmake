add_subdirectory("${PROFILER_LIB_DIR}/loguru")

if(TARGET loguru)
    set_target_properties(loguru PROPERTIES POSITION_INDEPENDENT_CODE ON)
endif()

