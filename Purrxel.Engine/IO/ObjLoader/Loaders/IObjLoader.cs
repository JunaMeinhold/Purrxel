namespace VoxelEngine.IO.ObjLoader.Loaders
{
    using System.IO;

    public interface IObjLoader
    {
        LoadResult Load(Stream lineStream);
    }
}