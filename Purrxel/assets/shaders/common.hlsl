#ifndef COMMON_H_INCLUDED
#define COMMON_H_INCLUDED

#define CHUNK_SIZE 16

struct ChunkVertex
{
    uint chunkPosition : POSITION;
    int aData : POSITION1;
    float4 color : COLOR;
};

float3 GetVertexPosition(ChunkVertex vertex)
{
    uint chunkPosPacked = vertex.chunkPosition;
    int data = vertex.aData;
    float3 chunkPos = float3((chunkPosPacked & 0xFF), ((chunkPosPacked >> 8) & 0xFF), ((chunkPosPacked >> 16) & 0xFF));
    float3 localPos = float3((data & (63)), ((data >> 6) & (63)), ((data >> 12) & (63)));
    return chunkPos * CHUNK_SIZE + localPos;
}

#endif