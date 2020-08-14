add_library(TCLATOR SHARED tclator.cpp)
add_library(modules::tclator ALIAS TCLATOR)

target_include_directories(TCLATOR PUBLIC ${PROJECT_SOURCE_DIR}/modules/src)

install (TARGETS TCLATOR)
