namespace VoxelEngine.Lights
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Lightning;

    public struct ShadowData
    {
        public Matrix4x4 View1;
        public Matrix4x4 View2;
        public Matrix4x4 View3;
        public Matrix4x4 View4;
        public Matrix4x4 View5;
        public Matrix4x4 View6;
        public Matrix4x4 View7;
        public Matrix4x4 View8;
        public float Cascade1;
        public float Cascade2;
        public float Cascade3;
        public float Cascade4;
        public float Cascade5;
        public float Cascade6;
        public float Cascade7;
        public float Cascade8;
        public float Size;
        public float Softness;
        public uint CascadeCount;
        public Vector4 Region1;
        public Vector4 Region2;
        public Vector4 Region3;
        public Vector4 Region4;
        public Vector4 Region5;
        public Vector4 Region6;
        public Vector4 Region7;
        public Vector4 Region8;
        public float Bias;
        public float SlopeBias;

        public ShadowData(DirectionalLight light, float size)
        {
            View1 = light.Transform.View;
            Size = size;
            Softness = light.LightBleedingReduction;
            CascadeCount = (uint)light.cascadeCount;
            Bias = 0;
            SlopeBias = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Matrix4x4* GetViews(ShadowData* data)
        {
            return &data->View1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float* GetCascades(ShadowData* data)
        {
            return &data->Cascade1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector4* GetRegions(ShadowData* data)
        {
            return &data->Region1;
        }
    }
}