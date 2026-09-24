#pragma once

#include "purrxel/core/io/stream.hpp"
#include "purrxel/utils/memory.hpp"
#include "purrxel/core/voxel/serialization/stream_mode.hpp"

namespace Purrxel::Core::Voxel::Serialization
{
    class Lz4Stream : public Stream
    {
        int32_t blockSize;
        StreamMode mode;
        int32_t compressionLevel;
        int32_t outputSize;
        Stream* innerStream;
        uint8_t* rawBuffer;
        uint8_t* compressedBuffer;

        int32_t bufferPosition;
        int32_t bufferedSize;

        Lz4Stream(Stream* stream, int32_t blockSize, StreamMode mode, int32_t compressionLevel);

    public:
        [[nodiscard]] static ObjPtr<Lz4Stream> Create(Stream* stream, int32_t blockSize, StreamMode mode, int32_t compressionLevel = 10);

        void Reset(Stream* stream, StreamMode mode);

    private:
        void WriteFrame();
        void ReadFrame();

        static size_t Lz4StreamRead(void* userdata, void* buffer, size_t size);
        static size_t Lz4StreamWrite(void* userdata, const void* buffer, size_t size);
        static int64_t Lz4StreamPosition(void* userdata);
        static int64_t Lz4StreamLength(void* userdata);
        static void Lz4StreamFlush(void* userdata);
        static void Lz4StreamClose(void* userdata);
    };
}
