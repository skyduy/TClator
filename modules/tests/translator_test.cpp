#include "translator.h"

#include <gtest/gtest.h>

#include <iostream>

Translator trans;

TEST(Modules, test_translate) {
    EXPECT_EQ(trans.query("test")[0], '[');
}
