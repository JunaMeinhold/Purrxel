#include "defs.hlsl"

cbuffer ColorBuffer
{
	float4 color;
};

float4 main(PixelInput input) : SV_TARGET
{
	return color;
}