#ifndef HEXA_UTILS_DENSE_SET_HPP
#define HEXA_UTILS_DENSE_SET_HPP

#include "common.hpp"

namespace HEXA_UTILS_NAMESPACE
{
    template <typename T, typename Hash = std::hash<T>, typename KeyEqual = std::equal_to<T>, typename Allocator = std::allocator<T>>
    class dense_set
    {
      private:
        struct EntryMetadata
        {
            static constexpr size_t FLAG_MASK = (size_t)1 << ((sizeof(size_t) * 8) - 1);
            static constexpr size_t HASH_MASK = ~FLAG_MASK;
            static constexpr size_t TOMBSTONE_STATE = HASH_MASK;
            static constexpr size_t EMPTY_STATE = 0;

            size_t raw = EMPTY_STATE;

            EntryMetadata() : raw(EMPTY_STATE) {}
            EntryMetadata(size_t hash) : raw(hash | FLAG_MASK) {}

            size_t get_hash() const
            {
                return raw & HASH_MASK;
            }

            void set_hash(size_t hash)
            {
                raw = (raw & FLAG_MASK) | (hash & HASH_MASK);
            }

            bool isEmpty() const
            {
                return raw == EMPTY_STATE;
            }
            bool isFilled() const
            {
                return raw & FLAG_MASK;
            }
            bool isTombstone() const
            {
                return raw == TOMBSTONE_STATE;
            }
            void setEmpty()
            {
                raw = EMPTY_STATE;
            }
            void setFilled()
            {
                raw |= FLAG_MASK;
            }
            void setTombstone()
            {
                raw = TOMBSTONE_STATE;
            }
        };

        struct Entry : public EntryMetadata
        {
            T key;

            Entry() = default;

            template <typename U>
            Entry(size_t hash, U&& key) : EntryMetadata(hash), key(std::forward<U>(key))
            {
            }
        };

      public:
        using entry_allocator = typename std::allocator_traits<Allocator>::template rebind_alloc<Entry>;

      private:
        float load_factor_m = 0.75f;
        Entry* buckets = nullptr;
        size_t capacity_m = 0;
        size_t size_m = 0;
        entry_allocator allocator;
        Hash hasher;
        KeyEqual key_equal;

        const Entry* cfind_entry(const Entry* entries, size_t capacity, const T& key, size_t hash) const
        {
            const Entry* tombstone = nullptr;
            size_t index = hash & (capacity - 1);
            const size_t start = index;
            do
            {
                const Entry* entry = &entries[index];
                if (!entry->isFilled())
                {
                    if (entry->isEmpty())
                    {
                        return tombstone != nullptr ? tombstone : entry;
                    }
                    else
                    {
                        if (tombstone == nullptr)
                        {
                            tombstone = entry;
                        }
                    }
                }
                else if (entry->get_hash() == hash && key_equal(entry->key, key))
                {
                    return entry;
                }

                index = (index + 1) & (capacity - 1);
            } while (index != start);

            // this should never happen only if someone forgot to call reserve.
            throw std::runtime_error("Infinite loop detected in find_entry.");
        }

        Entry* find_entry(Entry* entries, size_t capacity, const T& key, size_t hash) const
        {
            auto result = cfind_entry(entries, capacity, key, hash);
            return const_cast<Entry*>(result);
        }

        void resize(size_t newCapacity)
        {
            if (newCapacity < size_m)
            {
                throw std::runtime_error("Cannot reduce capacity below current size");
            }

            Entry* newBuckets = allocator.allocate(newCapacity);
            try
            {
                std::uninitialized_value_construct(newBuckets, newBuckets + newCapacity);

                if (buckets != nullptr)
                {
                    for (size_t i = 0; i < capacity_m; ++i)
                    {
                        Entry& entry = buckets[i];
                        if (!entry.isFilled())
                            continue;
                        Entry& dest = *find_entry(newBuckets, newCapacity, entry.key, entry.get_hash());
                        new (&dest) Entry(std::move(entry));
                        entry.~Entry();
                    }
                    allocator.deallocate(buckets, capacity_m);
                }

                buckets = newBuckets;
                capacity_m = newCapacity;
            }
            catch (...)
            {
                allocator.deallocate(newBuckets, newCapacity);
                throw;
            }
        }

        void dealloc_entries()
        {
            for (size_t i = 0; i < capacity_m; ++i)
            {
                Entry& entry = buckets[i];
                if (!entry.isFilled())
                    continue;
                entry.~Entry();
                entry.setEmpty();
            }
        }

        Entry* buckets_end()
        {
            return buckets == nullptr ? nullptr : (buckets + capacity_m);
        }

