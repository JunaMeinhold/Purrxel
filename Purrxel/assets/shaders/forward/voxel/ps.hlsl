#include "defs.hlsl"
#include "../../commonShading.hlsl"

Texture2DArray shaderTexture : register(t0);
SamplerState Sampler : register(s0);

float4 main(PixelInputType input) : SV_TARGET
{
	float4 diffuseColor = shaderTexture.Sample(Sampler, float3(input.uv, input.texID)) * input.color;
	PixelParams params;
	params.Pos = input.pos;
	params.N = input.normal;
	params.V = normalize(-input.pos);
	params.NdotV = dot(input.normal, params.V);
	params.DiffuseColor = diffuseColor.rgb;
	params.Specular = float3(0.8f, 0.8f, 0.8f); // unused
	params.SpecCoeff = 0; // unused

	float3 color = ComputeDirectLightning(params) + ComputeIndirectLightning(float2(0,0), params);

	return float4(color, diffuseColor.a);
}