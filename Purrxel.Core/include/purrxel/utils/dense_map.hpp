#ifndef HEXA_UTILS_DENSE_MAP_HPP
#define HEXA_UTILS_DENSE_MAP_HPP

#include "common.hpp"
#include "span.hpp"

namespace HEXA_UTILS_NAMESPACE
{
    namespace DenseMapInternal
    {
        template <typename T, typename U>
        concept HasIsTransparent = requires {
            typename T::is_transparent;
            typename U::is_transparent;
        };

        template <typename TKey>
        struct DefaultHash
        {
            using type = std::hash<TKey>;
        };

        template <typename TKey>
        struct DefaultEqual
        {
            using type = std::equal_to<TKey>;
        };

        template <>
        struct DefaultHash<std::string>
        {
            using type = HEXA_UTILS_NAMESPACE::StringSpan::TransparentHash;
        };

        template <>
        struct DefaultEqual<std::string>
        {
            using type = HEXA_UTILS_NAMESPACE::StringSpan::TransparentEqual;
        };

        template <typename TKey>
        using DefaultHash_t = typename DefaultHash<TKey>::type;

        template <typename TKey>
        using DefaultEqual_t = typename DefaultEqual<TKey>::type;
    } // namespace DenseMapInternal

    template <typename TKey, typename TValue, typename Hash = typename DenseMapInternal::DefaultHash_t<TKey>, typename KeyEqual = DenseMapInternal::DefaultEqual_t<TKey>,
              typename Allocator = std::allocator<std::pair<const TKey, TValue>>>
    class dense_map
    {
      public:
        using pair = std::pair<const TKey, TValue>;

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
            pair pair;

            Entry() = default;

            template <typename P>
            Entry(size_t hash, P&& pair) : EntryMetadata(hash), pair(std::forward<P>(pair))
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

        template <typename K = TKey>
        const Entry* cfind_entry(const Entry* entries, size_t capacity, const K& key, size_t hash) const
        {
            const Entry* tombstone = nullptr;
            size_t index = hash & (capacity - 1);
            size_t tag = hash | EntryMetadata::FLAG_MASK;
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
                else if (entry->raw == tag && key_equal(entry->pair.first, key))
                {
                    return entry;
                }

                index = (index + 1) & (capacity - 1);
            } while (index != start);

            // this should never happen only if someone forgot to call reserve.
            throw std::runtime_error("Infinite loop detected in find_entry.");
        }