        const Entry* buckets_end() const
        {
            return buckets == nullptr ? nullptr : (buckets + capacity_m);
        }

      public:
        dense_set()
        {
            allocator = {};
        }

        dense_set(const entry_allocator& alloc) : allocator(alloc) {}

        dense_set(const Hash& hasher, const KeyEqual& equals, const entry_allocator& alloc) : hasher(hasher), key_equal(equals), allocator(alloc) {}

        dense_set(const dense_set& other) : capacity_m(0), size_m(0), buckets(nullptr), allocator(other.allocator), hasher(other.hasher), key_equal(other.key_equal)
        {
            if (other.size_m > 0)
            {
                resize(other.capacity_m);
                for (const auto& key : other)
                {
                    insert(key);
                }
            }
        }

        dense_set(dense_set&& other) noexcept
            : buckets(other.buckets), capacity_m(other.capacity_m), size_m(other.size_m), allocator(std::move(other.allocator)), hasher(std::move(other.hasher)), key_equal(std::move(other.key_equal))
        {
            other.buckets = nullptr;
            other.capacity_m = 0;
            other.size_m = 0;
        }

        ~dense_set()
        {
            if (buckets)
            {
                try
                {
                    dealloc_entries();
                }
                catch (...)
                {
                }
                allocator.deallocate(buckets, capacity_m);
                buckets = nullptr;
                capacity_m = 0;
                size_m = 0;
            }
        }

        dense_set& operator=(const dense_set& other)
        {
            if (this != &other)
            {
                clear();
                allocator = other.allocator;
                hasher = other.hasher;
                key_equal = other.key_equal;
                if (other.size_m > 0)
                {
                    resize(other.capacity_m);
                    for (const auto& key : other)
                    {
                        insert(key);
                    }
                }
            }
            return *this;
        }

        dense_set& operator=(dense_set&& other) noexcept
        {
            if (this != &other)
            {
                clear();
                if (buckets)
                {
                    allocator.deallocate(buckets, capacity_m);
                }
                buckets = other.buckets;
                capacity_m = other.capacity_m;
                size_m = other.size_m;
                allocator = std::move(other.allocator);
                hasher = std::move(other.hasher);
                key_equal = std::move(other.key_equal);
                other.buckets = nullptr;
                other.capacity_m = 0;
                other.size_m = 0;
            }
            return *this;
        }

        template <typename U = T, typename UEntry = Entry>
        class iterator_base
        {
            friend class dense_set;
            UEntry* ptr;
            UEntry* end_ptr;

            void skip_to_filled()
            {
                while (ptr != end_ptr && !ptr->isFilled())
                {
                    ++ptr;
                }
            }

          public:
            using iterator_category = std::forward_iterator_tag;
            using value_type = U;
            using difference_type = std::ptrdiff_t;
            using pointer = U*;
            using reference = U&;

            iterator_base(UEntry* p, UEntry* end) : ptr(p), end_ptr(end)
            {
                skip_to_filled();
            }

            template <typename V, typename = std::enable_if_t<std::is_convertible_v<V*, U*>>>
            iterator_base(const iterator_base<V>& other) : ptr(other.ptr), end_ptr(other.end_ptr)
            {
                skip_to_filled();
            }

            reference operator*() const
            {
                return ptr->key;
            }
            pointer operator->() const
            {
                return &ptr->key;
            }

            iterator_base& operator++()
            {
                ++ptr;
                skip_to_filled();
                return *this;
            }

            iterator_base operator++(int)
            {
                iterator_base tmp = *this;
                ++(*this);
                return tmp;
            }

            bool operator==(const iterator_base& other) const
            {
                return ptr == other.ptr;
            }
            bool operator!=(const iterator_base& other) const
            {
                return ptr != other.ptr;
            }
        };

        using iterator = iterator_base<T>;
        using const_iterator = iterator_base<const T, const Entry>;

        iterator begin()
        {
            return iterator(buckets, buckets_end());
        }
        iterator end()
        {
            return iterator(buckets_end(), buckets_end());
        }

        const_iterator begin() const
        {
            return const_iterator(buckets, buckets_end());
        }
        const_iterator end() const
        {
            return const_iterator(buckets_end(), buckets_end());
        }
        const_iterator cbegin() const
        {
            return const_iterator(buckets, buckets_end());
        }
        const_iterator cend() const
        {
            return const_iterator(buckets_end(), buckets_end());
        }

        void reserve(size_t capacity)
        {
            if (capacity > capacity_m * load_factor_m)
            {
                size_t newCapacity = capacity_m * 2;
                if (newCapacity < capacity)
                {
                    newCapacity = NextPowerOfTwo(capacity);
                }
                resize(std::max(newCapacity, static_cast<size_t>(2)));
            }
        }

