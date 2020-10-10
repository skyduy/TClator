#ifndef __MODULES__TRANSLATOR_H__
#define __MODULES__TRANSLATOR_H__

#include "base.h"
#include "cache.h"

class Translator : public Base {
public:
    Translator() : SPLIT_TOKEN(1, 1), cache(1024) {}

    std::string query(std::string src);

private:
    const std::string SPLIT_TOKEN;
    LRUCache<std::string, std::string> cache;
};

#endif // __MODULES__TRANSLATOR_H__
