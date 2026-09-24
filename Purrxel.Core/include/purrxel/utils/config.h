#ifndef HXSL_UTILS_CONFIG_H
#define HXSL_UTILS_CONFIG_H

#ifdef HXSL_X86_64

#ifdef HXSL_AVX2
#define HXSL_SIMD_ALIGNMENT 32
#else
#define HXSL_SIMD_ALIGNMENT 16
#define HXSL_SSE2 1
#endif

#elif defined(HXSL_AARCH64) // ARM64 (64-bit ARM)
#define HXSL_SIMD_ALIGNMENT 16

#elif defined(HXSL_ARM) // ARM 32-bit
#define HXSL_SIMD_ALIGNMENT 16

#else
#define HXSL_SIMD_ALIGNMENT 8

#endif

#if __cplusplus
namespace SIMD
{
	constexpr size_t alignment = HXSL_SIMD_ALIGNMENT;
}
#endif // __cplusplus

#endif