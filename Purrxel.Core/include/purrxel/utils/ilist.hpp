#ifndef INTRUSIVE_LIST_HPP
#define INTRUSIVE_LIST_HPP

#include "common.hpp"
#include "bump_allocator.hpp"

namespace HXSL
{
	template<typename T>
	class ilist;

	template<typename T>
	class IntrusiveLinkedBase
	{
		friend class ilist<T>;
		T* prev = nullptr;
		T* next = nullptr;
	public:
		T* GetPrev() const noexcept { return prev; }
		T* GetNext() const noexcept { return next; }
	};

	template<typename T>
	class ilist
	{
		static_assert(std::is_base_of_v<IntrusiveLinkedBase<T>, T>, "T must inherit from IntrusiveLinkedBase<T>");

	private:
		T* head = nullptr;
		T* tail = nullptr;
		BumpAllocator* allocator;

		ilist(BumpAllocator* alloc) : allocator(alloc) {}

	public:
		class iterator
		{
			T* node;

		public:
			using difference_type = std::ptrdiff_t;
			using value_type = T;
			using pointer = T*;
			using reference = T&;
			using iterator_category = std::bidirectional_iterator_tag;

			explicit iterator(T* ptr = nullptr) : node(ptr) {}

			reference operator*() const { return *node; }
			pointer operator->() const { return node; }

			iterator& operator++()
			{
				node = node->next;
				return *this;
			}

			iterator operator++(int)
			{
				iterator temp = *this;
				++(*this);
				return temp;
			}

			iterator& operator--()
			{
				node = node->prev;
				return *this;
			}

			iterator operator--(int)
			{
				iterator temp = *this;
				--(*this);
				return temp;
			}

			bool operator==(const iterator& other) const { return node == other.node; }
			bool operator!=(const iterator& other) const { return node != other.node; }
		};

		iterator begin() { return iterator(head); }
		iterator end() { return iterator(nullptr); }

		class const_iterator
		{
			const T* node;

		public:
			using difference_type = std::ptrdiff_t;
			using value_type = T;
			using pointer = const T*;
			using reference = const T&;
			using iterator_category = std::bidirectional_iterator_tag;

			explicit const_iterator(const T* ptr = nullptr) : node(ptr) {}

			reference operator*() const { return *node; }
			pointer operator->() const { return node; }

			const_iterator& operator++()
			{
				node = node->next;
				return *this;
			}

			const_iterator operator++(int)
			{
				const_iterator temp = *this;
				++(*this);
				return temp;
			}

			const_iterator& operator--()
			{
				node = node->prev;
				return *this;
			}

			const_iterator operator--(int)
			{
				const_iterator temp = *this;
				--(*this);
				return temp;
			}

			bool operator==(const const_iterator& other) const { return node == other.node; }
			bool operator!=(const const_iterator& other) const { return node != other.node; }
		};

		const_iterator begin() const { return const_iterator(head); }
		const_iterator end() const { return const_iterator(nullptr); }
		const_iterator cbegin() const { return const_iterator(head); }
		const_iterator cend() const { return const_iterator(nullptr); }

		class reverse_iterator
		{
			T* node;

		public:
			using difference_type = std::ptrdiff_t;
			using value_type = T;
			using pointer = T*;
			using reference = T&;
			using iterator_category = std::bidirectional_iterator_tag;

			explicit reverse_iterator(T* ptr = nullptr) : node(ptr) {}

			reference operator*() const { return *node; }
			pointer operator->() const { return node; }

			reverse_iterator& operator++()
			{
				node = node->prev;
				return *this;
			}

			reverse_iterator operator++(int)
			{
				reverse_iterator temp = *this;
				++(*this);
				return temp;
			}

			reverse_iterator& operator--()
			{
				node = node->next;
				return *this;
			}

			reverse_iterator operator--(int)
			{
				reverse_iterator temp = *this;
				--(*this);
				return temp;
			}

			bool operator==(const reverse_iterator& other) const { return node == other.node; }
			bool operator!=(const reverse_iterator& other) const { return node != other.node; }
		};

		class const_reverse_iterator
		{
			const T* node;

		public:
			using difference_type = std::ptrdiff_t;
			using value_type = T;
			using pointer = const T*;
			using reference = const T&;
			using iterator_category = std::bidirectional_iterator_tag;

			explicit const_reverse_iterator(const T* ptr = nullptr) : node(ptr) {}

			reference operator*() const { return *node; }
			pointer operator->() const { return node; }

			const_reverse_iterator& operator++()
			{
				node = node->prev;
				return *this;
			}

			const_reverse_iterator operator++(int)
			{
				const_reverse_iterator temp = *this;
				++(*this);
				return temp;
			}

			const_reverse_iterator& operator--()
			{
				node = node->next;
				return *this;
			}

			const_reverse_iterator operator--(int)
			{
				const_reverse_iterator temp = *this;
				--(*this);
				return temp;
			}

