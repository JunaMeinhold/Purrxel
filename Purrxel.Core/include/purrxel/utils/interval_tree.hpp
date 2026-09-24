#ifndef INTERVAL_TREE_HPP
#define INTERVAL_TREE_HPP

#include "pch/std.hpp"

namespace HXSL
{
	template<typename T>
	struct Interval
	{
		T start;
		T end;

		constexpr Interval(const T& start, const T& end) : start(start), end(end)
		{
		}

		bool operator==(const Interval& other) const
		{
			return start == other.start && end == other.end;
		}

		bool operator!=(const Interval& other) const
		{
			return !(*this == other);
		}
	};

	template<typename T, typename IntervalT = size_t, typename IndexType = uint32_t>
	class IntervalTree
	{
		using Interval = Interval<IntervalT>;
		using height_type = std::make_signed_t<IndexType>;

		static constexpr IndexType INVALID_INDEX = std::numeric_limits<IndexType>::max();

		struct Node
		{
			IndexType parentIndex;
			IndexType left = INVALID_INDEX;
			IndexType right = INVALID_INDEX;
			Interval interval;
			IntervalT maxEnd;
			height_type height;

			T value;

			Node(IndexType parentIndex, const Interval& interval, height_type height, const T& value) : parentIndex(parentIndex), interval(interval), maxEnd(interval.end), height(height), value(value)
			{
			}
		};

		std::vector<Node> nodes;
		IndexType root = INVALID_INDEX;

		inline height_type BalanceFactor(IndexType index) const noexcept
		{
			auto& node = nodes[index];
			auto left = node.left;
			auto right = node.right;
			auto leftHeight = left == INVALID_INDEX ? 0 : nodes[left].height;
			auto rightHeight = right == INVALID_INDEX ? 0 : nodes[right].height;
			return leftHeight - rightHeight;
		}

		void UpdateNode(IndexType index) noexcept
		{
			auto& node = nodes[index];
			height_type leftHeight = 0;
			IntervalT leftMaxEnd = 0;
			if (node.left != INVALID_INDEX)
			{
				auto& left = nodes[node.left];
				leftHeight = left.height;
				leftMaxEnd = left.maxEnd;
			}

			height_type rightHeight = 0;
			IntervalT rightMaxEnd = 0;
			if (node.right != INVALID_INDEX)
			{
				auto& right = nodes[node.right];
				rightHeight = right.height;
				rightMaxEnd = right.maxEnd;
			}

			node.height = 1 + std::max(leftHeight, rightHeight);
			node.maxEnd = std::max({ node.interval.end, leftMaxEnd, rightMaxEnd });
		}

		inline void UpdateRelationRotate(IndexType srcIdx, IndexType dstIdx, IndexType t2) noexcept
		{
			auto& src = nodes[srcIdx];
			auto& dst = nodes[dstIdx];

			dst.parentIndex = src.parentIndex;
			src.parentIndex = dstIdx;
			if (t2 != INVALID_INDEX)
			{
				nodes[t2].parentIndex = srcIdx;
			}

			if (dst.parentIndex != INVALID_INDEX)
			{
				auto& gpNode = nodes[dst.parentIndex];
				if (gpNode.left == srcIdx)
				{
					gpNode.left = dstIdx;
				}
				else
				{
					gpNode.right = dstIdx;
				}
			}
			else
			{
				root = dstIdx;
			}
		}

		IndexType RotateRight(IndexType yidx) noexcept
		{
			auto& y = nodes[yidx];
			auto xidx = y.left;
			auto& x = nodes[xidx];
			auto t2 = x.right;

			x.right = yidx;
			y.left = t2;

			UpdateRelationRotate(yidx, xidx, t2);
			UpdateNode(yidx);
			UpdateNode(xidx);
			return xidx;
		}

		IndexType RotateLeft(IndexType xidx) noexcept
		{
			auto& x = nodes[xidx];
			auto yidx = x.right;
			auto& y = nodes[yidx];
			auto t2 = y.left;

			y.left = xidx;
			x.right = t2;

			UpdateRelationRotate(xidx, yidx, t2);
			UpdateNode(xidx);
			UpdateNode(yidx);
			return yidx;
		}

		IndexType Balance(IndexType n) noexcept
		{
			UpdateNode(n);
			auto& node = nodes[n];
			auto bf = BalanceFactor(n);
			if (bf > 1)
			{
				if (BalanceFactor(node.left) < 0)
				{
					node.left = RotateLeft(node.left);
				}
				return RotateRight(n);
			}
			if (bf < -1)
			{
				if (BalanceFactor(node.right) > 0)
				{
					node.right = RotateRight(node.right);
				}
				return RotateLeft(n);
			}
			return n;
		}

