#ifndef __MODULES__TCLATOR_H__
#define __MODULES__TCLATOR_H__

#include <string>

__declspec(dllexport) std::string _calculate(std::string expression);

__declspec(dllexport) std::string _translate(std::string src);

#define Interface_Str2Str(FUNC_NAME)                                                 \
    extern "C" __declspec(dllexport) void __stdcall FUNC_NAME(char* dst, int strlen, \
                                                              const char* src) {     \
        std::string result = _##FUNC_NAME(src);                                      \
        result = result.substr(0, strlen);                                           \
        std::copy(result.begin(), result.end(), dst);                                \
        dst[std::min(strlen - 1, (int)result.size())] = 0;                           \
    }

Interface_Str2Str(calculate);
Interface_Str2Str(translate);

#endif // !__MODULES__TCLATOR_H__
