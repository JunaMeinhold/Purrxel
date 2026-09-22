namespace VoxelEngine.Voxel.Meshing
{
    using System.Runtime.CompilerServices;

    public static unsafe class BlockVertex
    {
        public static int[] IndexToTextureShifted { get; set; } =
        [
            0, 0 << 18, 1 << 18, 2 << 18, 3 << 18, 4 << 18, 5 << 18, 6 << 18, 7 << 18, 8 << 18, 9 << 18, 10 << 18, 11 << 18, 12 << 18
        ];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendQuadX(ChunkVertexBuffer* buffer, int x, int yL, int yR, int kL, int kR, int normal, int light, uint tint)
        {
            int shared = x | light | normal;

            VoxelVertex* ptr = buffer->Increase(6);

            VoxelVertex v0 = new(yL | kL | shared, tint);
            VoxelVertex v1 = new(yR | kL | shared, tint);
            VoxelVertex v2 = new(yR | kR | shared, tint);
            VoxelVertex v3 = new(yL | kR | shared, tint);

            *ptr++ = v0;
            *ptr++ = v1;
            *ptr++ = v2;
            *ptr++ = v3;
            *ptr++ = v0;
            *ptr = v2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendQuadY(ChunkVertexBuffer* buffer, int xL, int xR, int y, int zL, int zR, int normal, int light, uint tint)
        {
            int shared = y | light | normal;

            VoxelVertex* ptr = buffer->Increase(6);

            VoxelVertex v0 = new(xL | zL | shared, tint);
            VoxelVertex v1 = new(xL | zR | shared, tint);
            VoxelVertex v2 = new(xR | zR | shared, tint);
            VoxelVertex v3 = new(xR | zL | shared, tint);

            *ptr++ = v0;
            *ptr++ = v1;
            *ptr++ = v2;
            *ptr++ = v3;
            *ptr++ = v0;
            *ptr = v2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendQuadZ(ChunkVertexBuffer* buffer, int xL, int xR, int yL, int yR, int z, int normal, int light, uint tint)
        {
            int shared = z | light | normal;

            VoxelVertex* ptr = buffer->Increase(6);

            VoxelVertex v0 = new(xR | yL | shared, tint);
            VoxelVertex v1 = new(xR | yR | shared, tint);
            VoxelVertex v2 = new(xL | yR | shared, tint);
            VoxelVertex v3 = new(xL | yL | shared, tint);

            *ptr++ = v0;
            *ptr++ = v1;
            *ptr++ = v2;
            *ptr++ = v3;
            *ptr++ = v0;
            *ptr = v2;
        }
    }
}