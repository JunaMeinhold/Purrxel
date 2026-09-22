namespace App.Renderers
{
    using System.Numerics;

    public struct CBWeather
    {
        public Vector4 LightDir;
        public Vector3 A;

#pragma warning disable CS0649 // 16 Byte padding for GPU Constant buffers.
        public float PaddA;
        public Vector3 B;
        public float PaddB;
        public Vector3 C;
        public float PaddC;
        public Vector3 D;
        public float PaddD;
        public Vector3 E;
        public float PaddE;
        public Vector3 F;
        public float PaddF;
        public Vector3 G;
        public float PaddG;
        public Vector3 H;
        public float PaddH;
        public Vector3 I;
        public float PaddI;
        public Vector3 Z;
        public float PaddZ;
#pragma warning restore CS0649
    }
}