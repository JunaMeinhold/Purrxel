namespace VoxelEngine.IO.ObjLoader.Data
{
    using System.Numerics;

    public class Material
    {
        public Material(string materialName)
        {
            Name = materialName;
        }

        public string Name { get; set; }

        public Vector3 AmbientColor { get; set; }

        public Vector3 DiffuseColor { get; set; }

        public Vector3 SpecularColor { get; set; }

        public float SpecularCoefficient { get; set; }

        public float Transparency { get; set; }

        public int IlluminationModel { get; set; }

        public string AmbientTextureMap { get; set; }
        public string DiffuseTextureMap { get; set; }

        public string SpecularTextureMap { get; set; }
        public string SpecularHighlightTextureMap { get; set; }

        public string BumpMap { get; set; }
        public string DisplacementMap { get; set; }
        public string StencilDecalMap { get; set; }

        public string AlphaTextureMap { get; set; }

        public string RoughnessTextureMap { get; set; }

        public string MetallicTextureMap { get; set; }

        public static implicit operator Objects.Material(Material material)
        {
            return new()
            {
                Name = material.Name,
                AmbientColor = material.AmbientColor,
                DiffuseColor = material.DiffuseColor,
                SpecularColor = material.SpecularColor,
                SpecularCoefficient = material.SpecularCoefficient,
                Transparency = material.Transparency,
                AmbientTextureMap = material.AmbientTextureMap,
                DiffuseTextureMap = material.DiffuseTextureMap,
                SpecularTextureMap = material.SpecularTextureMap,
                SpecularHighlightTextureMap = material.SpecularHighlightTextureMap,
                BumpMap = material.BumpMap,
                DisplacementMap = material.DisplacementMap,
                StencilDecalMap = material.StencilDecalMap,
                AlphaTextureMap = material.AlphaTextureMap,
                MetallicTextureMap = material.MetallicTextureMap,
                RoughnessTextureMap = material.RoughnessTextureMap,
            };
        }
    }
}