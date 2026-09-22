namespace Purrxel.Engine.Voxel
{
    using System.Numerics;
    using System.Runtime.CompilerServices;

    public unsafe struct ChunkStorage
    {
        public enum StorageType : byte
        {
            Empty,
            U4 = 4,
            U8 = 8,
            U16 = 16,
        }

        public StorageType Type;
        public byte* Data;
        public ushort* Palette;
        public uint PaletteSize;

        public const ushort INVALID_PALETTE_INDEX = ushort.MaxValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly uint GetBitStride()
        {
            return (uint)Type;
        }

        public readonly uint MaxPaletteSize()
        {
            return Type switch
            {
                StorageType.U4 => 16,
                StorageType.U8 => 256,
                _ => 0,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Broadcast16To64(ushort value)
        {
            return 0x0001000100010001ul * (ulong)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ushort IdToPaletteIndex(ushort id)
        {
            ulong* pPalette = (ulong*)Palette;
            ulong pattern = Broadcast16To64(id);
            uint size = PaletteSize / 4;

            for (uint i = 0; i < size; ++i)
            {
                ulong cmp = pPalette[i] ^ pattern;
                ulong hasZero = (cmp - 0x0001000100010001UL) & ~cmp & 0x8000800080008000UL;
                if (hasZero != 0)
                {
                    var lane = BitOperations.TrailingZeroCount(hasZero) >> 4;
                    return (ushort)(i * 4 + lane);
                }
            }

            return INVALID_PALETTE_INDEX;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort PaletteIndexToId(ushort paletteIndex)
        {
            return Palette[paletteIndex];
        }

        public ushort AddIndex(ushort type, byte* minY, byte* maxY)
        {
            var newSize = PaletteSize + 1u;
            var maxSize = MaxPaletteSize();
            if (newSize > maxSize)
            {
                ResizeUp(minY, maxY);
                if (Type == StorageType.U16)
                {
                    return INVALID_PALETTE_INDEX;
                }
            }
            var index = (ushort)PaletteSize;
            Palette[index] = type;
            PaletteSize = newSize;
            return index;
        }

        private void ResizeUp(byte* minY, byte* maxY)
        {
            byte* oldData = Data;

            switch (Type)
            {
                case StorageType.Empty:
                    {
                        Palette = AllocT<ushort>(16);
                        ZeroMemoryT(Palette, 16);
                        PaletteSize = 0;
                        Data = AllocT<byte>(Chunk.CHUNK_SIZE_CUBED / 2);
                        ZeroMemoryT(Data, Chunk.CHUNK_SIZE_CUBED / 2);
                        Type = StorageType.U4;
                    }
                    break;

                case StorageType.U4:
                    {
                        ushort* newPalette = AllocT<ushort>(256);
                        MemcpyT(Palette, newPalette, PaletteSize);
                        Free(Palette);
                        Palette = newPalette;

                        Data = AllocT<byte>(Chunk.CHUNK_SIZE_CUBED);
                        ZeroMemoryT(Data, Chunk.CHUNK_SIZE_CUBED);
                        Type = StorageType.U8;

                        for (uint z = 0; z < Chunk.CHUNK_SIZE; ++z)
                        {
                            var zShift = z << Chunk.CHUNK_SHIFT_Z;
                            var heightMapAccess = z * Chunk.CHUNK_SIZE;
                            for (uint x = 0; x < Chunk.CHUNK_SIZE; ++x)
                            {
                                var xShift = x << Chunk.CHUNK_SHIFT_Y;
                                var heightIdx = heightMapAccess++;

                                var startY = minY[heightIdx];
                                var endY = maxY[heightIdx];

                                var access = xShift + zShift + startY;

                                for (uint y = startY; y < endY; ++y, ++access)
                                {
                                    var bitPosition = access << 2;
                                    var bytePosition = bitPosition >> 3;
                                    var bitOffset = (int)(bitPosition & 0b111);
                                    var paletteIndex = (ushort)((oldData[bytePosition] >> bitOffset) & 0b1111);
                                    Data[access] = (byte)paletteIndex;
                                }
                            }
                        }
                    }
                    break;

                case StorageType.U8:
                    {
                        Data = AllocT<byte>(Chunk.CHUNK_SIZE_CUBED * 2);
                        ZeroMemoryT(Data, Chunk.CHUNK_SIZE_CUBED * 2);
                        Type = StorageType.U16;

                        for (uint z = 0; z < Chunk.CHUNK_SIZE; ++z)
                        {
                            var zShift = z << Chunk.CHUNK_SHIFT_Z;
                            var heightMapAccess = z * Chunk.CHUNK_SIZE;
                            for (uint x = 0; x < Chunk.CHUNK_SIZE; ++x)
                            {
                                var xShift = x << Chunk.CHUNK_SHIFT_Y;
                                var heightIdx = heightMapAccess++;

                                var startY = minY[heightIdx];
                                var endY = maxY[heightIdx];

                                var access = xShift + zShift + startY;

                                for (uint y = startY; y < endY; ++y, ++access)
                                {
                                    var paletteIndex = oldData[access];
                                    var blockId = Palette[paletteIndex];
                                    ((ushort*)Data)[access] = blockId;
                                }
                            }
                        }

                        Free(Palette);
                        Palette = null;
                        PaletteSize = 0;
                    }
                    break;

                default:
                    return;
            }

            Free(oldData);
        }

        public void SetBlock(uint index, Block block, byte* minY, byte* maxY)
        {
            if (Type == StorageType.U16)
            {
                var p = (ushort*)Data;
                p[index] = block.Type;
                return;
            }
            var paletteIndex = IdToPaletteIndex(block.Type);

            if (paletteIndex == INVALID_PALETTE_INDEX)
            {
                paletteIndex = AddIndex(block.Type, minY, maxY);
                if (paletteIndex == INVALID_PALETTE_INDEX)
                {
                    var p = (ushort*)Data;
                    p[index] = block.Type;
                    return;
                }
            }

            var bitStride = GetBitStride();

            var bitPosition = index * bitStride;
            var bytePosition = bitPosition >> 3;
            var bitOffset = (int)(bitPosition & 0b111);
            var ptr = Data + bytePosition;

            switch (Type)
            {
                case StorageType.U4:
                    *ptr &= (byte)~(0b1111 << bitOffset);
                    *ptr |= (byte)(paletteIndex << bitOffset);
                    break;

                case StorageType.U8:
                    *ptr = (byte)paletteIndex;
                    break;

                case StorageType.U16:
                    *(ushort*)ptr = paletteIndex;
                    break;
            }
        }

        public Block GetBlock(uint index)
        {
            if (Type == StorageType.U16)
            {
                var p = (ushort*)Data;
                return new(p[index]);
            }
            var bitStride = GetBitStride();
            var bitPosition = index * bitStride;
            var bytePosition = bitPosition >> 3;
            var bitOffset = (int)(bitPosition & 0b111);
            var ptr = Data + bytePosition;
            ushort paletteIndex = 0;

            switch (Type)
            {
                case StorageType.U4:
                    paletteIndex = (ushort)((*ptr >> bitOffset) & 0b1111);
                    break;

                case StorageType.U8:
                    paletteIndex = *ptr;
                    break;

                case StorageType.U16:
                    paletteIndex = *(ushort*)ptr;
                    break;
            }

            ushort blockId = PaletteIndexToId(paletteIndex);
            return new(blockId);
        }

        public void Release()
        {
            if (Data != null)
            {
                Free(Data);
                Data = null;
            }
            if (Palette != null)
            {
                Free(Palette);
                Palette = null;
            }
            PaletteSize = 0;
            Type = StorageType.Empty;
        }
    }
}