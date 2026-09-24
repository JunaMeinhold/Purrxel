#include "utils/sha256.hpp"

namespace HEXA_UTILS_NAMESPACE
{
	static constexpr uint32_t RoundConstants[64] = { 0x428A2F98, 0x71374491, 0xB5C0FBCF,
			0xE9B5DBA5, 0x3956C25B, 0x59F111F1, 0x923F82A4, 0xAB1C5ED5,
			0xD807AA98, 0x12835B01, 0x243185BE, 0x550C7DC3, 0x72BE5D74,
			0x80DEB1FE, 0x9BDC06A7, 0xC19BF174, 0xE49B69C1, 0xEFBE4786,
			0x0FC19DC6, 0x240CA1CC, 0x2DE92C6F, 0x4A7484AA, 0x5CB0A9DC,
			0x76F988DA, 0x983E5152, 0xA831C66D, 0xB00327C8, 0xBF597FC7,
			0xC6E00BF3, 0xD5A79147, 0x06CA6351, 0x14292967, 0x27B70A85,
			0x2E1B2138, 0x4D2C6DFC, 0x53380D13, 0x650A7354, 0x766A0ABB,
			0x81C2C92E, 0x92722C85, 0xA2BFE8A1, 0xA81A664B, 0xC24B8B70,
			0xC76C51A3, 0xD192E819, 0xD6990624, 0xF40E3585, 0x106AA070,
			0x19A4C116, 0x1E376C08, 0x2748774C, 0x34B0BCB5, 0x391C0CB3,
			0x4ED8AA4A, 0x5B9CCA4F, 0x682E6FF3, 0x748F82EE, 0x78A5636F,
			0x84C87814, 0x8CC70208, 0x90BEFFFA, 0xA4506CEB, 0xBEF9A3F7,
			0xC67178F2 };


	void SHA256Init(SHA256Context& ctx)
	{
		ctx.hash[0] = 0x6A09E667;
		ctx.hash[1] = 0xBB67AE85;
		ctx.hash[2] = 0x3C6EF372;
		ctx.hash[3] = 0xA54FF53A;
		ctx.hash[4] = 0x510E527F;
		ctx.hash[5] = 0x9B05688C;
		ctx.hash[6] = 0x1F83D9AB;
		ctx.hash[7] = 0x5BE0CD19;
		ctx.bufferLength = 0;
		ctx.totalByteLength = 0;
	}

	static void SHA256ProcessChunk(uint32_t hash[8], const uint8_t chunk[64])
	{
		uint8_t messageSchedule[64 * 4] alignas(4);
		uint32_t* messageScheduleWord = reinterpret_cast<uint32_t*>(messageSchedule);

		memcpy(messageSchedule, chunk, 64);

		/* Reverse data bytes upon fetching (big-endian) */
		for (size_t i = 0; i < 16; i++)
		{
			messageSchedule[4 * i + 0] ^= messageSchedule[4 * i + 3];
			messageSchedule[4 * i + 3] ^= messageSchedule[4 * i + 0];
			messageSchedule[4 * i + 0] ^= messageSchedule[4 * i + 3];
			messageSchedule[4 * i + 1] ^= messageSchedule[4 * i + 2];
			messageSchedule[4 * i + 2] ^= messageSchedule[4 * i + 1];
			messageSchedule[4 * i + 1] ^= messageSchedule[4 * i + 2];
		}

		for (size_t i = 16; i < 64; i++)
		{
			uint32_t s0 = ((messageScheduleWord[i - 15] >> 7) | (messageScheduleWord[i - 15] << (32 - 7)))
				^ ((messageScheduleWord[i - 15] >> 18) | (messageScheduleWord[i - 15] << (32 - 18)))
				^ (messageScheduleWord[i - 15] >> 3);
			uint32_t s1 = ((messageScheduleWord[i - 2] >> 17) | (messageScheduleWord[i - 2] << (32 - 17)))
				^ ((messageScheduleWord[i - 2] >> 19) | (messageScheduleWord[i - 2] << (32 - 19)))
				^ (messageScheduleWord[i - 2] >> 10);
			messageScheduleWord[i] = messageScheduleWord[i - 16]
				+ messageScheduleWord[i - 7] + s0 + s1;
		}

		uint32_t workingVars[8];
		memcpy(workingVars, hash, sizeof(uint32_t) * 8);

		for (size_t i = 0; i < 64; i++)
		{
			uint32_t s1 = ((workingVars[4] >> 6) | (workingVars[4] << (32 - 6)))
				^ ((workingVars[4] >> 11) | (workingVars[4] << (32 - 11)))
				^ ((workingVars[4] >> 25) | (workingVars[4] << (32 - 25)));
			uint32_t ch = (workingVars[4] & workingVars[5])
				^ ((~workingVars[4]) & workingVars[6]);
			uint32_t temp1 = workingVars[7] + s1 + ch + RoundConstants[i]
				+ messageScheduleWord[i];
			uint32_t s0 = ((workingVars[0] >> 2) | (workingVars[0] << (32 - 2)))
				^ ((workingVars[0] >> 13) | (workingVars[0] << (32 - 13)))
				^ ((workingVars[0] >> 22) | (workingVars[0] << (32 - 22)));
			uint32_t maj = (workingVars[0] & workingVars[1])
				^ (workingVars[0] & workingVars[2])
				^ (workingVars[1] & workingVars[2]);
			uint32_t temp2 = s0 + maj;

			workingVars[7] = workingVars[6];
			workingVars[6] = workingVars[5];
			workingVars[5] = workingVars[4];
			workingVars[4] = workingVars[3] + temp1;
			workingVars[3] = workingVars[2];
			workingVars[2] = workingVars[1];
			workingVars[1] = workingVars[0];
			workingVars[0] = temp1 + temp2;
		}

		for (size_t i = 0; i < 8; i++)
		{
			hash[i] += workingVars[i];
		}
	}

