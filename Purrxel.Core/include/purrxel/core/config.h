#ifndef PURRXEL_CORE_CONFIG_H
#define PURRXEL_CORE_CONFIG_H

#if defined _WIN32 || defined __CYGWIN__
#ifdef PURRXEL_NO_EXPORT
#define API
#else
#define API __declspec(dllexport)
#endif
#else
#ifdef __GNUC__
#define API __attribute__((__visibility__("default")))
#else
#define API
#endif
#endif

#if defined __cplusplus
#define EXTERN extern "C"
#else
#include <stdarg.h>
#include <stdbool.h>
#define EXTERN extern
#endif

#define PURRXEL_API EXTERN API

#ifndef PURRXEL_ENABLE_CAPI
#ifdef __cplusplus
#define PURRXEL_ENABLE_CAPI 0
#else
#define PURRXEL_ENABLE_CAPI 1
#endif
#endif

#ifdef __cplusplus
#define C_API_BEGIN extern "C" {
#define C_API_END   }
#include <cstddef>
#include <cstdint>
#else
#define C_API_BEGIN
#define C_API_END
#include <stdint.h>
#include <stddef.h>
#endif

extern bool EnableAsserts;
extern bool EnableErrorOutput;

#define PURRXEL_ASSERT(expr, message) \
    do { \
        bool _result = (expr);  \
        if (!_result && EnableAsserts) { \
            fprintf(stderr, "ASSERTION FAILED: %s\n", message); \
            assert(false && message); \
        } \
    } while (0);

#ifndef MAX_LOG_LENGTH
#define MAX_LOG_LENGTH 512
#endif

#endif
