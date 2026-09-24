#ifndef HEXA_UTILS_MATH_HPP
#define HEXA_UTILS_MATH_HPP

#include "common.hpp"

namespace HEXA_UTILS_NAMESPACE
{
	static consteval uint64_t as_uint64(double x)
	{
		return std::bit_cast<uint64_t>(x);
	}

	static consteval double as_double(uint64_t x)
	{
		return std::bit_cast<double>(x);
	}

	static consteval void decompose(double x, double& mantissa, int& exponent)
	{
		uint64_t bits = as_uint64(x);
		exponent = ((bits >> 52) & 0x7FF) - 1023;

		bits = (bits & ((1ULL << 52) - 1)) | (uint64_t(1023) << 52);
		mantissa = as_double(bits);
	}

	static consteval double log_mantissa(double m)
	{
		double x = m - 1;
		double result = 0;
		double term = x;
		for (int i = 1; i <= 40; ++i)
		{
			result += (i % 2 == 1 ? term : -term) / i;
			term *= x;
		}
		return result;
	}

	static consteval double log(double x)
	{
		double m;
		int e;
		decompose(x, m, e);
		return log_mantissa(m) + e * 0.6931471805599453; // ln(2)
	}

	static consteval double exp(double x)
	{
		// Handle large values by using exp(x) = exp(x/2)²
		if (x > 1.0)
		{
			double half_exp = exp(x * 0.5);
			return half_exp * half_exp;
		}
		if (x < -1.0)
		{
			return 1.0 / exp(-x);
		}

		// Taylor series: 1 + x + x²/2! + x³/3! + ...
		double result = 1.0;
		double term = 1.0;

		for (int i = 1; i <= 40; ++i)
		{
			term *= x / i;
			result += term;
		}
		return result;
	}

	static consteval double pow(double a, double b)
	{
		if (a == 0.0) return 0.0;
		return exp(b * log(a));
	}

	template<size_t Base>
	struct ExponentialSequence
	{
		template<size_t n>
		static consteval size_t ComputePower()
		{
			auto result = 1;
			for (size_t i = 0; i < n; ++i)
			{
				result *= Base;
			}
			return result;
		}

		template<size_t start, size_t length>
		static consteval auto value()
		{
			return[]<size_t... Indices>(std::index_sequence<Indices...>) {
				return std::array<size_t, sizeof...(Indices)>{ ComputePower<Indices + start>()... };
			}(std::make_index_sequence<length>{});
		}
	};
}

#endif