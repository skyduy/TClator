cmake_minimum_required(VERSION 3.5)
project(toys-modules)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
add_subdirectory(modules/src)

enable_testing()
add_subdirectory(modules/test)
