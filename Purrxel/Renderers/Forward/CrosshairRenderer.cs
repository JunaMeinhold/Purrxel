namespace App.Renderers.Forward
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using Hexa.NET.Mathematics;
    using System.Numerics;
    using VoxelEngine.Core;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Scenes;

    public class CrosshairRenderer : BaseRenderComponent
    {
        private GraphicsPipelineState pipeline;
        private ConstantBuffer<Matrix4x4> mvpBuffer;
        private VertexBuffer<OrthoVertex> vertexBuffer;
        private Texture2D texture;

        public string TexturePath { get; set; }

        private struct OrthoVertex
        {
            public Vector2 Position;
            public uint Color;
            public Vector2 Texture;

            public OrthoVertex(Vector2 position, uint color, Vector2 texture)
            {
                Position = position;
                Color = color;
                Texture = texture;
            }
        }

        public override int QueueIndex { get; } = (int)RenderQueueIndex.Overlay;

        public override void Awake()
        {
            texture = new(TexturePath);

            mvpBuffer = new(CpuAccessFlags.Write);
            InputElementDescription[] inputElements =
            {
                new("POSITION", 0, Format.R32G32Float, 0, -1, InputClassification.PerVertexData, 0),
                new("COLOR", 0, Format.R8G8B8A8Unorm, 0, -1, InputClassification.PerVertexData, 0),
                new("TEXCOORD", 0, Format.R32G32Float, 0, -1, InputClassification.PerVertexData, 0),
            };

            pipeline = GraphicsPipelineState.Create(new()
            {
                VertexShader = "forward/texture/vs.hlsl",
                PixelShader = "forward/texture/ps.hlsl",
            }, new GraphicsPipelineStateDesc()
            {
                Rasterizer = RasterizerDescription.CullNone,
                DepthStencil = DepthStencilDescription.None,
                Blend = BlendDescription.AlphaBlend,
                InputElements = inputElements,
            });
            pipeline.Bindings.SetCBV("ModelBuffer", mvpBuffer);
            pipeline.Bindings.SetSRV("tex", texture);

            int left = -12;
            int top = -12;
            int right = 12;
            int bottom = 12;

            OrthoVertex[] vertices =
            [
                new(new(right, bottom), uint.MaxValue, new(1, 0)),
                new(new(left, top), uint.MaxValue, new(0, 1)),
                new(new(left, bottom), uint.MaxValue, new(0, 0)),
                new(new(right, top), uint.MaxValue, new(1, 1)),
                new(new(left, top), uint.MaxValue, new(0, 1)),
                new(new(right, bottom), uint.MaxValue, new(1, 0)),
            ];

            vertexBuffer = new(0, vertices);
        }

        public override void Draw(GraphicsContext context, PassIdentifer pass, Camera camera, object? parameter)
        {
            if (pass == PassIdentifer.ForwardPass)
            {
                DrawForward(context);
            }
        }

        public void DrawForward(GraphicsContext context)
        {
            var width = Application.MainWindow.Width;
            var height = Application.MainWindow.Height;
            vertexBuffer.Bind(context);
            Matrix4x4 mvp = MathUtil.OrthoLH(width, height, 0.0001f, 1);

            mvpBuffer.Update(context, mvp);
            context.SetGraphicsPipelineState(pipeline);
            context.DrawInstanced((uint)vertexBuffer.Count, 1, 0, 0);
            context.SetGraphicsPipelineState(null);
        }

        public override void Destroy()
        {
            pipeline.Dispose();
            mvpBuffer.Dispose();
            vertexBuffer.Dispose();
            texture.Dispose();
        }
    }
}