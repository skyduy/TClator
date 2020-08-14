#ifndef __MODULES__CACULATOR_H__
#define __MODULES__CACULATOR_H__
#include <string>

extern "C" {

__declspec(dllexport) void __stdcall calculate(const char* expression, char* answer, int strlen) {
    std::string result(expression);

    result = result.substr(0, strlen);
    std::copy(result.begin(), result.end(), answer);
    answer[std::min(strlen - 1, (int)result.size())] = 0;
}

__declspec(dllexport) void __stdcall translate(const char* src, char* dst, int strlen) {
    std::string result(src);
    std::reverse(result.begin(), result.end());

    result = result.substr(0, strlen);
    std::copy(result.begin(), result.end(), dst);
    dst[std::min(strlen - 1, (int)result.size())] = 0;
}
}

#endif // !__MODULES__CACULATOR_H__
