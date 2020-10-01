cmake_minimum_required(VERSION 3.5)
project(toys-modules)

SET(CMAKE_RUNTIME_OUTPUT_DIRECTORY_DEBUG ${PROJECT_SOURCE_DIR}/runner/bin/x86/Debug)
SET(CMAKE_RUNTIME_OUTPUT_DIRECTORY_RELEASE ${PROJECT_SOURCE_DIR}/runner/bin/x86/Release)
add_subdirectory(modules/src)

enable_testing()
add_subdirectory(modules/tests)
