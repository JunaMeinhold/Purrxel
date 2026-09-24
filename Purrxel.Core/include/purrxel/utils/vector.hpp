#ifndef HEXA_UTILS_VECTOR_HPP
#define HEXA_UTILS_VECTOR_HPP

#include "common.hpp"
#include "memory.hpp"

namespace HEXA_UTILS_NAMESPACE
{
    template <class T, class Allocator = AlignedAllocator<T>>
    class vector
    {
      private:
        T* data_;
        size_t size_;
        size_t capacity_;
        Allocator allocator_;

        static constexpr size_t next_capacity(size_t current) noexcept
        {
            return current == 0 ? 1 : std::bit_ceil(current + 1);
        }

        void reallocate(size_t newCapacity)
        {
            if constexpr (HasAllocatorReallocate<Allocator>)
            {
                data_ = allocator_.reallocate(data_, capacity_, newCapacity, size_);
                capacity_ = newCapacity;
                return;
            }

            T* newData = allocator_.allocate(newCapacity);
            auto toCopy = std::min(newCapacity, size_);

            if (data_) [[likely]]
            {
                if constexpr (std::is_trivially_copyable_v<T>)
                {
                    std::memcpy(newData, data_, sizeof(T) * toCopy);
                }
                else
                {
                    std::uninitialized_move_n(data_, toCopy, newData);
                }

                if constexpr (!std::is_trivially_destructible_v<T>)
                {
                    std::destroy_n(data_, size_);
                }
            }

            allocator_.deallocate(data_, capacity_);
            data_ = newData;
            size_ = toCopy;
            capacity_ = newCapacity;
        }

      public:
        using value_type = T;
        using size_type = size_t;
        using difference_type = ptrdiff_t;
        using reference = T&;
        using const_reference = const T&;
        using pointer = T*;
        using const_pointer = const T*;
        using iterator = T*;
        using const_iterator = const T*;
        using allocator_type = Allocator;

        vector() noexcept : data_(nullptr), size_(0), capacity_(0), allocator_(Allocator()) {}

        explicit vector(size_t count, const Allocator& allocator = {}) : data_(nullptr), size_(0), capacity_(0), allocator_(allocator)
        {
            if (count > 0)
            {
                reserve(count);
                size_ = count;
            }
        }

        vector(size_t count, const T& value, const Allocator& allocator = {}) : data_(nullptr), size_(0), capacity_(0), allocator_(allocator)
        {
            if (count > 0)
            {
                reserve(count);
                std::uninitialized_fill(data_, data_ + count, value);
                size_ = count;
            }
        }

        vector(const vector& other) : data_(nullptr), size_(0), capacity_(0), allocator_(other.allocator_)
        {
            if (other.size_ > 0)
            {
                reserve(other.size_);
                if constexpr (std::is_trivially_copyable_v<T>)
                {
                    std::memcpy(data_, other.data_, sizeof(T) * other.size_);
                }
                else
                {
                    std::uninitialized_copy(other.begin(), other.end(), data_);
                }
                size_ = other.size_;
            }
        }

        vector(vector&& other) noexcept : data_(other.data_), size_(other.size_), capacity_(other.capacity_), allocator_(std::move(other.allocator_))
        {
            other.data_ = nullptr;
            other.size_ = 0;
            other.capacity_ = 0;
        }

        ~vector()
        {
            clear();
            allocator_.deallocate(data_, capacity_);
            data_ = nullptr;
            capacity_ = 0;
        }

        vector& operator=(const vector& other)
        {
            if (this != &other)
            {
                clear();
                if (other.size_ > capacity_ || other.allocator_ != allocator_)
                {
                    allocator_.deallocate(data_, capacity_);
                    allocator_ = other.allocator_;
                    data_ = allocator_.allocate(other.size_);
                    capacity_ = other.size_;
                }

                if constexpr (std::is_trivially_copyable_v<T>)
                {
                    std::memcpy(data_, other.data_, sizeof(T) * other.size_);
                }
                else
                {
                    std::uninitialized_copy(other.data_, other.data_ + other.size_, data_);
                }

                size_ = other.size_;
            }
            return *this;
        }

        vector& operator=(vector&& other) noexcept
        {
            if (this != &other)
            {
                clear();
                allocator_.deallocate(data_, capacity_);
                data_ = other.data_;
                size_ = other.size_;
                capacity_ = other.capacity_;
                allocator_ = std::move(other.allocator_);
                other.data_ = nullptr;
                other.size_ = 0;
                other.capacity_ = 0;
            }
            return *this;
        }

