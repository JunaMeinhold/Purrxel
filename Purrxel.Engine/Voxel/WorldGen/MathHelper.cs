namespace VoxelEngine.Voxel.WorldGen
{
    using Hexa.NET.Mathematics;

    public static class MathHelper
    {
        public static Point3 Abs(Point3 a)
        {
            return new(Math.Abs(a.X), Math.Abs(a.Y), Math.Abs(a.Z));
        }

        public static Point3 Min(Point3 a, Point3 b)
        {
            return new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
        }

        public static Point3 Max(Point3 a, Point3 b)
        {
            return new(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));
        }

        public static float Frac(float f)
        {
            return f - float.Truncate(f);
        }

        public static float Lerp(float a, float b, float v)
        {
            return a + v * (b - a);
        }

        public static float Smoothstep(float a, float b, float v)
        {
            // Scale, and clamp x to 0..1 range
            v = Clamp01((v - a) / (b - a));

            return v * v * (3.0f - 2.0f * v);
        }

        public static float Saturate(float value)
        {
            return (float)((value + 1.0) / 2.0);
        }

        public static float SaturateOctave(float value, int octaves, float persistence, float amplitude)
        {
            float result = 0;

            for (int i = 0; i < octaves; i++)
            {
                result += amplitude;
                amplitude *= persistence;
            }

            return value / result;
        }

        public static float Clamp01(float value)
        {
            return Math.Clamp(value, 0.0f, 1.0f);
        }

        public static float SaturateClamp01(float value)
        {
            value = Clamp01(value);
            return Saturate(value);
        }
    }
}