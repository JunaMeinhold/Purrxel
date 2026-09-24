#ifndef HEXA_UTILS_MEMORY_HPP
#define HEXA_UTILS_MEMORY_HPP

#include "common.hpp"

namespace HEXA_UTILS_NAMESPACE
{
    static constexpr size_t NextPowerOfTwo(size_t n)
    {
        if (n == 0)
            return 1;
        n--;
        n |= n >> 1;
        n |= n >> 2;
        n |= n >> 4;
        n |= n >> 8;
        n |= n >> 16;
        if constexpr (sizeof(size_t) == 8)
            n |= n >> 32;
        return n + 1;
    }

    static constexpr size_t AlignUp(size_t size, size_t alignment)
    {
        return (size + alignment - 1) & ~(alignment - 1);
    }

    static constexpr size_t AlignDown(size_t size, size_t alignment)
    {
        return size & ~(alignment - 1);
    }

    static constexpr void* AlignDown(void* ptr, size_t alignment)
    {
        return (void*)((uintptr_t)ptr & ~(alignment - 1));
    }

    static constexpr size_t DivCeil(size_t numerator, size_t denominator)
    {
        return (numerator + denominator - 1) / denominator;
    }

    static constexpr size_t BitsToBytes(size_t bits)
    {
        return DivCeil(bits, 8);
    }

    template <typename T>
        requires std::is_integral_v<T>
    static constexpr T SatSubtract(T a, T b)
    {
        if constexpr (std::is_unsigned_v<T>)
        {
            return a < b ? 0 : a - b;
        }
        return a - b;
    }

    inline void DumpMemory(const void* address, size_t size, size_t bytesPerLine = 16)
    {
        const uint8_t* ptr = static_cast<const uint8_t*>(address);

        for (size_t i = 0; i < size; i += bytesPerLine)
        {
            std::cout << std::setw(8) << std::setfill('0') << std::hex << i << "  ";

            for (size_t j = 0; j < bytesPerLine; ++j)
            {
                if (i + j < size)
                {
                    std::cout << std::setw(2) << static_cast<int>(ptr[i + j]) << " ";
                }
                else
                {
                    std::cout << "   ";
                }
            }

            std::cout << " ";

            for (size_t j = 0; j < bytesPerLine; ++j)
            {
                if (i + j < size)
                {
                    char c = static_cast<char>(ptr[i + j]);
                    std::cout << (std::isprint(static_cast<unsigned char>(c)) ? c : '.');
                }
            }

            std::cout << std::endl;
        }

        std::cout << std::dec;
    }

    template <typename T>
    class Ptr
    {
        T* ptr;

      public:
        Ptr() noexcept : ptr(nullptr) {}
        explicit Ptr(T* p) noexcept : ptr(p) {}

        ~Ptr() = default;

        Ptr(const Ptr&) = default;
        Ptr& operator=(const Ptr&) = default;
        Ptr(Ptr&&) noexcept = default;
        Ptr& operator=(Ptr&&) noexcept = default;

        T* get() const noexcept
        {
            return ptr;
        }
        T& operator*() const
        {
            return *ptr;
        }
        T* operator->() const
        {
            return ptr;
        }
        operator bool() const noexcept
        {
            return ptr != nullptr;
        }
    };

    struct DisabledDeleter
    {
      public:
        void operator()(void*) const noexcept {}
    };

    inline static void* aligned_alloc(size_t alignment, size_t size)
    {
#if defined(_MSC_VER)
        return _aligned_malloc(size, alignment);
#else
        return std::aligned_alloc(alignment, size);
#endif
    }

    inline static void aligned_free(void* ptr)
    {
#if defined(_MSC_VER)
        _aligned_free(ptr);
#else
        std::free(ptr);
#endif
    }

    template <typename T>
    class DisabledAllocator
    {
      public:
        using value_type = T;

        DisabledAllocator() noexcept = default;

        template <typename U>
        DisabledAllocator(const DisabledAllocator<U>& other) noexcept
        {
        }

        T* allocate(std::size_t)
        {
            throw std::bad_alloc{};
            return nullptr;
        }

        void deallocate(T*, std::size_t) noexcept {}

        template <typename U>
        inline bool operator==(const DisabledAllocator<U>& other) const noexcept
        {
            return true;
        }

