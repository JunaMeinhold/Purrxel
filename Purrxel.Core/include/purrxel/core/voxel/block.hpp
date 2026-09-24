#pragma once

#include "purrxel/core/io/stream.hpp"

namespace Purrxel::Core::Voxel
{
    struct Block
    {
        uint16_t Type = 0;

        constexpr Block() = default;
        constexpr explicit Block(uint16_t type) : Type(type) {}

        constexpr bool IsAir() const noexcept { return Type == 0; }

        friend constexpr bool operator==(const Block& a, const Block& b) noexcept { return a.Type == b.Type; }
        friend constexpr bool operator!=(const Block& a, const Block& b) noexcept { return !(a == b); }

        void Write(Stream* stream) const { stream->WriteLittleEndian(Type); }
        void Read(Stream* stream) { Type = stream->ReadLittleEndian<uint16_t>(); }
    };

    inline constexpr Block AirBlock{0};
}
