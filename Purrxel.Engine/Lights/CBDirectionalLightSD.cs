namespace VoxelEngine.Lightning
{
    using System.Numerics;
    using System.Runtime.CompilerServices;

    public unsafe struct CSMShadowParams
    {
        public Matrix4x4 View0;
        public Matrix4x4 View1;
        public Matrix4x4 View2;
        public Matrix4x4 View3;
        public Matrix4x4 View4;
        public Matrix4x4 View5;
        public Matrix4x4 View6;
        public Matrix4x4 View7;
        public uint CascadeCount;
        public uint ActiveCascades;
        public float ESMExponent;
        public uint Padding;

        public CSMShadowParams(Matrix4x4 view0, Matrix4x4 view1, Matrix4x4 view2, Matrix4x4 view3, Matrix4x4 view4, Matrix4x4 view5, Matrix4x4 view6, Matrix4x4 view7, uint cascadesCount, uint activeCascades, float esmExponent)
        {
            View0 = view0;
            View1 = view1;
            View2 = view2;
            View3 = view3;
            View4 = view4;
            View5 = view5;
            View6 = view6;
            View7 = view7;
            CascadeCount = cascadesCount;
            ActiveCascades = activeCascades;
            ESMExponent = esmExponent;
        }

        public CSMShadowParams(Matrix4x4* views, uint count)
        {
            fixed (CSMShadowParams* self = &this)
            {
                Matrix4x4* dest = (Matrix4x4*)self;
                Unsafe.CopyBlock(dest, views, (uint)(count * sizeof(Matrix4x4)));
            }
            CascadeCount = count;
        }

        public unsafe Matrix4x4 this[uint index]
        {
            get => ((Matrix4x4*)Unsafe.AsPointer(ref this))[index];
            set => ((Matrix4x4*)Unsafe.AsPointer(ref this))[index] = value;
        }

        public unsafe Matrix4x4 this[int index]
        {
            get => ((Matrix4x4*)Unsafe.AsPointer(ref this))[index];
            set => ((Matrix4x4*)Unsafe.AsPointer(ref this))[index] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Matrix4x4* GetViews(CSMShadowParams* data)
        {
            return (Matrix4x4*)data;
        }
    }

    public struct CBDirectionalLightSD
    {
        public static readonly unsafe int CascadePointerOffset = sizeof(Matrix4x4) * 8;

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
        public Vector4 Color;
        public Vector3 Direction;
        public int CastShadows;
        public uint CascadeCount;
        public float LightBleedingReduction;
        public Vector2 Padding;

        public CBDirectionalLightSD()
        {
            Color = Vector4.Zero;
            Direction = Vector3.Zero;
            CastShadows = default;
            CascadeCount = 4;
        }

        public CBDirectionalLightSD(DirectionalLight light)
        {
            Color = light.Color;
            Direction = light.Transform.Forward;
            CastShadows = light.CastShadows ? 1 : 0;
            CascadeCount = 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Matrix4x4* GetViews(CBDirectionalLightSD* data)
        {
            return &data->View1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float* GetCascades(CBDirectionalLightSD* data)
        {
            return &data->Cascade1;
        }
    }
}