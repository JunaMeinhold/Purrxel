namespace VoxelEngine.IO.ObjLoader.Loaders
{
    using System.IO;
    using VoxelEngine.IO;

    public class MaterialStreamProvider : IMaterialStreamProvider
    {
        private readonly string basePath;

        public MaterialStreamProvider()
        {
        }

        public MaterialStreamProvider(string basePath)
        {
            this.basePath = basePath;
        }

        public Stream Open(string materialFilePath)
        {
            if (basePath is not null)
            {
                return FileSystem.Open(basePath + materialFilePath);
            }
            return File.Open(materialFilePath, FileMode.Open, FileAccess.Read);
        }
    }

    public class MaterialNullStreamProvider : IMaterialStreamProvider
    {
        public Stream Open(string materialFilePath)
        {
            return null;
        }
    }
}