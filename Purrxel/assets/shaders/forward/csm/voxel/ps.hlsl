#include "defs.hlsl"

Texture2DArray shaderTexture : register(t0);
SamplerState Sampler : register(s0);

float4 main(PixelInput input) : SV_Target
{
	//float a = shaderTexture.Sample(Sampler, float3(input.uv, input.texID)).a;
	//if (a < 0.5f)
	//{
	//	discard;
	//}

	float depth = input.depth;
	return float4(depth, depth * depth, 0, 0);
}