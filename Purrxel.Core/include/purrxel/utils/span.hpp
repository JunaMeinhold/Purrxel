#ifndef HEXA_UTILS_SPAN_HPP
#define HEXA_UTILS_SPAN_HPP

#include "common.hpp"
#include "range.hpp"
#include "MurmurHash3.h"
#include "static_vector.hpp"
#include "vector.hpp"
#include <span>

namespace HEXA_UTILS_NAMESPACE
{
	template <class T>
	class ConstSpan;

	template <class T>
	class Span;

	template <class Elem, typename TDerived>
	class SpanBase
	{
	public:
		using T = std::remove_const_t<Elem>;
		using pointer = std::add_pointer_t<Elem>;
		using const_pointer = const pointer;
		using iterator = pointer;
		using const_iterator = const_pointer;

		using reverse_iterator = std::reverse_iterator<iterator>;
		using const_reverse_iterator = std::reverse_iterator<const_iterator>;

		using _is_const = typename std::is_const<Elem>;
		static constexpr bool is_const_t = _is_const::value;

		static constexpr size_t npos = static_cast<size_t>(-1);

	protected:
		pointer data_m;
		size_t size_m;

	public:
		constexpr SpanBase() : data_m(nullptr), size_m(0) {}
		constexpr SpanBase(pointer d, size_t size) : data_m(d), size_m(size) {}
		constexpr SpanBase(const std::span<T>& vec) : data_m(vec.data()), size_m(vec.size()) {}

		constexpr const pointer data() const noexcept
		{
			return data_m;
		}
		constexpr pointer data() noexcept
		{
			return data_m;
		}
		constexpr size_t size() const noexcept
		{
			return size_m;
		}
		constexpr size_t size_bytes() const noexcept
		{
			return size_m * sizeof(T);
		}
		constexpr bool empty() const noexcept
		{
			return size_m == 0 || data_m == nullptr;
		}

		constexpr iterator begin() noexcept
		{
			return data_m;
		}
		constexpr iterator end() noexcept
		{
			return data_m + size_m;
		}
		constexpr const_iterator begin() const noexcept
		{
			return data_m;
		}
		constexpr const_iterator end() const noexcept
		{
			return data_m + size_m;
		}
		constexpr const_iterator cbegin() const noexcept
		{
			return data_m;
		}
		constexpr const_iterator cend() const noexcept
		{
			return data_m + size_m;
		}

		constexpr reverse_iterator rbegin() noexcept
		{
			return std::make_reverse_iterator(end());
		}
		constexpr reverse_iterator rend() noexcept
		{
			return std::make_reverse_iterator(begin());
		}
		constexpr const_reverse_iterator rbegin() const noexcept
		{
			return std::make_reverse_iterator(end());
		}
		constexpr const_reverse_iterator rend() const noexcept
		{
			return std::make_reverse_iterator(begin());
		}
		constexpr const_reverse_iterator rcbegin() const noexcept
		{
			return std::make_reverse_iterator(cend());
		}
		constexpr const_reverse_iterator rcend() const noexcept
		{
			return std::make_reverse_iterator(cbegin());
		}

		constexpr const T& operator[](size_t index) const noexcept
		{
			return data_m[index];
		}

		constexpr T& operator[](size_t index) noexcept
			requires(!is_const_t)
		{
			return data_m[index];
		}

		constexpr const T& operator[](const RangeIndex& idx) const noexcept
		{
			return (*this)[idx.compute_noexcept(true, size_m)];
		}

		constexpr T& operator[](const RangeIndex& idx) noexcept
			requires(!is_const_t)
		{
			return (*this)[idx.compute_noexcept(true, size_m)];
		}

		constexpr TDerived operator[](const Range& range) const noexcept
		{
			auto [start, length] = range.compute_noexcept(size_m);
			return slice(start, length);
		}

