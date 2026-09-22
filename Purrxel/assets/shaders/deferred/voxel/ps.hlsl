#include "../../common.hlsl"
#include "../../gbuffer.hlsl"
#include "defs.hlsl"

Texture2DArray shaderTexture : register(t0);
SamplerState Sampler : register(s0);

GeometryData main(PixelInputType input)
{
	float4 albedo;
	float4 pos = input.pos;
	float3 normal = input.normal;
	float3 specular = float3(0.8f, 0.8f, 0.8f);
	float specCoeff = 0;

	albedo = shaderTexture.Sample(Sampler, float3(input.uv, input.texID)) * input.color;

	if (albedo.a < 0.5f)
	{
		discard;
	}

	return PackGeometryData(albedo.rgb, normal, specular, specCoeff);
}