        template <typename K = TKey>
        Entry* find_entry(Entry* entries, size_t capacity, const K& key, size_t hash) const
        {
            auto result = cfind_entry<K>(entries, capacity, key, hash);
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
                        Entry& dest = *find_entry(newBuckets, newCapacity, entry.pair.first, entry.get_hash());
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

      public:
        template <typename U = pair, typename UEntry = Entry>
        class iterator_base
        {
            friend class dense_map;
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
                return ptr->pair;
            }
            pointer operator->() const
            {
                return &ptr->pair;
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

        using iterator = iterator_base<>;
        using const_iterator = iterator_base<const pair, const Entry>;
        using insert_result = std::pair<iterator, bool>;

      private:
        inline Entry* buckets_end()
        {
            return buckets ? buckets + capacity_m : nullptr;
        }

        inline const Entry* buckets_end() const
        {
            return buckets ? buckets + capacity_m : nullptr;
        }

        inline insert_result make_result(Entry* entry, bool success)
        {
            return insert_result(iterator(entry, buckets_end()), success);
        }

      public:
        dense_map()
        {
            allocator = {};
        }

        dense_map(const entry_allocator& alloc) : allocator(alloc) {}

        dense_map(const Hash& hasher, const KeyEqual& equals, const entry_allocator& alloc) : hasher(hasher), key_equal(equals), allocator(alloc) {}

        dense_map(const dense_map& other) : capacity_m(0), size_m(0), buckets(nullptr), allocator(other.allocator), hasher(other.hasher), key_equal(other.key_equal)
        {
            if (other.size_m > 0)
            {
                resize(other.capacity_m);
                for (const auto& kv : other)
                {
                    insert(kv);
                }
            }
        }

        dense_map(dense_map&& other) noexcept
            : buckets(other.buckets), capacity_m(other.capacity_m), size_m(other.size_m), allocator(std::move(other.allocator)), hasher(std::move(other.hasher)), key_equal(std::move(other.key_equal))
        {
            other.buckets = nullptr;
            other.capacity_m = 0;
            other.size_m = 0;
        }

        dense_map& operator=(const dense_map& other)
        {
            if (this != &other)
            {
                clear();
                allocator = other.allocator;
                hasher = other.hasher;
                if (other.size_m > 0)
                {
                    resize(other.capacity_m);
                    for (const auto& kv : other)
                    {
                        insert(kv);
                    }
                }
            }
            return *this;
        }

        dense_map& operator=(dense_map&& other) noexcept
        {
            if (this != &other)
            {
                clear();
                allocator.deallocate(buckets, capacity_m);
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

        ~dense_map()
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

        size_t NextPowerOfTwo(size_t n)
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

        template <typename P>
        insert_result insert(P&& kv)
        {
            reserve(size_m + 1);

            const auto& key = kv.first;
            size_t hash = hasher(key);
            Entry* entry = find_entry(buckets, capacity_m, key, hash);

            if (entry->isFilled())
            {
                return make_result(entry, false);
            }

            new (entry) Entry(hash, std::forward<P>(kv));

            ++size_m;

            return make_result(entry, true);
        }

        insert_result insert(pair&& kv)
        {
            reserve(size_m + 1);

            const auto& key = kv.first;
            size_t hash = hasher(key);
            Entry* entry = find_entry(buckets, capacity_m, key, hash);

            if (entry->isFilled())
            {
                return make_result(entry, false);
            }

            new (entry) Entry(hash, std::move(kv));

            ++size_m;

            return make_result(entry, true);
        }

        insert_result insert(const pair& kv)
        {
            reserve(size_m + 1);

            const auto& key = kv.first;
            size_t hash = hasher(key);
            Entry* entry = find_entry(buckets, capacity_m, key, hash);

            if (entry->isFilled())
            {
                return make_result(entry, false);
            }

            new (entry) Entry(hash, kv);

            ++size_m;

            return make_result(entry, true);
        }

        template <typename P>
        bool insert_or_assign(P&& kv)
        {
            reserve(size_m + 1);

            const auto& key = kv.first;
            size_t hash = hasher(key);
            Entry* entry = find_entry(buckets, capacity_m, key, hash);

            if (entry->isFilled())
            {
                entry->pair.second = std::forward<decltype(kv.second)>(kv.second);
                return false;
            }

            new (entry) Entry(hash, std::forward<P>(kv));

            ++size_m;
            return true;
        }

        bool insert_or_assign(pair&& kv)
        {
            reserve(size_m + 1);

            const auto& key = kv.first;
            size_t hash = hasher(key);
            Entry* entry = find_entry(buckets, capacity_m, key, hash);

            if (entry->isFilled())
            {
                entry->pair.second = std::move(kv.second);
                return false;
            }

            new (entry) Entry(hash, std::move(kv));

            ++size_m;
            return true;
        }

        bool insert_or_assign(const pair& kv)
        {
            reserve(size_m + 1);

            const auto& key = kv.first;
            size_t hash = hasher(key);
            Entry* entry = find_entry(buckets, capacity_m, key, hash);

            if (entry->isFilled())
            {
                entry->pair.second = kv.second;
                return false;
            }

            new (entry) Entry(hash, kv);

            ++size_m;
            return true;
        }

        template <typename... Args>
        insert_result emplace(Args&&... args)
        {
            pair kv = pair(std::forward<Args>(args)...);
            return insert(std::move(kv));
        }

        iterator find(const TKey& key)
        {
            if (size_m == 0)
                return end();
            size_t hash = hasher(key);
            Entry* entry = find_entry(buckets, capacity_m, key, hash);

            if (entry->isFilled())
            {
                return iterator(entry, buckets + capacity_m);
            }

            return end();
        }

        const_iterator find(const TKey& key) const
        {
            if (size_m == 0)
                return end();
            size_t hash = hasher(key);
            const Entry* entry = cfind_entry(buckets, capacity_m, key, hash);

            if (entry->isFilled())
            {
                return const_iterator(entry, buckets + capacity_m);
            }

            return end();
        }

        template <typename K>
            requires DenseMapInternal::HasIsTransparent<Hash, KeyEqual>
        iterator find(const K& key)
        {
            if (size_m == 0)
                return end();
            size_t hash = hasher(key);
            Entry* entry = find_entry(buckets, capacity_m, key, hash);

            if (entry->isFilled())
            {
                return iterator(entry, buckets + capacity_m);
            }

            return end();
        }

        template <typename K>
            requires DenseMapInternal::HasIsTransparent<Hash, KeyEqual>
        const_iterator find(const K& key) const
        {
            if (size_m == 0)
                return end();
            size_t hash = hasher(key);
            const Entry* entry = cfind_entry(buckets, capacity_m, key, hash);

            if (entry->isFilled())
            {
                return const_iterator(entry, buckets + capacity_m);
            }

            return end();
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

            ++it;
            return it;
        }

        iterator erase(const TKey& key)
        {
            auto it = find(key);
            if (it != end())
            {
                return erase(it);
            }
            return end();
        }

        size_t erase_key(const TKey& key)
        {
            auto it = find(key);
            if (it != end())
            {
                erase(it);
                return 1;
            }
            return 0;
        }

        template <typename K>
            requires DenseMapInternal::HasIsTransparent<Hash, KeyEqual>
        iterator erase(const K& key)
        {
            auto it = find(key);
            if (it != end())
            {
                return erase(it);
            }
            return end();
        }

        template <typename K>
            requires DenseMapInternal::HasIsTransparent<Hash, KeyEqual>
        size_t erase_key(const K& key)
        {
            auto it = find(key);
            if (it != end())
            {
                erase(it);
                return 1;
            }
            return 0;
        }

        bool contains(const TKey& key) const
        {
            return find(key) != end();
        }

        template <typename K>
            requires DenseMapInternal::HasIsTransparent<Hash, KeyEqual>
        bool contains(const K& key) const
        {
            return find(key) != end();
        }

        void swap(dense_map& other) noexcept
        {
            std::swap(buckets, other.buckets);
            std::swap(capacity_m, other.capacity_m);
            std::swap(size_m, other.size_m);
            std::swap(allocator, other.allocator);
            std::swap(hasher, other.hasher);
            std::swap(key_equal, other.key_equal);
        }

        TValue& operator[](const TKey& key)
        {
            reserve(size_m + 1);
            size_t hash = hasher(key);
            Entry* entry = find_entry(buckets, capacity_m, key, hash);
            if (!entry->isFilled())
            {
                new (entry) Entry(hash, pair(key, TValue{}));
                ++size_m;
            }
            return entry->pair.second;
        }

        TValue& at(const TKey& key)
        {
            auto it = find(key);
            if (it == end())
                throw std::out_of_range("dense_map::at: key not found");
            return it->second;
        }

        const TValue& at(const TKey& key) const
        {
            auto it = find(key);
            if (it == end())
                throw std::out_of_range("dense_map::at: key not found");
            return it->second;
        }

        template <typename K>
            requires DenseMapInternal::HasIsTransparent<Hash, KeyEqual>
        TValue& at(const K& key)
        {
            auto it = find(key);
            if (it == end())
                throw std::out_of_range("dense_map::at: key not found");
            return it->second;
        }

        template <typename K>
            requires DenseMapInternal::HasIsTransparent<Hash, KeyEqual>
        const TValue& at(const K& key) const
        {
            auto it = find(key);
            if (it == end())
                throw std::out_of_range("dense_map::at: key not found");
            return it->second;
        }
    };
} // namespace HEXA_UTILS_NAMESPACE

#endif