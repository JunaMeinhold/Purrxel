#include "defs.hlsl"
#include "../../../common.hlsl"

cbuffer WorldData
{
	float3 chunkOffset;
	float padd;
};

cbuffer TexData
{
	BlockDescription descs[256];
};

GeometryInput main(float3 position : POSITION, int aData : POSITION1)
{
	float3 worldPos = position + chunkOffset * 16;
	GeometryInput output;
	output.position = worldPos;
	output.texID = int((aData >> 18) & (31));

	int normal = int((aData >> 27) & (7));

	if (normal < 2)
	{
		output.uv = position.xz * 1;
	}
	else
	{
		output.uv = (normal < 4 ? position.zy : position.xy) * 1;
	}

	[branch]
		switch (normal)
		{
		case 0:
			output.texID = ((descs[output.texID].packedY >> 8) & 0xff);
			break;
		case 1:
			output.texID = ((descs[output.texID].packedY) & 0xff);
			break;
		case 2:
			output.texID = ((descs[output.texID].packedX >> 8) & 0xff);
			break;
		case 3:
			output.texID = ((descs[output.texID].packedX) & 0xff);
			break;
		case 4:
			output.texID = ((descs[output.texID].packedZ >> 8) & 0xff);
			break;
		case 5:
			output.texID = ((descs[output.texID].packedZ) & 0xff);
			break;
		}

	output.uv.y = 1 - output.uv.y;

	return output;
}