		constexpr TDerived slice(size_t start) const
		{
			if (start >= size_m)
				throw std::out_of_range("Index out of range in span");
			return TDerived(data_m + start, size_m - start);
		}

		constexpr TDerived slice(size_t start, size_t length) const
		{
			if (start + length > this->size_m)
				throw std::out_of_range("Slice exceeds span bounds");
			return TDerived(data_m + start, length);
		}

		template <typename Equals = std::equal_to<T>>
		Index find(const T& c, const Equals& equals = {}) const noexcept
		{
			for (size_t i = 0; i < size_m; i++)
			{
				if (equals(data_m[i], c))
				{
					return i;
				}
			}
			return Index::Invalid();
		}

		struct KMPTable
		{
			uint32_t* values = nullptr;
			bool owns = false;

			constexpr KMPTable() = default;
			explicit KMPTable(uint32_t* values) : values(values), owns(false) {}
			explicit KMPTable(size_t size) : owns(true)
			{
				values = new uint32_t[size];
			}

			~KMPTable()
			{
				if (owns)
				{
					delete[] values;
				}
			}

			uint32_t& operator[](size_t idx) noexcept
			{
				return values[idx];
			}
			uint32_t operator[](size_t idx) const noexcept
			{
				return values[idx];
			}
		};

		static void BuildFailureFunction(KMPTable& lps, const TDerived& pattern) noexcept
		{
			size_t m = pattern.size();
			size_t len = 0;
			size_t i = 1;
			while (i < m)
			{
				if (pattern[i] == pattern[len])
				{
					len++;
					lps[i] = len;
					i++;
				}
				else
				{
					if (len != 0)
					{
						len = lps[len - 1];
					}
					else
					{
						lps[i] = 0;
						i++;
					}
				}
			}
		}

		Index find(const TDerived& other) const noexcept
		{
			size_t m = other.size();
			if (m == 0)
				return 0;
			if (m > size_m)
				return Index::Invalid();

			KMPTable table;
			size_t tableSizeBytes = m * sizeof(uint32_t);
			if (tableSizeBytes > 2048)
			{
				table = KMPTable(m);
			}
			else
			{
				void* p = alloca(tableSizeBytes);
				table = KMPTable(reinterpret_cast<uint32_t*>(p));
			}

			BuildFailureFunction(table, other);

			size_t i = 0, j = 0;
			while (i < size_m)
			{
				if (data_m[i] == other[j])
				{
					++i;
					++j;
				}

				if (j == m)
				{
					return i - j;
				}
				else if (i < size_m && data_m[i] != other[j])
				{
					if (j != 0)
					{
						j = table[j - 1];
					}
					else
					{
						++i;
					}
				}
			}

			return Index::Invalid();
		}

		size_t hash() const noexcept
		{
			uint128_t hash;
			MurmurHash3_x64_128(reinterpret_cast<const uint8_t*>(data_m), size_bytes(), 0, hash);
			return static_cast<size_t>(hash);
		}

		template <typename Func>
		Index IndexOf(size_t offset, Func func) const
		{
			for (size_t i = offset; i < size_m; i++)
			{
				if (func(data_m[i]))
				{
					return i;
				}
			}
			return Index::Invalid();
		}

		template <typename Func>
		Index LastIndexOf(size_t offset, Func func) const
		{
			for (size_t i = SatSubtract(size_m, offset); i-- != 0;)
			{
				if (func(data_m[i]))
				{
					return i;
				}
			}
			return Index::Invalid();
		}

		template <typename Func>
		Index IndexOf(Func&& func) const
		{
			return IndexOf(0, std::forward<Func>(func));
		}

		template <typename Func>
		Index LastIndexOf(Func&& func) const
		{
			return LastIndexOf(static_cast<size_t>(0), std::forward<Func>(func));
		}

		template <typename Equals = std::equal_to<T>>
		Index IndexOf(size_t offset, T c, const Equals& equals = {}) const
		{
			for (size_t i = offset; i < size_m; i++)
			{
				if (equals(data_m[i], c))
				{
					return i;
				}
			}
			return Index::Invalid();
		}

