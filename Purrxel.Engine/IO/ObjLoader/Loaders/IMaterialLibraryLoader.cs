namespace VoxelEngine.IO.ObjLoader.Loaders
{
    using System.IO;

    public interface IMaterialLibraryLoader
    {
        void Load(Stream lineStream);
    }
}