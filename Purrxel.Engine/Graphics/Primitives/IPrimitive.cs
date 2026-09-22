namespace Purrxel.Engine.Graphics.Primitives
{
    using Purrxel.Engine.Graphics;
    using Purrxel.Engine.Graphics.D3D11;

    public interface IPrimitive : IDisposable
    {
        void DrawAuto(GraphicsContext context, GraphicsPipelineState pso);

        void DrawAuto(GraphicsContext context);

        void Bind(GraphicsContext context, out int vertexCount, out int indexCount, out int instanceCount);

        void Unbind(GraphicsContext context);
    }
}