#ifndef HEXA_UTILS_TYPES_HPP
#define HEXA_UTILS_TYPES_HPP

#include "common.hpp"

namespace HEXA_UTILS_NAMESPACE
{
	struct alignas(16) uint128_t
	{
	private:
		uint64_t low;
		uint64_t high;
	public:
		constexpr uint128_t() : low(0), high(0) {}
		constexpr uint128_t(uint64_t low) : low(low), high(0) {}
		constexpr uint128_t(uint64_t high, uint64_t low) : low(low), high(high) {}

		uint64_t get_low() const { return low; }
		uint64_t get_high() const { return high; }

		friend bool operator==(const uint128_t& a, const uint128_t& b)
		{
			return a.low == b.low && a.high == b.high;
		}

		friend bool operator!=(const uint128_t& a, const uint128_t& b)
		{
			return !(a == b);
		}

		friend bool operator<(const uint128_t& a, const uint128_t& b)
		{
			return (a.high < b.high) || (a.high == b.high && a.low < b.low);
		}

		friend bool operator>(const uint128_t& a, const uint128_t& b)
		{
			return b < a;
		}

		friend bool operator<=(const uint128_t& a, const uint128_t& b)
		{
			return !(b < a);
		}

		friend bool operator>=(const uint128_t& a, const uint128_t& b)
		{
			return !(a < b);
		}

		friend uint128_t operator&(const uint128_t& a, const uint128_t& b)
		{
			return { a.high & b.high, a.low & b.low };
		}

		friend uint128_t operator|(const uint128_t& a, const uint128_t& b)
		{
			return { a.high | b.high, a.low | b.low };
		}

		friend uint128_t operator^(const uint128_t& a, const uint128_t& b)
		{
			return { a.high ^ b.high, a.low ^ b.low };
		}

		friend uint128_t operator~(const uint128_t& a)
		{
			return { ~a.high, ~a.low };
		}

		constexpr uint128_t operator<<(unsigned int shift) const
		{
			if (shift == 0) return *this;
			uint64_t s = shift & 127;
			uint64_t highA = (high << s) | (low >> (64 - s));
			uint64_t lowA = low << s;

			uint64_t highB = low << (s - 64);

			uint64_t maskA = -(uint64_t)(s < 64);
			uint64_t maskB = -(uint64_t)(s >= 64 && s < 128);

			return { (highA & maskA) | (highB & maskB), (lowA & maskA) };
		}

		constexpr uint128_t operator<<(int shift) const { return *this << (unsigned int)shift; }

		constexpr uint128_t operator>>(unsigned int shift) const
		{
			if (shift == 0) return *this;
			if (shift < 64)
				return { high >> shift, (low >> shift) | (high << (64 - shift)) };
			else if (shift < 128)
				return { 0, high >> (shift - 64) };
			else
				return { 0, 0 };
		}

		constexpr uint128_t operator>>(int shift) const { return *this >> (unsigned int)shift; }

		constexpr uint128_t operator+(const uint128_t& other) const
		{
			uint64_t result_low = low + other.low;
			uint64_t carry = result_low < low;
			uint64_t result_high = high + other.high + carry;
			return { result_high, result_low };
		}

		uint128_t& operator+=(const uint128_t& other)
		{
			*this = *this + other;
			return *this;
		}

		constexpr uint128_t operator-(const uint128_t& other) const
		{
			uint64_t result_low = low - other.low;
			uint64_t borrow = low < other.low;
			uint64_t result_high = high - other.high - borrow;
			return { result_high, result_low };
		}

		uint128_t& operator-=(const uint128_t& other)
		{
			*this = *this - other;
			return *this;
		}

		constexpr uint128_t operator*(const uint128_t& other) const
		{
			uint64_t a_lo = low;
			uint64_t a_hi = high;
			uint64_t b_lo = other.low;
			uint64_t b_hi = other.high;

			uint64_t lo_lo = a_lo * b_lo;
			uint64_t hi_lo = a_hi * b_lo;
			uint64_t lo_hi = a_lo * b_hi;
			uint64_t hi_hi = a_hi * b_hi;

			uint64_t mid1 = hi_lo + lo_hi;
			uint64_t carry = (mid1 < hi_lo) ? 1 : 0;

			uint64_t result_low = lo_lo;
			uint64_t result_high = hi_hi + ((mid1 + carry) >> 0);

			result_high += ((uint128_t(lo_lo) >> 64).low);

			return { result_high, result_low };
		}

		uint128_t& operator*=(const uint128_t& other)
		{
			*this = *this * other;
			return *this;
		}

		uint128_t operator/(const uint128_t& other) const
		{
			if (!other) throw std::domain_error("uint128_t division by zero");
			uint128_t quotient = 0;
			uint128_t remainder = 0;
			for (int i = 127; i >= 0; --i) {
				remainder <<= 1;
				remainder.low |= ((*this >> i) & uint128_t(1)).low;
				if (remainder >= other) {
					remainder -= other;
					quotient |= (uint128_t(1) << i);
				}
			}
			return quotient;
		}

		uint128_t& operator/=(const uint128_t& other)
		{
			*this = *this / other;
			return *this;
		}

		uint128_t operator%(const uint128_t& other) const
		{
			if (!other) throw std::domain_error("uint128_t modulo by zero");
			uint128_t remainder = 0;
			for (int i = 127; i >= 0; --i) {
				remainder <<= 1;
				remainder.low |= ((*this >> i) & uint128_t(1)).low;
				if (remainder >= other) {
					remainder -= other;
				}
			}
			return remainder;
		}

		uint128_t& operator%=(const uint128_t& other)
		{
			*this = *this % other;
			return *this;
		}

		uint128_t& operator&=(const uint128_t& other) { *this = *this & other; return *this; }
		uint128_t& operator|=(const uint128_t& other) { *this = *this | other; return *this; }
		uint128_t& operator^=(const uint128_t& other) { *this = *this ^ other; return *this; }

		uint128_t& operator<<=(unsigned int shift) { *this = *this << shift; return *this; }
		uint128_t& operator>>=(unsigned int shift) { *this = *this >> shift; return *this; }

		uint128_t& operator++() { *this += uint128_t(1); return *this; }
		uint128_t operator++(int) { uint128_t tmp = *this; ++*this; return tmp; }
		uint128_t& operator--() { *this -= uint128_t(1); return *this; }
		uint128_t operator--(int) { uint128_t tmp = *this; --*this; return tmp; }

		template<typename CharT, typename Traits>
		friend std::basic_ostream<CharT, Traits>& operator<<(std::ostream& os, const uint128_t& value)
		{
			if (value.high)
				os << std::hex << "0x" << value.high << std::setfill('0') << std::setw(16) << value.low;
			else
				os << std::hex << "0x" << value.low;
			return os;
		}

		explicit operator uint64_t() const
		{
			return low;
		}

		operator bool() const
		{
			return high != 0 || low != 0;
		}
	};
}

#if !HEXA_UTILS_NO_GLOBAL_UINT128_T
#if defined(__SIZEOF_INT128__) && __SIZEOF_INT128__ == 16
using uint128_t = __uint128_t;
#elif !defined(WIDEMATHAPI)
using uint128_t = HEXA_UTILS_NAMESPACE::uint128_t;
#endif
#endif

#endif