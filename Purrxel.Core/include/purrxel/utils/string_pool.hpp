#ifndef STRING_POOL_H
#define STRING_POOL_H

#include "span.hpp"
#include "dense_set.hpp"

namespace HXSL
{
	class StringPool
	{
	private:
		std::vector<std::unique_ptr<std::string>> strings;
		std::unordered_map<StringSpan, size_t> stringToIndex;

	public:
		const std::string& add(const std::string& str)
		{
			StringSpan view = str;

			auto it = stringToIndex.find(view);
			if (it != stringToIndex.end())
			{
				return *strings[it->second].get();
			}

			auto r = std::make_unique<std::string>(str);
			auto idx = strings.size();
			auto strPtr = r.get();

			strings.push_back(std::move(r));
			stringToIndex.insert(std::make_pair(*strPtr, idx));

			return *strPtr;
		}

		const std::string& add(const char* str)
		{
			StringSpan view = str;

			auto it = stringToIndex.find(view);
			if (it != stringToIndex.end())
			{
				return *strings[it->second].get();
			}

			auto r = std::make_unique<std::string>(str);
			auto idx = strings.size();
			auto strPtr = r.get();

			strings.push_back(std::move(r));
			stringToIndex.insert(std::make_pair(*strPtr, idx));

			return *strPtr;
		}

		const char* get_cstr(size_t index) const
		{
			if (index < strings.size())
			{
				return strings[index]->c_str();
			}
			return nullptr;
		}

		void clear()
		{
			strings.clear();
			stringToIndex.clear();
		}

		~StringPool()
		{
		}
	};

	class StringPool2
	{
	private:
		BumpAllocator allocator;
		dense_set<StringSpan> stringToIndex;

	public:
		StringSpan add(const StringSpan& str)
		{
			StringSpan view = str;

			auto it = stringToIndex.find(view);
			if (it != stringToIndex.end())
			{
				return *it;
			}

			auto span = allocator.CopyString(str);
			stringToIndex.insert(span);
			return span;
		}

		void clear()
		{
			stringToIndex.clear();
			allocator.Reset();
		}

		~StringPool2()
		{
		}
	};
}
#endif