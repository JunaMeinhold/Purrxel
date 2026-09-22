namespace VoxelEngine.Graphics.Primitives
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Graphics.D3D11;

    public interface IPrimitive : IDisposable
    {
        void DrawAuto(GraphicsContext context, GraphicsPipelineState pso);

        void DrawAuto(GraphicsContext context);

        void Bind(GraphicsContext context, out int vertexCount, out int indexCount, out int instanceCount);

        void Unbind(GraphicsContext context);
    }
}