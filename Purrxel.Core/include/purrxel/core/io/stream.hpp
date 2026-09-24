#ifndef STREAM_HPP
#define STREAM_HPP

#include "purrxel/core/allocator.h"
#include "purrxel/core/config.h"
#include "purrxel/core/io/stream.h"
#include "purrxel/utils/memory.hpp"
#include "purrxel/utils/span.hpp"
#include "purrxel/utils/endianness.hpp"

#include "purrxel/pch/std.hpp"

#if defined(_WIN32)
#include <io.h>
#else
#include <unistd.h>
#endif

namespace Purrxel
{
	static constexpr size_t LEB128Size(uint32_t v)
	{
		if (v < (1u << 7))  return 1;
		if (v < (1u << 14)) return 2;
		if (v < (1u << 21)) return 3;
		if (v < (1u << 28)) return 4;
		return 5;
	}

	static constexpr size_t LEB128Size(uint64_t v)
	{
		if (v < (1u << 7))  return 1;
		if (v < (1u << 14)) return 2;
		if (v < (1u << 21)) return 3;
		if (v < (1u << 28)) return 4;
		if (v < (1ull << 35)) return 5;
		if (v < (1ull << 42)) return 6;
		if (v < (1ull << 49)) return 7;
		if (v < (1ull << 56)) return 8;
		if (v < (1ull << 63)) return 9;
		return 10;
	}



	template<std::signed_integral T>
	static constexpr std::make_unsigned_t<T> EncodeZigZag(T value)
	{
		using UT = std::make_unsigned_t<T>;
		constexpr size_t bits = sizeof(T) * 8;
		return static_cast<UT>((value << 1) ^ (value >> (bits - 1)));
	}

	template<std::signed_integral T>
	static constexpr T DecodeZigZag(std::make_unsigned_t<T> value)
	{
		return static_cast<T>((value >> 1)) ^ -static_cast<T>((value & 1));
	}

	class Stream
	{
	private:
		std::atomic<uint32_t> refCount = { 1 };

	protected:
		size_t version;
		void* userdata;
		StreamReadFunc readFunc;
		StreamWriteFunc writeFunc;
		StreamSeekFunc seekFunc;
		StreamGetPositionFunc getPositionFunc;
		StreamGetLengthFunc getLengthFunc;
		StreamSetLengthFunc setLengthFunc;
		StreamFlushFunc flushFunc;
		StreamCloseFunc closeFunc;

	public:
		Stream(size_t version, void* userdata, const StreamReadFunc& readFunc, const StreamWriteFunc& writeFunc, const StreamSeekFunc& seekFunc, const StreamGetPositionFunc& getPositionFunc, const StreamGetLengthFunc& getLengthFunc, const StreamSetLengthFunc& setLengthFunc, const StreamFlushFunc& flushFunc, const StreamCloseFunc& closeFunc)
			: version(version), userdata(userdata), readFunc(readFunc), writeFunc(writeFunc), seekFunc(seekFunc), getPositionFunc(getPositionFunc), getLengthFunc(getLengthFunc), setLengthFunc(setLengthFunc), flushFunc(flushFunc), closeFunc(closeFunc)
		{
		}

	protected:
		~Stream()
		{
			Close();
		}

	public:
		uint32_t AddRef()
		{
			return refCount.fetch_add(1, std::memory_order_acq_rel);
		}

		void Release()
		{
			if (refCount.fetch_sub(1, std::memory_order_acq_rel) == 1)
			{
				delete this;
			}
		}

		size_t Read(void* buffer, size_t size) const
		{
			if (readFunc)
			{
				return readFunc(userdata, buffer, size);
			}
			return EOF;
		}

		size_t Write(const void* buffer, size_t size) const
		{
			if (writeFunc)
			{
				return writeFunc(userdata, buffer, size);
			}
			return EOF;
		}

		template<typename T>
		size_t Write(const T& val) const
		{
			return Write(&val, sizeof(T));
		}

		template<typename T>
		size_t WriteLittleEndian(const T& val) const
		{
			return Write(EndianUtils::ToLittleEndian(val));
		}

		template<typename T>
		size_t WriteBigEndian(const T& val) const
		{
			return Write(EndianUtils::ToBigEndian(val));
		}

		int64_t Seek(int64_t offset, SeekOrigin origin) const
		{
			if (seekFunc)
			{
				return seekFunc(userdata, offset, origin);
			}
			return EOF;
		}