        template <typename U>
        inline bool operator!=(const DisabledAllocator<U>& other) const noexcept
        {
            return !(*this == other);
        }

        template <typename U>
        friend class DisabledAllocator;
    };

    template <typename Alloc>
    concept IsAllocatorDisabled = std::is_same_v<Alloc, HEXA_UTILS_NAMESPACE::DisabledAllocator<typename Alloc::value_type>>;

    template <typename Alloc>
    concept IsAllocatorEnabled = !IsAllocatorDisabled<Alloc>;

    template <typename Alloc>
    concept HasAllocatorReallocate = requires(Alloc alloc, typename Alloc::value_type* ptr, std::size_t oldSize, std::size_t newSize, std::size_t itemCount) {
        { alloc.reallocate(ptr, oldSize, newSize, itemCount) } -> std::same_as<typename Alloc::value_type*>;
    };

    template <typename T, size_t Alignment = alignof(T)>
    class AlignedAllocator
    {
      public:
        using value_type = T;

        AlignedAllocator() noexcept = default;

        template <typename U>
        AlignedAllocator(const AlignedAllocator<U>& other) noexcept
        {
        }

        T* allocate(std::size_t n)
        {
            auto ptr = aligned_alloc(Alignment, sizeof(T) * n);
            if (ptr == nullptr)
                throw std::bad_alloc{};
            return reinterpret_cast<T*>(ptr);
        }

        void deallocate(T* p, std::size_t n) noexcept
        {
            if (p == nullptr)
                return;
            aligned_free(p);
        }

        template <typename U>
        inline bool operator==(const AlignedAllocator<U>& other) const noexcept
        {
            return true;
        }

        template <typename U>
        inline bool operator!=(const AlignedAllocator<U>& other) const noexcept
        {
            return !(*this == other);
        }

        template <typename U, size_t OtherAlignment>
        friend class AlignedAllocator;
    };

    class SharedObject
    {
      private:
        std::atomic<size_t> counter{1};

      protected:
        SharedObject() {}
        virtual void Destroy() = 0;

      public:
        SharedObject* AddRef()
        {
            counter.fetch_add(1, std::memory_order_acq_rel);
            return this;
        }
        void Release()
        {
            if (counter.fetch_sub(1, std::memory_order_acq_rel) == 1)
            {
                Destroy();
            }
        }

        size_t GetRefCount() const
        {
            return counter.load(std::memory_order_acquire);
        }
    };

    template <typename T>
    concept HasStaticNew = requires(std::size_t size) {
        { T::operator new(size) } -> std::same_as<void*>;
    };

    template <typename T>
    concept HasStaticDelete = requires(void* ptr) {
        { T::operator delete(ptr) } -> std::same_as<void>;
    };

    template <typename T>
    concept HasStaticNewTagged = requires(std::size_t size, T* self) {
        { T::operator new(size, self) } -> std::same_as<void*>;
    };

    template <typename T>
    concept HasStaticDeleteTagged = requires(void* ptr, T* self) {
        { T::operator delete(ptr, self) } -> std::same_as<void>;
    };

    template <typename TDerived>
    class SharedObjectCRTP
    {
      private:
        std::atomic<size_t> counter{1};

      protected:
        SharedObjectCRTP() {}

      public:
        SharedObjectCRTP* AddRef()
        {
            counter.fetch_add(1, std::memory_order_acq_rel);
            return this;
        }

        void Release()
        {
            if (counter.fetch_sub(1, std::memory_order_acq_rel) == 1)
            {
                auto* obj = static_cast<TDerived*>(this);
                if constexpr (HasStaticDeleteTagged<TDerived>)
                {
                    TDerived::operator delete(obj, obj);
                }
                else
                {
                    delete obj;
                }
            }
        }

        size_t GetRefCount() const
        {
            return counter.load(std::memory_order_acquire);
        }
    };

    template <typename T>
    class ObjPtr
    {
        T* ptr;

      public:
        ObjPtr() : ptr(nullptr) {}

        explicit ObjPtr(T* p) noexcept : ptr(p)
        {
            if (ptr)
                ptr->AddRef();
        }

        ObjPtr(const ObjPtr<T>& other) noexcept : ptr(other.ptr)
        {
            if (ptr)
                ptr->AddRef();
        }

