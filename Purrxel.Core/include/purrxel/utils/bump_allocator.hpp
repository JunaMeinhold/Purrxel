#ifndef BUMP_ALLOCATOR_HPP
#define BUMP_ALLOCATOR_HPP

#include "pch/std.hpp"
#include "span.hpp"
#include "memory.hpp"

namespace HEXA_UTILS_NAMESPACE
{
	class BumpAllocator
	{
		static constexpr size_t MaxDoublingSize = 1024 * 1024; // 1 MiB

		struct Block
		{
			Block* prev;
			uint32_t blockSize;
			uint32_t used;

			Block(Block* prev, uint32_t blockSize) : prev(prev), blockSize(blockSize), used(0)
			{
			}

			inline uint8_t* GetBaseAddress()
			{
				return reinterpret_cast<uint8_t*>(this) - blockSize;
			}

			inline void* Alloc(size_t bytes, size_t alignment) noexcept
			{
				size_t base = AlignUp(used, alignment);
				size_t newUsed = base + bytes;
				if (newUsed > blockSize)
				{
					return nullptr;
				}

				used = newUsed;
				return GetBaseAddress() + base;
			}
		};

		Block* head = nullptr;
		Block* tail = nullptr;

		static Block* AllocBlock(Block* prev, size_t minSize);
		static void DestroyBlock(Block* block);
		Block* CreateBlock(size_t minSize);

	public:
		BumpAllocator() = default;

		BumpAllocator(const BumpAllocator& other) = delete;
		BumpAllocator operator=(BumpAllocator other) = delete;

		BumpAllocator(BumpAllocator&& other) noexcept : head(other.head), tail(other.tail)
		{
			other.head = nullptr;
			other.tail = nullptr;
		}

		BumpAllocator& operator=(BumpAllocator&& other) noexcept
		{
			if (this != &other)
			{
				ReleaseAll();
				head = other.head;
				tail = other.tail;
				other.head = nullptr;
				other.tail = nullptr;
			}
			return *this;
		}

		~BumpAllocator()
		{
			ReleaseAll();
		}

		void* Alloc(size_t size, size_t alignment = alignof(std::max_align_t)) noexcept
		{
			void* ptr;
			if (tail && (ptr = tail->Alloc(size, alignment)))
			{
				return ptr;
			}

			return CreateBlock(size > MaxDoublingSize ? size : size * 2)->Alloc(size, alignment);
		}

		void Reset() noexcept
		{
			auto cur = tail;
			while (cur != nullptr)
			{
				cur->used = 0;
				cur = cur->prev;
			}
		}

		void ReleaseAll() noexcept
		{
			auto cur = tail;
			while (cur != nullptr)
			{
				auto prev = cur->prev;
				DestroyBlock(cur);
				cur = prev;
			}

			head = tail = nullptr;
		}

		template <typename T>
		T* AllocT(size_t count)
		{
			return reinterpret_cast<T*>(Alloc(sizeof(T) * count, alignof(T)));
		}

		template <class _Ty, class... _Types, std::enable_if_t<!std::is_array_v<_Ty>, int> = 0>
		_CONSTEXPR23 _Ty* Alloc(_Types&&... args)
		{
			void* rawMem = Alloc(sizeof(_Ty), alignof(_Ty));
			if (!rawMem) throw std::bad_alloc{};
			return new(rawMem) _Ty(std::forward<_Types>(args)...);
		}

		template <class _Ty, std::enable_if_t<!std::is_array_v<_Ty>, int> = 0>
		_CONSTEXPR23 _Ty* Alloc(_Ty&& val)
		{
			void* rawMem = Alloc(sizeof(_Ty), alignof(_Ty));
			if (!rawMem) throw std::bad_alloc{};
			return new(rawMem) _Ty(std::forward<_Ty>(val));
		}

