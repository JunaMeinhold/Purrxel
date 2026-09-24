#pragma once

#include "purrxel/pch/std.hpp"

namespace Purrxel::Core
{
    struct Point2
    {
        int32_t X = 0;
        int32_t Y = 0;

        static constexpr int32_t Count = 2;

        constexpr Point2() = default;
        constexpr explicit Point2(int32_t value) : X(value), Y(value) {}
        constexpr Point2(int32_t x, int32_t y) : X(x), Y(y) {}

        static const Point2 Zero;
        static const Point2 One;
        static const Point2 UnitX;
        static const Point2 UnitY;

        int32_t operator[](int32_t index) const
        {
            switch (index)
            {
                case 0: return X;
                case 1: return Y;
                default: throw std::out_of_range("Index must be smaller than 2 and larger or equals to 0");
            }
        }

        int32_t& operator[](int32_t index)
        {
            switch (index)
            {
                case 0: return X;
                case 1: return Y;
                default: throw std::out_of_range("Index must be smaller than 2 and larger or equals to 0");
            }
        }

        friend constexpr bool operator==(const Point2& a, const Point2& b) noexcept { return a.X == b.X && a.Y == b.Y; }
        friend constexpr bool operator!=(const Point2& a, const Point2& b) noexcept { return !(a == b); }

        friend constexpr Point2 operator+(const Point2& a, const Point2& b) noexcept { return Point2(a.X + b.X, a.Y + b.Y); }
        friend constexpr Point2 operator-(const Point2& a, const Point2& b) noexcept { return Point2(a.X - b.X, a.Y - b.Y); }
        friend constexpr Point2 operator*(const Point2& a, const Point2& b) noexcept { return Point2(a.X * b.X, a.Y * b.Y); }
        friend constexpr Point2 operator/(const Point2& a, const Point2& b) noexcept { return Point2(a.X / b.X, a.Y / b.Y); }

        friend constexpr Point2 operator*(const Point2& a, int32_t b) noexcept { return Point2(a.X * b, a.Y * b); }
        friend constexpr Point2 operator*(int32_t a, const Point2& b) noexcept { return b * a; }
        friend constexpr Point2 operator/(const Point2& a, int32_t b) noexcept { return Point2(a.X / b, a.Y / b); }

        friend constexpr Point2 operator-(const Point2& value) noexcept { return Point2(-value.X, -value.Y); }

        static constexpr Point2 Abs(const Point2& value) noexcept { return Point2(std::abs(value.X), std::abs(value.Y)); }

        static constexpr Point2 Clamp(const Point2& value, const Point2& min, const Point2& max) noexcept
        {
            return Point2(std::clamp(value.X, min.X, max.X), std::clamp(value.Y, min.Y, max.Y));
        }

        static constexpr int32_t CopySignScalar(int32_t value, int32_t sign) noexcept { return sign < 0 ? -std::abs(value) : std::abs(value); }
        static constexpr Point2 CopySign(const Point2& value, const Point2& sign) noexcept { return Point2(CopySignScalar(value.X, sign.X), CopySignScalar(value.Y, sign.Y)); }

        static constexpr int32_t Dot(const Point2& x, const Point2& y) noexcept { return (x.X * y.X) + (x.Y * y.Y); }

        constexpr int32_t LengthSquared() const noexcept { return Dot(*this, *this); }

        float Length() const noexcept { return std::sqrt(static_cast<float>(LengthSquared())); }

        static float Distance(const Point2& x, const Point2& y) noexcept { return (x - y).Length(); }

        static constexpr Point2 Min(const Point2& a, const Point2& b) noexcept { return Point2(std::min(a.X, b.X), std::min(a.Y, b.Y)); }
        static constexpr Point2 Max(const Point2& a, const Point2& b) noexcept { return Point2(std::max(a.X, b.X), std::max(a.Y, b.Y)); }
    };

    inline constexpr Point2 Point2::Zero{0};
    inline constexpr Point2 Point2::One{1};
    inline constexpr Point2 Point2::UnitX{1, 0};
    inline constexpr Point2 Point2::UnitY{0, 1};
}

template <>
struct std::hash<Purrxel::Core::Point2>
{
    size_t operator()(const Purrxel::Core::Point2& point) const noexcept
    {
        return std::hash<int64_t>{}((static_cast<int64_t>(point.X) << 32) ^ static_cast<uint32_t>(point.Y));
    }
};
