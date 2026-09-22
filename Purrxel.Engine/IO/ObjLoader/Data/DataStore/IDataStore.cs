namespace VoxelEngine.IO.ObjLoader.Data.DataStore
{
    using System.Collections.Generic;
    using VoxelEngine.IO.ObjLoader.Data;
    using VoxelEngine.IO.ObjLoader.Data.Elements;
    using VoxelEngine.IO.ObjLoader.Data.VertexData;

    public interface IDataStore
    {
        IList<Vertex> Vertices { get; }
        IList<Texture> Textures { get; }
        IList<Normal> Normals { get; }
        IList<Material> Materials { get; }
        IList<Group> Groups { get; }
    }
}