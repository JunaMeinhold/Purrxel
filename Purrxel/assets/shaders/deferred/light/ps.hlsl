#include "../../camera.hlsl"
#include "../../commonShading.hlsl"

struct PixelInput
{
	float4 pos : SV_Position;
	float2 tex : TEXCOORD;
};

float4 main(PixelInput input) : SV_TARGET
{
	GeometryAttributes attrs;
	ExtractGeometryData(input.tex, GBufferA, GBufferB, GBufferC, GBufferD, linearClampSampler, attrs);
	float depth = DepthTex.SampleLevel(linearClampSampler, input.tex, 0);
	float3 position = GetPositionRWS(input.tex, depth);
	float3 V = normalize(-position);

	PixelParams params;
	params.Pos = position;
	params.N = attrs.normal;
	params.V = V;
	params.NdotV = dot(params.N, V);
	params.DiffuseColor = attrs.albedo;
	params.Specular = attrs.specular;
	params.SpecCoeff = attrs.specCoeff;

	float3 color = ComputeDirectLightning(params) + ComputeIndirectLightning(input.tex, params);

	return float4(color, 1);
}