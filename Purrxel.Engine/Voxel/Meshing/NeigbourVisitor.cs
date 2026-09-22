namespace VoxelEngine.Voxel.Meshing
{
    using System.Numerics;

    public unsafe struct ChunkNeighbours
    {
        public Chunk* cXN, cXP, cYN, cYP, cZN, cZP;

        public void AddRef()
        {
            if (cXN != null) cXN->AddRef();
            if (cXP != null) cXP->AddRef();
            if (cYN != null) cYN->AddRef();
            if (cYP != null) cYP->AddRef();
            if (cZN != null) cZN->AddRef();
            if (cZP != null) cZP->AddRef();
        }

        public void Release()
        {
            if (cXN != null) cXN->Dispose(cXN);
            if (cXP != null) cXP->Dispose(cXP);
            if (cYN != null) cYN->Dispose(cYN);
            if (cYP != null) cYP->Dispose(cYP);
            if (cZN != null) cZN->Dispose(cZN);
            if (cZP != null) cZP->Dispose(cZP);
        }

        public readonly bool MissingNeighbours => cXN == null || cZN == null || cXP == null || cZP == null;
    }

    public static unsafe class NeighbourVisitor
    {
        public static ChunkNeighbours Visit(Chunk* chunk)
        {
            Vector3 position = chunk->Position;
            World map = chunk->Map;

            ChunkNeighbours neighbours = default;

            // Negative X side
            neighbours.cXN = map.Chunks[(int)(position.X - 1), (int)position.Y, (int)position.Z];
            if (neighbours.cXN != null)
            {
                if (!neighbours.cXN->InMemory)
                {
                    neighbours.cXN = null;
                }
            }

            // Positive X side
            neighbours.cXP = map.Chunks[(int)(position.X + 1), (int)position.Y, (int)position.Z];
            if (neighbours.cXP != null)
            {
                if (!neighbours.cXP->InMemory)
                {
                    neighbours.cXP = null;
                }
            }

            // Negative Y side
            neighbours.cYN = position.Y > 0 ? map.Chunks[(int)position.X, (int)(position.Y - 1), (int)position.Z] : null;
            if (neighbours.cYN != null)
            {
                if (!neighbours.cYN->InMemory)
                {
                    neighbours.cYN = null;
                }
            }

            // Positive Y side
            neighbours.cYP = position.Y < World.CHUNK_AMOUNT_Y - 1 ? map.Chunks[(int)position.X, (int)(position.Y + 1), (int)position.Z] : null;
            if (neighbours.cYP != null)
            {
                if (!neighbours.cYP->InMemory)
                {
                    neighbours.cYP = null;
                }
            }

            // Negative Z neighbour
            neighbours.cZN = map.Chunks[(int)position.X, (int)position.Y, (int)(position.Z - 1)];
            if (neighbours.cZN != null)
            {
                if (!neighbours.cZN->InMemory)
                {
                    neighbours.cZN = null;
                }
            }

            // Positive Z side
            neighbours.cZP = map.Chunks[(int)position.X, (int)position.Y, (int)(position.Z + 1)];
            if (neighbours.cZP != null)
            {
                if (!neighbours.cZP->InMemory)
                {
                    neighbours.cZP = null;
                }
            }

            return neighbours;
        }
    }
}