#include "../../common.hlsl"

struct PixelInputType
{
	float4 position : SV_POSITION;
	float3 normal : NORMAL;
	float2 uv : TEXCOORD0;
	int texID : TEXCOORD1;
	float4 pos : TEXCOORD2;
	float4 color : COLOR;
};

struct BlockDescription
{
	int packedX;
	int packedY;
	int packedZ;
	int padd;
};