#pragma once

#include "purrxel/core/point2.hpp"
#include "purrxel/pch/std.hpp"

namespace Purrxel::Core
{
    struct Point3
    {
        int32_t X = 0;
        int32_t Y = 0;
        int32_t Z = 0;

        static constexpr int32_t Count = 3;

        constexpr Point3() = default;
        constexpr explicit Point3(int32_t value) : X(value), Y(value), Z(value) {}
        constexpr Point3(int32_t x, int32_t y, int32_t z) : X(x), Y(y), Z(z) {}
        constexpr Point3(const Point2& point, int32_t z) : X(point.X), Y(point.Y), Z(z) {}

        static const Point3 Zero;
        static const Point3 One;
        static const Point3 UnitX;
        static const Point3 UnitY;
        static const Point3 UnitZ;

        int32_t operator[](int32_t index) const
        {
            switch (index)
            {
                case 0: return X;
                case 1: return Y;
                case 2: return Z;
                default: throw std::out_of_range("Index must be smaller than 3 and larger or equals to 0");
            }
        }

        int32_t& operator[](int32_t index)
        {
            switch (index)
            {
                case 0: return X;
                case 1: return Y;
                case 2: return Z;
                default: throw std::out_of_range("Index must be smaller than 3 and larger or equals to 0");
            }
        }

        friend constexpr bool operator==(const Point3& a, const Point3& b) noexcept { return a.X == b.X && a.Y == b.Y && a.Z == b.Z; }
        friend constexpr bool operator!=(const Point3& a, const Point3& b) noexcept { return !(a == b); }

        friend constexpr Point3 operator+(const Point3& a, const Point3& b) noexcept { return Point3(a.X + b.X, a.Y + b.Y, a.Z + b.Z); }
        friend constexpr Point3 operator-(const Point3& a, const Point3& b) noexcept { return Point3(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
        friend constexpr Point3 operator*(const Point3& a, const Point3& b) noexcept { return Point3(a.X * b.X, a.Y * b.Y, a.Z * b.Z); }
        friend constexpr Point3 operator/(const Point3& a, const Point3& b) noexcept { return Point3(a.X / b.X, a.Y / b.Y, a.Z / b.Z); }

        friend constexpr Point3 operator*(const Point3& a, int32_t b) noexcept { return Point3(a.X * b, a.Y * b, a.Z * b); }
        friend constexpr Point3 operator*(int32_t a, const Point3& b) noexcept { return b * a; }
        friend constexpr Point3 operator/(const Point3& a, int32_t b) noexcept { return Point3(a.X / b, a.Y / b, a.Z / b); }

        friend constexpr Point3 operator-(const Point3& value) noexcept { return Point3(-value.X, -value.Y, -value.Z); }

        static constexpr Point3 Abs(const Point3& value) noexcept { return Point3(std::abs(value.X), std::abs(value.Y), std::abs(value.Z)); }

        static constexpr Point3 Clamp(const Point3& value, const Point3& min, const Point3& max) noexcept
        {
            return Point3(std::clamp(value.X, min.X, max.X), std::clamp(value.Y, min.Y, max.Y), std::clamp(value.Z, min.Z, max.Z));
        }

        static constexpr int32_t CopySignScalar(int32_t value, int32_t sign) noexcept { return sign < 0 ? -std::abs(value) : std::abs(value); }
        static constexpr Point3 CopySign(const Point3& value, const Point3& sign) noexcept
        {
            return Point3(CopySignScalar(value.X, sign.X), CopySignScalar(value.Y, sign.Y), CopySignScalar(value.Z, sign.Z));
        }

        static constexpr int32_t Dot(const Point3& x, const Point3& y) noexcept { return (x.X * y.X) + (x.Y * y.Y) + (x.Z * y.Z); }

        constexpr int32_t LengthSquared() const noexcept { return Dot(*this, *this); }

        float Length() const noexcept { return std::sqrt(static_cast<float>(LengthSquared())); }

        static float Distance(const Point3& x, const Point3& y) noexcept { return (x - y).Length(); }

        static constexpr Point3 Min(const Point3& a, const Point3& b) noexcept { return Point3(std::min(a.X, b.X), std::min(a.Y, b.Y), std::min(a.Z, b.Z)); }
        static constexpr Point3 Max(const Point3& a, const Point3& b) noexcept { return Point3(std::max(a.X, b.X), std::max(a.Y, b.Y), std::max(a.Z, b.Z)); }
    };

    inline constexpr Point3 Point3::Zero{0};
    inline constexpr Point3 Point3::One{1};
    inline constexpr Point3 Point3::UnitX{1, 0, 0};
    inline constexpr Point3 Point3::UnitY{0, 1, 0};
    inline constexpr Point3 Point3::UnitZ{0, 0, 1};
}

template <>
struct std::hash<Purrxel::Core::Point3>
{
    size_t operator()(const Purrxel::Core::Point3& point) const noexcept
    {
        size_t h = std::hash<int32_t>{}(point.X);
        h = h * 31 + std::hash<int32_t>{}(point.Y);
        h = h * 31 + std::hash<int32_t>{}(point.Z);
        return h;
    }
};
