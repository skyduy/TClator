find_path(CPP_HTTPLIB_INCLUDE_DIRS "httplib.h")
find_package(nlohmann_json CONFIG REQUIRED)
find_package(OpenSSL REQUIRED)

# TCLATOR lib
add_library(tclator SHARED "tclator.cpp")
target_include_directories(tclator PUBLIC ${PROJECT_SOURCE_DIR}/modules/src PRIVATE ${CPP_HTTPLIB_INCLUDE_DIRS})
target_link_libraries(tclator PRIVATE nlohmann_json nlohmann_json::nlohmann_json OpenSSL::SSL OpenSSL::Crypto)
install (TARGETS tclator)