		int64_t Position() const
		{
			if (getPositionFunc)
			{
				return getPositionFunc(userdata);
			}
			return EOF;
		}

		void Position(int64_t position) const
		{
			Seek(position, SeekOrigin_Begin);
		}

		struct SeekScope
		{
			Stream* stream = nullptr;
			int64_t oldPos = -1;

			SeekScope(Stream* stream, int64_t oldPos) : stream(stream), oldPos(oldPos)
			{
			}

			SeekScope(const SeekScope&) = delete;
			SeekScope& operator=(const SeekScope&) = delete;

			SeekScope(SeekScope&& other) noexcept : stream(other.stream), oldPos(other.oldPos)
			{
				other.stream = nullptr;
				other.oldPos = -1;
			}

			SeekScope& operator=(SeekScope&& other) noexcept
			{
				if (this != &other)
				{
					Reset();

					stream = other.stream;
					oldPos = other.oldPos;

					other.stream = nullptr;
					other.oldPos = -1;
				}

				return *this;
			}

			void Reset()
			{
				if (stream && oldPos > -1)
				{
					stream->Position(oldPos);
					oldPos = -1;
				}
			}

			~SeekScope()
			{
				Reset();
			}
		};

		[[nodiscard]] SeekScope BeginSeek(int64_t offset, SeekOrigin origin)
		{
			auto old = Position();
			Seek(offset, origin);
			return SeekScope(this, old);
		}

		[[nodiscard]] SeekScope BeginJump(int64_t position)
		{
			auto old = Position();
			Position(position);
			return SeekScope(this, old);
		}

		[[nodiscard]] SeekScope BeginJump()
		{
			auto old = Position();
			return SeekScope(this, old);
		}

		int64_t Length() const
		{
			if (getLengthFunc)
			{
				return getLengthFunc(userdata);
			}
			return EOF;
		}

		bool Length(int64_t length)
		{
			if (setLengthFunc)
			{
				return setLengthFunc(userdata, length);
			}
			return false;
		}

		void Flush() const
		{
			if (flushFunc)
			{
				flushFunc(userdata);
			}
		}

		void Close()
		{
			if (closeFunc)
			{
				Flush();
				closeFunc(userdata);
				userdata = nullptr;
				readFunc = nullptr;
				writeFunc = nullptr;
				seekFunc = nullptr;
				getPositionFunc = nullptr;
				getLengthFunc = nullptr;
				setLengthFunc = nullptr;
				flushFunc = nullptr;
				closeFunc = nullptr;
			}
		}

		template<typename T>
		T Read() const
		{
			T val{};
			Read(&val, sizeof(T));
			return val;
		}

		uint8_t ReadByte()
		{
			return Read<uint8_t>();
		}

		template<typename T>
		T ReadLittleEndian() const
		{
			return EndianUtils::FromLittleEndian(Read<T>());
		}

		template<typename T>
		T ReadBigEndian() const
		{
			return EndianUtils::FromBigEndian(Read<T>());
		}

		void WriteUInt(uint32_t value) const
		{
			WriteLittleEndian(value);
		}

		uint32_t ReadUInt() const
		{
			return ReadLittleEndian<uint32_t>();
		}

		void WriteString(const std::string& str)
		{
			uint32_t len = static_cast<uint32_t>(str.size());
			WriteLEB128(len);
			if (len == 0) return;
			Write(str.data(), len);
		}

		void WriteString(const StringSpan& str)
		{
			uint32_t len = static_cast<uint32_t>(str.size());
			WriteLEB128(len);
			if (len == 0) return;
			Write(str.data(), len);
		}

		std::string ReadString()
		{
			uint32_t len = ReadLEB128<uint32_t>();
			std::string result(len, '\0');
			Read(result.data(), len);
			return result;
		}

		std::string ReadAllText() const
		{
			auto len = Length();
			if (len <= 0) return {};

			size_t lenU = static_cast<size_t>(len);
			std::string result;
			result.resize(lenU);
			Read(result.data(), lenU);
			return result;
		}

		template<std::unsigned_integral T>
		void WriteLEB128(T value)
		{
			constexpr size_t size = sizeof(T);
			constexpr size_t lebMaxCount = DivCeil(size * 8, 7);
			uint8_t buf[lebMaxCount]{};
			size_t i = 0;
			do
			{
				uint8_t b = value & 0x7F;
				value >>= 7;
				b |= (value != 0) << 7;
				buf[i++] = b;
			} while (value);
			Write(buf, i);
		}

