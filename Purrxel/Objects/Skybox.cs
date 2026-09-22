namespace App.Objects
{
    using App.Renderers;
    using VoxelEngine.Scenes;

    public class Skybox : GameObject
    {
        private readonly SkyboxRenderer component;

        public Skybox()
        {
            component = new SkyboxRenderer();
            component.TexturePath = "skybox.dds";
            AddComponent(component);
        }
    }
}