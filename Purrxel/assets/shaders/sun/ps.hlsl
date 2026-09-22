Texture2D tex : register(t0);
SamplerState linearWrapSampler : register(s0);

cbuffer SunParams
{
	float3 diffuse;
	float albedoFactor;
};

struct PSInput
{
	float4 Position : SV_POSITION;
	float2 TexCoord : TEX;
};

float4 main(PSInput pin) : SV_TARGET
{
	float4 texColor = tex.Sample(linearWrapSampler, pin.TexCoord) * float4(diffuse, 1.0) * albedoFactor;
	return texColor;
}