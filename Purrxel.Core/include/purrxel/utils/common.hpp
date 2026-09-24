#ifndef UTILS_COMMON_HPP
#define UTILS_COMMON_HPP

#include "pch/std.hpp"

#define HEXA_UTILS_NAMESPACE Purrxel

namespace HEXA_UTILS_NAMESPACE
{
    extern bool UtilsEnableAsserts;
}

#define HEXA_UTILS_ASSERT(expr, message) \
    do { \
        bool _result = (expr);  \
        if (!_result && ::HEXA_UTILS_NAMESPACE::UtilsEnableAsserts) { \
            fprintf(stderr, "ASSERTION FAILED: %s\n", message); \
            assert(false && message); \
        } \
    } while (0);

#endif