		template<typename T>
			requires std::is_enum_v<T>&& std::integral<std::underlying_type_t<T>>
		void WriteLEB128(T value)
		{
			WriteLEB128(static_cast<std::underlying_type_t<T>>(value));
		}

		template<std::signed_integral T>
		void WriteLEB128(T value)
		{
			WriteLEB128(EncodeZigZag(value));
		}

		template<std::unsigned_integral T>
		T ReadLEB128()
		{
			constexpr size_t size = sizeof(T);
			constexpr size_t lebMaxCount = DivCeil(size * 8, 7);
			T value = 0;
			size_t shift = 0;
			uint8_t b;
			do
			{
				b = ReadByte();
				value |= static_cast<T>(b & 0x7F) << (shift * 7);
				shift++;
			} while (b & 0x80 && shift < lebMaxCount);

			PURRXEL_ASSERT((b & 0x80) == 0, "Invalid LEB128 value.");

			return value;
		}

		template<typename T>
			requires std::is_enum_v<T>&& std::integral<std::underlying_type_t<T>>
		T ReadLEB128()
		{
			return static_cast<T>(ReadLEB128<std::underlying_type_t<T>>());
		}

		template<std::signed_integral T>
		T ReadLEB128()
		{
			return DecodeZigZag<T>(ReadLEB128<std::make_unsigned_t<T>>());
		}
	};

	class FileStream : public Stream
	{
		FILE* file;
		FileStream(FILE* file)
			: Stream(sizeof(FileStream), this, FileStreamRead, FileStreamWrite, FileStreamSeek, FileStreamPosition, FileStreamLength, FileStreamSetLength, FileStreamFlush, FileStreamClose),
			file(file)
		{
		}

	public:
		[[nodiscard]] static ObjPtr<FileStream> OpenRead(const char* path)
		{
			FILE* file;
			auto error = fopen_s(&file, path, "rb");
			if (error != 0 || file == nullptr)
			{
				return {};
			}
			return ObjPtr<FileStream>::Attach(new FileStream(file));
		}

		[[nodiscard]] static ObjPtr<FileStream> OpenCreate(const char* path)
		{
			FILE* file;
			auto error = fopen_s(&file, path, "wb+");
			if (error != 0 || file == nullptr)
			{
				return {};
			}
			return ObjPtr<FileStream>::Attach(new FileStream(file));
		}

		[[nodiscard]] static ObjPtr<FileStream> Open(const char* path, const char* mode)
		{
			FILE* file;
			auto error = fopen_s(&file, path, mode);
			if (error != 0 || file == nullptr)
			{
				return {};
			}
			return ObjPtr<FileStream>::Attach(new FileStream(file));
		}

	private:

		static size_t FileStreamRead(void* userdata, void* buffer, size_t size)
		{
			FileStream* fs = static_cast<FileStream*>(userdata);
			return fread(buffer, 1, size, fs->file);
		}

		static size_t FileStreamWrite(void* userdata, const void* buffer, size_t size)
		{
			FileStream* fs = static_cast<FileStream*>(userdata);
			return fwrite(buffer, 1, size, fs->file);
		}

		static int64_t FileStreamSeek(void* userdata, int64_t offset, SeekOrigin origin)
		{
			FileStream* fs = static_cast<FileStream*>(userdata);
			int originFlag = SEEK_SET;
			switch (origin)
			{
			case SeekOrigin_Begin: originFlag = SEEK_SET; break;
			case SeekOrigin_Current: originFlag = SEEK_CUR; break;
			case SeekOrigin_End: originFlag = SEEK_END; break;
			}
			if (fseek(fs->file, static_cast<long>(offset), originFlag) != 0)
				return -1;
			return ftell(fs->file);
		}

		static int64_t FileStreamPosition(void* userdata)
		{
			FileStream* fs = static_cast<FileStream*>(userdata);
			return ftell(fs->file);
		}

		static int64_t FileStreamLength(void* userdata)
		{
			FileStream* fs = static_cast<FileStream*>(userdata);
			long current = ftell(fs->file);
			fseek(fs->file, 0, SEEK_END);
			long length = ftell(fs->file);
			fseek(fs->file, current, SEEK_SET);
			return length;
		}

