#include "../../camera.hlsl"
#include "defs.hlsl"

cbuffer ModelBuffer : register(b0)
{
	float4x4 model;
};

PixelInputType main(VertexInputType input)
{
	PixelInputType output;

	output.position = mul(float4(input.position, 1), model);
	output.position = mul(output.position, relViewProj);

	output.tex = normalize(input.position.xyz);
	output.pos = input.position.xyz;
	return output;
}