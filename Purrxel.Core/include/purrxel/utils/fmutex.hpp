#ifndef HEXA_UTILS_FAST_MUTEX_HPP
#define HEXA_UTILS_FAST_MUTEX_HPP

#include "common.hpp"

namespace HEXA_UTILS_NAMESPACE
{
	struct fmutex
	{
		std::atomic_flag m = ATOMIC_FLAG_INIT;

		bool try_lock()
		{
			return !m.test_and_set(std::memory_order_acquire);
		}

		void lock()
		{
			while (m.test_and_set(std::memory_order_acquire))
			{
				m.wait(true, std::memory_order_relaxed);
			}
		}

		void wait() const
		{
			m.wait(true, std::memory_order_acquire);
		}

		void unlock()
		{
			m.clear(std::memory_order_release);
			m.notify_one();
		}
	};

	struct recurse_fmutex
	{
		using tid = std::thread::id;
		std::atomic_flag m = ATOMIC_FLAG_INIT;
		std::atomic<tid> ownerTid{ tid() };
		size_t counter = 0;

		static tid get_this_tid() { return std::this_thread::get_id(); }

		bool owns_lock(tid id) const { return ownerTid.load(std::memory_order_relaxed) == id; }
		bool owns_lock() const { return owns_lock(get_this_tid()); }

		bool try_lock()
		{
			auto result = owns_lock() || !m.test_and_set(std::memory_order_acquire);
			if (result)
			{
				++counter;
			}
			return counter;
		}

		void lock()
		{
			if (owns_lock()) 
			{
				++counter;
				return;
			}
			while (m.test_and_set(std::memory_order_acquire))
			{
				m.wait(true, std::memory_order_relaxed);
			}
			ownerTid.store(get_this_tid(), std::memory_order_acq_rel);
			++counter;
		}

		void unlock()
		{
			if (--counter == 0) return;
			ownerTid.store(tid(), std::memory_order_acq_rel);
			m.clear(std::memory_order_release);
			m.notify_one();
		}
	};

	struct shared_fmutex
	{
		std::atomic<size_t> state{ 0 };

		static constexpr size_t WRITER_BIT = static_cast<size_t>(1) << (sizeof(size_t) * 8 - 1);
		static constexpr size_t READER_MASK = ~WRITER_BIT;

		bool try_lock_shared()
		{
			size_t expected = state.load(std::memory_order_acquire);
			while (true)
			{
				if ((expected & WRITER_BIT) != 0)
				{
					return false;
				} 
				else if (state.compare_exchange_weak(expected, expected + 1, std::memory_order_release, std::memory_order_acquire))
				{
					return true;
				}
			}
		}

		void lock_shared()
		{
			size_t expected = state.load(std::memory_order_acquire);
			while (true)
			{
				if ((expected & WRITER_BIT) != 0)
				{
					state.wait(expected, std::memory_order_relaxed);
				}
				else if (state.compare_exchange_weak(expected, expected + 1, std::memory_order_release, std::memory_order_acquire))
				{
					break;
				}
			}
		}

		void unlock_shared()
		{
			auto prev = state.fetch_sub(1, std::memory_order_release);
			if ((prev & READER_MASK) == 1)
			{
				state.notify_all();
			}
		}

		void lock()
		{
			auto oldState = state.fetch_or(WRITER_BIT, std::memory_order_acquire);

			if ((oldState & WRITER_BIT) != 0)
			{
				do
				{
					state.wait(oldState, std::memory_order_relaxed);
					oldState = state.fetch_or(WRITER_BIT, std::memory_order_acquire);
				} while ((oldState & WRITER_BIT) != 0);
			}

			auto current = state.load(std::memory_order_acquire);
			while ((current & READER_MASK) != 0)
			{
				state.wait(current, std::memory_order_relaxed);
				current = state.load(std::memory_order_acquire);
			}
		}

		bool try_lock()
		{
			return state.fetch_or(WRITER_BIT, std::memory_order_acquire) == 0;
		}

		void unlock()
		{
			state.store(0, std::memory_order_release);
			state.notify_all();
		}

		void wait() const
		{
			state.wait(0, std::memory_order_acquire);
		}
	};

	class bit_fmutex
	{
	private:
		std::atomic<uint64_t> bits;
	public:
		bit_fmutex() : bits(0) {}

		void lock(uint8_t id)
		{
			const uint64_t mask = 1ull << id;
			uint64_t old = 0;
			while (((old = bits.fetch_or(mask, std::memory_order_acq_rel)) & mask) == mask)
			{
				bits.wait(old | mask, std::memory_order_relaxed);
			}
		}

		void unlock(uint8_t id)
		{
			const uint64_t mask = ~(1ull << id);
			bits.fetch_and(mask, std::memory_order_release);
			bits.notify_all();
		}

		bool try_lock(uint8_t id)
		{
			const uint64_t mask = 1ull << id;
			return (bits.fetch_or(mask, std::memory_order_acq_rel) & mask) == 0;
		}

		void wait(uint8_t id) const
		{
			const uint64_t mask = 1ull << id;
			uint64_t current = 0;
			while ((current = bits.load(std::memory_order_acquire)) & mask)
			{
				bits.wait(current, std::memory_order_relaxed);
			}
		}
	};
}

#endif