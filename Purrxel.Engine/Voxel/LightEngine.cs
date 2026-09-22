namespace VoxelEngine.Voxel
{
    using Hexa.NET.Mathematics;
    using VoxelEngine.Core;
    using VoxelEngine.Voxel.Blocks;

    public unsafe struct LightMap
    {
        public byte* Data;

        public readonly bool IsAllocated => Data != null;

        public void Alloc()
        {
            Data = AllocT<byte>(Chunk.CHUNK_SIZE_CUBED / 2);
        }

        public void Release()
        {
            if (Data != null)
            {
                Free(Data);
                Data = null;
            }
        }

        public byte this[int index]
        {
            get
            {
                int byteIndex = index >> 1;
                int shift = (index & 1) << 2;
                return (byte)((Data[byteIndex] >> shift) & 0xF);
            }
            set
            {
                int byteIndex = index >> 1;
                int shift = (index & 1) << 2;
                Data[byteIndex] &= (byte)~(0xF << shift);
                Data[byteIndex] |= (byte)((value & 0xF) << shift);
            }
        }
    }

    public unsafe class LightEngine
    {
        public void Compute(Chunk* chunk, LightMap map)
        {
            byte lightLevelSky = ComputeSkyLightLevel();
            for (int i = 0; i < Chunk.CHUNK_SIZE_SQUARED; i++)
            {
                var x = i & 15;
                var z = i >> 4;
                var max = chunk->MaxY[i];
                if (max == 0) continue;

                FloodFillY(lightLevelSky, x, max, z, chunk, ref map);
            }
        }

        private void FloodFillY(byte lightLevelSky, int x, byte max, int z, Chunk* chunk, ref LightMap map)
        {
            Block* voxels = chunk->Data;
            for (int i = max - 1; i >= 0; i--)
            {
                int index = Extensions.MapToIndex(x, i, z);
                map[index] = lightLevelSky;
                Block block = voxels[index];
                if (block == Block.Air) continue;
                if (BlockRegistry.IsSolid(block)) break;
                lightLevelSky--;
                if (lightLevelSky == 0) return;
            }
        }

        public static byte ComputeSkyLightLevel()
        {
            float gameTime = Time.GameTimeNormalized;
            gameTime = Math.Abs(MathUtil.Map01ToN1P1(gameTime)); // maps/wraps 0..1 (24h) to 0..1..0 where 1 == 12h

            if (gameTime > 0.6f)
            {
                return 15;
            }

            if (gameTime < 0.4f)
            {
                return 0;
            }

            gameTime = MathUtil.Remap(gameTime, 0.4f, 0.6f, 0, 1);

            return (byte)MathUtil.Lerp(0, 15, gameTime);
        }
    }
}