		static bool FileStreamSetLength(void* userdata, int64_t length)
		{
			FileStream* fs = static_cast<FileStream*>(userdata);
			auto file = fs->file;

			fflush(fs->file);

#if defined(_WIN32)
			return _chsize_s(_fileno(fs->file), static_cast<__int64>(length)) == 0;
#else
			return ftruncate(fileno(fs->file), static_cast<off_t>(length)) == 0;
#endif
		}

		static void FileStreamFlush(void* userdata)
		{
			FileStream* fs = static_cast<FileStream*>(userdata);
			fflush(fs->file);
		}

		static void FileStreamClose(void* userdata)
		{
			FileStream* fs = static_cast<FileStream*>(userdata);
			if (fs->file != nullptr)
			{
				fclose(fs->file);
				fs->file = nullptr;
			}
		}
	};

	class MemoryStream : public Stream
	{
	private:
		uint8_t* buffer;
		size_t position;
		size_t length;
		size_t capacity;
		bool isDynamic;

		void Reset()
		{
			buffer = nullptr;
			position = 0;
			length = 0;
			capacity = 0;
		}

		MemoryStream(uint8_t* buffer, size_t size, bool isDynamic) : Stream(sizeof(MemoryStream), this, MemoryStreamRead, MemoryStreamWrite, MemoryStreamSeek, MemoryStreamPosition, MemoryStreamLength, MemoryStreamSetLength, MemoryStreamFlush, MemoryStreamClose),
			buffer(buffer), position(0), length(size), capacity(size), isDynamic(isDynamic)
		{
		}

		MemoryStream(size_t capacity) : Stream(sizeof(MemoryStream), this, MemoryStreamRead, MemoryStreamWrite, MemoryStreamSeek, MemoryStreamPosition, MemoryStreamLength, MemoryStreamSetLength, MemoryStreamFlush, MemoryStreamClose),
			buffer((uint8_t*)Purrxel_Alloc(capacity)), position(0), length(capacity), capacity(capacity), isDynamic(true)
		{
		}
	public:
		[[nodiscard]] static ObjPtr<MemoryStream> Create(uint8_t* buffer, size_t size, bool isDynamic)
		{
			return ObjPtr<MemoryStream>::Attach(new MemoryStream(buffer, size, isDynamic));
		}

		[[nodiscard]] static ObjPtr<MemoryStream> Create(size_t capacity)
		{
			return ObjPtr<MemoryStream>::Attach(new MemoryStream(capacity));
		}

		uint8_t* GetBuffer(bool takeOwnership)
		{
			if (takeOwnership)
			{
				auto temp = buffer;
				Reset();
				return temp;
			}

			return buffer;
		}

		size_t GetBufferSize() const
		{
			return length;
		}

		size_t GetBufferCapacity() const
		{
			return capacity;
		}

	private:
		static size_t MemoryStreamRead(void* userdata, void* buffer, size_t size)
		{
			MemoryStream* stream = static_cast<MemoryStream*>(userdata);
			size_t toRead = std::min(size, stream->length - stream->position);
			std::memcpy(buffer, stream->buffer + stream->position, toRead);
			stream->position += toRead;
			return toRead;
		}

		static size_t MemoryStreamWrite(void* userdata, const void* buffer, size_t size)
		{
			auto stream = static_cast<MemoryStream*>(userdata);
			auto capacity = stream->capacity;
			auto newPosition = stream->position + size;

			if (stream->isDynamic && newPosition > capacity)
			{
				auto newCapacity = std::max(capacity * 2, newPosition);
				auto newBuffer = (uint8_t*)Purrxel_ReAlloc(stream->buffer, newCapacity);

				if (newBuffer == nullptr)
				{
					return 0;
				}

				stream->buffer = newBuffer;
				stream->capacity = capacity = newCapacity;
			}
			else if (newPosition > capacity)
			{
				return 0;
			}

			std::memcpy(stream->buffer + stream->position, buffer, size);
			stream->position = newPosition;
			stream->length = std::max(newPosition, stream->length);
			return size;
		}

		static int64_t MemoryStreamSeek(void* userdata, int64_t offset, SeekOrigin origin)
		{
			MemoryStream* stream = static_cast<MemoryStream*>(userdata);
			int64_t newPosition = stream->position;

			switch (origin)
			{
			case SeekOrigin_Begin: newPosition = offset; break;
			case SeekOrigin_Current: newPosition = stream->position + offset; break;
			case SeekOrigin_End: newPosition = stream->length + offset; break;
			}

			if (newPosition < 0 || newPosition > static_cast<int64_t>(stream->length))
			{
				return -1;
			}

			stream->position = newPosition;
			return stream->position;
		}