		template <typename Equals = std::equal_to<T>>
		Index LastIndexOf(size_t offset, T c, const Equals& equals = {}) const
		{
			for (size_t i = SatSubtract(size_m, offset); i-- != 0;)
			{
				if (equals(data_m[i], c))
				{
					return i;
				}
			}
			return Index::Invalid();
		}

		template <typename Equals = std::equal_to<T>>
		Index IndexOf(T c, const Equals& equals = {}) const
		{
			return IndexOf(0, c, equals);
		}

		template <typename Equals = std::equal_to<T>>
		Index LastIndexOf(T c, const Equals& equals = {}) const
		{
			return LastIndexOf(0, c, equals);
		}

		bool operator==(const TDerived& other) const
		{
			if (this->size_m != other.size())
				return false;
			return std::memcmp(this->begin(), other.begin(), size_bytes()) == 0;
		}

		static inline void copy(const ConstSpan<T>& from, const Span<T>& to);

		void copy_to(const Span<T>& other) const;
	};

	template <typename T, typename TDerived>
	struct SpanHashBase
	{
		using SpanType = SpanBase<T, TDerived>;
		size_t operator()(const SpanType& span) const noexcept
		{
			return static_cast<size_t>(span.hash());
		}
	};

	template <typename T, typename TDerived>
	struct SpanEqualBase
	{
		using SpanType = SpanBase<T, TDerived>;
		bool operator()(const SpanType& a, const SpanType& b) const noexcept
		{
			if (a.size() != b.size())
				return false;
			return std::memcmp(a.begin(), b.begin(), a.size() * sizeof(T)) == 0;
		}
	};

	template <class T>
	class Span : public SpanBase<T, Span<T>>
	{
	public:
		using Base = SpanBase<T, Span<T>>;
		using Hash = SpanHashBase<T, Span<T>>;
		using Equal = SpanEqualBase<T, Span<T>>;

		using Base::Base;

		template <class Alloc>
		constexpr Span(std::vector<T, Alloc>& vec) : Base(vec.data(), vec.size())
		{
		}

		template <class Alloc>
		constexpr Span(const HEXA_UTILS_NAMESPACE::vector<T, Alloc>& vec) : Base(vec.data(), vec.size())
		{
		}

		template <class Alloc>
		constexpr Span(const HEXA_UTILS_NAMESPACE::static_vector<T, Alloc>& vec) : Base(vec.data(), vec.size())
		{
		}

		constexpr Span(const std::vector<T>& vec) : Base(vec.data(), vec.size()) {}
		constexpr Span(const HEXA_UTILS_NAMESPACE::vector<T>& vec) : Base(vec.data(), vec.size()) {}
		constexpr Span(const HEXA_UTILS_NAMESPACE::static_vector<T>& vec) : Base(vec.data(), vec.size()) {}

		operator Span<const T>() const
		{
			return { this->data_m, this->size_m };
		}
	};

	template <class T>
	class ConstSpan : public SpanBase<const T, ConstSpan<T>>
	{
	public:
		using Base = SpanBase<const T, ConstSpan<T>>;
		using Hash = SpanHashBase<T, Span<T>>;
		using Equal = SpanEqualBase<T, Span<T>>;

		using Base::Base;

		template <class U, class Alloc = class std::vector<U>::allocator_type>
		constexpr ConstSpan(const std::vector<U, Alloc>& vec) : Base(vec.data(), vec.size())
		{
		}

		template <class U, class Alloc = class HEXA_UTILS_NAMESPACE::vector<U>::allocator_type>
		constexpr ConstSpan(const HEXA_UTILS_NAMESPACE::vector<U, Alloc>& vec) : Base(vec.data(), vec.size())
		{
		}

