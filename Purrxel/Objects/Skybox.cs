namespace Purrxel.Objects
{
    using Purrxel.Engine.Scenes;
    using Purrxel.Renderers;

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