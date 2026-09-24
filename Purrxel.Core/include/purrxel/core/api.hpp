#pragma once

#if defined(_WIN32)
#if defined(PURRXEL_CORE_EXPORTS)
#define PURRXEL_CORE_API __declspec(dllexport)
#else
#define PURRXEL_CORE_API __declspec(dllimport)
#endif
#else
#define PURRXEL_CORE_API __attribute__((visibility("default")))
#endif
