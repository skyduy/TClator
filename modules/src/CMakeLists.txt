find_path(CPP_HTTPLIB_INCLUDE_DIRS "httplib.h")
find_package(nlohmann_json CONFIG REQUIRED)
find_package(OpenSSL REQUIRED)

# calculator
add_library(calculator "calculator.cpp")
target_include_directories(calculator PUBLIC ${PROJECT_SOURCE_DIR}/modules/src)

# translator
add_library(translator "translator.cpp")
target_include_directories(translator PUBLIC ${PROJECT_SOURCE_DIR}/modules/src PRIVATE ${CPP_HTTPLIB_INCLUDE_DIRS})
target_link_libraries(translator PRIVATE nlohmann_json nlohmann_json::nlohmann_json OpenSSL::SSL OpenSSL::Crypto)

# modules lib
add_library(modules SHARED "port.cpp")
target_include_directories(modules PUBLIC ${PROJECT_SOURCE_DIR}/modules/src)
target_link_libraries(modules PRIVATE calculator translator)
