namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using System.ComponentModel;
    using System.Numerics;
    using System.Xml.Serialization;

    public struct GraphicsPipelineStateDesc : IEquatable<GraphicsPipelineStateDesc>
    {
        public RasterizerDesc2 Rasterizer;
        public DepthStencilDesc DepthStencil;
        public BlendDesc1 Blend;

        [XmlAttribute]
        [DefaultValue(PrimitiveTopology.Trianglelist)]
        public PrimitiveTopology Topology;

        public Vector4 BlendFactor;

        [XmlAttribute]
        [DefaultValue(int.MaxValue)]
        public uint SampleMask;

        [XmlAttribute]
        [DefaultValue(0)]
        public uint StencilRef;

        [DefaultValue(null)]
        public InputElementDescription[]? InputElements;

        public GraphicsPipelineStateDesc()
        {
            Rasterizer = RasterizerDescription.CullBack;
            DepthStencil = DepthStencilDescription.Default;
            Blend = BlendDescription.Opaque;
            Topology = PrimitiveTopology.Trianglelist;
            BlendFactor = default;
            SampleMask = int.MaxValue;
            StencilRef = 0;
        }

        public static GraphicsPipelineStateDesc Default => new() { DepthStencil = DepthStencilDescription.Default, Rasterizer = RasterizerDescription.CullBack, Blend = BlendDescription.Opaque, Topology = PrimitiveTopology.Trianglelist, BlendFactor = default, SampleMask = int.MaxValue };

        public static GraphicsPipelineStateDesc DefaultFullscreen => new() { DepthStencil = DepthStencilDescription.None, Rasterizer = RasterizerDescription.CullBack, Blend = BlendDescription.Opaque, Topology = PrimitiveTopology.Trianglestrip, BlendFactor = default, SampleMask = int.MaxValue };

        /// <summary>
        /// Gets a default graphics pipeline state with alpha blending.
        /// </summary>
        public static GraphicsPipelineStateDesc DefaultAlphaBlend => new() { DepthStencil = DepthStencilDescription.Default, Rasterizer = RasterizerDescription.CullBack, Blend = BlendDescription.AlphaBlend, Topology = PrimitiveTopology.Trianglelist, BlendFactor = default, SampleMask = int.MaxValue };

        /// <summary>
        /// Gets a default fullscreen graphics pipeline state for rendering to the entire screen with scissors enabled.
        /// </summary>
        public static GraphicsPipelineStateDesc DefaultFullscreenScissors => new() { DepthStencil = DepthStencilDescription.None, Rasterizer = RasterizerDescription.CullBackScissors, Blend = BlendDescription.Opaque, Topology = PrimitiveTopology.Trianglestrip, BlendFactor = default, SampleMask = int.MaxValue };

        /// <summary>
        /// Gets a default additive fullscreen graphics pipeline state for rendering to the entire screen with additive blending.
        /// </summary>
        public static GraphicsPipelineStateDesc DefaultAdditiveFullscreen => new() { DepthStencil = DepthStencilDescription.None, Rasterizer = RasterizerDescription.CullBack, Blend = BlendDescription.Additive, Topology = PrimitiveTopology.Trianglestrip, BlendFactor = default, SampleMask = int.MaxValue };

        /// <summary>
        /// Gets a default alpha blend fullscreen graphics pipeline state for rendering to the entire screen with alpha blending.
        /// </summary>
        public static GraphicsPipelineStateDesc DefaultAlphaBlendFullscreen => new() { DepthStencil = DepthStencilDescription.None, Rasterizer = RasterizerDescription.CullBack, Blend = BlendDescription.AlphaBlend, Topology = PrimitiveTopology.Trianglestrip, BlendFactor = default, SampleMask = int.MaxValue };

        public override readonly bool Equals(object? obj)
        {
            return obj is GraphicsPipelineStateDesc state && Equals(state);
        }

        public readonly bool Equals(GraphicsPipelineStateDesc other)
        {
            return EqualityComparer<RasterizerDesc2>.Default.Equals(Rasterizer, other.Rasterizer) &&
                   EqualityComparer<DepthStencilDesc>.Default.Equals(DepthStencil, other.DepthStencil) &&
                   EqualityComparer<BlendDesc1>.Default.Equals(Blend, other.Blend) &&
                   Topology == other.Topology &&
                   BlendFactor.Equals(other.BlendFactor) &&
                   SampleMask == other.SampleMask &&
                   StencilRef == other.StencilRef;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Rasterizer, DepthStencil, Blend, Topology, BlendFactor, SampleMask, StencilRef);
        }

        public static bool operator ==(GraphicsPipelineStateDesc left, GraphicsPipelineStateDesc right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GraphicsPipelineStateDesc left, GraphicsPipelineStateDesc right)
        {
            return !(left == right);
        }
    }
}