		template <class _Ty, std::enable_if_t<!std::is_array_v<_Ty>, int> = 0>
		_CONSTEXPR23 _Ty* Alloc(const _Ty& val)
		{
			void* rawMem = Alloc(sizeof(_Ty), alignof(_Ty));
			if (!rawMem) throw std::bad_alloc{};
			return new(rawMem) _Ty(val);
		}

		template <class _Ty, class... _Types, std::enable_if_t<!std::is_array_v<_Ty>, int> = 0>
		_CONSTEXPR23 _Ty* AllocAddit(size_t additional, _Types&&... args)
		{
			void* rawMem = Alloc(sizeof(_Ty) + additional, alignof(_Ty));
			if (!rawMem) throw std::bad_alloc{};
			return new(rawMem) _Ty(std::forward<_Types>(args)...);
		}

		template <class _Ty, std::enable_if_t<!std::is_array_v<_Ty>, int> = 0>
		_CONSTEXPR23 _Ty* AllocAddit(size_t additional, _Ty&& val)
		{
			void* rawMem = Alloc(sizeof(_Ty) + additional, alignof(_Ty));
			if (!rawMem) throw std::bad_alloc{};
			return new(rawMem) _Ty(std::forward<_Ty>(val));
		}

		template <class _Ty, std::enable_if_t<!std::is_array_v<_Ty>, int> = 0>
		_CONSTEXPR23 _Ty* AllocAddit(size_t additional, const _Ty& val)
		{
			void* rawMem = Alloc(sizeof(_Ty) + additional, alignof(_Ty));
			if (!rawMem) throw std::bad_alloc{};
			return new(rawMem) _Ty(val);
		}

		StringSpan CopyString(const StringSpan& span)
		{
			if (span.size() == 0) return {};
			auto copyPtr = reinterpret_cast<char*>(Alloc((span.size() + 1) * sizeof(char), alignof(char)));
			std::memcpy(copyPtr, span.data(), span.size() * sizeof(char));
			copyPtr[span.size()] = '\0';
			return StringSpan(copyPtr, span.size());
		}

		template<typename T>
		Span<T> CopySpan(const Span<T>& span)
		{
			if (span.size() == 0) return {};
			auto copied = reinterpret_cast<T*>(Alloc(span.size() * sizeof(T), alignof(T)));
			std::memcpy(copied, span.data(), span.size() * sizeof(T));
			return Span<T>(copied, span.size());
		}

		template<typename T>
		Span<T> CopySpan(const std::vector<T>& span)
		{
			if (span.size() == 0) return {};
			auto copied = reinterpret_cast<T*>(Alloc(span.size() * sizeof(T), alignof(T)));
			std::memcpy(copied, span.data(), span.size() * sizeof(T));
			return Span<T>(copied, span.size());
		}
	};

	template <typename T>
	class StdBumpAllocator
	{
		BumpAllocator* bumpAllocator = nullptr;
	public:
		using value_type = T;

		StdBumpAllocator() noexcept = default;

		StdBumpAllocator(BumpAllocator& alloc) noexcept : bumpAllocator(&alloc) {}

		template <typename U>
		StdBumpAllocator(const StdBumpAllocator<U>& other) noexcept : bumpAllocator(other.bumpAllocator) {}

		T* allocate(std::size_t n)
		{
			void* ptr = bumpAllocator->Alloc(n * sizeof(T), alignof(T));
			if (!ptr) throw std::bad_alloc{};
			return static_cast<T*>(ptr);
		}

		void deallocate(T* p, std::size_t n) noexcept
		{
		}

		inline operator BumpAllocator& () const
		{
			assert(bumpAllocator != nullptr);
			return *bumpAllocator;
		}

		template <typename U>
		inline bool operator==(const StdBumpAllocator<U>& other) const noexcept
		{
			return bumpAllocator == other.bumpAllocator;
		}

		template <typename U>
		inline bool operator!=(const StdBumpAllocator<U>& other) const noexcept
		{
			return !(*this == other);
		}
	};
}

#endif