namespace App.Renderers
{
    using Hexa.NET.Mathematics.Sky;
    using Hexa.NET.Mathematics.Sky.Preetham;
    using System.Numerics;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Graphics.Primitives;
    using VoxelEngine.Scenes;

    public class SkyboxRenderer : BaseRenderComponent
    {
        private GraphicsPipelineState pipeline;

        private ConstantBuffer<Matrix4x4> mvpBuffer;
        private ConstantBuffer<CBWeather> constantBuffer;
        public Texture2D Texture;
        public UVSphere sphere;
        public string TexturePath;
        private CBWeather colors = new();

        private readonly float turbidity = 3;

        public override int QueueIndex { get; } = (int)RenderQueueIndex.Background;

        public static Vector3 SunDir;

        public override void Awake()
        {
            pipeline = GraphicsPipelineState.Create(new()
            {
                VertexShader = "forward/skybox/vs.hlsl",
                PixelShader = "forward/skybox/preethamSky.hlsl"
            }, new GraphicsPipelineStateDesc()
            {
                Rasterizer = RasterizerDescription.CullNone,
                DepthStencil = DepthStencilDescription.DepthRead
            });
            sphere = new();
            Texture = new(TexturePath);

            mvpBuffer = new(CpuAccessFlags.Write);
            constantBuffer = new(CpuAccessFlags.Write);
            pipeline.Bindings.SetSRV("skyTexture", Texture);
            pipeline.Bindings.SetCBV("ModelBuffer", mvpBuffer);
            pipeline.Bindings.SetCBV("WeatherCBuf", constantBuffer);
        }

        public override void Draw(GraphicsContext context, PassIdentifer pass, Camera camera, object? parameter)
        {
            if (pass == PassIdentifer.ForwardPass)
            {
                DrawForward(context, camera);
            }
        }

        public void DrawForward(GraphicsContext context, Camera camera)
        {
            Vector3 sunDir = Vector3.Normalize(-SunDir);
            SkyParameters skyParams = SkyModel.CalculateSkyParameters(turbidity, sunDir, 0, 0);
            colors.A = skyParams[(int)EnumSkyParams.A];
            colors.B = skyParams[(int)EnumSkyParams.B];
            colors.C = skyParams[(int)EnumSkyParams.C];
            colors.D = skyParams[(int)EnumSkyParams.D];
            colors.E = skyParams[(int)EnumSkyParams.E];
            colors.F = skyParams[(int)EnumSkyParams.F];
            colors.G = skyParams[(int)EnumSkyParams.G];
            colors.H = skyParams[(int)EnumSkyParams.H];
            colors.I = skyParams[(int)EnumSkyParams.I];
            colors.Z = skyParams[(int)EnumSkyParams.Z];
            colors.LightDir = new(sunDir, 1);

            constantBuffer.Update(context, colors);
            mvpBuffer.Update(context, Matrix4x4.Transpose(Matrix4x4.CreateScale(camera.Transform.Far)));
            sphere.Bind(context);
            context.SetGraphicsPipelineState(pipeline);
            context.DrawIndexedInstanced((uint)sphere.IndexBuffer.Count, 1, 0, 0, 0);
            context.SetGraphicsPipelineState(null);
        }

        public override void Destroy()
        {
            pipeline.Dispose();
            mvpBuffer.Dispose();
            constantBuffer.Dispose();
            sphere.Dispose();
            Texture.Dispose();
        }
    }
}