		template <class U, class Alloc = class HEXA_UTILS_NAMESPACE::static_vector<U>::allocator_type>
		constexpr ConstSpan(const HEXA_UTILS_NAMESPACE::static_vector<U, Alloc>& vec) : Base(vec.data(), vec.size())
		{
		}

		template <typename U = T>
		constexpr ConstSpan(const Span<U>& span)
			requires(std::is_convertible<U*, const T*>::value)
		: Base(span.data(), span.size())
		{
		}
	};

	template <typename T, typename TDerived>
	inline void SpanBase<T, TDerived>::copy(const ConstSpan<T>& from, const Span<T>& to)
	{
		if (to.size() < from.size())
			throw std::out_of_range("Destination span too short.");
		std::memcpy(to.data(), from.data(), from.size() * sizeof(T));
	}

	template <typename T, typename TDerived>
	inline void SpanBase<T, TDerived>::copy_to(const Span<T>& other) const
	{
		if (other.size() < this->size_m)
			throw std::out_of_range("Destination span too short.");
		std::memcpy(other.data(), this->data_m, this->size_m * sizeof(T));
	}

	template <typename T>
	class StringSpanBase : public SpanBase<const T, StringSpanBase<T>>
	{
	public:
		using Base = SpanBase<const T, StringSpanBase<T>>;
		using Hash = SpanHashBase<const T, Span<T>>;
		using Equal = SpanEqualBase<const T, Span<T>>;

		using Base::Base;

		template <class Traits = std::char_traits<T>, class Alloc = std::allocator<T>>
		constexpr StringSpanBase(const std::basic_string<T, Traits, Alloc>& str) : Base(str.data(), str.length())
		{
		}

		constexpr StringSpanBase(const T* str) : Base(str, strlen(str)) {}

		constexpr StringSpanBase(const T* str, size_t start, size_t length) : Base(str + start, length) {}

		constexpr StringSpanBase(const Span<T>& span) : Base(span.data(), span.size()) {}

		constexpr StringSpanBase(const ConstSpan<T>& span) : Base(span.data(), span.size()) {}

		template <class Traits = std::char_traits<T>>
		constexpr StringSpanBase(const std::basic_string_view<T, Traits>& span) : Base(span.data(), span.size())
		{
		}

		template <class Traits = std::char_traits<T>>
		constexpr std::basic_string_view<T, Traits> view() const
		{
			return std::basic_string_view<T, Traits>(this->data_m, this->size_m);
		}

		std::string str() const
		{
			return std::string(this->data_m, this->size_m);
		}

		std::string str(size_t start) const
		{
			if (start > this->size_m)
				throw std::out_of_range("start index was out of range.");

			return std::string(this->data_m + start, this->size_m - start);
		}

		std::string str(size_t start, size_t length) const
		{
			if (start > length)
				throw std::out_of_range("start index was out of range.");

			if (start + length > this->size_m)
				throw std::out_of_range("substring length out of range.");

			return std::string(this->data_m + start, length);
		}

		StringSpanBase TrimStart() const
		{
			size_t start = 0;
			while (start < this->size_m && std::isspace(this->data_m[start])) ++start;
			return this->slice(start);
		}

		StringSpanBase TrimEnd() const
		{
			size_t end = this->size_m;
			while (end > 0 && std::isspace(this->data_m[end - 1])) --end;
			return this->slice(0, end);
		}

		StringSpanBase Trim() const
		{
			size_t start = 0;
			while (start < this->size_m && std::isspace(this->data_m[start])) ++start;

			size_t end = this->size_m;
			while (end > start && std::isspace(this->data_m[end - 1])) --end;

			return this->slice(start, end - start);
		}

		StringSpanBase TrimStart(const char c) const
		{
			size_t start = 0;
			while (start < this->size_m && this->data_m[start] == c) ++start;
			return this->slice(start);
		}

		StringSpanBase TrimEnd(const char c) const
		{
			size_t end = this->size_m;
			while (end > 0 && this->data_m[end - 1] == c) --end;
			return this->slice(0, end);
		}