        [[nodiscard]] reference operator[](size_t index) noexcept
        {
            return data_[index];
        }

        [[nodiscard]] const_reference operator[](size_t index) const noexcept
        {
            return data_[index];
        }

        [[nodiscard]] reference front() noexcept
        {
            return data_[0];
        }
        [[nodiscard]] const_reference front() const noexcept
        {
            return data_[0];
        }
        [[nodiscard]] reference back() noexcept
        {
            return data_[size_ - 1];
        }
        [[nodiscard]] const_reference back() const noexcept
        {
            return data_[size_ - 1];
        }
        [[nodiscard]] T* data() noexcept
        {
            return data_;
        }
        [[nodiscard]] const T* data() const noexcept
        {
            return data_;
        }

        [[nodiscard]] iterator begin() noexcept
        {
            return data_;
        }
        [[nodiscard]] const_iterator begin() const noexcept
        {
            return data_;
        }
        [[nodiscard]] const_iterator cbegin() const noexcept
        {
            return data_;
        }
        [[nodiscard]] iterator end() noexcept
        {
            return data_ + size_;
        }
        [[nodiscard]] const_iterator end() const noexcept
        {
            return data_ + size_;
        }
        [[nodiscard]] const_iterator cend() const noexcept
        {
            return data_ + size_;
        }

        [[nodiscard]] bool empty() const noexcept
        {
            return size_ == 0;
        }
        [[nodiscard]] size_t size() const noexcept
        {
            return size_;
        }
        [[nodiscard]] size_t capacity() const noexcept
        {
            return capacity_;
        }

        void reserve(size_t newCapacity)
        {
            if (newCapacity > capacity_)
            {
                reallocate(newCapacity);
            }
        }

        void shrink_to_fit()
        {
            if (size_ < capacity_)
            {
                if (size_ == 0)
                {
                    allocator_.deallocate(data_, capacity_);
                    data_ = nullptr;
                    capacity_ = 0;
                }
                else
                {
                    reallocate(size_);
                }
            }
        }

        void clear() noexcept
        {
            if constexpr (!std::is_trivially_destructible_v<T>)
            {
                std::destroy(data_, data_ + size_);
            }
            size_ = 0;
        }

        void push_back(const T& value)
        {
            if (size_ == capacity_)
            {
                reallocate(next_capacity(capacity_));
            }
            new (data_ + size_) T(value);
            ++size_;
        }

        void push_back(T&& value)
        {
            if (size_ == capacity_)
            {
                reallocate(next_capacity(capacity_));
            }
            new (data_ + size_) T(std::move(value));
            ++size_;
        }

        template <typename... Args>
        reference emplace_back(Args&&... args)
        {
            if (size_ == capacity_)
            {
                reallocate(next_capacity(capacity_));
            }
            new (data_ + size_) T(std::forward<Args>(args)...);
            return data_[size_++];
        }

        void pop_back() noexcept
        {
            --size_;
            if constexpr (!std::is_trivially_destructible_v<T>)
            {
                data_[size_].~T();
            }
        }

        void resize(size_t count)
        {
            if (count > capacity_)
            {
                reallocate(count);
            }

            if (count > size_)
            {
                if constexpr (!std::is_trivially_constructible_v<T>)
                {
                    std::uninitialized_value_construct(data_ + size_, data_ + count);
                }
            }
            else if (count < size_)
            {
                if constexpr (!std::is_trivially_destructible_v<T>)
                {
                    std::destroy(data_ + count, data_ + size_);
                }
            }
            size_ = count;
        }

        void resize(size_t count, const T& value)
        {
            if (count > capacity_)
            {
                reallocate(count);
            }

            if (count > size_)
            {
                std::uninitialized_fill(data_ + size_, data_ + count, value);
            }
            else if (count < size_)
            {
                if constexpr (!std::is_trivially_destructible_v<T>)
                {
                    std::destroy(data_ + count, data_ + size_);
                }
            }
            size_ = count;
        }

        void unsafe_set_size(size_t new_size) noexcept
        {
            size_ = new_size;
        }

        T* unsafe_data() noexcept
        {
            return data_;
        }
    };
} // namespace HEXA_UTILS_NAMESPACE

#endif