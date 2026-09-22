TextureCube skyTexture : register(t0);

SamplerState linearClampSampler : register(s0);

cbuffer WeatherCBuf
{
	float4 light_dir;
	float3 A;
	float3 B;

	float3 C;
	float3 D;
	float3 E;

	float3 F;
	float3 G;
	float3 H;

	float3 I;
	float3 Z;
}

struct VertexOut
{
	float4 position : SV_POSITION;
	float3 pos : POSITION;
	float3 tex : TEXCOORD0;
};

#define PI     3.14159265359

float saturatedDot(in float3 a, in float3 b)
{
	return max(dot(a, b), 0.0);
}

float3 YxyToXYZ(in float3 Yxy)
{
	float Y = Yxy.r;
	float x = Yxy.g;
	float y = Yxy.b;

	float X = x * (Y / y);
	float Z = (1.0 - x - y) * (Y / y);

	return float3(X, Y, Z);
}

float3 XYZToRGB(in float3 XYZ)
{
	// CIE/E
	float3x3 M = float3x3
	(
		2.3706743, -0.9000405, -0.4706338,
		-0.5138850, 1.4253036, 0.0885814,
		0.0052982, -0.0146949, 1.0093968
	);

	return mul(M, XYZ);
}

float3 YxyToRGB(in float3 Yxy)
{
	float3 XYZ = YxyToXYZ(Yxy);
	float3 RGB = XYZToRGB(XYZ);
	return RGB;
}

void calculatePerezDistribution(in float T, out float3 A, out float3 B, out float3 C, out float3 D, out float3 E)
{
	A = float3(0.17872f * T - 1.46303f, -0.01925f * T - 0.25922f, -0.01669f * T - 0.26078f);
	B = float3(-0.35540f * T + 0.42749f, -0.06651f * T + 0.00081f, -0.09495f * T + 0.00921f);
	C = float3(-0.02266f * T + 5.32505f, -0.00041f * T + 0.21247f, -0.00792f * T + 0.21023f);
	D = float3(0.12064f * T - 2.57705f, -0.06409f * T - 0.89887f, -0.04405f * T - 1.65369f);
	E = float3(-0.06696f * T + 0.37027f, -0.00325f * T + 0.04517f, -0.01092f * T + 0.05291f);
}

float3 calculateZenithLuminanceYxy(in float t, in float thetaS)
{
	float chi = (4.0 / 9.0 - t / 120.0) * (PI - 2.0 * thetaS);
	float Yz = (4.0453 * t - 4.9710) * tan(chi) - 0.2155 * t + 2.4192;

	float theta2 = thetaS * thetaS;
	float theta3 = theta2 * thetaS;
	float T = t;
	float T2 = t * t;

	float xz =
		(0.00165 * theta3 - 0.00375 * theta2 + 0.00209 * thetaS + 0.0) * T2 +
		(-0.02903 * theta3 + 0.06377 * theta2 - 0.03202 * thetaS + 0.00394) * T +
		(0.11693 * theta3 - 0.21196 * theta2 + 0.06052 * thetaS + 0.25886);

	float yz =
		(0.00275 * theta3 - 0.00610 * theta2 + 0.00317 * thetaS + 0.0) * T2 +
		(-0.04214 * theta3 + 0.08970 * theta2 - 0.04153 * thetaS + 0.00516) * T +
		(0.15346 * theta3 - 0.26756 * theta2 + 0.06670 * thetaS + 0.26688);

	return float3(Yz, xz, yz);
}

float3 calculatePerezLuminanceYxy(in float theta, in float gamma, in float3 A, in float3 B, in float3 C, in float3 D, in float3 E)
{
	return (1.0 + A * exp(B / cos(theta))) * (1.0 + C * exp(D * gamma) + E * cos(gamma) * cos(gamma));
}

float3 calculateSkyLuminanceRGB(in float3 s, in float3 e, in float t)
{
	float3 A, B, C, D, E;
	calculatePerezDistribution(t, A, B, C, D, E);

	float cosTheta = s.y;
	float thetaS = acos(clamp(s.y, 0, 1));
	float thetaE = acos(saturatedDot(e, float3(0, 1, 0)));
	float gammaE = acos(saturatedDot(s, e));

	float3 Yz = calculateZenithLuminanceYxy(t, thetaS);

	if (cosTheta < 0.0f)    // Handle sun going below the horizon
	{
		float s = clamp(1.0f + cosTheta * 50.0f, 0, 1);   // goes from 1 to 0 as the sun sets
		float a = clamp(1.0f + cosTheta * 5.0f, 0, 1);

		A *= a;

		// Take C/E which control sun term to zero
		C *= s;
		E *= s;
	}

	float3 fThetaGamma = calculatePerezLuminanceYxy(thetaE, gammaE, A, B, C, D, E);
	float3 fZeroThetaS = calculatePerezLuminanceYxy(0.0, thetaS, A, B, C, D, E);

	float3 Yp = Yz * (fThetaGamma / fZeroThetaS);

	return YxyToRGB(Yp);
}

float4 main(VertexOut pin) : SV_TARGET
{
	float3 dir = normalize(pin.pos);

	float3 skyLuminance = calculateSkyLuminanceRGB(light_dir.xyz, dir, 2);

	skyLuminance *= 0.005;

	float lerpFactor = 1;

	float cosTheta = light_dir.y;
	if (cosTheta < 0.0f)    // Handle sun going below the horizon
	{
		float a = clamp(1.0f + cosTheta * 2.0f, 0, 1);
		lerpFactor *= max(a, 0.1);
	}

	float3 skyColor = skyTexture.Sample(linearClampSampler, pin.tex).xyz * 0.4;

	float3 finalColor = lerp(skyLuminance, skyColor * (1 - lerpFactor), saturate(pin.tex.y)) + skyLuminance * lerpFactor;

	return float4(finalColor, 1.0);
}