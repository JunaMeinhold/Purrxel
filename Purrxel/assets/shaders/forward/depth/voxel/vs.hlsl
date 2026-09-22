#include "../../../common.hlsl"
#include "../../../camera.hlsl"
#include "defs.hlsl"

cbuffer MatrixBuffer
{
	float4x4 model;
};

cbuffer WorldData
{
	float3 chunkOffset;
	float padd;
};

PixelInputType main(int aData : POSITION)
{
	PixelInputType output;

	float3 position = float3(float(aData & (63)), float((aData >> 6) & (63)), float((aData >> 12) & (63)));

	output.position = mul(float4(position, 1), model);
	output.position = mul(output.position, viewProj);

	return output;
}