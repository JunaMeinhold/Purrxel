namespace VoxelEngine.Voxel.Serialization
{
    using K4os.Compression.LZ4;
    using System.Buffers.Binary;
    using System.IO;
    using System.Runtime.CompilerServices;

    public unsafe class UnsafeLZ4Stream : Stream
    {
        private readonly int blockSize;
        private StreamMode mode;
        private readonly LZ4Level level;
        private readonly int outputSize;
        private Stream innerStream;
        private byte* rawBuffer;
        private byte* compressedBuffer;

        private int bufferPosition;
        private int bufferedSize;

        public override bool CanRead => mode == StreamMode.Read;

        public override bool CanSeek { get; }

        public override bool CanWrite => mode == StreamMode.Write;

        public override long Length { get => innerStream.Length; }

        public override long Position { get => innerStream.Position; set => innerStream.Position = value; }

        public UnsafeLZ4Stream(Stream stream, int blockSize, StreamMode mode, LZ4Level level)
        {
            innerStream = stream;
            this.blockSize = blockSize;
            this.mode = mode;
            this.level = level;
            outputSize = LZ4Codec.MaximumOutputSize(blockSize) + 4;
            rawBuffer = AllocT<byte>(blockSize);
            compressedBuffer = AllocT<byte>(outputSize);
        }

        public void Reset(Stream stream, StreamMode mode)
        {
            innerStream = stream;
            this.mode = mode;
            bufferPosition = 0;
            bufferedSize = 0;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            fixed (byte* pBuffer = buffer)
            {
                Write(pBuffer, buffer.Length);
            }
        }

        public override void Write(byte[] buffer, int offset, int length)
        {
            if (buffer.Length < offset + length) throw new ArgumentOutOfRangeException(nameof(buffer), "Offset or length exceeded the buffer size.");
            fixed (byte* pBuffer = buffer)
            {
                Write(pBuffer + offset, length);
            }
        }

        public void Write(byte* buffer, int length)
        {
            if (mode != StreamMode.Write) throw new InvalidOperationException("Cannot write to an read-only stream.");
            while (length > 0)
            {
                int remaining = blockSize - bufferPosition;
                int toWrite = Math.Min(remaining, length);
                MemcpyT(buffer, rawBuffer + bufferPosition, toWrite);
                bufferPosition += toWrite;
                length -= toWrite;
                buffer += toWrite;
                if (bufferPosition == blockSize)
                {
                    WriteFrame();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteFrame()
        {
            int written = LZ4Codec.Encode(rawBuffer, bufferPosition, compressedBuffer + 4, outputSize - 4, level);
            Span<byte> buffer = new(compressedBuffer, written + 4);
            BinaryPrimitives.WriteInt32LittleEndian(buffer, written);
            innerStream.Write(buffer);
            bufferPosition = 0;
        }

        public override int Read(Span<byte> buffer)
        {
            fixed (byte* pBuffer = buffer)
            {
                return Read(pBuffer, buffer.Length);
            }
        }

        public override int Read(byte[] buffer, int offset, int length)
        {
            if (buffer.Length < offset + length) throw new ArgumentOutOfRangeException(nameof(buffer), "Offset or length exceeded the buffer size.");
            fixed (byte* pBuffer = buffer)
            {
                return Read(pBuffer + offset, length);
            }
        }

        public int Read(byte* buffer, int length)
        {
            if (mode != StreamMode.Read) throw new InvalidOperationException("Cannot read from an write-only stream.");
            int read = 0;
            while (length > 0)
            {
                if (bufferPosition == bufferedSize)
                {
                    ReadFrame();
                }

                int remaining = bufferedSize - bufferPosition;
                int toRead = Math.Min(remaining, length);
                MemcpyT(rawBuffer + bufferPosition, buffer, toRead);

                bufferPosition += toRead;
                length -= toRead;
                buffer += toRead;
                read += toRead;
            }

            return read;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadFrame()
        {
            Span<byte> lengthBuffer = new(compressedBuffer, 4);
            innerStream.ReadExactly(lengthBuffer);
            int compressedSize = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);

            innerStream.ReadExactly(new Span<byte>(compressedBuffer + 4, compressedSize));

            bufferedSize = LZ4Codec.Decode(compressedBuffer + 4, compressedSize, rawBuffer, blockSize);
            bufferPosition = 0;
        }

        public override void Flush()
        {
            if (mode == StreamMode.Write)
            {
                if (bufferPosition > 0)
                {
                    WriteFrame();
                }
                innerStream.Flush();
            }
        }

        public void Release()
        {
            if (rawBuffer != null)
            {
                Free(rawBuffer);
                rawBuffer = null;
            }
            if (compressedBuffer != null)
            {
                Free(compressedBuffer);
                compressedBuffer = null;
            }
        }

        protected override void Dispose(bool value)
        {
            Flush();
            Release();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}