		inline void BalanceUp(IndexType index) noexcept
		{
			while (true)
			{
				index = Balance(index);
				auto& node = nodes[index];
				if (node.parentIndex == INVALID_INDEX)
				{
					break;
				}
				index = node.parentIndex;
			}

			root = index;
		}

		IndexType AddNode(IndexType parentIndex, const Interval& interval, const T& value)
		{
			auto idx = nodes.size();
			nodes.push_back(Node(parentIndex, interval, 1, value));
			return static_cast<IndexType>(idx);
		}

		void RemoveNode(IndexType index)
		{
			auto last = static_cast<IndexType>(nodes.size() - 1);
			if (last != index)
			{
				auto& swapped = nodes[last];
				if (swapped.left != INVALID_INDEX)
				{
					nodes[swapped.left].parentIndex = index;
				}
				if (swapped.right != INVALID_INDEX)
				{
					nodes[swapped.right].parentIndex = index;
				}

				if (swapped.parentIndex != INVALID_INDEX)
				{
					auto& parent = nodes[swapped.parentIndex];
					if (parent.left == last)
					{
						parent.left = index;
					}
					else
					{
						parent.right = index;
					}
				}

				std::swap(nodes[index], nodes[last]);
			}
			nodes.pop_back();
		}

		void RemoveAt(IndexType index)
		{
			auto& nodeFront = nodes[index];
			if (nodeFront.left != INVALID_INDEX && nodeFront.right != INVALID_INDEX)
			{
				IndexType successor = nodeFront.right;
				while (nodes[successor].left != INVALID_INDEX)
				{
					successor = nodes[successor].left;
				}

				std::swap(nodeFront.interval, nodes[successor].interval);
				std::swap(nodeFront.value, nodes[successor].value);

				index = successor;
			}

			auto& node = nodes[index];

			IndexType child = (node.left != INVALID_INDEX) ? node.left : node.right;
			IndexType parent = node.parentIndex;

			if (child != INVALID_INDEX)
			{
				nodes[child].parentIndex = parent;
			}

			if (parent != INVALID_INDEX)
			{
				auto& parentNode = nodes[parent];
				if (parentNode.left == index)
				{
					parentNode.left = child;
				}
				else
				{
					parentNode.right = child;
				}
			}
			else
			{
				root = child;
			}

			RemoveNode(index);
			BalanceUp(parent);
		}

	public:
		IntervalTree()
		{
		}

		void Insert(const Interval& interval, const T& value)
		{
			if (root == INVALID_INDEX)
			{
				root = AddNode(INVALID_INDEX, interval, value);
				return;
			}

			IndexType current = root;
			while (true)
			{
				auto& node = nodes[current];

				if (interval.start < node.interval.start)
				{
					if (node.left == INVALID_INDEX)
					{
						current = node.left = AddNode(current, interval, value);
						break;
					}
					else
					{
						current = node.left;
					}
				}
				else
				{
					if (node.right == INVALID_INDEX)
					{
						current = node.right = AddNode(current, interval, value);
						break;
					}
					else
					{
						current = node.right;
					}
				}
			}

			BalanceUp(current);
		}

		void Remove(const Interval& interval)
		{
			if (root == INVALID_INDEX) return;
			if (nodes.size() == 1)
			{
				auto& node = nodes[0];
				if (node.interval == interval)
				{
					nodes.pop_back();
					root = INVALID_INDEX;
				}
				return;
			}

			IndexType current = root;
			while (current != INVALID_INDEX)
			{
				auto& node = nodes[current];

				if (interval.start < node.interval.start)
				{
					current = node.left;
				}
				else if (interval.start > node.interval.start)
				{
					current = node.right;
				}
				else if (interval == node.interval)
				{
					break;
				}
				else
				{
					current = node.right;
				}
			}

			if (current == INVALID_INDEX) return;
			RemoveAt(current);
		}

		void SearchOverlapping(const Interval& i, std::vector<const Node*>& result) const
		{
			if (root == INVALID_INDEX) return;
			std::stack<IndexType> stack;
			stack.push(root);

			while (!stack.empty())
			{
				IndexType current = stack.top();
				stack.pop();

				const Node& node = nodes[current];

				if (node.left != INVALID_INDEX && nodes[node.left].maxEnd >= i.start)
				{
					stack.push(node.left);
				}

				if (node.interval.start < i.end && i.start < node.interval.end)
				{
					result.push_back(&nodes[current]);
				}

				if (node.right != INVALID_INDEX && nodes[node.right].maxEnd >= i.start)
				{
					stack.push(node.right);
				}
			}
		}

		void SearchOverlapping(const IntervalT& point, std::vector<const Node*>& result) const
		{
			SearchOverlapping(Interval(point, point + 1), result);
		}
	};
}

#endif