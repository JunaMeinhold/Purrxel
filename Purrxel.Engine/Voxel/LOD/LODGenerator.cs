namespace VoxelEngine.Voxel.LOD
{
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.Intrinsics;
    using System.Runtime.Intrinsics.Arm;
    using System.Runtime.Intrinsics.X86;

    public unsafe static class LODGenerator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LevelToBasis(int level)
        {
            return level switch
            {
                0 => 16,
                1 => 8,
                2 => 4,
                3 => 2,
                4 => 1,
                _ => 0,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LevelToZShift(int level)
        {
            return level switch
            {
                0 => 8,
                1 => 6,
                2 => 4,
                3 => 2,
                4 => 0,
                _ => 0,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LevelToYShift(int level)
        {
            return level switch
            {
                0 => 4,
                1 => 3,
                2 => 2,
                3 => 1,
                4 => 0,
                _ => 0,
            };
        }

        public static void Simplify(Block* blocks, byte* minYs, byte* maxYs, int level, Block* dst, byte* dstMinYs, byte* dstMaxYs)
        {
            int z = 0;

            int basis = LevelToBasis(level);
            int zShift = LevelToZShift(level);
            int yShift = LevelToYShift(level);
            int zShiftLower = LevelToZShift(level + 1);
            int yShiftLower = LevelToYShift(level + 1);

            Block* tmp = stackalloc Block[8];
            byte* tmpMin = stackalloc byte[16];
            MemsetT(tmpMin, 0xFF, 16);
            byte* tmpMax = stackalloc byte[16];
            MemsetT(tmpMax, 0x00, 16);

            for (; z < basis; z += 2)
            {
                int heightMapAccess = z << yShift;
                int heightMapAccessLower = (z / 2) << yShiftLower;

                for (int x = 0; x < basis; x += 2)
                {
                    int heightAccess = heightMapAccess + x;
                    int heightAccessLower = heightMapAccessLower + x / 2;
                    GatherHeight(heightAccess, minYs, tmpMin, yShift);
                    byte minY = SelectHeightMin(tmpMin);

                    GatherHeight(heightAccess, maxYs, tmpMax, yShift);
                    byte maxY = SelectHeightMax(tmpMax);

                    int alignedMinY = minY & ~1;
                    int alignedMaxY = (maxY + 1) & ~1;

                    for (int y = alignedMinY; y < alignedMaxY; y += 2)
                    {
                        GatherVoxel(x, y, z, blocks, tmp, zShift, yShift);
                        var block = SelectVoxel(tmp);
                        var dstIndex = ToIndex(x, y, z, zShiftLower, yShiftLower);
                        dst[dstIndex] = block;
                        if (!block.IsAir)
                        {
                            dstMinYs[heightAccessLower] = (byte)Math.Min(dstMinYs[heightAccessLower], y);
                            dstMaxYs[heightAccessLower] = (byte)Math.Max(dstMaxYs[heightAccessLower], y);
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToIndex(int x, int y, int z, int zShift, int yShift)
        {
            return (z << zShift) + (x << yShift) + y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GatherVoxel(int x, int y, int z, Block* src, Block* dst, int zShift, int yShift)
        {
            dst[0] = src[ToIndex(x, y, z, zShift, yShift)];
            dst[1] = src[ToIndex(x + 1, y, z, zShift, yShift)];
            dst[2] = src[ToIndex(x, y, z + 1, zShift, yShift)];
            dst[3] = src[ToIndex(x + 1, y, z + 1, zShift, yShift)];
            dst[4] = src[ToIndex(x, y + 1, z, zShift, yShift)];
            dst[5] = src[ToIndex(x + 1, y + 1, z, zShift, yShift)];
            dst[6] = src[ToIndex(x, y + 1, z + 1, zShift, yShift)];
            dst[7] = src[ToIndex(x + 1, y + 1, z + 1, zShift, yShift)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GatherHeight(int heightAccess, byte* src, byte* dst, int yShift)
        {
            dst[0] = src[heightAccess];
            dst[1] = src[heightAccess + 1];
            dst[2] = src[heightAccess + (1 << yShift)];
            dst[3] = src[heightAccess + 1 + (1 << yShift)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte SelectHeightMin(byte* h)
        {
            if (AdvSimd.Arm64.IsSupported)
            {
                Vector128<byte> v = Vector128.Load(h);
                return AdvSimd.Arm64.MinAcross(v).GetElement(0);
            }
            else if (Sse2.IsSupported)
            {
                Vector128<byte> v = Vector128.Load(h);
                v = Sse2.Min(v, Sse2.ShiftRightLogical128BitLane(v, 2));
                v = Sse2.Min(v, Sse2.ShiftRightLogical128BitLane(v, 1));
                return v.GetElement(0);
            }
            else // scalar fallback
            {
                return Math.Min(h[0], Math.Min(h[1], Math.Min(h[2], h[3])));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte SelectHeightMax(byte* h)
        {
            if (AdvSimd.Arm64.IsSupported)
            {
                Vector128<byte> v = Vector128.Load(h);
                return AdvSimd.Arm64.MaxAcross(v).GetElement(0);
            }
            else if (Sse2.IsSupported)
            {
                Vector128<byte> v = Vector128.Load(h);
                v = Sse2.Max(v, Sse2.ShiftRightLogical128BitLane(v, 2));
                v = Sse2.Max(v, Sse2.ShiftRightLogical128BitLane(v, 1));
                return v.GetElement(0);
            }
            else // scalar fallback
            {
                return Math.Max(h[0], Math.Max(h[1], Math.Max(h[2], h[3])));
            }
        }

        private const int SolidThreshold = 4;

        public static Block SelectVoxel(Block* cell)
        {
            Vector128<ushort> v = Vector128.Load((ushort*)cell);

            uint solidMask = ~Vector128.Equals(v, Vector128<ushort>.Zero).ExtractMostSignificantBits() & 0xFF;
            if (BitOperations.PopCount(solidMask) < SolidThreshold)
            {
                return Block.Air;
            }

            Block best = Block.Air;
            int bestCount = 0;

            for (int i = 7; i >= 0; i--) // top layer first for the surface tie-break
            {
                if ((solidMask & (1u << i)) == 0) continue;

                int count = BitOperations.PopCount(Vector128.Equals(v, Vector128.Create(cell[i].Type)).ExtractMostSignificantBits());

                if (count > bestCount)
                {
                    bestCount = count;
                    best = cell[i];
                }
            }

            return best;
        }
    }
}