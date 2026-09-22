namespace VoxelEngine.Lightning
{
    using System.Numerics;
    using VoxelEngine.Core;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Lights;
    using VoxelEngine.Scenes;

    public abstract class Light : GameObject
    {
        protected const float DegToRadFactor = 0.0174532925f;
        protected Vector4 color = Vector4.One;
        public int ShadowMapIndex;
        private bool castShadows;

        public Vector4 Color
        {
            get => color;
            set
            {
                SetAndNotifyWithEqualsTest(ref color, value);
            }
        }

        public bool CastShadows
        {
            get => castShadows;
            set
            {
                if (SetAndNotifyWithEqualsTest(ref castShadows, value))
                {
                    CastsShadowsChanged?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<Light, bool>? CastsShadowsChanged;

        public abstract LightType Type { get; }

        public abstract bool HasShadowMap { get; }

        public abstract void CreateShadowMap();

        public abstract void DestroyShadowMap();

        public abstract void UpdateShadowBuffer(StructuredBuffer<ShadowData> shadowDataBuffer, Camera camera);
    }
}