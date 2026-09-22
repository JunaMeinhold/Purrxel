struct Material {
	float3 AmbientColor;
	float reserved0;
	float3 DiffuseColor;
	float reserved1;
	float3 SpecularColor;
	float reserved2;
	float SpecularCoefficient;
	float Transparency;
	float reserved3;
	float reserved4;

	bool HasAmbientTextureMap;
	bool HasDiffuseTextureMap;
	bool HasSpecularTextureMap;
	bool HasSpecularHighlightTextureMap;
	bool HasBumpMap;
	bool HasDisplacementMap;
	bool HasStencilDecalMap;
	bool HasAlphaTextureMap;
	bool HasMetallicMap;
	bool HasRoughnessMap;
	float reserved8;
	float reserved9;
};