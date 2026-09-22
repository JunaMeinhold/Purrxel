#include "defs.hlsl"

float4 main(PixelInputType input) : SV_Target
{
	float depth = input.position.z / input.position.w;
	return float4(depth, depth, depth, 1);
}