namespace VoxelEngine.Lights
{
    using System.Numerics;
    using VoxelEngine.Lightning;

    public struct LightData
    {
        public uint Type;

        public Vector4 Color;
        public Vector4 Position;
        public Vector4 Direction;
        public float Range;
        public int CastsShadows;
        public bool CascadedShadows;
        public int ShadowMapIndex;

        public LightData(DirectionalLight light)
        {
            Type = (uint)light.Type;
            Color = light.Color;
            Position = new(light.Transform.GlobalPosition, 1);
            Direction = new(light.Transform.Forward, 1);
            Range = light.Transform.Far;
            CastsShadows = light.CastShadows ? 1 : 0;
            CascadedShadows = true;
            ShadowMapIndex = light.ShadowMapIndex;
        }
    }
}