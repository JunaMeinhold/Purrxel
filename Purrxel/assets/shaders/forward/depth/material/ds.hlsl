// Copyright (c) 2022 Juna Meinhold
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#include "defs.hlsl"
#include "../../../common.hlsl"
#include "../../../material.hlsl"

Texture2D displacmentTexture : register(t0);
SamplerState SampleTypeWrap : register(s0);

cbuffer MatrixBuffer : register(b0)
{
    MVPBuffer mvp;
};

cbuffer MaterialBuffer : register(b1)
{
	Material material;
}

[domain("tri")]
PixelInput main(PatchTess patchTess, float3 bary : SV_DomainLocation, const OutputPatch<DomainInput, 3> tri)
{
	PixelInput output;

	// Interpolate patch attributes to generated vertices.
	output.position = float4(bary.x * tri[0].pos + bary.y * tri[1].pos + bary.z * tri[2].pos, 1);
	output.tex = bary.x * tri[0].tex + bary.y * tri[1].tex + bary.z * tri[2].tex;
	output.normal = bary.x * tri[0].normal + bary.y * tri[1].normal + bary.z * tri[2].normal;

	// Calculate the position of the vertex against the world, view, and projection matrices.

    output.position = mul(output.position, mvp.model);

	if (material.HasDisplacementMap)
	{
		float h = displacmentTexture.SampleLevel(SampleTypeWrap, (float2)output.tex, 0).r;
		output.position += float4((h - 1.0) * output.normal, 0);
	}

	output.pos = output.position;
    output.position = mul(output.position, mvp.view);
    output.position = mul(output.position, mvp.proj);

	// Store the texture coordinates for the pixel shader.
	output.tex = output.tex;

	// Calculate the normal vector against the world matrix only.
    output.normal = mul(output.normal, (float3x3) mvp.model);
	output.normal = normalize(output.normal);

	return output;
}