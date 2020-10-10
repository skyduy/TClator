#include "calculator.h"

#include <gtest/gtest.h>

#include <iostream>
#include <vector>

Calculator calcu;

TEST(Modules, test_calculate) {
    std::vector<std::string> expressions{
            "1+1",         "1 + 1",         "1+ 1",  "1-1", "1-2", "1-3",
            "1*1",         "0*1",           "99*99", "1/2", "1/3", "1/4",

            "1+2-3*4/5^6", "(1+2-3*4/5)^6",
    };
    std::vector<std::string> answers{
            "2",        "2",        "2",    "0",   "-1",       "-2",
            "1",        "0",        "9801", "0.5", "0.333333", "0.25",

            "2.999232", "0.046656",
    };

    for (size_t i = 0; i < expressions.size(); i++) {
        EXPECT_EQ(calcu.query(expressions[i]), answers[i]);
    }
}
