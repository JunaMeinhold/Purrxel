#ifndef SHADOW_H_INCLUDED
#define SHADOW_H_INCLUDED

#include "lights.hlsl"
#include "camera.hlsl"

inline float3 GetShadowUVD(float3 pos, float4x4 view)
{
	float4 fragPosLightSpace = mul(float4(pos, 1.0), view);
	fragPosLightSpace.y = -fragPosLightSpace.y;
	float3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
	projCoords.xy = projCoords.xy * 0.5 + 0.5;
	return projCoords;
}

float Linstep(float a, float b, float v)
{
	return saturate((v - a) / (b - a));
}

float ReduceLightBleeding(float pMax, float amount)
{
	// Remove the [0, amount] tail and linearly rescale (amount, 1].
	return Linstep(amount, 1.0f, pMax);
}

float Chebyshev(float2 moments, float depth)
{
	if (depth <= moments.x)
	{
		return 1.0;
	}

	float variance = moments.y - (moments.x * moments.x);

	float d = depth - moments.x; // attenuation
	float pMax = variance / (variance + d * d);

	return pMax;
}

float SampleVSM(SamplerState state, Texture2D depthTex, float2 texCoords, float fragDepth, float bias, float lightBleedingReduction)
{
	float2 moments = depthTex.Sample(state, texCoords).rg;
	float p = Chebyshev(moments, fragDepth);
	p = ReduceLightBleeding(p, lightBleedingReduction);
	return max(p, fragDepth <= moments.x);
}

float SampleVSMArray(SamplerState state, Texture2DArray depthTex, float2 texCoords, uint layer, float fragDepth, float bias, float lightBleedingReduction)
{
	// note: bias is not used in VSM, not recommended.
	float2 moments = depthTex.Sample(state, float3(texCoords, layer)).rg;
	float p = Chebyshev(moments, fragDepth);
	p = ReduceLightBleeding(p, lightBleedingReduction);
	return max(p, fragDepth <= moments.x);
}

float SampleESM(SamplerState state, Texture2D depthTex, float2 texCoords, float fragDepth, float bias, float exponent)
{
	float lit = 0.0f;
	float moment = depthTex.Sample(state, texCoords).r + bias;
	float visibility = exp(-exponent * fragDepth) * moment;
	return clamp(visibility, 0, 1);
}

float SampleESMArray(SamplerState state, Texture2DArray depthTex, float2 texCoords, uint layer, float fragDepth, float bias, float exponent)
{
	float lit = 0.0f;
	float moment = depthTex.Sample(state, float3(texCoords.xy, layer)).r + bias;
	float visibility = exp(-exponent * fragDepth) * moment;
	return clamp(visibility, 0, 1);
}

float SampleEVSM(SamplerState state, Texture2D depthTex, float2 texCoords, float fragDepth, float bias, float exponent, float lightBleedingReduction)
{
	float shadow = 0.0;
	float4 moments = depthTex.Sample(state, texCoords.xy); // pos, pos^2, neg, neg^2

	fragDepth = 2 * fragDepth - 1;
	float pos = exp(exponent * fragDepth);
	float neg = -exp(-exponent * fragDepth);

	float posShadow = Chebyshev(moments.xy, pos);
	float negShadow = Chebyshev(moments.zw, neg);

	posShadow = ReduceLightBleeding(posShadow, lightBleedingReduction);
	negShadow = ReduceLightBleeding(negShadow, lightBleedingReduction);

	shadow = min(posShadow, negShadow);
	return shadow;
}

float SampleEVSMArray(SamplerState state, Texture2DArray depthTex, float2 texCoords, uint layer, float fragDepth, float bias, float exponent, float lightBleedingReduction)
{
	float shadow = 0.0;
	float4 moments = depthTex.Sample(state, float3(texCoords.xy, layer)); // pos, pos^2, neg, neg^2

	fragDepth = 2 * fragDepth - 1;
	float pos = exp(exponent * fragDepth);
	float neg = -exp(-exponent * fragDepth);

	float posShadow = Chebyshev(moments.xy, pos);
	float negShadow = Chebyshev(moments.zw, neg);

	posShadow = ReduceLightBleeding(posShadow, lightBleedingReduction);
	negShadow = ReduceLightBleeding(negShadow, lightBleedingReduction);

	shadow = min(posShadow, negShadow);
	return shadow;
}

float SampleShadowArray(SamplerState state, Texture2DArray shadowMap, float3 uvd, uint layer, float bias, float lightBleedingReduction)
{
	return SampleVSMArray(state, shadowMap, uvd.xy, layer, uvd.z, bias, lightBleedingReduction);
}

float ShadowFactorDirectionalLightCascaded(Texture2DArray depthTex, SamplerState state, ShadowData light, float3 position, float NdotL)
{
	float cascadePlaneDistances[8] = (float[8]) light.cascades;

	// select cascade layer
	float4 fragPosViewSpace = mul(float4(position, 1.0), view);
	float depthValue = abs(fragPosViewSpace.z);
	float cascadePlaneDistance;
	uint layer = light.cascadeCount;
	for (uint i = 0; i < (uint)light.cascadeCount; ++i)
	{
		if (depthValue < cascadePlaneDistances[i])
		{
			cascadePlaneDistance = cascadePlaneDistances[i];
			layer = i;
			break;
		}
	}

	float3 uvd = GetShadowUVD(position, light.views[layer]);
	if (uvd.z > 1.0f)
		return 1.0;

	float bias = max(0.05 * (1.0 - NdotL), 0.005);
	if (layer == light.cascadeCount)
	{
		bias *= 1 / (camFar * 0.5f);
	}
	else
	{
		bias *= 1 / (cascadePlaneDistance * 0.5f);
	}

	return SampleShadowArray(state, depthTex, uvd, layer, bias, light.softness);
}

#endif