struct PixelInput
{
	float4 pos : SV_POSITION;
	float4 col : COLOR0;
	float2 tex : TEXCOORD;
};

struct VertexInput
{
	float2 pos : POSITION;
	float4 col : COLOR0;
	float2 tex : TEXCOORD;
};

cbuffer ModelBuffer
{
	float4x4 model;
};

PixelInput main(VertexInput input)
{
	PixelInput output;
	output.pos = mul(float4(input.pos, 0, 1), model);
	output.col = input.col;
	output.tex = input.tex;
	return output;
}