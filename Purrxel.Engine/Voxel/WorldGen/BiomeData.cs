namespace VoxelEngine.Voxel.WorldGen
{
    using VoxelEngine.Mathematics.Noise;
    using VoxelEngine.Voxel.WorldGen.Biomes;
    using static System.Runtime.InteropServices.JavaScript.JSType;

    public struct BiomeData
    {
        public int Octaves;
        public float Persistence;
        public float Amplitude;
        public float Redistribution;
        public int MinHeight;
        public int MaxHeight;

        public static void GetBiomeData(PerlinNoise noise, List<Biome> biomes, float x, float y, out int majorBiome, out int minorBiome, out float blend)
        {
            // Maybe tweak values.
            float v = noise.OctavePerlin2D(0.002f * x, 0.002f * y, 10, 0.25f, 3);

            // Saturate value to 0 .. 1 range.
            v = MathHelper.SaturateOctave(v, 10, 0.25f, 3);

            // Redistribution.
            v = MathF.Pow(v, 1f);

            // Lerp between 1 and count and remap to index.
            v = MathHelper.Lerp(1, biomes.Count, MathHelper.Clamp01(v)) - 1;

            majorBiome = (int)MathF.Truncate(v);
            minorBiome = (int)MathF.Round(v, MidpointRounding.ToEven);

            blend = v - majorBiome;

            // remap from 0.5 .. 1 to 0 .. 0.5
            if (blend >= 0.5f)
            {
                blend -= 0.5f;
            }
            else
            {
                blend = 0.5f - blend;
            }

            // remap from 0 .. 0.5 to 0 .. 1
            blend *= 2f;
        }

        public static BiomeData GetBlendedBiomeData(PerlinNoise noise, List<Biome> biomes, float globalX, float globalZ)
        {
            GetBiomeData(noise, biomes, globalX, globalZ, out int majorBiomeIdx, out int minorBiomeIdx, out float blend);
            Biome majorBiome = biomes[majorBiomeIdx];
            Biome minorBiome = biomes[minorBiomeIdx];
            float smoothstepBlend = MathHelper.Smoothstep(0, 1, blend);
            return new BiomeData
            {
                Octaves = (int)MathHelper.Lerp(majorBiome.Octaves, minorBiome.Octaves, smoothstepBlend),
                Persistence = MathHelper.Lerp(majorBiome.Persistence, minorBiome.Persistence, smoothstepBlend),
                Amplitude = MathHelper.Lerp(majorBiome.Amplitude, minorBiome.Amplitude, smoothstepBlend),
                Redistribution = MathHelper.Lerp(majorBiome.Redistribution, minorBiome.Redistribution, smoothstepBlend),
                MinHeight = (int)MathHelper.Lerp(majorBiome.MinHeight, minorBiome.MinHeight, smoothstepBlend),
                MaxHeight = (int)MathHelper.Lerp(majorBiome.MaxHeight, minorBiome.MaxHeight, smoothstepBlend)
            };
        }

        public readonly float ComputeHeight(PerlinNoise noise, float globalX, float globalZ)
        {
            float v = noise.OctavePerlin2D(0.008f * globalX, 0.008f * globalZ, Octaves, Persistence, Amplitude);
            v = MathHelper.SaturateOctave(v, Octaves, Persistence, Amplitude);
            v = MathF.Pow(v, Redistribution);

            return MathHelper.Lerp(MinHeight, MaxHeight, MathHelper.Clamp01(v));
        }
    }
}