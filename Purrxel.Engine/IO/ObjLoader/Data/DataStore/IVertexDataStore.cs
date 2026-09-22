namespace VoxelEngine.IO.ObjLoader.Data.DataStore
{
    using VoxelEngine.IO.ObjLoader.Data.VertexData;

    public interface IVertexDataStore
    {
        void AddVertex(Vertex vertex);
    }
}