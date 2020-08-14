#include "tclator.h"

#include <gtest/gtest.h>

#include <iostream>

using std::cout;
using std::endl;

TEST(TClator, test_calculate) {
    std::string src = "1+11";
    cout << _calculate(src) << endl;
}

TEST(TClator, test_translate) {
    std::string src = "1+11";
    cout << _translate(src) << endl;
}
