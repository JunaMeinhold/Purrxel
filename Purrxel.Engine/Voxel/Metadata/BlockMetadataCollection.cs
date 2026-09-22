namespace VoxelEngine.Voxel.Metadata
{
    using Hexa.NET.Utilities;
    using System.Collections;
    using VoxelEngine.Voxel.Serialization;

    public struct BlockMetadataCollection : IList<BlockMetadata>, ICollection<BlockMetadata>
    {
        private UnsafeList<BlockMetadata> metadata;

        public void Release()
        {
            foreach (var meta in metadata)
            {
                meta.Release();
            }
            metadata.Release();
        }

        public BlockMetadata this[int index] { get => metadata[index]; set => metadata[index] = value; }

        public readonly int Count => metadata.Count;

        public readonly bool IsReadOnly => false;

        public void Add(BlockMetadata item)
        {
            metadata.Add(item);
        }

        public void Clear()
        {
            metadata.Clear();
        }

        public bool Contains(BlockMetadata item)
        {
            return metadata.Contains(item);
        }

        public readonly void CopyTo(BlockMetadata[] array, int arrayIndex)
        {
            metadata.AsSpan().CopyTo(array.AsSpan(arrayIndex));
        }

        public readonly IEnumerator<BlockMetadata> GetEnumerator()
        {
            return metadata.GetEnumerator();
        }

        public int IndexOf(BlockMetadata item)
        {
            return metadata.IndexOf(item);
        }

        public void Insert(int index, BlockMetadata item)
        {
            metadata.Insert(index, item);
        }

        public bool Remove(BlockMetadata item)
        {
            return metadata.Remove(item);
        }

        public void RemoveAt(int index)
        {
            metadata.RemoveAt(index);
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return metadata.GetEnumerator();
        }

        public void Serialize(Stream stream)
        {
            BlockMetadataCollectionHeader.Write(stream, metadata.Count);
            for (int i = 0; i < metadata.Count; i++)
            {
                BlockMetadata data = metadata[i];
                data.Write(stream);
            }
        }

        public readonly int SizeOf()
        {
            return BlockMetadataCollectionHeader.Size + metadata.Sum(x => x.Length + BlockMetadata.StaticSize);
        }

        public void Deserialize(Stream stream)
        {
            BlockMetadataCollectionHeader.Read(stream, out int metadataCount);
            metadata.Capacity = metadataCount;
            for (int i = 0; i < metadataCount; i++)
            {
                metadata.Add(BlockMetadata.ReadFrom(stream));
            }
        }
    }
}