#ifndef HEXA_UTILS_ARRAY_HPP
#define HEXA_UTILS_ARRAY_HPP

#include "common.hpp"
#include "span.hpp"

namespace HEXA_UTILS_NAMESPACE
{
	template<typename T, size_t Count>
	class Array
	{
	public:
		using size_type = size_t;
		using iterator = T*;
		using const_iterator = const T*;
		using reverse_iterator = std::reverse_iterator<iterator>;
		using const_reverse_iterator = std::reverse_iterator<const_iterator>;

	private:
		T values[Count]{};

	public:
		constexpr Array() = default;
		template<typename... Args>
			requires (sizeof...(Args) == Count)
		constexpr Array(Args... args) : values{ static_cast<T>(args)... } {}

		constexpr T& operator[](size_type idx) { if (idx >= Count) throw std::out_of_range("Array indexer out of range"); return values[idx]; }
		constexpr const T& operator[](size_type idx) const { if (idx >= Count) throw std::out_of_range("Array indexer out of range"); return values[idx]; }
		constexpr size_type size() const noexcept { return Count; }
		constexpr bool empty() const noexcept { return Count == 0; }

		constexpr iterator begin() noexcept { return values; }
		constexpr iterator end() noexcept { return values + Count; }
		constexpr const_iterator begin() const noexcept { return values; }
		constexpr const_iterator end() const noexcept { return values + Count; }
		constexpr const_iterator cbegin() const noexcept { return values; }
		constexpr const_iterator cend() const noexcept { return values + Count; }

		constexpr reverse_iterator rbegin() noexcept { return reverse_iterator(end()); }
		constexpr reverse_iterator rend() noexcept { return reverse_iterator(begin()); }
		constexpr const_reverse_iterator rbegin() const noexcept { return const_reverse_iterator(end()); }
		constexpr const_reverse_iterator rend() const noexcept { return const_reverse_iterator(begin()); }
		constexpr const_reverse_iterator crbegin() const noexcept { return const_reverse_iterator(cend()); }
		constexpr const_reverse_iterator crend() const noexcept { return const_reverse_iterator(cbegin()); }

		constexpr T& at(size_type idx)
		{
			if (idx >= Count) throw std::out_of_range("Array indexer out of range");
			return values[idx];
		}

		constexpr const T& at(size_type idx) const
		{
			if (idx >= Count) throw std::out_of_range("Array indexer out of range");
			return values[idx];
		}

		constexpr T& front() { return values[0]; }
		constexpr const T& front() const { return values[0]; }
		constexpr T& back() { return values[Count - 1]; }
		constexpr const T& back() const { return values[Count - 1]; }

		constexpr void fill(const T& value)
		{
			for (size_type i = 0; i < Count; ++i)
			{
				values[i] = value;
			}
		}

		constexpr T* data() noexcept { return values; }
		constexpr const T* data() const noexcept { return values; }

		constexpr bool operator==(const Array& other) const
		{
			for (size_type i = 0; i < Count; ++i)
			{
				if (!(values[i] == other.values[i])) return false;
			}
			return true;
		}

		constexpr bool operator!=(const Array& other) const
		{
			return !(*this == other);
		}

		constexpr void swap(Array& other) noexcept(std::is_nothrow_swappable_v<T>)
		{
			for (size_type i = 0; i < Count; ++i)
			{
				using std::swap;
				swap(values[i], other.values[i]);
			}
		}

		constexpr Array(const Array& other) = default;
		constexpr Array& operator=(const Array& other) = default;
		constexpr Array(Array&& other) = default;
		constexpr Array& operator=(Array&& other) = default;

		constexpr Span<T> as_span() noexcept { return Span<T>(values, Count); }
		constexpr operator Span<T>() noexcept { return as_span(); }
	};

	template<typename T, size_t Count>
	constexpr void swap(Array<T, Count>& lhs, Array<T, Count>& rhs) noexcept(noexcept(lhs.swap(rhs)))
	{
		lhs.swap(rhs);
	}

	template<typename T, size_t First, size_t... Rest>
	struct JArrayImpl
	{
		using type = HEXA_UTILS_NAMESPACE::Array<typename JArrayImpl<T, Rest...>::type, First>;
	};

	template<typename T, size_t First>
	struct JArrayImpl<T, First>
	{
		using type = HEXA_UTILS_NAMESPACE::Array<T, First>;
	};

	template<typename T, size_t... Sizes>
	using JArray = typename JArrayImpl<T, Sizes...>::type;
}

#endif