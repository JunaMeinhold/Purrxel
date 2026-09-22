namespace VoxelEngine.Voxel.Serialization
{
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities;
    using System.IO;

    public struct VoxelRegion
    {
        public const int CHUNK_REGION_SIZE = 32;
        public const int CHUNK_REGION_SIZE_SQUARED = 32 * 32;

        private Point2 position;
        private VoxelRegionSeekTable seekTable;

        private UnsafeList<FreeListEntry> freeList = [];
        private long fragmentedBytes;

        private long segmentsStart;
        private long segmentsLength;

        public VoxelRegion()
        {
            seekTable.AsSpan().Fill(new() { Position = -1, Length = 0 });
        }

        public void Serialize(Stream stream)
        {
            ChunkRegionHeader.Write(stream);
            for (int i = 0; i < CHUNK_REGION_SIZE_SQUARED; i++)
            {
                seekTable[i].Write(stream);
            }
            segmentsStart = stream.Position;
        }

        public unsafe void Deserialize(Stream stream)
        {
            segmentsLength = 0;
            ChunkRegionHeader.Read(stream);
            UnsafeList<(long Start, long End)> usedRanges = [];
            for (int i = 0; i < CHUNK_REGION_SIZE_SQUARED; i++)
            {
                seekTable[i].Read(stream);
                var entry = seekTable[i];
                segmentsLength = Math.Max(segmentsLength, entry.Position + entry.Length);
                if (entry.Position != -1)
                {
                    usedRanges.Add((entry.Position, entry.Position + entry.Length));
                }
            }
            segmentsStart = stream.Position;

            QSort(usedRanges.Data, usedRanges.Size, (a, b) => a.Start.CompareTo(b.Start));

            long currentPosition = 0;
            foreach (var (Start, End) in usedRanges)
            {
                if (Start > currentPosition)
                {
                    FreeListEntry entry = new(currentPosition, Start);
                    freeList.Add(entry);
                    fragmentedBytes += entry.Length;
                }
                currentPosition = Math.Max(currentPosition, End);
            }
            usedRanges.Release();
        }

        public readonly unsafe bool ReadSegment(Stream baseStream, Stream compressedStream, World world, ChunkSegment* segment, Point2 point)
        {
            int index = (point.Y << 5) + point.X;
            var entry = seekTable[index];

            if (entry.Position == -1) return false;

            baseStream.Position = segmentsStart + entry.Position;
            segment->LoadFromStream(compressedStream, world);

            return true;
        }

        public unsafe void WriteSegment(Stream baseStream, Stream compressedStream, ChunkSegment* segment, Point2 point)
        {
            int index = (point.Y << 5) + point.X;
            var entry = seekTable[index];

            Span<ChunkPreSerialized> serializeds = stackalloc ChunkPreSerialized[ChunkSegment.CHUNK_SEGMENT_SIZE];
            segment->PreSerialize(serializeds);

            bool moved = entry.Position != -1;

            if (moved)
            {
                FreeEntry(baseStream, entry);
            }

            entry.Position = segmentsLength;

            long start = baseStream.Position = segmentsStart + entry.Position;

            segment->Serialize(compressedStream, serializeds);
            compressedStream.Flush();

            long end = baseStream.Position;
            long size = end - start;

            entry.Length = size;
            segmentsLength += size;

            seekTable[index] = entry;
        }

        private void FreeEntry(Stream stream, VoxelRegionSeekTableEntry entry)
        {
            if (entry.Position + entry.Length == segmentsLength)
            {
                segmentsLength -= entry.Length;
                stream.SetLength(segmentsStart + segmentsLength);
                return;
            }

            long start = entry.Position;
            long end = entry.Position + entry.Length;
            fragmentedBytes += entry.Length;

            int index = FindInsertionIndex(start);

            if (index > 0 && freeList[index - 1].End == start)
            {
                freeList[index - 1] = new(freeList[index - 1].Start, end);
            }
            else if (index < freeList.Count && freeList[index].Start == end)
            {
                freeList[index] = new(start, freeList[index].End);
            }
            else
            {
                freeList.Insert(index, new(start, end));
            }

            if (fragmentedBytes > segmentsLength * 0.4f)
            {
                Defragment(stream);
            }
        }

        private int FindInsertionIndex(long start)
        {
            int low = 0, high = freeList.Count - 1;

            while (low <= high)
            {
                int mid = (low + high) / 2;

                if (freeList[mid].Start == start)
                {
                    return mid;
                }
                else if (freeList[mid].Start < start)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return Math.Min(low, freeList.Count);
        }

        private unsafe void Defragment(Stream stream)
        {
            for (int i = 0; i < freeList.Size; i++)
            {
                var current = freeList[i];
                if (i == freeList.Size - 1)
                {
                    MoveBlock(stream, current.End, current.Start, segmentsLength - current.End);
                    goto end;
                }

                var next = freeList.GetPointer(i + 1);

                // Move data X to start of free range and add it to the next.
                // 000XXX000XXX (0)
                // XXX000000XXX (1)
                // XXXXXX       (2)

                long length = next->Start - current.End;
                MoveBlock(stream, current.End, current.Start, length);
                next->Start = current.Start + length;
            }

        end:
            segmentsLength -= fragmentedBytes;
            stream.SetLength(segmentsStart + segmentsLength); // trim excess.
            freeList.Clear();
            fragmentedBytes = 0;
            return;
        }

        private void MoveBlock(Stream stream, long fromPos, long toPos, long length)
        {
            const int bufferSize = 8192;
            Span<byte> buffer = stackalloc byte[bufferSize];

            long readerPosition = segmentsStart + fromPos;
            long writerPosition = segmentsStart + toPos;

            long toMove = length;
            while (toMove > 0)
            {
                int toRead = (int)Math.Min(toMove, bufferSize);
                var span = buffer[..toRead];
                stream.Position = readerPosition;
                stream.ReadExactly(span);
                readerPosition += toRead;
                stream.Position = writerPosition;
                stream.Write(span);
                writerPosition += toRead;
            }

            long offset = toPos - fromPos;
            long endBlock = toPos + length;
            for (int i = 0; i < CHUNK_REGION_SIZE_SQUARED; i++)
            {
                var entry = seekTable[i];

                if (entry.Position > fromPos && entry.Position < endBlock)
                {
                    entry.Position -= offset;
                    seekTable[i] = entry;
                }
            }
        }

        public void Flush(Stream stream)
        {
            stream.Position = 0;
            Serialize(stream);
            stream.Flush();
        }

        public readonly bool Exists(Point2 point)
        {
            int index = (point.Y << 5) + point.X;
            var entry = seekTable[index];

            return entry.Position != -1 && entry.Length != 0;
        }

        public void Release()
        {
            freeList.Release();
        }
    }
}