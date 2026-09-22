namespace Purrxel.Renderers
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using Hexa.NET.DXGI;
    using Hexa.NET.Mathematics;
    using Purrxel.Engine.Graphics;
    using Purrxel.Engine.Graphics.Buffers;
    using Purrxel.Engine.Graphics.D3D11;
    using Purrxel.Engine.Lights;
    using Purrxel.Engine.Scenes;
    using Purrxel.Engine.Voxel;
    using Purrxel.Engine.Voxel.Blocks;
    using System;
    using System.Numerics;
    using VoxelEngine.Voxel;

    public struct WorldData
    {
        public Vector3 chunkOffset;
        public float padd;

        public WorldData(Vector3 chunkOffset)
        {
            this.chunkOffset = chunkOffset;
            padd = 0;
        }

        public WorldData(Point2 chunkOffset, Vector3 globalPosition)
        {
            this.chunkOffset = new Point3(chunkOffset.X, 0, chunkOffset.Y) * 16 - globalPosition;
            padd = 0;
        }
    }

    public unsafe class WorldForwardRenderer : BaseRenderComponent
    {
        private readonly WorldRenderer shared;
        private Texture2D textures;
        private SamplerState samplerState;

        private World? world;
        private GraphicsPipelineState geometry;
        private ConstantBuffer<Matrix4x4> mvpBuffer;
        private ConstantBuffer<WorldData> worldDataBuffer;
        private ConstantBuffer<BlockDescriptionPacked> blockBuffer;
        private bool dirty = true;

        public WorldForwardRenderer(WorldRenderer shared)
        {
            this.shared = shared;
        }

        public override int QueueIndex { get; } = (int)RenderQueueIndex.Transparent;

        public override void Awake()
        {
            world = (World)GameObject;
            textures = shared.textures;
            samplerState = shared.samplerState;
            mvpBuffer = shared.mvpBuffer;
            worldDataBuffer = shared.worldDataBuffer;
            blockBuffer = shared.blockBuffer;

            geometry = GraphicsPipelineState.Create(new GraphicsPipelineDesc()
            {
                VertexShader = "forward/voxel/vs.hlsl",
                PixelShader = "forward/voxel/ps.hlsl",
            }, new()
            {
                DepthStencil = DepthStencilDescription.Default,
                Rasterizer = RasterizerDescription.CullBack,
                Blend = new BlendDescription(Blend.SrcAlpha, Blend.InvSrcAlpha, Blend.One, Blend.InvSrcAlpha),
                InputElements = (
                [
                    new("POSITION", 0, Format.R32Uint, 0, -1, InputClassification.PerVertexData, 0),
                    new("POSITION", 1, Format.R32Sint, 0, -1, InputClassification.PerVertexData, 0),
                    new("COLOR", 0, Format.R8G8B8A8Unorm, 0, -1, InputClassification.PerVertexData, 0),
                ])
            });

            geometry.Bindings.SetSRV("shaderTexture", textures);
            geometry.Bindings.SetSampler("Sampler", samplerState);
            geometry.Bindings.SetCBV("ModelBuffer", mvpBuffer);
            geometry.Bindings.SetCBV("WorldData", worldDataBuffer);
            geometry.Bindings.SetCBV("TexData", blockBuffer);
        }

        public override void Destroy()
        {
            geometry.Dispose();
        }

        public override void Draw(GraphicsContext context, PassIdentifer pass, Camera camera, object? parameter)
        {
            if (pass == PassIdentifer.ForwardPass)
            {
                ForwardPass(context, camera);
            }
        }

        private void Update(GraphicsContext context)
        {
            if (!dirty) return;
            dirty = false;

            worldDataBuffer.Update(context, new WorldData() { chunkOffset = Vector3.Zero });
            blockBuffer.UpdateRange(context, BlockRegistry.GetDescriptionPackeds().ToArray());
        }

        private void ForwardPass(GraphicsContext context, Camera camera)
        {
            if (world == null) return;
            Update(context);
            context.SetGraphicsPipelineState(geometry);
            var frustum = camera.RelFrustum;

            var comparer = RenderRegionZComparer.Instance;
            comparer.CameraPosition = camera.Transform.GlobalPosition;
            world.LoadedRenderRegions.Sort(comparer);
            for (int j = 0; j < world.LoadedRenderRegions.Count; j++)
            {
                RenderRegion region = world.LoadedRenderRegions[j];
                BoundingBox box = new(region.BoundingBox.Min - camera.Transform.GlobalPosition, region.BoundingBox.Max - camera.Transform.GlobalPosition);
                if (frustum.Intersects(box))
                {
                    if (region.BindTransparent(context))
                    {
                        worldDataBuffer.Update(context, new WorldData(region.Offset, camera.Transform.GlobalPosition));
                        context.DrawInstanced((uint)region.TransparentVertexBuffer.VertexCount, 1, 0, 0);
                    }
                }
            }
            context.SetGraphicsPipelineState(null);
        }
    }

    public class RenderRegionZComparer : IComparer<RenderRegion>
    {
        public static readonly RenderRegionZComparer Instance = new();

        public Vector3 CameraPosition;

        public int Compare(RenderRegion? x, RenderRegion? y)
        {
            if (x == null || y == null) return 0;
            float distA = (x.BoundingBox.Center - CameraPosition).LengthSquared();
            float distB = (y.BoundingBox.Center - CameraPosition).LengthSquared();
            return distB.CompareTo(distA);
        }
    }

    public unsafe class WorldRenderer : BaseRenderComponent
    {
        internal Texture2D textures;
        internal SamplerState samplerState;

        private GraphicsPipelineState geometry;
        private GraphicsPipelineState csmPass;
        private World? world;
        private bool debugChunksRegion;
        internal ConstantBuffer<Matrix4x4> mvpBuffer;
        internal ConstantBuffer<WorldData> worldDataBuffer;
        internal ConstantBuffer<BlockDescriptionPacked> blockBuffer;

        public override int QueueIndex { get; } = (int)RenderQueueIndex.Geometry;

        public bool DebugChunksRegion { get => debugChunksRegion; set => debugChunksRegion = value; }

        public override void Awake()
        {
            if (GameObject is not World world)
            {
                throw new InvalidOperationException("WorldRenderer only works on World");
            }
            this.world = world;

            textures = new([.. BlockRegistry.Textures]);
            samplerState = new SamplerState(SamplerStateDescription.PointWrap);
            blockBuffer = new(256, CpuAccessFlags.Write);

            geometry = GraphicsPipelineState.Create(new GraphicsPipelineDesc()
            {
                VertexShader = "deferred/voxel/vs.hlsl",
                PixelShader = "deferred/voxel/ps.hlsl",
            }, new()
            {
                DepthStencil = DepthStencilDescription.Default,
                Rasterizer = RasterizerDescription.CullBack,
                Blend = BlendDescription.Opaque,
                InputElements = (
                [
                    new("POSITION", 0, Format.R32Uint, 0, -1, InputClassification.PerVertexData, 0),
                    new("POSITION", 1, Format.R32Sint, 0, -1, InputClassification.PerVertexData, 0),
                    new("COLOR", 0, Format.R8G8B8A8Unorm, 0, -1, InputClassification.PerVertexData, 0),
                ])
            });

            csmPass = GraphicsPipelineState.Create(new GraphicsPipelineDesc()
            {
                VertexShader = "forward/csm/voxel/vs.hlsl",
                GeometryShader = "forward/csm/voxel/gs.hlsl",
                PixelShader = "forward/csm/voxel/ps.hlsl",
            }, new()
            {
                DepthStencil = DepthStencilDescription.Default,
                Rasterizer = RasterizerDescription.CullNone,
                Blend = BlendDescription.Opaque,
                Topology = PrimitiveTopology.Trianglelist,
            });

            mvpBuffer = new(CpuAccessFlags.Write);
            worldDataBuffer = new(CpuAccessFlags.Write);

            geometry.Bindings.SetSRV("shaderTexture", textures);
            geometry.Bindings.SetSampler("Sampler", samplerState);
            geometry.Bindings.SetCBV("ModelBuffer", mvpBuffer);
            geometry.Bindings.SetCBV("WorldData", worldDataBuffer);
            geometry.Bindings.SetCBV("TexData", blockBuffer);

            csmPass.Bindings.SetSRV("shaderTexture", textures);
            csmPass.Bindings.SetSampler("Sampler", samplerState);
            csmPass.Bindings.SetCBV("MatrixBuffer", mvpBuffer);
            csmPass.Bindings.SetCBV("WorldData", worldDataBuffer);
            csmPass.Bindings.SetCBV("TexData", blockBuffer);
        }

        public override void Destroy()
        {
            textures.Dispose();
            samplerState.Dispose();
            mvpBuffer.Dispose();
            worldDataBuffer.Dispose();
            blockBuffer.Dispose();
            geometry.Dispose();
            csmPass.Dispose();
        }

        public override void Draw(GraphicsContext context, PassIdentifer pass, Camera camera, object? parameter)
        {
            if (pass == PassIdentifer.DirectionalLightShadowPass && parameter is DirectionalLight light)
            {
                DirectionalLightShadowPass(context, camera, light);
            }
            else if (pass == PassIdentifer.DeferredPass)
            {
                DeferredPass(context, camera);
            }
        }

        private bool dirty = true;

        private void Update(GraphicsContext context)
        {
            if (!dirty) return;
            dirty = false;

            worldDataBuffer.Update(context, new WorldData() { chunkOffset = Vector3.Zero });
            blockBuffer.UpdateRange(context, BlockRegistry.GetDescriptionPackeds().ToArray());
        }

        private void DeferredPass(GraphicsContext context, Camera camera)
        {
            if (world == null) return;
            Update(context);
            context.SetGraphicsPipelineState(geometry);
            var frustum = camera.RelFrustum;
            for (int j = 0; j < world.LoadedRenderRegions.Count; j++)
            {
                RenderRegion region = world.LoadedRenderRegions[j];
                BoundingBox box = new(region.BoundingBox.Min - camera.Transform.GlobalPosition, region.BoundingBox.Max - camera.Transform.GlobalPosition);
                if (region.OpaqueVertexBuffer.VertexCount != 0 && frustum.Intersects(box))
                {
                    if (region.BindOpaque(context))
                    {
                        worldDataBuffer.Update(context, new WorldData(region.Offset, camera.Transform.GlobalPosition));
                        context.DrawInstanced((uint)region.OpaqueVertexBuffer.VertexCount, 1, 0, 0);
                    }
                }
            }
            context.SetGraphicsPipelineState(null);
        }

        private void DirectionalLightShadowPass(GraphicsContext context, Camera camera, DirectionalLight light)
        {
            if (world == null) return;
            Update(context);
            context.SetGraphicsPipelineState(csmPass);
            var frustra = light.ShadowFrustra;
            for (int i = 0; i < world.LoadedRenderRegions.Count; i++)
            {
                RenderRegion region = world.LoadedRenderRegions[i];
                for (int j = 0; j < light.cascadeCount; j++)
                {
                    var frustum = frustra[j];
                    if (region.OpaqueVertexBuffer.VertexCount != 0 && frustum.Intersects(region.BoundingBox))
                    {
                        if (region.BindOpaque(context))
                        {
                            worldDataBuffer.Update(context, new WorldData(region.Offset, camera.Transform.GlobalPosition));
                            context.DrawInstanced((uint)region.OpaqueVertexBuffer.VertexCount, 1, 0, 0);
                        }

                        break;
                    }
                }
            }
            context.SetGraphicsPipelineState(null);
        }
    }
}