        template <typename U, std::enable_if_t<std::is_convertible_v<U*, T*>, int> = 0>
        ObjPtr(const ObjPtr<U>& other) noexcept : ptr(other.Get())
        {
            if (ptr)
                ptr->AddRef();
        }

        template <typename U, std::enable_if_t<std::is_convertible_v<U*, T*>, int> = 0>
        ObjPtr(ObjPtr<U>& other) noexcept : ptr(other.Get())
        {
            if (ptr)
                ptr->AddRef();
        }

        ObjPtr(ObjPtr<T>&& other) noexcept : ptr(other.ptr)
        {
            other.ptr = nullptr;
        }

        ~ObjPtr() noexcept
        {
            if (ptr)
                ptr->Release();
            ptr = nullptr;
        }

        ObjPtr<T>& operator=(const ObjPtr<T>& other)
        {
            if (this != &other)
            {
                if (other.ptr)
                    other.ptr->AddRef();
                if (ptr)
                    ptr->Release();
                ptr = other.ptr;
            }
            return *this;
        }

        ObjPtr<T>& operator=(ObjPtr<T>&& other) noexcept
        {
            if (this != &other)
            {
                if (ptr)
                    ptr->Release();
                ptr = other.ptr;
                other.ptr = nullptr;
            }
            return *this;
        }

        ObjPtr<T>& operator=(T* p) noexcept
        {
            if (ptr != p)
            {
                if (ptr)
                    ptr->Release();
                ptr = p;
            }
            return *this;
        }

        T& operator*() const noexcept
        {
            return *ptr;
        }

        T* operator->() const noexcept
        {
            return ptr;
        }

        T* Get() const noexcept
        {
            return ptr;
        }

        explicit operator bool() const noexcept
        {
            return ptr != nullptr;
        }

        bool operator==(const ObjPtr<T>& other) const noexcept
        {
            return ptr == other.ptr;
        }

        bool operator!=(const ObjPtr<T>& other) const noexcept
        {
            return ptr != other.ptr;
        }

        bool operator==(T* p) const noexcept
        {
            return ptr == p;
        }

        bool operator!=(T* p) const noexcept
        {
            return ptr != p;
        }

        ObjPtr<T> AddRef() noexcept
        {
            if (ptr)
            {
                return ObjPtr<T>(ptr);
            }
            return ObjPtr<T>();
        }

        T* Detach() noexcept
        {
            T* p = ptr;
            ptr = nullptr;
            return p;
        }

        T* AddRefDetached() const noexcept
        {
            if (ptr)
            {
                ptr->AddRef();
                return ptr;
            }
            return nullptr;
        }

        void Reset(T* p = nullptr) noexcept
        {
            if (ptr)
                ptr->Release();
            ptr = p;
        }

        static ObjPtr<T> Attach(T* ptr) noexcept
        {
            ObjPtr<T> sptr;
            sptr.Reset(ptr);
            return sptr;
        }

        static ObjPtr<T> AttachFromAddress(void* ptr) noexcept
        {
            ObjPtr<T> sptr;
            sptr.Reset(static_cast<T*>(ptr));
            return sptr;
        }

        void Swap(ObjPtr<T>& other) noexcept
        {
            std::swap(ptr, other.ptr);
        }
    };

    template <typename TDerived>
    class SharedObjectBase : public SharedObject
    {
      protected:
        SharedObjectBase() : SharedObject() {}

        void Destroy() override
        {
            auto ptr = static_cast<TDerived*>(this);
            ptr->~TDerived();
            HEXA_UTILS_NAMESPACE::aligned_free(ptr);
        }

      public:
        template <typename... TArgs>
        [[nodiscard]] static ObjPtr<TDerived> Create(TArgs&&... args)
        {
            auto ptr = reinterpret_cast<TDerived*>(HEXA_UTILS_NAMESPACE::aligned_alloc(alignof(TDerived), sizeof(TDerived)));
            new (ptr) TDerived(std::forward<TArgs>(args)...);
            return ObjPtr<TDerived>(ptr);
        }

        template <typename... TArgs>
        [[nodiscard]] static ObjPtr<TDerived> CreateAdditional(size_t additionalSize, TArgs&&... args)
        {
            auto ptr = reinterpret_cast<TDerived*>(HEXA_UTILS_NAMESPACE::aligned_alloc(alignof(TDerived), sizeof(TDerived) + additionalSize));
            new (ptr) TDerived(std::forward<TArgs>(args)...);
            return ObjPtr<TDerived>(ptr);
        }

