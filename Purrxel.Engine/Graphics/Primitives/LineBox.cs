namespace Purrxel.Engine.Graphics.Primitives
{
    using Purrxel.Engine.Mathematics;
    using Purrxel.Engine.Objects;

    public class LineBox : Mesh<LineVertex, ushort>
    {
        private static readonly LineVertex[] vertices =
        [
            new(new(-0.5f, -0.5f, -0.5f, 0.5f)),
            new(new(-0.5f, -0.5f, 0.5f, 0.5f)),
            new(new(-0.5f, 0.5f, -0.5f, 0.5f)),
            new(new(-0.5f, 0.5f, 0.5f, 0.5f)),
            new(new(0.5f, -0.5f, -0.5f, 0.5f)),
            new(new(0.5f, -0.5f, 0.5f, 0.5f)),
            new(new(0.5f, 0.5f, -0.5f, 0.5f)),
            new(new(0.5f, 0.5f, 0.5f, 0.5f))
        ];

        private static readonly ushort[] indices =
        [
            7, 3, 5, 1,
            6, 2, 4, 0,
            5, 7, 4, 6,
            1, 3, 0, 2,
            7, 6, 3, 2,
            5, 4, 1, 0
        ];

        protected override void Initialize()
        {
            VertexBuffer = new(0, vertices);
            IndexBuffer = new(0, indices);
        }
    }
}