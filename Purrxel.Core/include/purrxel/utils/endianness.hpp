#ifndef ENDIAN_UTILS_HPP
#define ENDIAN_UTILS_HPP

#include <stdio.h>
#include <stdint.h>

namespace EndianUtils
{
	namespace Internal
	{
#if defined(__GNUC__) || defined(__clang__) || defined(_MSC_VER)
#define BYTE_SWAP_INTRINSIC_CONSTEXPR inline static
#else
#define BYTE_SWAP_INTRINSIC_CONSTEXPR constexpr // Only generic can use constexpr.
#endif
		template <typename T, std::size_t... I>
		constexpr T ByteSwapGenericImpl(T value, std::index_sequence<I...>) {
			return (
				static_cast<T>(
					((value >> (I * 8)) & static_cast<T>(0xFF)) << ((sizeof(T) - 1 - I) * 8)
					) | ...
				);
		}

		template <typename T>
		constexpr T ByteSwapGeneric(T value)
		{
			static_assert(std::is_integral_v<T>, "Only works for integral types.");
			using U = std::make_unsigned_t<T>;
			return std::bit_cast<T>(ByteSwapGenericImpl<U>(std::bit_cast<U>(value), std::make_index_sequence<sizeof(T)>()));
		}

		BYTE_SWAP_INTRINSIC_CONSTEXPR uint16_t ByteSwap16(uint16_t value)
		{
#if defined(__GNUC__) || defined(__clang__)
			return __builtin_bswap16(value);
#elif defined(_MSC_VER)
			return _byteswap_ushort(value);
#else
			return ByteSwapGeneric(value);
#endif
		}

		BYTE_SWAP_INTRINSIC_CONSTEXPR uint32_t ByteSwap32(uint32_t value)
		{
#if defined(__GNUC__) || defined(__clang__)
			return __builtin_bswap32(value);
#elif defined(_MSC_VER)
			return _byteswap_ulong(value);
#else
			return ByteSwapGeneric(value);
#endif
		}

		BYTE_SWAP_INTRINSIC_CONSTEXPR uint64_t ByteSwap64(uint64_t value)
		{
#if defined(__GNUC__) || defined(__clang__)
			return __builtin_bswap64(value);
#elif defined(_MSC_VER)
			return _byteswap_uint64(value);
#else
			return ByteSwapGeneric(value);
#endif
		}
	}

	template <typename T>
	concept EndianConvertible = std::is_enum_v<T> || std::is_integral_v<T> || std::is_floating_point_v<T>;

	constexpr bool IsLittleEndian()
	{
		return std::endian::native == std::endian::little;
	}

	constexpr bool IsBigEndian()
	{
		return std::endian::native == std::endian::big;
	}

	template <EndianConvertible T>
	constexpr T ReverseEndianness(T value)
	{
		if constexpr (std::is_floating_point_v<T>)
		{
			using IntType = std::conditional_t<sizeof(T) == 4, uint32_t, uint64_t>;
			IntType asInt = std::bit_cast<IntType>(value);
			IntType reversed = ReverseEndianness(asInt);
			return std::bit_cast<T>(reversed);
		}

		if constexpr (std::is_enum_v<T>)
		{
			using BaseType = typename std::underlying_type<T>::type;
			BaseType asBase = static_cast<BaseType>(value);
			BaseType reversed = ReverseEndianness(asBase);
			return static_cast<T>(reversed);
		}

		if constexpr (sizeof(T) == 1)
		{
			return value;
		}
		else if constexpr (sizeof(T) == 2)
		{
			return std::bit_cast<T>(Internal::ByteSwap16(std::bit_cast<uint16_t>(value)));
		}
		else if constexpr (sizeof(T) == 4)
		{
			return std::bit_cast<T>(Internal::ByteSwap32(std::bit_cast<uint32_t>(value)));
		}
		else if constexpr (sizeof(T) == 8)
		{
			return std::bit_cast<T>(Internal::ByteSwap64(std::bit_cast<uint64_t>(value)));
		}
		else
		{
			return Internal::ByteSwapGeneric(value);
		}
	}

	template <EndianConvertible T>
	constexpr T ToLittleEndian(T value)
	{
		if constexpr (!IsLittleEndian())
		{
			return ReverseEndianness(value);
		}
		return value;
	}

	template <EndianConvertible T>
	constexpr T ToBigEndian(T value)
	{
		if constexpr (!IsBigEndian())
		{
			return ReverseEndianness(value);
		}
		return value;
	}

	template <EndianConvertible T>
	constexpr T FromLittleEndian(T value) { return ToLittleEndian(value); }

	template <EndianConvertible T>
	constexpr T FromBigEndian(T value) { return ToBigEndian(value); }
}

#endif