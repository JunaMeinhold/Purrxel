//-----------------------------------------------------------------------------
// MurmurHash3 was written by Austin Appleby, and is placed in the public
// domain. The author hereby disclaims copyright to this source code.

#ifndef _MURMURHASH3_H_
#define _MURMURHASH3_H_

#include "types.hpp"

namespace HEXA_UTILS_NAMESPACE
{
	void MurmurHash3_x86_32(const void* key, size_t len, uint32_t seed, uint32_t& out);

	void MurmurHash3_x86_128(const void* key, size_t len, uint32_t seed, uint128_t& out);

	void MurmurHash3_x64_128(const void* key, size_t len, uint32_t seed, uint128_t& out);
}

//-----------------------------------------------------------------------------

#endif // _MURMURHASH3_H_
