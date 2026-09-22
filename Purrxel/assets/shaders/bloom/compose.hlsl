struct VSOut
{
	float4 Pos : SV_Position;
	float2 Tex : TEXCOORD;
};

Texture2D sceneTexture;
Texture2D bloomTexture;
SamplerState linearClampSampler;

inline float3 Bloom(float2 texCoord, float3 hdr)
{
	float3 blm = bloomTexture.SampleLevel(linearClampSampler, texCoord, 0).rgb;
	float3 drt = 0;
#if LensDirt
	drt = lensDirt.SampleLevel(linearClampSampler, texCoord, 0).rgb;
#endif
	return lerp(hdr, blm + blm * drt, float3(BloomStrength, BloomStrength, BloomStrength));
}

float4 main(VSOut vs) : SV_Target
{
	float4 color = sceneTexture.Sample(linearClampSampler, vs.Tex);
	color.rgb = Bloom(vs.Tex, color.rgb);
	return color;
}