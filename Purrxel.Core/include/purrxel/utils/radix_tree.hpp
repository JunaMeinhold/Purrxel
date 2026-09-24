#ifndef RADIX_TREE_HPP
#define RADIX_TREE_HPP

#include "span.hpp"
#include "pch/std.hpp"

namespace HXSL
{
	// This radix tree is made for lookup performance not for low memory footprint.
	template <typename T, typename IndexType = uint32_t, typename Equals = std::equal_to<T>>
	class RadixTree
	{
		static constexpr IndexType RADIX_TREE_INVALID_INDEX = std::numeric_limits<IndexType>::max();
		static constexpr IndexType RADIX_TREE_ROOT = 0;
		static constexpr size_t MAX_CHILDREN_COUNT = 256;

		struct Node
		{
			IndexType parentIndex;
			std::string key;
			IndexType children[MAX_CHILDREN_COUNT];
			uint16_t childrenCount;
			T value;

			class iterator
			{
				const Node* node;
				size_t index = 0;
				uint16_t innerIndex = 0;

				void skipInvalid()
				{
					while (index < MAX_CHILDREN_COUNT && innerIndex < node->childrenCount)
					{
						if (node->children[index] != RADIX_TREE_INVALID_INDEX)
						{
							++innerIndex;
							break;
						}
						++index;
					}
				}

			public:
				using iterator_category = std::forward_iterator_tag;
				using value_type = std::pair<char, IndexType>;
				using difference_type = std::ptrdiff_t;
				using pointer = value_type*;
				using reference = value_type;

				iterator(const Node* node, uint16_t startIndex = 0) : node(node), innerIndex(startIndex), index(0)
				{
					skipInvalid();
				}

				reference operator*() const
				{
					return { static_cast<char>(index), node->children[index] };
				}

				value_type operator->() const
				{
					return { static_cast<char>(index), node->children[index] };
				}

				iterator& operator++()
				{
					++index;
					skipInvalid();
					return *this;
				}

				iterator operator++(int)
				{
					iterator tmp = *this;
					++(*this);
					return tmp;
				}

				bool operator==(const iterator& other) const
				{
					return node == other.node && innerIndex == other.innerIndex;
				}

				bool operator!=(const iterator& other) const
				{
					return !(*this == other);
				}
			};

			iterator begin() const
			{
				return iterator(this, 0);
			}

			iterator end() const
			{
				return iterator(this, childrenCount);
			}

			Node(IndexType parentIndex, const std::string& key, const T& value) : parentIndex(parentIndex), key(key), value(value), childrenCount(0)
			{
				std::fill(std::begin(children), std::end(children), RADIX_TREE_INVALID_INDEX);
			}

			bool empty() const
			{
				return childrenCount == 0;
			}

			size_t size() const
			{
				return childrenCount;
			}

			const IndexType& find(char c) const
			{
				return children[c];
			}

			IndexType& find(char c)
			{
				return children[c];
			}

			void Clear()
			{
				std::memset(children, 0, MAX_CHILDREN_COUNT * sizeof(IndexType));
				childrenCount = 0;
			}

			void AddChild(char c, IndexType index)
			{
				if (children[c] == RADIX_TREE_INVALID_INDEX)
				{
					++childrenCount;
				}
				children[c] = index;
			}

			void RemoveChild(char c)
			{
				children[c] = RADIX_TREE_INVALID_INDEX;
				--childrenCount;
			}
		};

		std::vector<Node> nodes; // root is always 0
		T defaultValue;
		Equals equals;

		IndexType AddNode(IndexType parentIndex, const std::string& key)
		{
			size_t index = nodes.size();
			nodes.push_back(Node(parentIndex, key, defaultValue));
			return static_cast<IndexType>(index);
		}

		void RemoveNode(IndexType index)
		{
			auto& nodeToRemove = nodes[index];
			if (!nodeToRemove.empty())
			{
				throw std::runtime_error("Cannot remove node with children.");
			}

			if (nodeToRemove.parentIndex != RADIX_TREE_INVALID_INDEX)
			{
				auto& parent = nodes[nodeToRemove.parentIndex];
				parent.RemoveChild(nodeToRemove.key[0]);
			}

			IndexType last = static_cast<IndexType>(nodes.size() - 1);
			if (last != index)
			{
				auto& swapNode = nodes[last];

				for (auto& child : swapNode)
				{
					nodes[child.second].parentIndex = index;
				}

				if (swapNode.parentIndex != RADIX_TREE_INVALID_INDEX)
				{
					auto& parent = nodes[swapNode.parentIndex];
					auto& it = parent.find(swapNode.key[0]);
					if (it != RADIX_TREE_INVALID_INDEX)
					{
						it = index;
					}
					else
					{
						throw std::runtime_error("Radix tree corruption detected.");
					}
				}

				std::swap(nodes[index], nodes[last]);
			}

			nodes.pop_back();
		}

		IndexType SplitNode(IndexType index, size_t commonLength)
		{
			auto& currentNode = nodes[index];

			auto lowerPart = currentNode.key.substr(0, commonLength);
			currentNode.key = currentNode.key.substr(commonLength);

			auto middleIndex = AddNode(currentNode.parentIndex, lowerPart);
			auto& middleNode = nodes[middleIndex];
			auto& upperNode = nodes[index];
			auto& parentNode = nodes[upperNode.parentIndex];

			parentNode.AddChild(lowerPart[0], middleIndex);
			middleNode.AddChild(upperNode.key[0], index);
			upperNode.parentIndex = middleIndex;

			return middleIndex;
		}