		template <typename Equals = std::equal_to<T>>
		bool StartsWith(size_t offset, T c, const Equals& equals = {})
		{
			return SatSubtract(this->size_m, offset) > 0 && equals(this->data_m[offset], c);
		}

		template <typename Equals = std::equal_to<T>>
		bool EndsWith(size_t offset, T c, const Equals& equals = {})
		{
			return SatSubtract(this->size_m, offset) > 0 && equals(this->data_m[this->size_m - 1 - offset], c);
		}

		template <typename Equals = std::equal_to<T>>
		bool StartsWith(T c, const Equals& equals = {})
		{
			return StartsWith(0, c, equals);
		}

		template <typename Equals = std::equal_to<T>>
		bool EndsWith(T c, const Equals& equals = {})
		{
			return EndsWith(0, c, equals);
		}

		template <typename Equals = std::equal_to<T>>
		bool StartsWith(const StringSpanBase& other, const Equals& equals = {}) const
		{
			if (other.size() > this->size_m) return false;
			for (size_t i = 0; i < other.size(); i++)
			{
				if (!equals(this->data_m[i], other.data_m[i]))
					return false;
			}
			return true;
		}

		template <typename Equals = std::equal_to<T>>
		bool EndsWith(const StringSpanBase& other, const Equals& equals = {}) const
		{
			if (other.size() > this->size_m) return false;
			size_t offset = this->size_m - other.size();
			for (size_t i = 0; i < other.size(); i++)
			{
				if (!equals(this->data_m[offset + i], other.data_m[i]))
					return false;
			}
			return true;
		}

		bool operator==(const StringSpanBase& other) const
		{
			if (this->size_m != other.size())
				return false;
			return std::memcmp(this->begin(), other.begin(), this->size_m * sizeof(T)) == 0;
		}

		bool operator!=(const StringSpanBase& other) const
		{
			return !(*this == other);
		}

		struct TransparentHash
		{
			using is_transparent = void;

			size_t operator()(const std::string& s) const noexcept
			{
				return StringSpanBase(s).hash();
			}

			size_t operator()(const StringSpanBase& span) const noexcept
			{
				return span.hash();
			}
		};

		struct TransparentEqual
		{
			using is_transparent = void;

			bool operator()(const std::string& a, const std::string& b) const noexcept
			{
				return a == b;
			}

			bool operator()(const std::string& a, const StringSpanBase& b) const noexcept
			{
				return StringSpanBase(a) == b;
			}

			bool operator()(const StringSpanBase& a, const std::string& b) const noexcept
			{
				return a == StringSpanBase(b);
			}

			bool operator()(const StringSpanBase& a, const StringSpanBase& b) const noexcept
			{
				return a == b;
			}
		};
	};

	using StringSpan = StringSpanBase<char>;

	inline std::ostream& operator<<(std::ostream& os, const StringSpan& span)
	{
		return os.write(span.data(), span.size());
	}
} // namespace HEXA_UTILS_NAMESPACE

namespace std
{
	template <typename T>
	struct hash<HEXA_UTILS_NAMESPACE::Span<T>>
	{
		size_t operator()(const HEXA_UTILS_NAMESPACE::Span<T>& span) const noexcept
		{
			return static_cast<size_t>(span.hash());
		}
	};

	template <typename T>
	struct hash<HEXA_UTILS_NAMESPACE::ConstSpan<T>>
	{
		size_t operator()(const HEXA_UTILS_NAMESPACE::ConstSpan<T>& span) const noexcept
		{
			return static_cast<size_t>(span.hash());
		}
	};

	template <>
	struct hash<HEXA_UTILS_NAMESPACE::StringSpan>
	{
		size_t operator()(const HEXA_UTILS_NAMESPACE::StringSpan& span) const noexcept
		{
			return static_cast<size_t>(span.hash());
		}
	};
} // namespace std

#endif