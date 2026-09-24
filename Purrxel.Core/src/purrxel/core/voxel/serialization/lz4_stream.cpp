#include "purrxel/core/voxel/serialization/lz4_stream.hpp"

#include "purrxel/core/allocator.h"

#include <lz4.h>
#include <lz4hc.h>

namespace Purrxel::Core::Voxel::Serialization
{
    Lz4Stream::Lz4Stream(Stream* stream, int32_t blockSize, StreamMode mode, int32_t compressionLevel)
        : Stream(sizeof(Lz4Stream), this, Lz4StreamRead, Lz4StreamWrite, nullptr, Lz4StreamPosition, Lz4StreamLength, nullptr, Lz4StreamFlush, Lz4StreamClose),
          blockSize(blockSize),
          mode(mode),
          compressionLevel(compressionLevel),
          outputSize(LZ4_compressBound(blockSize) + 4),
          innerStream(stream),
          rawBuffer(static_cast<uint8_t*>(Purrxel_Alloc(static_cast<size_t>(blockSize)))),
          compressedBuffer(static_cast<uint8_t*>(Purrxel_Alloc(static_cast<size_t>(outputSize)))),
          bufferPosition(0),
          bufferedSize(0)
    {
    }

    ObjPtr<Lz4Stream> Lz4Stream::Create(Stream* stream, int32_t blockSize, StreamMode mode, int32_t compressionLevel)
    {
        return ObjPtr<Lz4Stream>::Attach(new Lz4Stream(stream, blockSize, mode, compressionLevel));
    }

    void Lz4Stream::Reset(Stream* stream, StreamMode newMode)
    {
        innerStream = stream;
        mode = newMode;
        bufferPosition = 0;
        bufferedSize = 0;
    }

    void Lz4Stream::WriteFrame()
    {
        int32_t written = LZ4_compress_HC(reinterpret_cast<const char*>(rawBuffer), reinterpret_cast<char*>(compressedBuffer + 4), bufferPosition, outputSize - 4, compressionLevel);

        int32_t writtenLE = EndianUtils::ToLittleEndian(written);
        std::memcpy(compressedBuffer, &writtenLE, sizeof(writtenLE));

        innerStream->Write(compressedBuffer, static_cast<size_t>(written + 4));
        bufferPosition = 0;
    }

    void Lz4Stream::ReadFrame()
    {
        innerStream->Read(compressedBuffer, 4);
        int32_t compressedSize;
        std::memcpy(&compressedSize, compressedBuffer, sizeof(compressedSize));
        compressedSize = EndianUtils::FromLittleEndian(compressedSize);

        innerStream->Read(compressedBuffer + 4, static_cast<size_t>(compressedSize));

        bufferedSize = LZ4_decompress_safe(reinterpret_cast<const char*>(compressedBuffer + 4), reinterpret_cast<char*>(rawBuffer), compressedSize, blockSize);
        bufferPosition = 0;
    }

    size_t Lz4Stream::Lz4StreamRead(void* userdata, void* buffer, size_t size)
    {
        Lz4Stream* self = static_cast<Lz4Stream*>(userdata);
        uint8_t* dst = static_cast<uint8_t*>(buffer);
        size_t remaining = size;
        size_t read = 0;

        while (remaining > 0)
        {
            if (self->bufferPosition == self->bufferedSize)
            {
                self->ReadFrame();
            }

            int32_t available = self->bufferedSize - self->bufferPosition;
            int32_t toRead = static_cast<int32_t>(std::min<size_t>(static_cast<size_t>(available), remaining));
            std::memcpy(dst, self->rawBuffer + self->bufferPosition, static_cast<size_t>(toRead));

            self->bufferPosition += toRead;
            remaining -= static_cast<size_t>(toRead);
            dst += toRead;
            read += static_cast<size_t>(toRead);
        }

        return read;
    }

    size_t Lz4Stream::Lz4StreamWrite(void* userdata, const void* buffer, size_t size)
    {
        Lz4Stream* self = static_cast<Lz4Stream*>(userdata);
        const uint8_t* src = static_cast<const uint8_t*>(buffer);
        size_t remaining = size;

        while (remaining > 0)
        {
            int32_t space = self->blockSize - self->bufferPosition;
            int32_t toWrite = static_cast<int32_t>(std::min<size_t>(static_cast<size_t>(space), remaining));
            std::memcpy(self->rawBuffer + self->bufferPosition, src, static_cast<size_t>(toWrite));

            self->bufferPosition += toWrite;
            remaining -= static_cast<size_t>(toWrite);
            src += toWrite;

            if (self->bufferPosition == self->blockSize)
            {
                self->WriteFrame();
            }
        }

        return size;
    }

    int64_t Lz4Stream::Lz4StreamPosition(void* userdata)
    {
        Lz4Stream* self = static_cast<Lz4Stream*>(userdata);
        return self->innerStream->Position();
    }

    int64_t Lz4Stream::Lz4StreamLength(void* userdata)
    {
        Lz4Stream* self = static_cast<Lz4Stream*>(userdata);
        return self->innerStream->Length();
    }

    void Lz4Stream::Lz4StreamFlush(void* userdata)
    {
        Lz4Stream* self = static_cast<Lz4Stream*>(userdata);
        if (self->mode == StreamMode::Write)
        {
            if (self->bufferPosition > 0)
            {
                self->WriteFrame();
            }
            self->innerStream->Flush();
        }
    }

    void Lz4Stream::Lz4StreamClose(void* userdata)
    {
        Lz4Stream* self = static_cast<Lz4Stream*>(userdata);
        if (self->rawBuffer != nullptr)
        {
            Purrxel_Free(self->rawBuffer);
            self->rawBuffer = nullptr;
        }
        if (self->compressedBuffer != nullptr)
        {
            Purrxel_Free(self->compressedBuffer);
            self->compressedBuffer = nullptr;
        }
    }
}
