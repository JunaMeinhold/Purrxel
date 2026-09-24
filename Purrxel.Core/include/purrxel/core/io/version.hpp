#pragma once

#include "purrxel/pch/std.hpp"

namespace Purrxel::Core::IO
{
    struct Version
    {
        uint8_t Major = 0;
        uint8_t Minor = 0;
        uint8_t Patch = 0;
        uint8_t Build = 0;

        constexpr Version() = default;

        constexpr Version(uint8_t major, uint8_t minor, uint8_t patch, uint8_t build)
            : Major(major), Minor(minor), Patch(patch), Build(build)
        {
        }

        constexpr explicit Version(uint32_t packed)
            : Major(static_cast<uint8_t>((packed >> 24) & 0xff)),
              Minor(static_cast<uint8_t>((packed >> 16) & 0xff)),
              Patch(static_cast<uint8_t>((packed >> 8) & 0xff)),
              Build(static_cast<uint8_t>(packed & 0xff))
        {
        }

        constexpr uint32_t ToUInt32() const noexcept
        {
            return (static_cast<uint32_t>(Major) << 24) |
                   (static_cast<uint32_t>(Minor) << 16) |
                   (static_cast<uint32_t>(Patch) << 8) |
                   static_cast<uint32_t>(Build);
        }

        friend constexpr bool operator==(const Version& a, const Version& b) noexcept
        {
            return a.Major == b.Major && a.Minor == b.Minor && a.Patch == b.Patch && a.Build == b.Build;
        }

        friend constexpr bool operator!=(const Version& a, const Version& b) noexcept { return !(a == b); }

        friend constexpr bool operator<(const Version& a, const Version& b) noexcept
        {
            return a.ToUInt32() < b.ToUInt32();
        }

        friend constexpr bool operator>(const Version& a, const Version& b) noexcept { return b < a; }
        friend constexpr bool operator<=(const Version& a, const Version& b) noexcept { return !(b < a); }
        friend constexpr bool operator>=(const Version& a, const Version& b) noexcept { return !(a < b); }
    };
}
