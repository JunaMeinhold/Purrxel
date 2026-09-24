#ifndef STATIC_VECTOR_HPP
#define STATIC_VECTOR_HPP

#include "pch/std.hpp"

namespace HEXA_UTILS_NAMESPACE
{

	template <typename T, typename Allocator = std::allocator<T>>
	class static_vector
	{
	public:
		using allocator_type = Allocator;
		using size_type = std::size_t;
		using value_type = T;
		using pointer = T*;
		using const_pointer = const T*;
		using reference = T&;
		using const_reference = const T&;

		using iterator = T*;
		using const_iterator = const T*;
		using reverse_iterator = std::reverse_iterator<iterator>;
		using const_reverse_iterator = std::reverse_iterator<const_iterator>;

	private:
		allocator_type alloc_m;
		pointer data_m = nullptr;
		size_type size_m = 0;

	public:
		static_vector() = default;
		explicit static_vector(size_type size, const allocator_type& alloc = allocator_type()) : size_m(size), alloc_m(alloc)
		{
			resize(size);
		}
		explicit static_vector(const allocator_type& alloc = allocator_type()) : alloc_m(alloc)
		{
		}

		~static_vector()
		{
			clear();
			alloc_m.deallocate(data_m, size_m);
			data_m = nullptr;
			size_m = 0;
		}

		template<typename... Args>
		void assign(Args&&... args)
		{
			resize(sizeof...(args));
			std::size_t i = 0;
			((data_m[i++] = std::forward<Args>(args)), ...);
		}

		reference operator[](size_type index)
		{
			if (index >= size_m)
			{
				throw std::out_of_range("static_vector index out of range");
			}
			return data_m[index];
		}

		const_reference operator[](size_type index) const
		{
			if (index >= size_m)
			{
				throw std::out_of_range("static_vector index out of range");
			}
			return data_m[index];
		}

		size_type size() const noexcept { return size_m; }
		pointer data() const noexcept { return data_m; }

		void resize(size_type new_size)
		{
			pointer new_data = alloc_m.allocate(new_size);

			try
			{
				if (data_m != nullptr)
				{
					auto min_size = std::min(size_m, new_size);
					std::uninitialized_move(data_m, data_m + min_size, new_data);
					if (new_size > size_m)
					{
						std::uninitialized_value_construct(new_data + size_m, new_data + new_size);
					}
					else
					{
						std::destroy(data_m + min_size, data_m + size_m);
					}

					alloc_m.deallocate(data_m, size_m);
				}
				else
				{
					std::uninitialized_value_construct(new_data, new_data + new_size);
				}
			}
			catch (...)
			{
				std::destroy(new_data, new_data + new_size);
				alloc_m.deallocate(new_data, new_size);
				throw;
			}

			data_m = new_data;
			size_m = new_size;
		}

		void clear()
		{
			std::destroy(data_m, data_m + size_m);
		}

		iterator begin() noexcept { return data_m; }
		const_iterator begin() const noexcept { return data_m; }
		const_iterator cbegin() const noexcept { return data_m; }

		iterator end() noexcept { return data_m + size_m; }
		const_iterator end() const noexcept { return data_m + size_m; }
		const_iterator cend() const noexcept { return data_m + size_m; }

		reverse_iterator rbegin() noexcept { return reverse_iterator(end()); }
		const_reverse_iterator rbegin() const noexcept { return const_reverse_iterator(end()); }
		const_reverse_iterator crbegin() const noexcept { return const_reverse_iterator(end()); }

		reverse_iterator rend() noexcept { return reverse_iterator(begin()); }
		const_reverse_iterator rend() const noexcept { return const_reverse_iterator(begin()); }
		const_reverse_iterator crend() const noexcept { return const_reverse_iterator(begin()); }
	};
}

#endif