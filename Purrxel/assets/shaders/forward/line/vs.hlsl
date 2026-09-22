#include "../../camera.hlsl"
#include "defs.hlsl"

cbuffer ModelBuffer
{
	float4x4 model;
};

PixelInput main(VertexInput input)
{
	PixelInput output;

	output.pos = mul(input.pos, model);
	output.pos = mul(output.pos, relViewProj);

	return output;
}