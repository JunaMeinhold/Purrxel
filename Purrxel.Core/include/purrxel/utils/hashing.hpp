#ifndef HASHING_HPP
#define HASHING_HPP

#include "pch/std.hpp"

#define XXH_INLINE_ALL
#include "xxhash.h"

struct Fnv1Hash64
{
	uint64_t hash = 14695981039346656037U;

	void Combine(uint64_t value)
	{
		hash ^= value;
		hash *= 1099511628211U;
	}

	template<typename T>
	void Combine(T value)
	{
		Combine(static_cast<uint64_t>(value));
	}
};

struct Murmur3Hash
{
	uint32_t hash;
	size_t length;

	explicit Murmur3Hash(uint32_t seed = 0) : hash(seed) {}

	static inline uint32_t murmur_32_scramble(uint32_t k)
	{
		k *= 0xcc9e2d51;
		k = (k << 15) | (k >> 17);
		k *= 0x1b873593;
		return k;
	}

	void Combine(uint32_t value)
	{
		uint32_t k = murmur_32_scramble(value);
		hash ^= k;
		hash = (hash << 13) | (hash >> 19);
		hash = hash * 5 + 0xe6546b64;
		length += 4;
	}

	void Combine(uint64_t value)
	{
		Combine(static_cast<uint32_t>(value & 0xFFFFFFFF));
		Combine(static_cast<uint32_t>(value >> 32));
	}

	uint32_t Finalize()
	{
		hash ^= static_cast<uint32_t>(length);
		hash ^= hash >> 16;
		hash *= 0x85ebca6b;
		hash ^= hash >> 13;
		hash *= 0xc2b2ae35;
		hash ^= hash >> 16;
		return hash;
	}
};

template<typename Derived>
class HashAlgorithm
{
	void CombineInternal(const void* data, size_t length)
	{
		static_cast<Derived*>(this)->CombineInternal(data, length);
	}
public:
	template<typename T>
	void Combine(const T* value, size_t count) { CombineInternal(value, count * sizeof(T)); }

	template<>
	void Combine(const void* data, size_t length) { CombineInternal(data, length); }

	template<typename T>
	void Combine(const T& value) { CombineInternal(&value, sizeof(T)); }
};

class XXHash3_64 : public HashAlgorithm<XXHash3_64>
{
	XXH3_state_t* state;
	friend class HashAlgorithm<XXHash3_64>;

	void CombineInternal(const void* data, size_t length)
	{
		XXH3_64bits_update(state, data, length);
	}

public:

	XXHash3_64()
	{
		state = XXH3_createState();
		XXH3_64bits_reset(state);
	}

	XXHash3_64(const XXHash3_64&) = delete;
	XXHash3_64& operator=(const XXHash3_64&) = delete;

	~XXHash3_64()
	{
		XXH3_freeState(state);
	}

	uint64_t Finalize() const
	{
		return XXH3_64bits_digest(state);
	}
};

#endif