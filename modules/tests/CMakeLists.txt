find_package(GTest CONFIG REQUIRED)
message( ${CMAKE_BINARY_DIR})

add_executable(calculator_test "calculator_test.cpp")
target_link_libraries(calculator_test GTest::gtest_main calculator)
add_test(test_all calculator_test)

add_executable(translator_test "translator_test.cpp")
target_link_libraries(translator_test GTest::gtest_main translator)
add_test(test_all translator_test)
