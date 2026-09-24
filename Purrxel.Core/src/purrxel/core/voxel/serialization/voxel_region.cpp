#include "purrxel/core/voxel/serialization/voxel_region.hpp"

#include "purrxel/core/voxel/serialization/chunk_region_header.hpp"

namespace Purrxel::Core::Voxel::Serialization
{
    VoxelRegion::VoxelRegion()
    {
        for (auto& entry : seekTable)
        {
            entry.Position = -1;
            entry.Length = 0;
        }
    }

    void VoxelRegion::Serialize(Stream* stream)
    {
        ChunkRegionHeader::Write(stream);
        for (int32_t i = 0; i < CHUNK_REGION_SIZE_SQUARED; i++)
        {
            seekTable[i].Write(stream);
        }
        segmentsStart = stream->Position();
    }

    void VoxelRegion::Deserialize(Stream* stream)
    {
        segmentsLength = 0;
        ChunkRegionHeader::Read(stream);

        std::vector<FreeListEntry> usedRanges;
        for (int32_t i = 0; i < CHUNK_REGION_SIZE_SQUARED; i++)
        {
            seekTable[i].Read(stream);
            const auto& entry = seekTable[i];
            segmentsLength = std::max(segmentsLength, entry.Position + entry.Length);
            if (entry.Position != -1)
            {
                usedRanges.emplace_back(entry.Position, entry.Position + entry.Length);
            }
        }
        segmentsStart = stream->Position();

        std::sort(usedRanges.begin(), usedRanges.end(), [](const FreeListEntry& a, const FreeListEntry& b) { return a.Start < b.Start; });

        int64_t currentPosition = 0;
        for (const auto& range : usedRanges)
        {
            if (range.Start > currentPosition)
            {
                FreeListEntry entry(currentPosition, range.Start);
                freeList.push_back(entry);
                fragmentedBytes += entry.Length();
            }
            currentPosition = std::max(currentPosition, range.End);
        }
    }

    bool VoxelRegion::ReadSegment(Stream* baseStream, Stream* compressedStream, ChunkSegment* segment, Point2 point)
    {
        int32_t index = (point.Y << 5) + point.X;
        const auto& entry = seekTable[index];

        if (entry.Position == -1) return false;

        baseStream->Position(segmentsStart + entry.Position);
        segment->LoadFromStream(compressedStream);

        return true;
    }

    void VoxelRegion::WriteSegment(Stream* baseStream, Stream* compressedStream, ChunkSegment* segment, Point2 point)
    {
        int32_t index = (point.Y << 5) + point.X;
        VoxelRegionSeekTableEntry entry = seekTable[index];

        ChunkPreSerialized serializeds[ChunkSegment::CHUNK_SEGMENT_SIZE];
        segment->PreSerialize(serializeds);

        bool moved = entry.Position != -1;

        if (moved)
        {
            FreeEntry(baseStream, entry);
        }

        entry.Position = segmentsLength;

        baseStream->Position(segmentsStart + entry.Position);
        int64_t start = baseStream->Position();

        segment->Serialize(compressedStream, serializeds);
        compressedStream->Flush();

        int64_t end = baseStream->Position();
        int64_t size = end - start;

        entry.Length = size;
        segmentsLength += size;

        seekTable[index] = entry;
    }

    void VoxelRegion::FreeEntry(Stream* stream, VoxelRegionSeekTableEntry entry)
    {
        if (entry.Position + entry.Length == segmentsLength)
        {
            segmentsLength -= entry.Length;
            stream->Length(segmentsStart + segmentsLength);
            return;
        }

        int64_t start = entry.Position;
        int64_t end = entry.Position + entry.Length;
        fragmentedBytes += entry.Length;

        int32_t index = FindInsertionIndex(start);

        if (index > 0 && freeList[index - 1].End == start)
        {
            freeList[index - 1] = FreeListEntry(freeList[index - 1].Start, end);
        }
        else if (index < static_cast<int32_t>(freeList.size()) && freeList[index].Start == end)
        {
            freeList[index] = FreeListEntry(start, freeList[index].End);
        }
        else
        {
            freeList.insert(freeList.begin() + index, FreeListEntry(start, end));
        }

        if (static_cast<double>(fragmentedBytes) > static_cast<double>(segmentsLength) * 0.4)
        {
            Defragment(stream);
        }
    }

    int32_t VoxelRegion::FindInsertionIndex(int64_t start) const
    {
        int32_t low = 0, high = static_cast<int32_t>(freeList.size()) - 1;

        while (low <= high)
        {
            int32_t mid = (low + high) / 2;

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

        return std::min(low, static_cast<int32_t>(freeList.size()));
    }

    void VoxelRegion::Defragment(Stream* stream)
    {
        for (size_t i = 0; i < freeList.size(); i++)
        {
            FreeListEntry current = freeList[i];
            if (i == freeList.size() - 1)
            {
                MoveBlock(stream, current.End, current.Start, segmentsLength - current.End);
                goto end;
            }

            FreeListEntry& next = freeList[i + 1];

            // Move data X to start of free range and add it to the next.
            // 000XXX000XXX (0)
            // XXX000000XXX (1)
            // XXXXXX       (2)

            int64_t length = next.Start - current.End;
            MoveBlock(stream, current.End, current.Start, length);
            next.Start = current.Start + length;
        }

    end:
        segmentsLength -= fragmentedBytes;
        stream->Length(segmentsStart + segmentsLength); // trim excess.
        freeList.clear();
        fragmentedBytes = 0;
    }

    void VoxelRegion::MoveBlock(Stream* stream, int64_t fromPos, int64_t toPos, int64_t length)
    {
        constexpr int32_t bufferSize = 8192;
        uint8_t buffer[bufferSize];

        int64_t readerPosition = segmentsStart + fromPos;
        int64_t writerPosition = segmentsStart + toPos;

        int64_t toMove = length;
        while (toMove > 0)
        {
            int32_t toRead = static_cast<int32_t>(std::min<int64_t>(toMove, bufferSize));
            stream->Position(readerPosition);
            stream->Read(buffer, toRead);
            readerPosition += toRead;
            stream->Position(writerPosition);
            stream->Write(buffer, toRead);
            writerPosition += toRead;
            toMove -= toRead;
        }

        int64_t offset = toPos - fromPos;
        int64_t endBlock = toPos + length;
        for (int32_t i = 0; i < CHUNK_REGION_SIZE_SQUARED; i++)
        {
            VoxelRegionSeekTableEntry entry = seekTable[i];

            if (entry.Position > fromPos && entry.Position < endBlock)
            {
                entry.Position -= offset;
                seekTable[i] = entry;
            }
        }
    }

    void VoxelRegion::Flush(Stream* stream)
    {
        stream->Position(0);
        Serialize(stream);
        stream->Flush();
    }

    bool VoxelRegion::Exists(Point2 point) const
    {
        int32_t index = (point.Y << 5) + point.X;
        const auto& entry = seekTable[index];
        return entry.Position != -1 && entry.Length != 0;
    }
}
