#ifndef __MODULES__BASE_H__
#define __MODULES__BASE_H__
#include <string>

class Base {
public:
    virtual std::string query(std::string question) = 0;
};

#endif // __MODULES__BASE_H__
