namespace VoxelEngine.Voxel.WorldGen
{
    using System.Runtime.CompilerServices;
    using Hexa.NET.Mathematics;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Mathematics.Noise;

    public struct CaveCoefficients
    {
        public float A;
        public float B;
        public float InvB;
        public float V;

        public static CaveCoefficients Generate(PerlinNoise noise, float x, float z, float minHeight, float maxHeight)
        {
            CaveCoefficients coefficients = default;
            float caveNoiseScaleA = 0.004f;
            float caveNoiseScaleB = 0.008f;
            float caveNoiseScaleV = 0.008f;

            float noiseA = noise.OctavePerlin2DSat(x * caveNoiseScaleA, z * caveNoiseScaleA, 3, 0.8f, 2);

            noiseA = MathF.Pow(noiseA, 2);
            noiseA = MathHelper.Clamp01(noiseA);

            float A = MathUtil.Lerp(minHeight, maxHeight, noiseA);
            float noiseB = MathUtil.Lerp(minHeight, maxHeight, noise.OctavePerlin2DSat(x * caveNoiseScaleB, z * caveNoiseScaleB, 2, 0.4f, 4));

            // 2) Compute how large B can be while still satisfying A-B >= minHeight and A+B <= maxHeight
            float maxBLower = A - minHeight;     // so that A - B >= minHeight
            float maxBUpper = maxHeight - A;     // so that A + B <= maxHeight
            float maxAllowedB = MathF.Min(maxBLower, maxBUpper);

            // 3) Clamp the rawB
            float B = MathF.Min(noiseB, maxAllowedB);
            B = MathF.Max(B, 0.0001f); // or some small positive value to avoid division by zero

            coefficients.A = A;
            coefficients.B = B;
            coefficients.InvB = 1 / coefficients.B;

            float V = noise.OctavePerlin2DSat(x * caveNoiseScaleV, z * caveNoiseScaleV, 3, 0.4f, 2);
            V = MathF.Pow(V, 2);
            V = MathUtil.Clamp01(V);

            coefficients.V = V;

            return coefficients;
        }

        /// <summary>
        /// Computes a normalized parabolic value based on the given height.
        /// Formula: f(x) = 1 - (((x - A) / B) ^ 2)
        /// </summary>
        /// <param name="height">The height value to evaluate.</param>
        /// <returns>The normalized parabolic value in the range [0, 1].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float Parabolic(float height)
        {
            float v = (height - A) * InvB;
            return 1 - v * v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float ComputeValue(float height, float caveThresholdBase = 0.4f)
        {
            float result = V * Parabolic(height);

            if (result < caveThresholdBase) // clamp to zero.
            {
                result = 0;
            }

            // > 0 means cave.
            return result;
        }
    }
}