#ifndef INCLUDE_H_COMMON_SHADING
#define INCLUDE_H_COMMON_SHADING

#include "gbuffer.hlsl"
#include "lights.hlsl"
#include "camera.hlsl"
#include "shadows.hlsl"

StructuredBuffer<Light> LightBuffer;
StructuredBuffer<ShadowData> ShadowDataBuffer;

cbuffer LightParams
{
	uint lightCount;
	float3 ambient;
};

Texture2D<float4> GBufferA;
Texture2D<float4> GBufferB;
Texture2D<float4> GBufferC;
Texture2D<float4> GBufferD;
Texture2D<float> DepthTex;

Texture2DArray CSMDepthBuffer;
Texture2D<float> AOBuffer;

SamplerState linearClampSampler;

#define PI 3.14159265358979323846

float Attenuation(float distance, float range)
{
	float att = saturate(1.0f - (distance * distance / (range * range)));
	return att * att;
}

float3 BlinnPhong(float3 radiance, float NdotL, float3 L, float3 V, float3 N, float3 baseColor, float shininess)
{
	NdotL = max(0, NdotL);
	float3 diffuse = radiance * NdotL;

	const float kEnergyConservation = (8.0 + shininess) / (8.0 * PI);
	float3 halfwayDir = normalize(L + V);
	float spec = kEnergyConservation * pow(max(dot(N, halfwayDir), 0.0), shininess);

	float3 specular = radiance * spec;

	return (diffuse + specular) * baseColor;
}

float3 ComputeDirectionalLight(PixelParams attrs, Light light)
{
	float3 radiance = light.color.rgb;
	float3 L = normalize(-light.direction);
	float NdotL = dot(attrs.N, L);

	float visibility = 1;

	if (light.castsShadows)
	{
		ShadowData data = ShadowDataBuffer[light.shadowMapIndex];
		//visibility = ShadowFactorDirectionalLightCascaded(CSMDepthBuffer, linearClampSampler, data, position + GetCameraPos(), NdotL);
	}

	return BlinnPhong(radiance, NdotL, L, attrs.V, attrs.N, attrs.DiffuseColor, 32) * visibility;
}

float3 ComputePointLight(PixelParams attrs, Light light)
{
	float3 LN = light.position.xyz - attrs.Pos;
	float distance = length(LN);
	float3 L = normalize(LN);
	float NdotL = dot(attrs.N, L);

	float attenuation = Attenuation(distance, light.range);
	float3 radiance = light.color.rgb * attenuation;
	return BlinnPhong(radiance, NdotL, L, attrs.V, attrs.N, attrs.DiffuseColor, 16);
}

float3 ComputeDirectLightning(PixelParams params)
{
	float3 Lo = 0;
	[loop]
		for (uint i = 0; i < lightCount; i++)
		{
			Light light = LightBuffer[i];
			[branch]
				switch (light.type)
				{
				case POINT_LIGHT:
					Lo += ComputePointLight(params, light);
					break;
				case DIRECTIONAL_LIGHT:
					Lo += ComputeDirectionalLight(params, light);
					break;
				}
		}

	return Lo;
}

float3 ComputeIndirectLightning(float2 screenUV, PixelParams params)
{
	float ao = AOBuffer.SampleLevel(linearClampSampler, screenUV, 0);
	return params.DiffuseColor * (ambient * ao);
}

#endif