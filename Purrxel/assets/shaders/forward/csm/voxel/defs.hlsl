struct GeometryInput
{
	float3 position : POSITION;
	float2 uv : TEXCOORD0;
	int texID : TEXCOORD1;
};

struct PixelInput
{
	float4 position : SV_POSITION;
	uint rtvIndex : SV_RenderTargetArrayIndex;
	float depth : DEPTH;
	float2 uv : TEXCOORD0;
	int texID : TEXCOORD1;
};

struct BlockDescription
{
	int packedX;
	int packedY;
	int packedZ;
	int padd;
};