			bool operator==(const const_reverse_iterator& other) const { return node == other.node; }
			bool operator!=(const const_reverse_iterator& other) const { return node != other.node; }
		};

		reverse_iterator rbegin() { return reverse_iterator(tail); }
		reverse_iterator rend() { return reverse_iterator(nullptr); }
		const_reverse_iterator rbegin() const { return const_reverse_iterator(tail); }
		const_reverse_iterator rend() const { return const_reverse_iterator(nullptr); }
		const_reverse_iterator crbegin() const { return const_reverse_iterator(tail); }
		const_reverse_iterator crend() const { return const_reverse_iterator(nullptr); }

		explicit ilist(BumpAllocator& alloc) : allocator(&alloc) {}

		ilist(ilist&& other) noexcept : head(other.head), tail(other.tail), allocator(other.allocator)
		{
			other.head = nullptr;
			other.tail = nullptr;
			other.allocator = nullptr;
		}

		ilist& operator=(ilist&& other) noexcept
		{
			if (this != &other)
			{
				head = other.head;
				tail = other.tail;
				allocator = other.allocator;

				other.head = nullptr;
				other.tail = nullptr;
				other.allocator = nullptr;
			}
			return *this;
		}

		ilist(const ilist& other)
			: head(nullptr), tail(nullptr), allocator(other.allocator)
		{
			for (const T& value : other)
			{
				append(value);
			}
		}

		ilist& operator=(const ilist& other)
		{
			if (this != &other)
			{
				clear();
				allocator = other.allocator;
				for (const T& value : other)
				{
					append(value);
				}
			}
			return *this;
		}

		~ilist()
		{
			clear();
		}

		T& front() { return *head; }
		T& back() { return *tail; }
		BumpAllocator& get_allocator() { return *allocator; }

		T* pop_front_move()
		{
			if (!head)
			{
				return nullptr;
			}

			T* node = head;
			head = node->next;
			if (head)
			{
				head->prev = nullptr;
			}
			else
			{
				tail = nullptr;
			}

			return node;
		}

		T* pop_back_move()
		{
			if (!tail)
			{
				return nullptr;
			}

			T* node = tail;
			tail = node->prev;
			if (tail)
			{
				tail->next = nullptr;
			}
			else
			{
				head = nullptr;
			}

			return node;
		}

		T* append_move(T* newNode)
		{
			newNode->next = nullptr;
			if (!head)
			{
				newNode->prev = nullptr;
				head = tail = newNode;
			}
			else
			{
				tail->next = newNode;
				newNode->prev = tail;
				tail = newNode;
			}

			return tail;
		}

		T* append(const T& val)
		{
			T* newNode = allocator->Alloc<T>(val);
			return append_move(newNode);
		}

		T* append_move(ilist& list)
		{
			if (!list.head) return nullptr;

			if (!head)
			{
				head = list.head;
				tail = list.tail;
			}
			else
			{
				tail->next = list.head;
				list.head->prev = tail;
				list.head = tail;
				tail = list.tail;
			}

			list.head = nullptr;
			list.tail = nullptr;

			return tail;
		}

		T* prepend_move(T* newNode)
		{
			newNode->prev = nullptr;
			if (!head)
			{
				newNode->next = nullptr;
				head = tail = newNode;
			}
			else
			{
				newNode->next = head;
				head->prev = newNode;
				head = newNode;
			}

			return head;
		}

		T* prepend(const T& val)
		{
			T* newNode = allocator->Alloc<T>(val);
			return prepend_move(newNode);
		}

		T* prepend_move(ilist& list)
		{
			if (!list.head) return nullptr;

			if (!head)
			{
				head = list.head;
				tail = list.tail;
			}
			else
			{
				list.tail->next = head;
				head->prev = list.tail;
				head = list.head;
			}

			list.head = nullptr;
			list.tail = nullptr;

			return head;
		}

		template<typename... Args>
		T* emplace_append(Args&&... args)
		{
			T* newNode = allocator->Alloc<T>(std::forward<Args>(args)...);
			if (!head)
			{
				head = tail = newNode;
			}
			else
			{
				tail->next = newNode;
				newNode->prev = tail;
				tail = newNode;
			}

			return newNode;
		}

		template<typename... Args>
		T* emplace_prepend(Args&&... args)
		{
			T* newNode = allocator->Alloc<T>(std::forward<Args>(args)...);
			if (!head)
			{
				head = tail = newNode;
			}
			else
			{
				newNode->next = head;
				head->prev = newNode;
				head = newNode;
			}

			return newNode;
		}

		T* insert(const iterator& it, const T& val)
		{
			if (it == end())
			{
				return append(val);
			}

			T* newNode = allocator->Alloc<T>(val);
			T* currentNode = &*it;

			newNode->next = currentNode;
			newNode->prev = currentNode->prev;

			if (currentNode->prev)
			{
				currentNode->prev->next = newNode;
			}
			else
			{
				head = newNode;
			}
			currentNode->prev = newNode;

			return newNode;
		}