		static int64_t MemoryStreamPosition(void* userdata)
		{
			MemoryStream* stream = static_cast<MemoryStream*>(userdata);
			return stream->position;
		}

		static int64_t MemoryStreamLength(void* userdata)
		{
			MemoryStream* stream = static_cast<MemoryStream*>(userdata);
			return stream->length;
		}

		static bool MemoryStreamSetLength(void* userdata, int64_t length)
		{
			if (length < 0) return false;
			MemoryStream* stream = static_cast<MemoryStream*>(userdata);
			size_t newLength = static_cast<size_t>(length);

			if (newLength > stream->capacity)
			{
				if (!stream->isDynamic) return false;
				auto newCapacity = std::max(stream->capacity * 2, newLength);
				auto newBuffer = (uint8_t*)Purrxel_ReAlloc(stream->buffer, newCapacity);
				if (newBuffer == nullptr) return false;
				stream->buffer = newBuffer;
				stream->capacity = newCapacity;
			}

			if (newLength > stream->length)
			{
				std::memset(stream->buffer + stream->length, 0, newLength - stream->length);
			}

			stream->length = newLength;
			if (stream->position > newLength)
			{
				stream->position = newLength;
			}
			return true;
		}

		static void MemoryStreamFlush(void* userdata)
		{
		}

		static void MemoryStreamClose(void* userdata)
		{
			MemoryStream* stream = static_cast<MemoryStream*>(userdata);
			if (stream->isDynamic && stream->buffer)
			{
				Purrxel_Free(stream->buffer);
				stream->Reset();
			}
		}
	};

	class BufferedStream : public Stream
	{
	private:
		enum class LastAction
		{
			None,
			Read,
			Write,
		};

		ObjPtr<Stream> inner;
		bool closeInner;
		uint8_t* buffer;
		size_t bufferSize;
		size_t bufferPosition;
		size_t bufferReadSize;
		LastAction lastAction;

		BufferedStream(const ObjPtr<Stream>& inner, bool closeInner = true, size_t bufferSize = 4096)
			: Stream(sizeof(BufferedStream), this, BufferedStreamRead, BufferedStreamWrite, BufferedStreamSeek, BufferedStreamPosition, BufferedStreamLength, BufferedStreamSetLength, BufferedStreamFlush, BufferedStreamClose),
			inner(inner),
			closeInner(closeInner),
			bufferSize(bufferSize),
			bufferPosition(0),
			bufferReadSize(0),
			lastAction(LastAction::None)
		{
			buffer = new uint8_t[bufferSize];
		}

		BufferedStream(const BufferedStream&) = delete;
		BufferedStream& operator=(const BufferedStream&) = delete;

	public:
		[[nodiscard]] static ObjPtr<BufferedStream> Create(const ObjPtr<Stream>& inner, bool closeInner = true, size_t bufferSize = 4096)
		{
			return ObjPtr<BufferedStream>::Attach(new BufferedStream(inner, closeInner, bufferSize));
		}

	private:
		static size_t BufferedStreamRead(void* userdata, void* buffer, size_t size)
		{
			if (size == 0) return 0;
			auto stream = static_cast<BufferedStream*>(userdata);
			auto& lastAction = stream->lastAction;
			if (lastAction == LastAction::Write) { stream->Flush(); }

			auto& bufferPosition = stream->bufferPosition;
			auto innerBuffer = stream->buffer;
			const size_t bufferSize = stream->bufferSize;
			auto& bufferReadSize = stream->bufferReadSize;

			auto start = static_cast<uint8_t*>(buffer);
			auto dst = start;

			while (size > 0)
			{
				size_t space = bufferReadSize - bufferPosition;
				if (space == 0)
				{
					size_t read = stream->inner->Read(innerBuffer, bufferSize);
					if (read == EOF)
					{
						return (dst - start);
					}
					bufferPosition = 0;
					bufferReadSize = read;
					space = read;
				}

				size_t toCopy = std::min(space, size);
				std::memcpy(dst, innerBuffer + bufferPosition, toCopy);
				bufferPosition += toCopy;
				dst += toCopy;
				size -= toCopy;
			}

			lastAction = LastAction::Read;
			return (dst - start);
		}

