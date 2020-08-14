#include "tclator.h"

std::string _calculate(const std::string& expression) {
    std::string res = expression;
    std::reverse(res.begin(), res.end());
    return res;
}

std::string _translate(const std::string& src) {
    std::string res = src;
    std::reverse(res.begin(), res.end());
    return res;
}