        template <typename U, typename... TArgs>
        [[nodiscard]] static ObjPtr<TDerived> CreateU(TArgs&&... args)
        {
            auto ptr = reinterpret_cast<U*>(HEXA_UTILS_NAMESPACE::aligned_alloc(alignof(U), sizeof(U)));
            new (ptr) U(std::forward<TArgs>(args)...);
            return ObjPtr<TDerived>(ptr);
        }

        template <typename U, typename... TArgs>
        [[nodiscard]] static ObjPtr<TDerived> CreateUAdditional(size_t additionalSize, TArgs&&... args)
        {
            auto ptr = reinterpret_cast<U*>(HEXA_UTILS_NAMESPACE::aligned_alloc(alignof(U), sizeof(U) + additionalSize));
            new (ptr) U(std::forward<TArgs>(args)...);
            return ObjPtr<TDerived>(ptr);
        }
    };

    template <typename T, typename... TArgs>
    inline ObjPtr<T> make_objptr(TArgs&&... args)
    {
        T* ptr = nullptr;
        if constexpr (HasStaticNew<T>)
        {
            void* mem = T::operator new(sizeof(T), ptr);
            ptr = new (mem) T(std::forward<TArgs>(args)...);
        }
        else
        {
            ptr = new T(std::forward<TArgs>(args)...);
        }

        return ObjPtr<T>::Attach(ptr);
    }

    template <typename T, std::size_t Align>
    struct alignas(Align) Aligned : T
    {
        using T::T;
    };

    // NOTE: This is only safe if pointers fit in 48 bits, which is common on some platforms
    // (e.g., x86-64 where canonical addresses use 48 bits).
    // Using pointers that require more than 48 bits will corrupt the pointer.
    template <typename T>
    struct tagged_ptr
    {
        static_assert(sizeof(uintptr_t) <= 8, "Pointer size too large");
        uint64_t raw = 0;

        static constexpr uint64_t PointerMask = ((1ull << 48ull) - 1);
        static constexpr uint64_t Encode(T* ptr, uint16_t tag)
        {
            return (uint64_t)std::bit_cast<uintptr_t>(ptr) | ((uint64_t)tag << 48ull);
        }

        static constexpr T* Decode(uint64_t taggedPtr)
        {
            return std::bit_cast<T*>(taggedPtr & PointerMask);
        }

        constexpr tagged_ptr() = default;
        constexpr tagged_ptr(T* ptr, uint16_t tag) : raw(Encode(ptr, tag)) {}
        constexpr tagged_ptr(T* ptr) : raw((uint64_t)std::bit_cast<uintptr_t>(ptr)) {}

        constexpr uint16_t tag() const noexcept
        {
            return static_cast<uint16_t>(raw >> 48);
        }
        constexpr void tag(uint16_t tag) noexcept
        {
            raw = Encode(Decode(raw), tag);
        }
        constexpr T* get() const noexcept
        {
            return Decode(raw);
        }
        constexpr T* release() noexcept
        {
            auto ptr = Decode(raw);
            raw = 0;
            return ptr;
        }
        constexpr T* reset(T* newPtr = nullptr, uint16_t tag = 0) noexcept
        {
            auto ptr = Decode(raw);
            raw = Encode(newPtr, tag);
            return ptr;
        }

        constexpr T* operator->() const noexcept
        {
            return Decode(raw);
        }
        constexpr T& operator*() const noexcept
        {
            return *Decode(raw);
        }
        constexpr operator T*() const noexcept
        {
            return Decode(raw);
        }
        constexpr operator bool() const noexcept
        {
            return raw & PointerMask;
        }
        constexpr bool operator==(const tagged_ptr& other) const noexcept
        {
            return raw == other.raw;
        }
        constexpr bool operator!=(const tagged_ptr& other) const noexcept
        {
            return !(*this == other);
        }
        constexpr void swap(tagged_ptr& other) noexcept
        {
            std::swap(raw, other.raw);
        }
    };
} // namespace HEXA_UTILS_NAMESPACE

#endif // UTILS_MEMORY_HPP