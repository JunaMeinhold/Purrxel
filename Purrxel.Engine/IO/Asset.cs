namespace VoxelEngine.IO
{
    public class Asset
    {
        public string Path { get; set; }

        public long Pointer { get; set; }

        public long Length { get; set; }

        public AssetBundle Bundle { get; set; }

        public VirtualStream GetStream()
        {
            return new(Bundle.GetStream(), Pointer, Length, false);
        }

        public byte[] GetData()
        {
            Stream fs = Bundle.GetStream();
            fs.Position = Pointer;
            return fs.Read(Length);
        }
    }
}