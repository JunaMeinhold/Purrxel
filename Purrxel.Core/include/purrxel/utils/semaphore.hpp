#ifndef HEXA_UTILS_SEMAPHORE_HPP
#define HEXA_UTILS_SEMAPHORE_HPP

#include "common.hpp"

namespace HEXA_UTILS_NAMESPACE
{
	class semaphore
	{
		size_t maxCount;
		std::atomic<size_t> count;
		
	public:
		semaphore(size_t maxCount) : maxCount(maxCount), count(maxCount)
		{
		}

		semaphore(size_t maxCount, size_t initialCount) : maxCount(maxCount), count(initialCount)
		{
		}
		
		bool try_acquire()
		{
			size_t expected = count.load(std::memory_order_acquire);
			while (true)
			{
				if (expected == 0)
				{
					return false;
				}
				else if (count.compare_exchange_weak(expected, expected - 1, std::memory_order_release, std::memory_order_acquire))
				{
					return true;
				}
			}
		}

		void acquire()
		{
			size_t expected = count.load(std::memory_order_acquire);
			while (true)
			{
				if (expected == 0)
				{
					count.wait(expected, std::memory_order_relaxed);
				}
				else if (count.compare_exchange_weak(expected, expected - 1, std::memory_order_release, std::memory_order_acquire))
				{
					break;
				}
			}
		}

		size_t release()
		{
			size_t expected = count.load(std::memory_order_relaxed);
			while (count.compare_exchange_weak(expected, std::min(expected + 1, maxCount), std::memory_order_release, std::memory_order_acquire));
			count.notify_one();
			return expected;
		}

		size_t current_count() const
		{
			return count.load(std::memory_order_acquire);
		}
	};
}

#endif