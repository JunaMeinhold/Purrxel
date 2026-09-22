namespace VoxelEngine.Scenes
{
    public interface ILightComponent : IComponent
    {
        public bool CastShadows { get; set; }

        public float ShadowDistance { get; set; }
    }

    public interface IDirectionalLightComponent : ILightComponent
    {
    }
}