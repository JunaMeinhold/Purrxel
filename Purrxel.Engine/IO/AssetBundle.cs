namespace VoxelEngine.IO
{
    using System.IO;

    public class AssetBundle
    {
        private readonly Stream stream;

        public AssetBundle(string path)
        {
            FileStream fs = File.OpenRead(path);
            int count = fs.ReadInt32();
            Assets = new Asset[count];
            for (int i = 0; i < count; i++)
            {
                string apath = fs.ReadString();
                long length = fs.ReadInt64();
                long pointer = fs.Position;
                fs.Position += length;
                Assets[i] = new Asset() { Path = apath, Pointer = pointer, Length = length, Bundle = this };
            }
            stream = fs;
        }

        public Asset[] Assets { get; }

        public Stream GetStream()
        {
            return stream;
        }
    }
}