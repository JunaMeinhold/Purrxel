#ifndef RTTI_HELPER_HPP
#define RTTI_HELPER_HPP

namespace HXSL
{
	template<auto... Values>
	struct rtti_type_equals_checker
	{
		template<typename T>
		constexpr bool operator()(T&& value) const
		{
			return ((std::forward<T>(value) == Values) || ...);
		}

		template<typename T>
		static constexpr bool check(T&& value)
		{
			return ((std::forward<T>(value) == Values) || ...);
		}
	};
}

#endif