find_package(GTest CONFIG REQUIRED)
message( ${CMAKE_BINARY_DIR})
add_executable(tclator_test "tclator_test.cpp")
target_link_libraries(tclator_test GTest::gtest_main tclator)

add_test(test_all tclator_test)