        void clear()
        {
            dealloc_entries();
            size_m = 0;
        }

        void shrink_to_fit()
        {
            resize(size_m);
        }

        size_t size() const
        {
            return size_m;
        }
        size_t capacity() const
        {
            return capacity_m;
        }
        bool empty() const
        {
            return size_m == 0;
        }
        float load_factor() const
        {
            return load_factor_m;
        }
        void load_factor(float value)
        {
            load_factor_m = value;
        }

        Allocator get_allocator() const
        {
            return allocator;
        }
        Hash hash_function() const
        {
            return hasher;
        }
        KeyEqual key_eq() const
        {
            return key_equal;
        }

        std::pair<iterator, bool> insert(const T& value)
        {
            reserve(size_m + 1);

            size_t hash = hasher(value);
            Entry* entry = find_entry(buckets, capacity_m, value, hash);

            if (entry->isFilled())
            {
                return {iterator(entry, buckets_end()), false};
            }

            new (entry) Entry(hash, value);
            ++size_m;
            return {iterator(entry, buckets_end()), true};
        }

        std::pair<iterator, bool> insert(T&& value)
        {
            reserve(size_m + 1);

            size_t hash = hasher(value);
            Entry* entry = find_entry(buckets, capacity_m, value, hash);

            if (entry->isFilled())
            {
                return {iterator(entry, buckets_end()), false};
            }

            new (entry) Entry(hash, std::move(value));
            ++size_m;
            return {iterator(entry, buckets_end()), true};
        }

        template <typename... Args>
        std::pair<iterator, bool> emplace(Args&&... args)
        {
            T value(std::forward<Args>(args)...);
            return insert(std::move(value));
        }

        template <typename Range>
        void insert_range(Range&& rg)
        {
            if constexpr (requires { std::ranges::size(rg); })
            {
                reserve(size_m + std::ranges::size(rg));
            }

            for (auto&& elem : rg)
            {
                insert(std::forward<decltype(elem)>(elem));
            }
        }

        iterator find(const T& key)
        {
            if (size_m == 0)
                return end();
            size_t hash = hasher(key);
            Entry* entry = find_entry(buckets, capacity_m, key, hash);

            if (entry->isFilled())
            {
                return iterator(entry, buckets_end());
            }

            return end();
        }

        const_iterator find(const T& key) const
        {
            if (size_m == 0)
                return end();
            size_t hash = hasher(key);
            const Entry* entry = cfind_entry(buckets, capacity_m, key, hash);

            if (entry->isFilled())
            {
                return const_iterator(entry, buckets_end());
            }

            return end();
        }

        bool contains(const T& key) const
        {
            return find(key) != end();
        }

        std::pair<iterator, iterator> equal_range(const T& key)
        {
            auto it = find(key);
            if (it != end())
            {
                auto next_it = it;
                ++next_it;
                return {it, next_it};
            }
            return {end(), end()};
        }

        std::pair<const_iterator, const_iterator> equal_range(const T& key) const
        {
            auto it = find(key);
            if (it != end())
            {
                auto next_it = it;
                ++next_it;
                return {it, next_it};
            }
            return {end(), end()};
        }

        iterator erase(iterator it)
        {
            Entry& entry = *it.ptr;

            if (!entry.isFilled())
            {
                return end();
            }

            entry.~Entry();
            entry.setTombstone();
            --size_m;

            ++it;
            return it;
        }

        iterator erase(const T& key)
        {
            auto it = find(key);
            if (it != end())
            {
                return erase(it);
            }
            return end();
        }

        template <typename H2, typename P2>
        void merge(dense_set<T, H2, P2, Allocator>& source)
        {
            merge_impl(source);
        }

        template <typename H2, typename P2>
        void merge(dense_set<T, H2, P2, Allocator>&& source)
        {
            merge_impl(source);
        }

        void swap(dense_set& other) noexcept
        {
            std::swap(buckets, other.buckets);
            std::swap(capacity_m, other.capacity_m);
            std::swap(size_m, other.size_m);
            std::swap(allocator, other.allocator);
            std::swap(hasher, other.hasher);
            std::swap(key_equal, other.key_equal);
            std::swap(load_factor_m, other.load_factor_m);
        }

      private:
        template <typename OtherSet>
        void merge_impl(OtherSet& source)
        {
            reserve(size_m + source.size_m);

            auto it = source.begin();
            while (it != source.end())
            {
                auto current = it++;

                auto result = insert(*current);

                if (result.second)
                {
                    source.erase(current);
                }
            }
        }
    };
} // namespace HEXA_UTILS_NAMESPACE

#endif