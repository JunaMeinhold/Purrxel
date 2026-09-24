#ifndef TERNARY_SEARCH_TREE_DICTIONARY_HPP
#define TERNARY_SEARCH_TREE_DICTIONARY_HPP

#include "span.hpp"
#include "pch/std.hpp"

namespace HXSL
{
	template <typename T, typename Equals = std::equal_to<T>>
	class TernarySearchTreeDictionary
	{
		static constexpr size_t TST_INVALID_INDEX = static_cast<size_t>(-1);

	private:
		struct Node
		{
			size_t parentIndex;
			char splitChar;
			size_t left;
			size_t middle;
			size_t right;
			T value;

			Node(size_t parentIndex, char splitChar, const T& value)
				: parentIndex(parentIndex), splitChar(splitChar), left(TST_INVALID_INDEX), middle(TST_INVALID_INDEX), right(TST_INVALID_INDEX), value(value)
			{
			}
		};

		std::vector<Node> nodes;
		T defaultValue;
		Equals equals;

		size_t AddNode(size_t parentIndex, char splitChar)
		{
			size_t index = nodes.size();
			nodes.push_back(Node(parentIndex, splitChar, defaultValue));
			return index;
		}

		void RemoveNode(size_t index)
		{
			// Could potentially degrade performance because of the loss of cache locality when looking up, but it's as not bad as a linked list.
			size_t last = Count() - 1;
			if (last != index)
			{
				auto lastNode = nodes[last];
				nodes[index] = lastNode;
				size_t parentIndex = lastNode.ParentIndex;
				if (parentIndex != -1)
				{
					auto parent = &nodes[parentIndex];
					if (parent->right == last)
						parent->right = index;
					if (parent->middle == last)
						parent->middle = index;
					if (parent->left == last)
						parent->left = index;
				}
			}

			nodes.pop_back();
		}

		size_t InsertChar(size_t& index, char c)
		{
			while (true)
			{
				Node node = nodes[index];

				if (node.splitChar == '\0')
				{
					node.splitChar = c;

					size_t newIndex = AddNode(index, '\0');
					node.middle = newIndex;

					nodes[index] = node;

					return newIndex;
				}

				if (c < node.splitChar)
				{
					if (node.left == -1)
					{
						node.left = AddNode(index, '\0');
						nodes[index] = node;
					}
					index = node.left;
				}
				else if (c > node.splitChar)
				{
					if (node.right == -1)
					{
						node.right = AddNode(index, '\0');
						nodes[index] = node;
					}
					index = node.right;
				}
				else
				{
					if (node.middle == -1)
					{
						node.middle = AddNode(index, '\0');
						nodes[index] = node;
					}
					return node.middle;
				}
			}
		}

		bool LookupNode(const StringSpan& key, size_t& nodeIndex, size_t& matchedLength) const
		{
			size_t index = 0;
			size_t lastMatchedIndex = TST_INVALID_INDEX;
			matchedLength = 0;

			size_t i = 0;
			while (i < key.size())
			{
				Node node = nodes[index];
				char c = key[i];

				if (c < node.splitChar)
				{
					if (node.left == TST_INVALID_INDEX)
					{
						break;
					}

					index = node.left;
				}
				else if (c > node.splitChar)
				{
					if (node.right == TST_INVALID_INDEX)
					{
						break;
					}

					index = node.right;
				}
				else
				{
					i++;

					if (node.middle == TST_INVALID_INDEX)
					{
						break;
					}

					lastMatchedIndex = node.middle;
					matchedLength = i;
					index = node.middle;
				}
			}

			if (lastMatchedIndex != TST_INVALID_INDEX)
			{
				nodeIndex = lastMatchedIndex;
				return true;
			}

			nodeIndex = TST_INVALID_INDEX;
			return false;
		}

	public:
		TernarySearchTreeDictionary(T defaultVal = T(), Equals equalsFunc = Equals()) : defaultValue(defaultVal), equals(equalsFunc)
		{
			AddNode(TST_INVALID_INDEX, '\0');
		}

		virtual ~TernarySearchTreeDictionary()
		{
		}

		size_t Count() const
		{
			return nodes.size();
		}

		void ShrinkToFit()
		{
			nodes.shrink_to_fit();
		}

		void Clear()
		{
			nodes.clear();
			AddNode(-1, '\0');
		}

		bool RemoveKey(const StringSpan& key)
		{
			if (Count() == 0)
			{
				return false;
			}

			size_t index;
			size_t matchedLength;
			if (!LookupNode(key, index, matchedLength) || matchedLength != key.size())
			{
				return false;
			}

			while (index != -1)
			{
				const Node& node = nodes[index];
				if (equals(node.value, defaultValue) && node.left == -1 && node.middle == -1 && node.right == -1)
				{
					size_t parentIndex = node.ParentIndex;

					if (parentIndex != -1)
					{
						Node& parent = nodes[parentIndex];
						if (parent.left == index)
							parent.left = -1;
						if (parent.middle == index)
							parent.middle = -1;
						if (parent.right == index)
							parent.right = -1;
					}

					RemoveNode(index);

					index = parentIndex;
				}
				else
				{
					break;
				}
			}

			return true;
		}

		void InsertRange(const std::vector<std::pair<const StringSpan&, T>>& dict)
		{
			for (const auto& pair : dict)
			{
				const StringSpan& key = pair.first;
				const T& value = pair.second;
				Insert(key, value);
			}
		}

		void Insert(const StringSpan& key, const T& value)
		{
			size_t index = 0;

			for (char c : key)
			{
				index = InsertChar(index, c);
			}

			nodes[index].value = value;
		}

		bool MatchLongestPrefix(const StringSpan& text, T& value, size_t& matchedLength) const
		{
			if (Count() == 1)
			{
				value = defaultValue;
				matchedLength = 0;
				return false;
			}

			size_t nodeIndex;
			if (LookupNode(text, nodeIndex, matchedLength))
			{
				auto& node = nodes[nodeIndex];
				value = node.value;
				return true;
			}
			value = defaultValue;
			return false;
		}

		T operator[](const StringSpan& key) const
		{
			size_t index;
			size_t len;
			if (!LookupNode(key, index, len) || key.size() != len)
			{
				throw std::runtime_error("Key not found.");
			}
			return nodes[index].value;
		}
	};
}

#endif // TERNARY_SEARCH_TREE_DICTIONARY_H