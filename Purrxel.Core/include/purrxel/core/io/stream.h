#ifndef STREAM_H
#define STREAM_H

#include "purrxel/core/config.h"

enum SeekOrigin
{
	SeekOrigin_Begin = 0,
	SeekOrigin_Current = 1,
	SeekOrigin_End = 2
};

typedef size_t(*StreamReadFunc)(void* userdata, void* buffer, size_t size);
typedef size_t(*StreamWriteFunc)(void* userdata, const void* buffer, size_t size);
typedef int64_t(*StreamSeekFunc)(void* userdata, int64_t offset, SeekOrigin origin);
typedef int64_t(*StreamGetPositionFunc)(void* userdata);
typedef int64_t(*StreamGetLengthFunc)(void* userdata);
typedef bool(*StreamSetLengthFunc)(void* userdata, int64_t length);
typedef void(*StreamFlushFunc)(void* userdata);
typedef void(*StreamCloseFunc)(void* userdata);

#if PURRXEL_ENABLE_CAPI
C_API_BEGIN

typedef struct PurrxelStream PurrxelStream;
typedef struct PurrxelMemoryStream PurrxelMemoryStream;

typedef struct
{
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
} PurrxelStreamDesc;

/**
 * @brief Creates a new stream using the provided descriptor.
 * @param desc The stream descriptor that defines how the stream behaves.
 * @return A pointer to the created PurrxelStream object, or NULL on failure.
 */
PURRXEL_API PurrxelStream* Purrxel_CreateStream(PurrxelStreamDesc* desc);

/**
 * @brief Creates a file stream for reading or writing at the specified path.
 * @param path The path to the file.
 * @return A pointer to the created PurrxelStream object, or NULL on failure.
 */
PURRXEL_API PurrxelStream* Purrxel_CreateFileStream(const char* path);

/**
 * @brief Creates a file stream for reading at the specified path.
 * @param path The path to the file.
 * @return A pointer to the created PurrxelStream object, or NULL on failure.
 */
PURRXEL_API PurrxelStream* Purrxel_ReadFileStream(const char* path);

/**
 * @brief Opens a file stream for reading or writing HxSL data.
 *
 * This function creates an PurrxelStream object by opening a file using `fopen_s`.
 *
 * @param path The path to the file to open.
 * @param mode The file access mode string, as used in `fopen_s`
 *             (e.g., "r", "w", "rb", "wb").
 * @return Pointer to an PurrxelStream object on success, or NULL on failure.
 */
PURRXEL_API PurrxelStream* Purrxel_OpenFileStream(const char* path, const char* mode);

/**
 * @brief Creates a memory stream with the specified initial capacity.
 * @param capacity The initial capacity of the memory stream.
 * @return A pointer to the created PurrxelStream object, or NULL on failure.
 */
PURRXEL_API PurrxelStream* Purrxel_CreateMemoryStream(size_t capacity);

/**
 * @brief Creates a memory stream from an existing buffer.
 *
 * Initializes an PurrxelStream using a user-provided buffer. The buffer can optionally be dynamic.
 *
 * @param buffer Pointer to the memory buffer to use.
 * @param size The size of the buffer in bytes.
 * @param isDynamic If true, the stream can grow beyond the initial size (i.e., the buffer is resizable).
 *                  If false, the stream is fixed to the given size.
 * @return A pointer to the created PurrxelStream object, or NULL on failure.
 */
PURRXEL_API PurrxelStream* Purrxel_CreateMemoryStreamFromBuffer(uint8_t* buffer, size_t size, bool isDynamic);

/**
 * @brief Returns the pointer to the internal buffer of a memory stream.
 * @param self A pointer to the memory stream.
 * @param takeOwnership Flag indicating whether to take ownership of the buffer.
 * @return A pointer to the buffer or NULL on failure.
 */
PURRXEL_API uint8_t* Purrxel_MemoryStreamGetBuffer(PurrxelStream* self, bool takeOwnership);

/**
 * @brief Retrieves the current size of the memory stream's buffer.
 *
 * This represents the number of bytes currently used in the buffer.
 *
 * @param self Pointer to the PurrxelStream memory stream.
 * @return The current size in bytes, or -1 on failure.
 */
PURRXEL_API int64_t Purrxel_MemoryStreamGetBufferSize(PurrxelStream* self);

/**
 * @brief Retrieves the total capacity of the memory stream's buffer.
 *
 * This is the maximum number of bytes the buffer can hold without resizing.
 *
 * @param self Pointer to the PurrxelStream memory stream.
 * @return The buffer's capacity in bytes, or -1 on failure.
 */
PURRXEL_API int64_t Purrxel_MemoryStreamGetBufferCapacity(PurrxelStream* self);

PURRXEL_API void Purrxel_CloseStream(PurrxelStream* self);

PURRXEL_API uint32_t Purrxel_StreamAddRef(PurrxelStream* self);

PURRXEL_API void Purrxel_StreamRelease(PurrxelStream* self);

C_API_END
#endif

#endif