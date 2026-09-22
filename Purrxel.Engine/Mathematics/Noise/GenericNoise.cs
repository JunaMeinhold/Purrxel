namespace VoxelEngine.Mathematics.Noise
{
    public class GenericNoise
    {
        private readonly double coefficient0;
        private readonly double coefficient1;
        private readonly double coefficient2;
        private readonly double coefficient3;

        public GenericNoise()
        {
            coefficient0 = 43758.5453123;
            coefficient1 = 12.9898;
            coefficient2 = 78.233;
            coefficient3 = 1.0;
        }

        public GenericNoise(int seed)
        {
            float factor = (float)seed / int.MaxValue;
            coefficient0 = 43758.5453123 * factor;
            coefficient1 = 12.9898 * factor;
            coefficient2 = 78.233 * factor;
            coefficient3 = 1.0 * factor;
        }

        private static double Frac(double v)
        {
            return v - double.Truncate(v);
        }

        private static double Dot(double x1, double y1, double x2, double y2)
        {
            return x1 * x2 + y1 * y2;
        }

        private static double Dot(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return x1 * x2 + y1 * y2 + z1 * z2;
        }

        public double Noise(double x)
        {
            return Frac(Math.Sin(x) * coefficient0);
        }

        public double Noise(double x, double y)
        {
            return Frac(Math.Sin(Dot(x, y, coefficient1, coefficient2)) * coefficient0);
        }

        public double Noise(double x, double y, double z)
        {
            return Frac(Math.Sin(Dot(x, y, z, coefficient1, coefficient2, coefficient3)) * coefficient0);
        }
    }
}