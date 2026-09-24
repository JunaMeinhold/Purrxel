#include "utils/bump_allocator.hpp"

static constexpr size_t PageSize = 8192;

namespace HEXA_UTILS_NAMESPACE
{
	BumpAllocator::Block* BumpAllocator::AllocBlock(Block* prev, size_t minSize)
	{
		size_t size = AlignUp(minSize + sizeof(Block), PageSize);
		uint8_t* mem = static_cast<uint8_t*>(aligned_alloc(size, PageSize));
		size_t usableSpace = size - sizeof(Block);
		Block* block = new(mem + usableSpace) Block(prev, usableSpace);
		return block;
	}

	void BumpAllocator::DestroyBlock(Block* block)
	{
		if (block == nullptr) return;
		block->~Block();
		aligned_free(block->GetBaseAddress());
	}

	BumpAllocator::Block* BumpAllocator::CreateBlock(size_t minSize)
	{
		Block* block = AllocBlock(tail, minSize);
		tail = block;
		if (head == nullptr) head = block;
		return block;
	}

	
}