#ifndef HEXA_UTILS_RANGE_HPP
#define HEXA_UTILS_RANGE_HPP

#include "common.hpp"

namespace HEXA_UTILS_NAMESPACE
{
    struct Index
    {
        static constexpr size_t npos = std::string::npos;

        size_t value = npos;

        constexpr Index() : value(npos) {}
        constexpr Index(size_t v) : value(v) {}

        constexpr bool IsValid() const noexcept
        {
            return value != npos;
        }
        constexpr bool IsInvalid() const noexcept
        {
            return value == npos;
        }
        constexpr bool operator==(Index other) const noexcept
        {
            return value == other.value;
        }
        constexpr bool operator!=(Index other) const noexcept
        {
            return value != other.value;
        }

        template <std::integral U>
        constexpr bool operator==(U other) const noexcept
        {
            return value == static_cast<size_t>(other);
        }
        template <std::integral U>
        constexpr bool operator!=(U other) const noexcept
        {
            return value != static_cast<size_t>(other);
        }

        constexpr bool operator>(Index other) const noexcept
        {
            return value > other.value;
        }
        constexpr bool operator<(Index other) const noexcept
        {
            return value < other.value;
        }

        constexpr Index operator+(Index other) const noexcept
        {
            return Index(value + other.value);
        }
        constexpr Index operator-(Index other) const noexcept
        {
            return Index(value - other.value);
        }
        constexpr Index operator*(Index other) const noexcept
        {
            return Index(value * other.value);
        }
        constexpr Index operator/(Index other) const noexcept
        {
            return Index(value / other.value);
        }
        constexpr Index operator%(Index other) const noexcept
        {
            return Index(value % other.value);
        }
        constexpr Index operator^(Index other) const noexcept
        {
            return Index(value ^ other.value);
        }
        constexpr Index operator<<(Index other) const noexcept
        {
            return Index(value << other.value);
        }
        constexpr Index operator>>(Index other) const noexcept
        {
            return Index(value >> other.value);
        }
        constexpr Index operator|(Index other) const noexcept
        {
            return Index(value | other.value);
        }
        constexpr Index operator&(Index other) const noexcept
        {
            return Index(value & other.value);
        }

        template <std::integral U>
        constexpr Index operator+(U other) const noexcept
        {
            return Index(value + static_cast<size_t>(other));
        }
        template <std::integral U>
        constexpr Index operator-(U other) const noexcept
        {
            return Index(value - static_cast<size_t>(other));
        }
        template <std::integral U>
        constexpr Index operator*(U other) const noexcept
        {
            return Index(value * static_cast<size_t>(other));
        }
        template <std::integral U>
        constexpr Index operator/(U other) const noexcept
        {
            return Index(value / static_cast<size_t>(other));
        }
        template <std::integral U>
        constexpr Index operator%(U other) const noexcept
        {
            return Index(value % static_cast<size_t>(other));
        }
        template <std::integral U>
        constexpr Index operator^(U other) const noexcept
        {
            return Index(value ^ static_cast<size_t>(other));
        }
        template <std::integral U>
        constexpr Index operator<<(U other) const noexcept
        {
            return Index(value << static_cast<size_t>(other));
        }
        template <std::integral U>
        constexpr Index operator>>(U other) const noexcept
        {
            return Index(value >> static_cast<size_t>(other));
        }
        template <std::integral U>
        constexpr Index operator|(U other) const noexcept
        {
            return Index(value | static_cast<size_t>(other));
        }
        template <std::integral U>
        constexpr Index operator&(U other) const noexcept
        {
            return Index(value & static_cast<size_t>(other));
        }

        constexpr Index operator~() const noexcept
        {
            return ~value;
        }
        constexpr Index operator!() const noexcept
        {
            return !value;
        }
        constexpr Index operator-() const noexcept
        {
            return -value;
        }

        constexpr Index& operator++() noexcept
        {
            ++value;
            return *this;
        }
        constexpr Index& operator--() noexcept
        {
            --value;
            return *this;
        }
        constexpr Index operator++(int) noexcept
        {
            Index temp = *this;
            ++value;
            return temp;
        }
        constexpr Index operator--(int) noexcept
        {
            Index temp = *this;
            --value;
            return temp;
        }
        constexpr operator size_t() const noexcept
        {
            return value;
        }
        constexpr explicit operator bool() const noexcept
        {
            return value != npos;
        }

        static constexpr Index Invalid()
        {
            return Index();
        }
        constexpr Index ToMask() const noexcept
        {
            return Index(1ull << value);
        }
        constexpr bool TestBit(uint64_t bitmap) const noexcept
        {
            return bitmap & (1ull << value);
        }
    };

