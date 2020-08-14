#ifndef __MODULES__CACULATOR_H__
#define __MODULES__CACULATOR_H__

#include <string>

std::string calculate(const std::string& expression);

// src: appKey,secretKey,text
std::string translate(const std::string& src);

#define Interface_Str2Str(FUNC_NAME)                                                 \
    extern "C" __declspec(dllexport) void __stdcall FUNC_NAME(char* dst, int strlen, \
                                                              const char* src) {     \
        std::string result = FUNC_NAME(src);                                         \
        result = result.substr(0, strlen);                                           \
        std::copy(result.begin(), result.end(), dst);                                \
        dst[std::min(strlen - 1, (int)result.size())] = 0;                           \
    }

Interface_Str2Str(calculate);
Interface_Str2Str(translate);

#endif // !__MODULES__CACULATOR_H__