		bool MergeNode(IndexType index)
		{
			auto& currentNode = nodes[index];
			if (currentNode.size() != 1 || !equals(currentNode.value, defaultValue))
			{
				return false;
			}

			auto childIndex = (*currentNode.begin()).second;
			auto& childNode = nodes[childIndex];

			currentNode.key += childNode.key;
			std::memcpy(currentNode.children, childNode.children, MAX_CHILDREN_COUNT * sizeof(IndexType));
			currentNode.childrenCount = childNode.childrenCount;
			currentNode.value = std::move(childNode.value);

			for (auto& pair : currentNode)
			{
				nodes[pair.second].parentIndex = index;
			}

			childNode.parentIndex = RADIX_TREE_INVALID_INDEX;
			RemoveNode(childIndex);
			return true;
		}

		void LinkNode(IndexType from, IndexType to)
		{
			nodes[from].AddChild(nodes[to].key[0], to);
		}

		bool LookupNode(const StringSpan& key, IndexType& nodeIndex, size_t& commonLength, size_t& matchedLength) const
		{
			IndexType currentNode = RADIX_TREE_ROOT;
			IndexType lastMatchedIndex = RADIX_TREE_INVALID_INDEX;
			size_t idx = 0;
			const size_t keySize = key.size();
			while (idx < keySize)
			{
				const auto& node = nodes[currentNode];

				const auto& it = node.find(key[idx]);
				if (it == RADIX_TREE_INVALID_INDEX)
				{
					commonLength = 0;
					break;
				}

				lastMatchedIndex = currentNode = it;

				++idx;

				const auto& childNode = nodes[currentNode];

				const auto& edgeKey = childNode.key;
				const size_t edgeSize = edgeKey.size();

				size_t edgeIdx = 1;
				while (idx < keySize && edgeIdx < edgeSize && edgeKey[edgeIdx] == key[idx])
				{
					++idx;
					++edgeIdx;
				}

				commonLength = edgeIdx;

				if (commonLength < edgeSize)
				{
					break;
				}
			}

			matchedLength = idx;

			if (lastMatchedIndex != RADIX_TREE_INVALID_INDEX)
			{
				nodeIndex = lastMatchedIndex;
				return true;
			}

			nodeIndex = RADIX_TREE_INVALID_INDEX;
			return false;
		}

	public:
		RadixTree(T defaultVal = T(), Equals equalsFunc = Equals()) : defaultValue(defaultVal), equals(equalsFunc)
		{
			AddNode(RADIX_TREE_INVALID_INDEX, "\0");
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
			AddNode(RADIX_TREE_INVALID_INDEX, "\0");
		}

		bool RemoveKey(const StringSpan& key)
		{
			IndexType nodeIndex = RADIX_TREE_ROOT;
			size_t commonLength = 0;
			size_t matchedLength = 0;

			if (!LookupNode(key, nodeIndex, commonLength, matchedLength) || matchedLength != key.size())
			{
				return false;
			}

			auto& node = nodes[nodeIndex];

			node.value = defaultValue;

			if (!node.empty())
			{
				return true;
			}

			IndexType current = nodeIndex;
			while (current != RADIX_TREE_ROOT)
			{
				auto parentNode = nodes[current].parentIndex;
				RemoveNode(current);
				if (!MergeNode(parentNode))
				{
					break;
				}
				current = parentNode;
			}
			return true;
		}

		void Insert(const StringSpan& key, const T& value)
		{
			IndexType currentNode = RADIX_TREE_ROOT;
			size_t commonLength = 0;
			size_t matchedLength = 0;

			if (!LookupNode(key, currentNode, commonLength, matchedLength))
			{
				currentNode = RADIX_TREE_ROOT;
			}

			if (matchedLength == key.size() && commonLength == 0)
			{
				nodes[currentNode].value = value;
				return;
			}
			else if (commonLength != 0)
			{
				currentNode = SplitNode(currentNode, commonLength);
			}

			if (matchedLength == key.size())
			{
				nodes[currentNode].value = value;
				return;
			}

			auto newNode = AddNode(currentNode, key.str(matchedLength));
			LinkNode(currentNode, newNode);

			nodes[newNode].value = value;
		}

		bool MatchLongestPrefix(const StringSpan& text, T& value, size_t& matchedLength) const
		{
			if (nodes.size() == 1)
			{
				value = defaultValue;
				matchedLength = 0;
				return false;
			}

			size_t commonLength = 0;
			IndexType nodeIndex;
			if (LookupNode(text, nodeIndex, commonLength, matchedLength))
			{
				auto& node = nodes[nodeIndex];
				value = node.value;
				return true;
			}
			value = defaultValue;
			return false;
		}

		bool Find(const StringSpan& text, T& value, size_t& matchedLength) const
		{
			if (nodes.size() == 1)
			{
				value = defaultValue;
				matchedLength = 0;
				return false;
			}

			size_t commonLength = 0;
			IndexType nodeIndex;
			if (LookupNode(text, nodeIndex, commonLength, matchedLength))
			{
				auto& node = nodes[nodeIndex];
				if (!equals(node.value, defaultValue) && commonLength == 0)
				{
					value = node.value;
					return true;
				}
			}
			value = defaultValue;
			return false;
		}

		const T& operator[](const StringSpan& key) const
		{
			IndexType index;
			size_t commonLength;
			size_t matchedLength;
			if (!LookupNode(key, index, commonLength, matchedLength) || key.size() != matchedLength)
			{
				throw std::runtime_error("Key not found.");
			}
			return nodes[index].value;
		}
	};
}

#endif