#ifndef LIGHT_H_INCLUDED
#define LIGHT_H_INCLUDED

#define POINT_LIGHT 0
#define SPOT_LIGHT 1
#define DIRECTIONAL_LIGHT 2

struct DirectionalLightSD
{
	float4x4 views[8];
	float4 cascades[2];
	float4 color;
	float3 dir;
	bool castsShadows;
	uint cascadeCount;
	float lightBleedingReduction;
};

struct PointLight
{
	float4 color;
	float3 position;
	float range;
};

struct Light
{
	uint type;

	float4 color;
	float4 position;
	float4 direction;
	float range;
	int castsShadows;
	bool cascadedShadows;
	int shadowMapIndex;
};

struct ShadowData
{
	float4x4 views[8];
	float4 cascades[2];
	float size;
	float softness;
	uint cascadeCount;
	float4 regions[8];
	float bias;
	float slopeBias;
};

struct PixelParams
{
	float3 Pos;
	float3 N;
	float3 V;
	float NdotV;
	float3 DiffuseColor;
	float3 Specular;
	float SpecCoeff;
};

#endif