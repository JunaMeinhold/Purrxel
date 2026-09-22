#include "defs.hlsl"
#define MAX_CASCADED_NUM 8

cbuffer CSMCascadeBuffer : register(b0)
{
	float4x4 views[MAX_CASCADED_NUM];
	uint cascadeCount;
	uint activeCascades;
};

[maxvertexcount(3 * MAX_CASCADED_NUM)]
void main(triangle GeometryInput input[3], inout TriangleStream<PixelInput> triStream)
{
	PixelInput output = (PixelInput)0;

	[unroll(MAX_CASCADED_NUM)]
		for (uint i = 0; i < cascadeCount; i++)
		{
			bool isActive = (activeCascades & (1 << i)) != 0;
			if (isActive)
			{
				[unroll(3)]
					for (uint j = 0; j < 3; j++)
					{
						output.position = mul(float4(input[j].position, 1), views[i]);
						output.rtvIndex = i;
						output.depth = output.position.z / output.position.w;
						output.texID = input[j].texID;
						output.uv = input[j].uv;
						triStream.Append(output);
					}
				triStream.RestartStrip();
			}
		}
}