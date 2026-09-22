namespace VoxelEngine.Graphics.D3D11
{
    public enum ShaderStage
    {
        /// <summary>
        /// Vertex shader stage.
        /// </summary>
        Vertex,

        /// <summary>
        /// Hull (tessellation control) shader stage.
        /// </summary>
        Hull,

        /// <summary>
        /// Domain (tessellation evaluation) shader stage.
        /// </summary>
        Domain,

        /// <summary>
        /// Geometry shader stage.
        /// </summary>
        Geometry,

        /// <summary>
        /// Pixel (fragment) shader stage.
        /// </summary>
        Pixel,

        /// <summary>
        /// Compute shader stage.
        /// </summary>
        Compute
    }
}