namespace VoxelEngine.IO.ObjLoader.Loaders
{
    public interface IObjLoaderFactory
    {
        IObjLoader Create(IMaterialStreamProvider materialStreamProvider);

        IObjLoader Create();
    }
}