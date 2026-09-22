namespace App
{
    using App.Objects;
    using App.Renderers;
    using App.Renderers.Forward;
    using App.Scripts;
    using System.Numerics;
    using VoxelEngine.Lightning;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel;
    using VoxelEngine.Voxel.Blocks;
    using VoxelEngine.Voxel.WorldGen;

    public class MainScene
    {
        public static Scene Create()
        {
            Camera camera = new();
            camera.Far = 1000;
            camera.Near = 0.1f;
            camera.Transform.Position = new(0, 100, 0);
            camera.Transform.Rotation = new(0, 0, 0);

            Scene scene = new()
            {
                Camera = camera
            };

            DirectionalLight directionalLight = new()
            {
                Color = new Vector4(196 / 255f, 220 / 255f, 1, 1) * 1.4f
            };
            directionalLight.Transform.Far = 100;
            directionalLight.Transform.Rotation = new(0, 100, 0);
            directionalLight.CastShadows = true;

            scene.Add(directionalLight);

            scene.Add(camera);

            scene.Add(new Skybox());

            World world = new("world");

            world.Generator = new DefaultChunkGenerator(68458);
            world.AddComponent(new WorldController());

            WorldRenderer worldRenderer = new();
            WorldForwardRenderer worldForwardRenderer = new(worldRenderer);

            world.AddComponent(worldRenderer);
            world.AddComponent(worldForwardRenderer);
            scene.Add(world);

            CPlayer player = new(new(0, 74, 0));
            player.AddComponent(new BlockHighlightRenderer());
            player.AddComponent(new CrosshairRenderer()
            {
                TexturePath = "crosshair.png"
            });
            scene.Add(player);

            world.Player = player;

            BlockRegistry.Reset();
            BlockRegistry.RegisterBlock(new("Dirt", new("blocks/dirt.dds")));
            BlockRegistry.RegisterBlock(new("Stone", new("blocks/stone.dds")));
            BlockRegistry.RegisterBlock(new("Grass", new("blocks/grass_top.dds", "blocks/dirt.dds", "blocks/grass_side.dds")));
            BlockRegistry.RegisterBlock(new("Stonebrick", new("blocks/stonebrick.dds")));
            BlockRegistry.RegisterBlock(new("Cobblestone", new("blocks/cobblestone.dds")));
            BlockRegistry.RegisterBlock(new("Brick", new("blocks/brick.dds")));
            BlockRegistry.RegisterBlock(new("Iron Block", new("blocks/iron_block.dds")));
            BlockRegistry.RegisterBlock(new("Oak Log", new("blocks/log_oak_top.dds", "blocks/log_oak_top.dds", "blocks/log_oak.dds")));
            BlockRegistry.RegisterBlock(new("Oak Planks", new("blocks/planks_oak.dds")));
            BlockRegistry.RegisterBlock(new("Oak Leaves", new("blocks/oak_leaves.dds"), true));
            BlockRegistry.RegisterBlock(new("Quartz Block", new("blocks/quartz_block.dds")));
            BlockRegistry.RegisterBlock(new("Water", new("blocks/water.dds"), transparent: true));

            return scene;
        }
    }
}