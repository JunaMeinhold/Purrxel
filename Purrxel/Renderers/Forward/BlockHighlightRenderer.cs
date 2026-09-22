namespace Purrxel.Renderers.Forward
{
    using Hexa.NET.D3DCommon;
    using Hexa.NET.Mathematics;
    using Purrxel.Engine.Graphics;
    using Purrxel.Engine.Graphics.Buffers;
    using Purrxel.Engine.Graphics.D3D11;
    using Purrxel.Engine.Graphics.Primitives;
    using Purrxel.Engine.Scenes;
    using Purrxel.Engine.Voxel;
    using System.Numerics;

    public class BlockHighlightRenderer : BaseRenderComponent
    {
        private GraphicsPipelineState linePipeline;
        private ConstantBuffer<Matrix4x4> mvpBuffer;
        private ConstantBuffer<Vector4> colorBuffer;
        private LineBox lineBox;
        private Player player;

        public Vector4 Color;

        public override int QueueIndex { get; } = (int)RenderQueueIndex.GeometryLast;

        public override void Awake()
        {
            if (GameObject is Player player)
            {
                this.player = player;
            }
            mvpBuffer = new(CpuAccessFlags.Write);
            colorBuffer = new(CpuAccessFlags.Write);
            linePipeline = GraphicsPipelineState.Create(new()
            {
                VertexShader = "forward/line/vs.hlsl",
                PixelShader = "forward/line/ps.hlsl",
            }, new GraphicsPipelineStateDesc()
            {
                Topology = PrimitiveTopology.Linelist
            });
            linePipeline.Bindings.SetCBV("ModelBuffer", mvpBuffer);
            linePipeline.Bindings.SetCBV("ColorBuffer", colorBuffer);
            Color = Colors.Gray;
            lineBox = new();
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
            if (player == null) return;
            if (player.IsLookingAtBlock)
            {
                mvpBuffer.Update(context, Matrix4x4.Transpose(Matrix4x4.CreateScale(0.51f) * Matrix4x4.CreateTranslation((Vector3)player.LookAtBlock - camera.Transform.GlobalPosition + new Vector3(0.5f))));
                colorBuffer.Update(context, Color);

                lineBox.Bind(context);
                context.SetGraphicsPipelineState(linePipeline);
                context.DrawIndexedInstanced((uint)lineBox.IndexBuffer.Count, 1, 0, 0, 0);
                context.SetGraphicsPipelineState(null);
            }
        }

        public override void Destroy()
        {
            linePipeline.Dispose();
            mvpBuffer.Dispose();
            colorBuffer.Dispose();
            lineBox.Dispose();
        }
    }
}