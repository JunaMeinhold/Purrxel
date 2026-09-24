#ifndef STACK_HPP
#define STACK_HPP

#include "pch/std.hpp"

namespace HXSL
{
	template <typename T>
	class IterStack {
	private:
		std::vector<T> data;

	public:
		IterStack() = default;

		IterStack(const IterStack& other)
		{
			data = other.data;
		}

		IterStack& operator=(const IterStack& other)
		{
			if (this != &other) {
				data = other.data;
			}
			return *this;
		}

		IterStack& operator=(IterStack&& other) noexcept
		{
			if (this != &other) {
				data = std::move(other.data);
			}
			return *this;
		}

		void push(const T& value)
		{
			data.push_back(value);
		}

		void pop()
		{
			if (!empty())
			{
				data.pop_back();
			}
		}

		T& top()
		{
			if (!empty())
			{
				return data.back();
			}
			throw std::out_of_range("Stack is empty");
		}

		bool empty() const
		{
			return data.empty();
		}

		size_t size() const
		{
			return data.size();
		}

		class ReverseIterator
		{
		private:
			typename std::vector<T>::reverse_iterator it;

		public:
			ReverseIterator(typename std::vector<T>::reverse_iterator start) : it(start) {}

			T& operator*() { return *it; }

			ReverseIterator& operator++() {
				++it;
				return *this;
			}

			bool operator!=(const ReverseIterator& other) const {
				return it != other.it;
			}
		};

		class ConstReverseIterator
		{
		private:
			typename std::vector<T>::const_reverse_iterator it;

		public:
			ConstReverseIterator(typename std::vector<T>::const_reverse_iterator start) : it(start) {}

			const T& operator*() { return *it; }

			ConstReverseIterator& operator++() {
				++it;
				return *this;
			}

			bool operator!=(const ConstReverseIterator& other) const {
				return it != other.it;
			}
		};

		ReverseIterator rbegin()
		{
			return ReverseIterator(data.rbegin());
		}

		ReverseIterator rend()
		{
			return ReverseIterator(data.rend());
		}

		ConstReverseIterator rbegin() const
		{
			return ConstReverseIterator(data.rbegin());
		}

		ConstReverseIterator rend() const
		{
			return ConstReverseIterator(data.rend());
		}
	};
}

#endif