		static size_t BufferedStreamWrite(void* userdata, const void* buffer, size_t size)
		{
			if (size == 0) return 0;
			auto stream = static_cast<BufferedStream*>(userdata);
			auto& lastAction = stream->lastAction;
			if (lastAction == LastAction::Read) { stream->Flush(); }

			auto& bufferPosition = stream->bufferPosition;
			auto innerBuffer = stream->buffer;
			const size_t bufferSize = stream->bufferSize;

			auto start = static_cast<const uint8_t*>(buffer);
			auto src = start;

			while (size > 0)
			{
				size_t space = bufferSize - bufferPosition;
				if (space == 0)
				{
					size_t written = stream->inner->Write(innerBuffer, bufferSize);
					if (written != bufferPosition)
					{
						return written == EOF ? written : (src - start);
					}
					bufferPosition = 0;
					space = bufferSize;
				}

				size_t toCopy = std::min(size, space);
				std::memcpy(innerBuffer + bufferPosition, src, toCopy);
				bufferPosition += toCopy;
				src += toCopy;
				size -= toCopy;
			}

			stream->bufferReadSize = 0;
			lastAction = LastAction::Write;
			return (src - start);
		}

		static int64_t BufferedStreamSeek(void* userdata, int64_t offset, SeekOrigin origin)
		{
			auto stream = static_cast<BufferedStream*>(userdata);
			auto& lastAction = stream->lastAction;
			if (lastAction == LastAction::Write) { stream->Flush(); }
			auto& inner = stream->inner;
			auto& bufferReadSize = stream->bufferReadSize;
			auto& bufferPosition = stream->bufferPosition;

			const int64_t oldInnerPositionFront = inner->Position();
			const int64_t oldInnerPosition = oldInnerPositionFront - bufferReadSize;
			const int64_t oldPosition = oldInnerPosition + bufferPosition;
			const size_t length = inner->Length();
			int64_t newPosition = oldPosition;

			switch (origin)
			{
			case SeekOrigin_Begin: newPosition = offset; break;
			case SeekOrigin_Current: newPosition = oldPosition + offset; break;
			case SeekOrigin_End: newPosition = length + offset; break;
			}

			if (newPosition < 0 || newPosition > static_cast<int64_t>(length))
			{
				return EOF;
			}

			if (lastAction == LastAction::Read)
			{
				if (newPosition >= oldInnerPosition && newPosition < oldInnerPositionFront)
				{
					bufferPosition = static_cast<size_t>(newPosition - oldInnerPosition);
					return newPosition;
				}
			}

			bufferReadSize = 0;
			bufferPosition = 0;
			lastAction = LastAction::None;
			return inner->Seek(newPosition, SeekOrigin_Begin);
		}

		static int64_t BufferedStreamPosition(void* userdata)
		{
			auto stream = static_cast<BufferedStream*>(userdata);
			return stream->inner->Position() - stream->bufferReadSize + stream->bufferPosition;
		}

		static int64_t BufferedStreamLength(void* userdata)
		{
			auto stream = static_cast<BufferedStream*>(userdata);
			return stream->inner->Length();
		}

		static bool BufferedStreamSetLength(void* userdata, int64_t length)
		{
			auto stream = static_cast<BufferedStream*>(userdata);
			if (stream->lastAction == LastAction::Write) { stream->Flush(); }
			return stream->inner->Length(length);
		}

		static void BufferedStreamFlush(void* userdata)
		{
			auto stream = static_cast<BufferedStream*>(userdata);
			auto& inner = stream->inner;
			auto& bufferPosition = stream->bufferPosition;
			auto& lastAction = stream->lastAction;

			if (bufferPosition > 0 && lastAction == LastAction::Write)
			{
				inner->Write(stream->buffer, bufferPosition);
				bufferPosition = 0;
			}
			stream->bufferReadSize = 0;
			lastAction = LastAction::None;

			inner->Flush();
		}

		static void BufferedStreamClose(void* userdata)
		{
			auto stream = static_cast<BufferedStream*>(userdata);

			if (stream->buffer == nullptr)
			{
				return;
			}

			delete[] stream->buffer;
			stream->buffer = nullptr;
			stream->bufferSize = 0;
			stream->bufferPosition = 0;
			stream->lastAction = LastAction::None;

			if (stream->closeInner)
			{
				stream->inner->Close();
				stream->inner = nullptr;
				stream->closeInner = false;
			}
			stream->Release();
		}
	};
}
#endif