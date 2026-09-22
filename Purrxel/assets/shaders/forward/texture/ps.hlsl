struct PixelInput
{
	float4 pos : SV_POSITION;
	float4 col : COLOR0;
	float2 tex : TEXCOORD;
};

Texture2D tex;
SamplerState linearClampSampler;

float4 main(PixelInput input) : SV_TARGET
{
	return tex.Sample(linearClampSampler, input.tex) * input.col;
}