	void SHA256Update(SHA256Context& ctx, const uint8_t* inputData, size_t inputByteLength)
	{
		ctx.totalByteLength += inputByteLength;

		size_t offset = 0;

		/* Fill the internal buffer first if it has leftover bytes */
		if (ctx.bufferLength > 0)
		{
			size_t needed = 64 - ctx.bufferLength;
			size_t toCopy = inputByteLength < needed ? inputByteLength : needed;
			memcpy(ctx.buffer + ctx.bufferLength, inputData, toCopy);
			ctx.bufferLength += toCopy;
			offset += toCopy;

			if (ctx.bufferLength == 64)
			{
				SHA256ProcessChunk(ctx.hash, ctx.buffer);
				ctx.bufferLength = 0;
			}
		}

		/* Process full 64-byte chunks directly from input */
		while (offset + 64 <= inputByteLength)
		{
			SHA256ProcessChunk(ctx.hash, inputData + offset);
			offset += 64;
		}

		/* Buffer remaining bytes */
		size_t remaining = inputByteLength - offset;
		if (remaining > 0)
		{
			memcpy(ctx.buffer, inputData + offset, remaining);
			ctx.bufferLength = remaining;
		}
	}

	void SHA256Final(SHA256Context& ctx, SHA256Hash& output)
	{
		/* Padding */
		uint64_t inputBitLength = ctx.totalByteLength * 8;
		size_t padStart = ctx.bufferLength;

		ctx.buffer[padStart++] = 0x80;

		if (padStart > 56)
		{
			/* Not enough room for length — pad to end of this block and process it */
			while (padStart < 64)
				ctx.buffer[padStart++] = 0x00;
			SHA256ProcessChunk(ctx.hash, ctx.buffer);
			padStart = 0;
		}

		/* Zero-fill up to byte 56, then write 64-bit big-endian bit length */
		while (padStart < 56)
			ctx.buffer[padStart++] = 0x00;

		for (size_t i = 0; i < 8; i++)
			ctx.buffer[56 + i] = static_cast<uint8_t>(inputBitLength >> (56 - 8 * i));

		SHA256ProcessChunk(ctx.hash, ctx.buffer);

		for (size_t i = 0; i < 8; i++)
		{
			output[4 * i + 0] = static_cast<uint8_t>(ctx.hash[i] >> 24);
			output[4 * i + 1] = static_cast<uint8_t>(ctx.hash[i] >> 16);
			output[4 * i + 2] = static_cast<uint8_t>(ctx.hash[i] >> 8);
			output[4 * i + 3] = static_cast<uint8_t>(ctx.hash[i]);
		}
	}

	void SHA256(SHA256Hash& output, const uint8_t* inputData, size_t inputByteLength)
	{
		SHA256Context ctx;
		SHA256Init(ctx);
		SHA256Update(ctx, inputData, inputByteLength);
		SHA256Final(ctx, output);
	}

	}