    template <std::integral T>
    constexpr Index operator+(T lhs, Index rhs) noexcept
    {
        return Index(static_cast<size_t>(lhs) + rhs.value);
    }
    template <std::integral T>
    constexpr Index operator-(T lhs, Index rhs) noexcept
    {
        return Index(static_cast<size_t>(lhs) - rhs.value);
    }
    template <std::integral T>
    constexpr Index operator*(T lhs, Index rhs) noexcept
    {
        return Index(static_cast<size_t>(lhs) * rhs.value);
    }
    template <std::integral T>
    constexpr Index operator/(T lhs, Index rhs) noexcept
    {
        return Index(static_cast<size_t>(lhs) / rhs.value);
    }
    template <std::integral T>
    constexpr Index operator%(T lhs, Index rhs) noexcept
    {
        return Index(static_cast<size_t>(lhs) % rhs.value);
    }

    template <std::integral T>
    constexpr Index operator^(T lhs, Index rhs) noexcept
    {
        return Index(static_cast<size_t>(lhs) ^ rhs.value);
    }
    template <std::integral T>
    constexpr Index operator&(T lhs, Index rhs) noexcept
    {
        return Index(static_cast<size_t>(lhs) & rhs.value);
    }
    template <std::integral T>
    constexpr Index operator|(T lhs, Index rhs) noexcept
    {
        return Index(static_cast<size_t>(lhs) | rhs.value);
    }
    template <std::integral T>
    constexpr Index operator<<(T lhs, Index rhs) noexcept
    {
        return Index(static_cast<size_t>(lhs) << rhs.value);
    }
    template <std::integral T>
    constexpr Index operator>>(T lhs, Index rhs) noexcept
    {
        return Index(static_cast<size_t>(lhs) >> rhs.value);
    }

    struct RangeIndex : Index
    {
        bool fromEnd = false;

        constexpr RangeIndex() : Index(npos), fromEnd(false) {}
        constexpr explicit RangeIndex(size_t v, bool fromEnd = false) : Index(v), fromEnd(fromEnd) {}
        constexpr explicit RangeIndex(Index v, bool fromEnd = false) : Index(v), fromEnd(fromEnd) {}

        static RangeIndex from_end(size_t v)
        {
            return RangeIndex(v, true);
        }

        constexpr size_t compute(bool isStart, size_t length) const
        {
            if (value == npos)
                return isStart ? 0 : length;

            if (fromEnd)
            {
                if (value > length)
                {
                    throw std::out_of_range("Index from end out of range");
                }
                return length - value;
            }
            if (value > length)
            {
                throw std::out_of_range("Index out of range");
            }
            return value;
        };

        constexpr size_t compute_noexcept(bool isStart, size_t length) const noexcept
        {
            if (value == npos)
                return isStart ? 0 : length;

            if (fromEnd)
            {
                if (value > length)
                {
                    return npos;
                }
                return length - value;
            }
            if (value > length)
            {
                return npos;
            }
            return value;
        };
    };

    struct Range
    {
        RangeIndex start;
        RangeIndex end;

        constexpr Range() = default;
        constexpr Range(RangeIndex s) : start(s), end(RangeIndex()) {}
        constexpr Range(RangeIndex s, RangeIndex e) : start(s), end(e) {}

        template <typename T1, typename T2>
        constexpr Range(T1 s, T2 e) : start(s), end(e)
        {
        }

        std::tuple<size_t, size_t> compute(size_t length) const
        {
            size_t begin = start.compute(true, length);
            size_t endPos = end.compute(false, length);

            if (begin > endPos)
            {
                throw std::out_of_range("Invalid range: start > end");
            }

            return {begin, endPos - begin};
        }

        std::tuple<size_t, size_t> compute_noexcept(size_t length) const noexcept
        {
            size_t begin = start.compute_noexcept(true, length);
            size_t endPos = end.compute_noexcept(false, length);

            if (begin > endPos)
            {
                return {Index::npos, Index::npos};
            }

            return {begin, endPos - begin};
        }

        bool contains(Index index) const noexcept
        {
            return index >= start && index < end;
        }
    };
} // namespace HEXA_UTILS_NAMESPACE

static inline HEXA_UTILS_NAMESPACE::RangeIndex operator""_r(unsigned long long value)
{
    return HEXA_UTILS_NAMESPACE::RangeIndex(static_cast<size_t>(value), false);
}

static inline HEXA_UTILS_NAMESPACE::RangeIndex operator""_rr(unsigned long long value)
{
    return HEXA_UTILS_NAMESPACE::RangeIndex(static_cast<size_t>(value), true);
}

#endif