namespace VoxelEngine.Voxel.Blocks
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public static class BlockRegistry
    {
        private static readonly List<BlockEntry> _blocks = new();
        private static readonly List<string> _textures = new();
        private static readonly List<BlockDescription> _descriptions = new();
        private static readonly ConcurrentDictionary<string, int> blockNameToIndex = new();
        private static readonly ConcurrentDictionary<int, string> indexToBlockName = new();
        private static HashSet<ushort> alphaTest = [];
        private static HashSet<ushort> transparent = [];

        private static readonly object _lock = new();

        public static readonly BlockEntry Air = new("Air", default);

        static BlockRegistry()
        {
        }

        public static IReadOnlyList<BlockEntry> Blocks => _blocks;

        public static IReadOnlyList<string> Textures => _textures;

        public static IReadOnlyList<BlockDescription> Description => _descriptions;

        public static IReadOnlySet<ushort> AlphaTest => alphaTest;

        public static IReadOnlySet<ushort> Transparent => transparent;

        public static int Count
        {
            get
            {
                lock (_lock)
                {
                    return _blocks.Count + 1;
                }
            }
        }

        public static object SyncObject => _lock;

        public static void Reset()
        {
            lock (_lock)
            {
                _blocks.Clear();
                _textures.Clear();
                _descriptions.Clear();
                blockNameToIndex.Clear();
                indexToBlockName.Clear();
            }
        }

        public static void RegisterBlock(BlockEntry entry)
        {
            lock (_lock)
            {
                int blockIndex = _blocks.Count;
                entry.Id = (ushort)(blockIndex + 1);
                blockNameToIndex.TryAdd(entry.Name, blockIndex);
                indexToBlockName.TryAdd(blockIndex, entry.Name);
                _blocks.Add(entry);
                if (entry.Description.IsSingle)
                {
                    int textureIndex = _textures.Count;
                    _textures.Add(entry.Description.Single);
                    _descriptions.Add(new((byte)textureIndex));
                }
                else
                {
                    int index = _textures.Count;
                    _textures.AddRange(entry.Description.ToArray());
                    _descriptions.Add(new((byte)index, (byte)(index + 1), (byte)(index + 2), (byte)(index + 3), (byte)(index + 4), (byte)(index + 5)));
                }
                if (entry.AlphaTest)
                {
                    alphaTest.Add(entry.Id);
                }
                if (entry.Transparent)
                {
                    transparent.Add(entry.Id);
                }
            }
        }

        public static BlockEntry GetBlockByName(string name)
        {
            if (name == Air.Name)
            {
                return Air;
            }

            lock (_lock)
            {
                return _blocks[blockNameToIndex[name]];
            }
        }

        public static int GetBlockIdByName(string name)
        {
            if (name == Air.Name)
            {
                return Air.Id;
            }

            lock (_lock)
            {
                return blockNameToIndex[name] + 1;
            }
        }

        public static string GetBlockNameById(int id)
        {
            if (id == 0)
            {
                return Air.Name;
            }

            lock (_lock)
            {
                return indexToBlockName[id];
            }
        }

        public static BlockEntry GetBlockById(int id)
        {
            if (id == 0)
            {
                return Air;
            }

            lock (_lock)
            {
                return _blocks[id - 1];
            }
        }

        public static IEnumerable<BlockDescriptionPacked> GetDescriptionPackeds()
        {
            lock (_lock)
            {
                for (int i = 0; i < _descriptions.Count; i++)
                {
                    BlockDescription desc = _descriptions[i];
                    yield return desc;
                }
            }
        }

        public static bool IsTransparent(Block block)
        {
            return alphaTest.Contains(block);
        }

        public static bool IsSolid(Block block)
        {
            return !alphaTest.Contains(block);
        }
    }
}