		T* insert(const iterator& it, T* newNode)
		{
			if (it == end())
			{
				return append_move(newNode);
			}

			T* currentNode = &*it;

			newNode->next = currentNode;
			newNode->prev = currentNode->prev;

			if (currentNode->prev)
			{
				currentNode->prev->next = newNode;
			}
			else
			{
				head = newNode;
			}
			currentNode->prev = newNode;

			return newNode;
		}

		template<typename... Args>
		T* emplace_insert(const iterator& it, Args&&... args)
		{
			if (it == end())
			{
				return emplace_append(std::forward<Args>(args)...);
			}

			T* newNode = allocator->Alloc<T>(std::forward<Args>(args)...);
			T* currentNode = &*it;

			newNode->next = currentNode;
			newNode->prev = currentNode->prev;

			if (currentNode->prev)
			{
				currentNode->prev->next = newNode;
			}
			else
			{
				head = newNode;
			}
			currentNode->prev = newNode;

			return newNode;
		}

		void remove(T* node)
		{
			if (!node) return;

			if (node->prev)
			{
				node->prev->next = node->next;
			}
			else
			{
				head = node->next;
			}

			if (node->next)
			{
				node->next->prev = node->prev;
			}
			else
			{
				tail = node->prev;
			}

			node->prev = node->next = nullptr;
			node->~T();
		}

		T* replace(T* node, const T& val)
		{
			T* newNode = allocator->Alloc<T>(val);
			newNode->prev = node->prev;
			newNode->next = node->next;
			if (newNode->next)
			{
				newNode->next->prev = newNode;
			}
			if (newNode->prev)
			{
				newNode->prev->next = newNode;
			}
			if (node == tail)
			{
				tail = newNode;
			}
			if (node == head)
			{
				head = newNode;
			}
			node->~T();
			return newNode;
		}

		template<typename U, typename... Args>
		T* emplace_replace(T* node, Args&&... args)
		{
			T* newNode = allocator->Alloc<U>(std::forward<Args>(args)...);
			newNode->prev = node->prev;
			newNode->next = node->next;
			if (newNode->next)
			{
				newNode->next->prev = newNode;
			}
			if (newNode->prev)
			{
				newNode->prev->next = newNode;
			}
			if (node == tail)
			{
				tail = newNode;
			}
			if (node == head)
			{
				head = newNode;
			}
			node->~T();
			return newNode;
		}

		void append_range_move(T* start, T* end)
		{
			HEXA_UTILS_ASSERT(!start->prev && !end->next, "Dangling chain detected");
			if (tail)
			{
				tail->next = start;
				start->prev = tail;
			}
			tail = end;
			if (!head)
			{
				head = start;
			}
		}

		void prepend_range_move(T* start, T* end)
		{
			HEXA_UTILS_ASSERT(!start->prev && !end->next, "Dangling chain detected");
			if (head)
			{
				head->prev = end;
				end->next = head;
			}
			head = start;
			if (!tail)
			{
				tail = end;
			}
		}

		void split_before_core(T* node, ilist<T>& other, bool append)
		{
			HEXA_UTILS_ASSERT(node, "Cannot split null node");
			if (!head) return;
			auto oldTail = tail;
			auto prev = node->prev;
			node->prev = nullptr;
			if (prev)
			{
				prev->next = nullptr;
				tail = prev;
			}
			else
			{
				head = nullptr;
				tail = nullptr;
			}
			if (append)
			{
				other.append_range_move(node, oldTail);
			}
			else
			{
				other.prepend_range_move(node, oldTail);
			}
		}

		void split_before_prepend(T* node, ilist<T>& other) { split_before_core(node, other, false); }

		void split_before_append(T* node, ilist<T>& other) { split_before_core(node, other, true); }

		void split_after_core(T* node, ilist<T>& other, bool append)
		{
			if (!node->next) return;
			split_before_core(node->next, other, append);
		}

		void split_after_prepend(T* node, ilist<T>& other) { split_after_core(node, other, false); }

		void split_after_append(T* node, ilist<T>& other) { split_after_core(node, other, true); }

		void clear()
		{
			T* current = head;
			while (current)
			{
				T* next = current->next;
				current->~T();
				current = next;
			}

			head = nullptr;
			tail = nullptr;
		}

		bool empty() const { return head == nullptr; }

		size_t size() const
		{
			size_t count = 0;
			for (auto it = begin(); it != end(); ++it)
				++count;
			return count;
		}

		void trim_start(T* node)
		{
			if (!node || head == node)
				return;

			T* current = head;
			while (current && current != node)
			{
				T* next = current->next;
				current->~T();
				current = next;
			}
			head = node;
			head->prev = nullptr;
		}

		void trim_end(T* node)
		{
			if (!node || tail == node)
				return;

			T* current = node->next;
			while (current)
			{
				T* next = current->next;
				current->~T();
				current = next;
			}
			tail = node;
			tail->next = nullptr;
		}
	};
}

#endif