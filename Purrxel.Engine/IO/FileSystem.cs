namespace VoxelEngine.IO
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    public class FileSystem
    {
        private static readonly List<Asset> assetBundles = new();

        static FileSystem()
        {
            foreach (string file in Directory.GetFiles("assets/", "*.assets", SearchOption.TopDirectoryOnly))
            {
                assetBundles.AddRange(new AssetBundle(file).Assets);
            }
        }

        public static bool Exists(string path)
        {
            if (path == null)
            {
                return false;
            }

            if (File.Exists(path))
            {
                return true;
            }
            else
            {
                string rel = Path.GetRelativePath("assets/", path);
                return assetBundles.Find(x => x.Path == rel) != null; ;
            }
        }

        public static VirtualStream Open(string path)
        {
            if (File.Exists(path))
            {
                FileStream fs = File.OpenRead(path);

                return new(fs, 0, fs.Length, true);
            }
            else
            {
                string rel = Path.GetRelativePath("assets/", path);
                Asset asset = assetBundles.Find(x => x.Path == rel);
#if DEBUG
                // Please check if you tick always copy in properties window of the file.
                Debug.Assert(asset != null, $"Warning asset {path} is missing");
#endif
                return asset?.GetStream();
            }
        }

        public static string[] ReadAllLines(string path)
        {
            using VirtualStream fs = Open(path);
            using StreamReader reader = new(fs);
            return reader.ReadToEnd().Split(Environment.NewLine);
        }

        public static string ReadAllText(string path)
        {
            using VirtualStream fs = Open(path);
            using StreamReader reader = new(fs);
            return reader.ReadToEnd();
        }

        public static byte[] ReadAllBytes(string path)
        {
            using VirtualStream fs = Open(path);
            byte[] data = new byte[fs.Length];
            fs.ReadExactly(data);
            return data;
        }
    }
}