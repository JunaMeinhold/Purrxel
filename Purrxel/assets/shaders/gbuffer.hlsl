#ifndef GBUFFER_H_INCLUDED
#define GBUFFER_H_INCLUDED

float3 PackNormal(float3 normal)
{
	return 0.5 * normal + 0.5;
}

float3 UnpackNormal(float3 normal)
{
	return 2 * normal - 1;
}

struct GeometryAttributes
{
	float3 albedo;
	float3 normal;
	float3 specular;
	float specCoeff;
};

void ExtractGeometryData(
	in float2 uv,
	in Texture2D<float4> GBufferA,
	in Texture2D<float4> GBufferB,
	in Texture2D<float4> GBufferC,
	in Texture2D<float4> GBufferD,
	SamplerState state,
	out GeometryAttributes attrs)
{
	float4 a = GBufferA.Sample(state, uv);
	float4 b = GBufferB.Sample(state, uv);
	float4 c = GBufferC.Sample(state, uv);
	float4 d = GBufferD.Sample(state, uv);

	attrs.albedo = a.rgb;
	attrs.normal = UnpackNormal(b.xyz);
	attrs.specular = c.rgb;
	attrs.specCoeff = c.w;
}

struct GeometryData
{
	float4 GBufferA : SV_TARGET0;
	float4 GBufferB : SV_TARGET1;
	float4 GBufferC : SV_TARGET2;
	float4 GBufferD : SV_TARGET3;
};

GeometryData PackGeometryData(
	in float3 color,
	in float3 normal,
	in float3 specular,
	in float specCoeff)
{
	GeometryData data;
	data.GBufferA.rgba = float4(color, 1);
	data.GBufferB.rgb = PackNormal(normal);
	data.GBufferC.rgb = specular;
	data.GBufferC.a = specCoeff;
	return data;
}

#endif