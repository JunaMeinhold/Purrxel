#ifndef SHA256_HPP_
#define SHA256_HPP_

#include "common.hpp"
#include "array.hpp"


namespace HEXA_UTILS_NAMESPACE
{
	using SHA256Hash = Array<uint8_t, 32>;

    struct SHA256Context
    {
        uint32_t hash[8];
        uint8_t buffer[64];
        size_t bufferLength;
        uint64_t totalByteLength;
    };

	void SHA256Init(SHA256Context& ctx);
	void SHA256Update(SHA256Context& ctx, const uint8_t* inputData, size_t inputByteLength);
	void SHA256Final(SHA256Context& ctx, SHA256Hash& output);
	void SHA256(SHA256Hash& output, const uint8_t* inputData, size_t inputByteLength);
}

#endif /* SHA256_HPP_ */
