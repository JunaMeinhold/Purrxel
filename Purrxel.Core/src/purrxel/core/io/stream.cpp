#include "purrxel/core/io/stream.h"
#include "purrxel/core/io/stream.hpp"

#if PURRXEL_ENABLE_CAPI

PURRXEL_API PurrxelStream* Purrxel_CreateStream(PurrxelStreamDesc* desc)
{
	Purrxel::Stream* stream = new Purrxel::Stream(desc->version, desc->userdata, desc->readFunc, desc->writeFunc, desc->seekFunc, desc->getPositionFunc, desc->getLengthFunc, desc->setLengthFunc, desc->flushFunc, desc->closeFunc);
	return reinterpret_cast<PurrxelStream*>(stream);
}

PURRXEL_API PurrxelStream* Purrxel_CreateFileStream(const char* path)
{
	return reinterpret_cast<PurrxelStream*>(Purrxel::FileStream::OpenCreate(path).Detach());
}

PURRXEL_API PurrxelStream* Purrxel_ReadFileStream(const char* path)
{
	return reinterpret_cast<PurrxelStream*>(Purrxel::FileStream::OpenRead(path).Detach());
}

PURRXEL_API PurrxelStream* Purrxel_OpenFileStream(const char* path, const char* mode)
{
	return reinterpret_cast<PurrxelStream*>(Purrxel::FileStream::Open(path, mode).Detach());
}

PURRXEL_API PurrxelStream* Purrxel_CreateMemoryStream(size_t capacity)
{
	return reinterpret_cast<PurrxelStream*>(Purrxel::MemoryStream::Create(capacity).Detach());
}

PURRXEL_API PurrxelStream* Purrxel_CreateMemoryStreamFromBuffer(uint8_t* buffer, size_t size, bool isDynamic)
{
	return reinterpret_cast<PurrxelStream*>(Purrxel::MemoryStream::Create(buffer, size, isDynamic).Detach());
}

PURRXEL_API uint8_t* Purrxel_MemoryStreamGetBuffer(PurrxelStream* self, bool takeOwnership)
{
	Purrxel::Stream* base = reinterpret_cast<Purrxel::Stream*>(self);
	if (base->version < sizeof(Purrxel::MemoryStream))
	{
		return nullptr;
	}
	auto* ms = static_cast<Purrxel::MemoryStream*>(base);
	return ms->GetBuffer(takeOwnership);
}

PURRXEL_API int64_t Purrxel_MemoryStreamGetBufferSize(PurrxelStream* self)
{
	Purrxel::Stream* base = reinterpret_cast<Purrxel::Stream*>(self);
	if (base->version < sizeof(Purrxel::MemoryStream))
	{
		return -1;
	}
	auto* ms = static_cast<Purrxel::MemoryStream*>(base);
	return static_cast<int64_t>(ms->GetBufferSize());
}

PURRXEL_API int64_t Purrxel_MemoryStreamGetBufferCapacity(PurrxelStream* self)
{
	Purrxel::Stream* base = reinterpret_cast<Purrxel::Stream*>(self);
	if (base->version < sizeof(Purrxel::MemoryStream))
	{
		return -1;
	}
	auto* ms = static_cast<Purrxel::MemoryStream*>(base);
	return static_cast<int64_t>(ms->GetBufferCapacity());
}

PURRXEL_API void Purrxel_CloseStream(PurrxelStream* self)
{
	if (self == nullptr)
		return;
	Purrxel::Stream* base = reinterpret_cast<Purrxel::Stream*>(self);
	base->Close();
}

PURRXEL_API uint32_t Purrxel_StreamAddRef(PurrxelStream* self)
{
	Purrxel::Stream* base = reinterpret_cast<Purrxel::Stream*>(self);
	return base->AddRef();
}

PURRXEL_API void Purrxel_StreamRelease(PurrxelStream* self)
{
	Purrxel::Stream* base = reinterpret_cast<Purrxel::Stream*>(self);
	base->Release();
}

#endif