namespace VoxelEngine.Voxel.Meshing
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Voxel.Blocks;

    public enum MeshLayer : byte
    {
        Opaque,
        Transparent,
    }

    public static unsafe class VoxelMeshFactory
    {
        public static void GenerateMesh(Chunk* chunk)
        {
            ChunkVertexBuffer* opaqueVertexBuffer = &chunk->OpaqueVertexBuffer;
            ChunkVertexBuffer* transparentVertexBuffer = &chunk->TransparentVertexBuffer;
            opaqueVertexBuffer->Lock();
            transparentVertexBuffer->Lock();

            opaqueVertexBuffer->Reset(opaqueVertexBuffer->Count == 0 ? 4096 : opaqueVertexBuffer->Count);
            transparentVertexBuffer->Reset(transparentVertexBuffer->Count == 0 ? 4096 : transparentVertexBuffer->Count);

            ChunkHelper chunkHelperOpaque = new();
            ChunkHelper chunkHelperTransparent = new();

            Vector3 position = chunk->Position;

            ChunkNeighbours neighbours = NeighbourVisitor.Visit(chunk);

            chunk->MissingNeighbours = neighbours.MissingNeighbours;

            // Precalculate the map-relative Y position of the chunk in the map
            int chunkY = (int)(position.Y * Chunk.CHUNK_SIZE);

            // Allocate variables on the stack
            int access, heightMapAccess, iCS, kCS2, i1, k1, y, topJ;
            bool minX, maxX, minZ, maxZ;

            k1 = 1;

            for (int z = 0; z < Chunk.CHUNK_SIZE; z++, k1++)
            {
                // Calculate this once, rather than multiple times in the inner loop
                kCS2 = z * Chunk.CHUNK_SIZE_SQUARED;

                i1 = 1;
                heightMapAccess = z * Chunk.CHUNK_SIZE;

                // Is the current run on the Z- or Z+ edge of the chunk
                minZ = z == 0;
                maxZ = z == Chunk.CHUNK_SIZE_MINUS_ONE;

                for (int x = 0; x < Chunk.CHUNK_SIZE; x++, i1++)
                {
                    // Determine where to start the innermost loop
                    y = chunk->MinY[heightMapAccess];
                    topJ = chunk->MaxY[heightMapAccess];
                    heightMapAccess++;

                    // Calculate this once, rather than multiple times in the inner loop
                    iCS = x * Chunk.CHUNK_SIZE;

                    // Calculate access here and increment it each time in the innermost loop
                    access = kCS2 + iCS + y;

                    // Is the current run on the X- or X+ edge of the chunk
                    minX = x == 0;
                    maxX = x == Chunk.CHUNK_SIZE_MINUS_ONE;

                    // X and Z runs search upwards to create runs, so start at the botto
                    for (; y < topJ; y++, access++)
                    {
                        Block* b = chunk->Data + access;

                        if (b->Type != Chunk.EMPTY)
                        {
                            if (BlockRegistry.Transparent.Contains(b->Type))
                            {
                                CreateRun(transparentVertexBuffer, chunkHelperTransparent, &neighbours, chunk, b, x, y, z, z << 12, i1, k1 << 12, y + chunkY, access, minX, maxX, y == 0, y == Chunk.CHUNK_SIZE_MINUS_ONE, minZ, maxZ, iCS, kCS2, MeshLayer.Transparent);
                            }
                            else
                            {
                                CreateRun(opaqueVertexBuffer, chunkHelperOpaque, &neighbours, chunk, b, x, y, z, z << 12, i1, k1 << 12, y + chunkY, access, minX, maxX, y == 0, y == Chunk.CHUNK_SIZE_MINUS_ONE, minZ, maxZ, iCS, kCS2, MeshLayer.Opaque);
                            }
                        }
                    }
                }
            }

            chunkHelperTransparent.Release();
            chunkHelperOpaque.Release();

            opaqueVertexBuffer->ReleaseLock();
            transparentVertexBuffer->ReleaseLock();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsEmpty(Block* type, MeshLayer layer)
        {
            return IsEmpty(type->Type, layer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsEmpty(ushort type, MeshLayer layer)
        {
            return layer switch
            {
                MeshLayer.Opaque => type == 0 || BlockRegistry.AlphaTest.Contains(type) || BlockRegistry.Transparent.Contains(type),
                MeshLayer.Transparent => type == 0 || BlockRegistry.AlphaTest.Contains(type) || !BlockRegistry.Transparent.Contains(type),
                _ => false,
            };
        }

        private static void CreateRun(ChunkVertexBuffer* vertexBuffer, ChunkHelper chunkHelper, ChunkNeighbours* neighbours, Chunk* chunk, Block* b, int x, int y, int z, int k, int i1, int k1, int yG, int access, bool minX, bool maxX, bool minY, bool maxY, bool minZ, bool maxZ, int iCS, int kCS2, MeshLayer layer)
        {
            Block* data = chunk->Data;
            int type = b->Type;
            int textureHealth16 = BlockVertex.IndexToTextureShifted[type];
            int accessIncremented = access + 1;
            int chunkAccess;
            int j1 = y + 1;
            int jS = y << 6;
            int jS1 = j1 << 6;
            int length;
            uint tint = GetTint(type, access);

            // Left (X-)
            if (!chunkHelper.visitXN[access] && DrawFaceXN(neighbours, chunk, y, access, minX, kCS2, layer))
            {
                chunkHelper.visitXN[access] = true;
                chunkAccess = accessIncremented;

                int r = 0;
                for (length = jS1; length < Chunk.CHUNK_SIZE_SHIFTED; length += 1 << 6, r++)
                {
                    if (DifferentBlock(data + chunkAccess, type, layer))
                    {
                        break;
                    }

                    if (!DrawFaceXN(neighbours, chunk, y + r, chunkAccess, minX, kCS2, layer))
                    {
                        break;
                    }

                    chunkHelper.visitXN[chunkAccess++] = true;
                }
                //length -= 1 << 6;

                // k1 and k are already shifted
                BlockVertex.AppendQuadX(vertexBuffer, x, jS, length, k1, k, (int)FaceTypeShifted.XN, textureHealth16, tint);
            }

            // Right (X+)
            if (!chunkHelper.visitXP[access] && DrawFaceXP(neighbours, chunk, y, access, maxX, kCS2, layer))
            {
                chunkHelper.visitXP[access] = true;

                chunkAccess = accessIncremented;

                int r = 0;
                for (length = jS1; length < Chunk.CHUNK_SIZE_SHIFTED; length += 1 << 6, r++)
                {
                    if (DifferentBlock(data + chunkAccess, type, layer))
                    {
                        break;
                    }

                    if (!DrawFaceXP(neighbours, chunk, y + r, chunkAccess, maxX, kCS2, layer))
                    {
                        break;
                    }

                    chunkHelper.visitXP[chunkAccess++] = true;
                }

                BlockVertex.AppendQuadX(vertexBuffer, i1, jS, length, k, k1, (int)FaceTypeShifted.XP, textureHealth16, tint);
            }

            // Back (Z-)
            if (!chunkHelper.visitZN[access] && DrawFaceZN(neighbours, chunk, y, access, minZ, iCS, layer))
            {
                chunkHelper.visitZN[access] = true;

                chunkAccess = accessIncremented;

                int r = 0;
                for (length = jS1; length < Chunk.CHUNK_SIZE_SHIFTED; length += 1 << 6, r++)
                {
                    if (DifferentBlock(data + chunkAccess, type, layer))
                    {
                        break;
                    }

                    if (!DrawFaceZN(neighbours, chunk, y + r, chunkAccess, minZ, iCS, layer))
                    {
                        break;
                    }

                    chunkHelper.visitZN[chunkAccess++] = true;
                }

                BlockVertex.AppendQuadZ(vertexBuffer, i1, x, jS, length, k, (int)FaceTypeShifted.ZN, textureHealth16, tint);
            }

            // Front (Z+)
            if (!chunkHelper.visitZP[access] && DrawFaceZP(neighbours, chunk, y, access, maxZ, iCS, layer))
            {
                chunkHelper.visitZP[access] = true;

                chunkAccess = accessIncremented;

                int r = 0;
                for (length = jS1; length < Chunk.CHUNK_SIZE_SHIFTED; length += 1 << 6, r++)
                {
                    if (DifferentBlock(data + chunkAccess, type, layer))
                    {
                        break;
                    }

                    if (!DrawFaceZP(neighbours, chunk, y + r, chunkAccess, maxZ, iCS, layer))
                    {
                        break;
                    }

                    chunkHelper.visitZP[chunkAccess++] = true;
                }

                BlockVertex.AppendQuadZ(vertexBuffer, x, i1, jS, length, k1, (int)FaceTypeShifted.ZP, textureHealth16, tint);
            }

            // Bottom (Y-)
            if (!chunkHelper.visitYN[access] && DrawFaceYN(neighbours, chunk, access, minY, iCS, kCS2, layer))
            {
                chunkHelper.visitYN[access] = true;

                chunkAccess = access + Chunk.CHUNK_SIZE;

                for (length = i1; length < Chunk.CHUNK_SIZE; length++)
                {
                    if (DifferentBlock(data + chunkAccess, type, layer))
                    {
                        break;
                    }

                    if (!DrawFaceYN(neighbours, chunk, chunkAccess, minY, length << 4, kCS2, layer))
                    {
                        break;
                    }

                    chunkHelper.visitYN[chunkAccess] = true;

                    chunkAccess += Chunk.CHUNK_SIZE;
                }

                BlockVertex.AppendQuadY(vertexBuffer, x, length, jS, k1, k, (int)FaceTypeShifted.YN, textureHealth16, tint);
            }

            // Top (Y+)
            if (!chunkHelper.visitYP[access] && DrawFaceYP(neighbours, chunk, access, maxY, iCS, kCS2, layer))
            {
                chunkHelper.visitYP[access] = true;

                chunkAccess = access + Chunk.CHUNK_SIZE;

                for (length = i1; length < Chunk.CHUNK_SIZE; length++)
                {
                    if (DifferentBlock(data + chunkAccess, type, layer))
                    {
                        break;
                    }

                    if (!DrawFaceYP(neighbours, chunk, chunkAccess, maxY, length << 4, kCS2, layer))
                    {
                        break;
                    }

                    chunkHelper.visitYP[chunkAccess] = true;

                    chunkAccess += Chunk.CHUNK_SIZE;
                }

                BlockVertex.AppendQuadY(vertexBuffer, x, length, jS1, k, k1, (int)FaceTypeShifted.YP, textureHealth16, tint);
            }
        }

        private static uint GetTint(int type, int access)
        {
            if (type == 10)
            {
                return 0xFF619961; // ABGR
            }

            return uint.MaxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DrawFaceXN(ChunkNeighbours* neighbours, Chunk* chunk, int j, int access, bool min, int kCS2, MeshLayer layer)
        {
            if (min)
            {
                if (neighbours->cXN == null || !neighbours->cXN->InMemory)
                {
                    return true;
                }

                // If it is outside this chunk, get the block from the neighbouring chunk
                return IsEmpty(neighbours->cXN->Data[Chunk.CHUNK_SIZE_MINUS_ONE * Chunk.CHUNK_SIZE + j + kCS2].Type, layer);
            }

            return IsEmpty(chunk->Data[access - Chunk.CHUNK_SIZE].Type, layer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DrawFaceXP(ChunkNeighbours* neighbours, Chunk* chunk, int j, int access, bool max, int kCS2, MeshLayer layer)
        {
            if (max)
            {
                if (neighbours->cXP == null || !neighbours->cXP->InMemory)
                {
                    return true;
                }

                // If it is outside this chunk, get the block from the neighbouring chunk
                return IsEmpty(neighbours->cXP->Data[j + kCS2].Type, layer);
            }

            return IsEmpty(chunk->Data[access + Chunk.CHUNK_SIZE].Type, layer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DrawFaceYN(ChunkNeighbours* neighbours, Chunk* chunk, int access, bool min, int iCS, int kCS2, MeshLayer layer)
        {
            if (min)
            {
                if (chunk->Position.Y == 0)
                {
                    return true;
                }

                if (neighbours->cYN == null || !neighbours->cYN->InMemory)
                {
                    return true;
                }

                // If it is outside this chunk, get the block from the neighbouring chunk
                return IsEmpty(neighbours->cYN->Data[iCS + Chunk.CHUNK_SIZE_MINUS_ONE + kCS2].Type, layer);
            }

            return IsEmpty(chunk->Data[access - 1].Type, layer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DrawFaceYP(ChunkNeighbours* neighbours, Chunk* chunk, int access, bool max, int iCS, int kCS2, MeshLayer layer)
        {
            if (max)
            {
                // Don't check chunkYPos here as players can move above the map
                if (neighbours->cYP == null || !neighbours->cYP->InMemory)
                {
                    return true;
                }
                else
                {
                    return IsEmpty(neighbours->cYP->Data[iCS + kCS2].Type, layer);
                }
            }
            else
            {
                return IsEmpty(chunk->Data[access + 1].Type, layer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DrawFaceZN(ChunkNeighbours* neighbours, Chunk* chunk, int j, int access, bool min, int iCS, MeshLayer layer)
        {
            if (min)
            {
                if (neighbours->cZN == null || !neighbours->cZN->InMemory)
                {
                    return true;
                }

                return IsEmpty(neighbours->cZN->Data[iCS + j + Chunk.CHUNK_SIZE_MINUS_ONE * Chunk.CHUNK_SIZE_SQUARED].Type, layer);
            }

            return IsEmpty(chunk->Data[access - Chunk.CHUNK_SIZE_SQUARED].Type, layer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DrawFaceZP(ChunkNeighbours* neighbours, Chunk* chunk, int j, int access, bool max, int iCS, MeshLayer layer)
        {
            if (max)
            {
                if (neighbours->cZP == null || !neighbours->cZP->InMemory)
                {
                    return true;
                }

                return IsEmpty(neighbours->cZP->Data[iCS + j].Type, layer);
            }

            return IsEmpty(chunk->Data[access + Chunk.CHUNK_SIZE_SQUARED].Type, layer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DifferentBlock(Chunk chunk, int chunkAccess, int compare, MeshLayer layer)
        {
            Block b = chunk.Data[chunkAccess];
            if (BlockRegistry.AlphaTest.Contains(b)) return true;
            return b.Type != compare;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DifferentBlock(Block* data, int compare, MeshLayer layer)
        {
            Block b = *data;
            if (BlockRegistry.AlphaTest.Contains(b)) return true;
            return b.Type != compare;
        }
    }
}