namespace App.Pipelines.Effects
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System.Numerics;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Lightning;
    using VoxelEngine.Scenes;

    public unsafe class GodRaysEffect : DisposableBase
    {
        private Hexa.NET.Mathematics.Viewport viewport;
        private readonly VoxelEngine.Graphics.Primitives.Plane plane;
        private readonly ConstantBuffer<SunParams> paramsSunBuffer;
        private readonly ConstantBuffer<CBWorld> paramsWorldBuffer;
        private readonly SamplerState sunSampler;
        private readonly GraphicsPipelineState sun;

        private readonly ConstantBuffer<GodRaysParams> paramsBuffer;
        private readonly SamplerState sampler;
        private readonly GraphicsPipelineState godrays;

        private readonly Texture2D sunsprite;
        private readonly Texture2D sunBuffer;
        private readonly Texture2D noiseTex;

        private readonly float godraysDensity = 0.975f;
        private readonly float godraysWeight = 0.25f;
        private readonly float godraysDecay = 0.825f;
        private readonly float godraysExposure = 2.0f;

        public struct GodRaysParams
        {
            public Vector4 ScreenSpacePosition;
            public float GodraysDensity;
            public float GodraysWeight;
            public float GodraysDecay;
            public float GodraysExposure;
            public Vector4 Color;
        }

        public struct SunParams
        {
            public Vector3 Diffuse;
            public float AlbedoFactor;
        }

        public GodRaysEffect(int width, int height)
        {
            plane = new(5);

            paramsSunBuffer = new(CpuAccessFlags.Write);
            paramsWorldBuffer = new(CpuAccessFlags.Write);
            sunSampler = new(SamplerStateDescription.LinearWrap);

            sampler = new(SamplerStateDescription.LinearClamp);
            paramsBuffer = new(CpuAccessFlags.Write);
            sunsprite = new("sun/sunsprite.png");
            sunBuffer = new(Format.R16G16B16A16Float, width, height, 1, 1, 0, GpuAccessFlags.RW);
            noiseTex = new(Format.R32Float, 1024, 1024, 1, 1, 0, GpuAccessFlags.RW);

            sun = GraphicsPipelineState.Create(new()
            {
                VertexShader = "sun/vs.hlsl",
                PixelShader = "sun/ps.hlsl"
            }, new GraphicsPipelineStateDesc()
            {
                Blend = BlendDescription.AlphaBlend,
                BlendFactor = Vector4.One,
                DepthStencil = DepthStencilDescription.DepthRead,
                Rasterizer = RasterizerDescription.CullBack,
                SampleMask = int.MaxValue,
                StencilRef = 0,
                Topology = PrimitiveTopology.Trianglelist
            });

            sun.Bindings.SetCBV("WorldBuffer", paramsWorldBuffer);
            sun.Bindings.SetCBV("SunParams", paramsSunBuffer);
            sun.Bindings.SetSRV("tex", sunsprite);
            sun.Bindings.SetSampler("linearWrapSampler", sunSampler);

            godrays = GraphicsPipelineState.Create(new()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "godrays/ps.hlsl"
            },
            new GraphicsPipelineStateDesc()
            {
                DepthStencil = DepthStencilDescription.Default,
                Rasterizer = RasterizerDescription.CullBack,
                Blend = BlendDescription.Additive,
                Topology = PrimitiveTopology.Trianglestrip,
                BlendFactor = default,
                SampleMask = int.MaxValue
            });

            godrays.Bindings.SetCBV("GodrayParams", paramsBuffer);
            godrays.Bindings.SetSRV("sunTexture", sunBuffer);
            godrays.Bindings.SetSampler("linearClampSampler", sampler);

            viewport = new(width, height);
        }

        public void Resize(int width, int height)
        {
            sunBuffer.Resize(Format.R16G16B16A16Float, width, height, 1, 1);
            viewport = new(width, height);
        }

        public void Update(GraphicsContext context, Camera camera, DirectionalLight light)
        {
            GodRaysParams raysParams = default;

            raysParams.GodraysDecay = godraysDecay;
            raysParams.GodraysWeight = godraysWeight;
            raysParams.GodraysDensity = godraysDensity;
            raysParams.GodraysExposure = godraysExposure;
            raysParams.Color = light.Color;

            var camera_position = camera.Transform.GlobalPosition;

            var translation = Matrix4x4.CreateTranslation(camera_position);

            var far = camera.Transform.Far;
            var light_position = Vector3.Transform(light.Transform.Backward * (far / 2f), translation);

            var transform = Matrix4x4.CreateTranslation(light.Transform.Backward * (far / 15));

            var light_posH = Vector4.Transform(light_position, camera.Transform.ViewProjection);
            var ss_sun_pos = new Vector4(0.5f * light_posH.X / light_posH.W + 0.5f, -0.5f * light_posH.Y / light_posH.W + 0.5f, light_posH.Z / light_posH.W, 1.0f);

            raysParams.ScreenSpacePosition = ss_sun_pos;

            paramsBuffer.Update(context, raysParams);

            CBWorld world = default;

            world.World = Matrix4x4.Transpose(transform);
            world.WorldInv = Matrix4x4.Transpose(light.Transform.GlobalInverse);

            paramsWorldBuffer.Update(context, world);

            SunParams sunParams = default;

            sunParams.Diffuse = Vector3.One;
            sunParams.AlbedoFactor = 1f;

            paramsSunBuffer.Update(context, sunParams);
        }

        public void PrePass(GraphicsContext context, DepthStencil depth)
        {
            context.ClearRenderTargetView(sunBuffer, default);
            context.SetRenderTarget(sunBuffer, depth);
            context.SetViewport(viewport);
            plane.DrawAuto(context, sun);
            context.ClearState();
        }

        public void Pass(GraphicsContext context)
        {
            context.SetGraphicsPipelineState(godrays);
            context.DrawInstanced(4, 1, 0, 0);
            context.SetGraphicsPipelineState(null);
        }

        protected override void DisposeCore()
        {
            plane.Dispose();
            sun.Dispose();
            sunSampler.Dispose();
            paramsSunBuffer.Dispose();
            paramsWorldBuffer.Dispose();

            godrays.Dispose();
            sampler.Dispose();
            paramsBuffer.Dispose();

            sunsprite.Dispose();
            sunBuffer.Dispose();
            noiseTex.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}