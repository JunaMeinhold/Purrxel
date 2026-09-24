#ifndef HEXA_UTILS_TRAILING_OBJECTS_HPP
#define HEXA_UTILS_TRAILING_OBJECTS_HPP

#include "common.hpp"
#include "memory.hpp"
#include "array.hpp"

namespace HEXA_UTILS_NAMESPACE
{
	namespace TrailingObjectsInternal
	{
		template <size_t Index, typename... Ts>
		struct trailing_type_at;

		template <typename T, typename... Ts>
		struct trailing_type_at<0, T, Ts...>
		{
			using type = T;
		};

		template <size_t Index, typename T, typename... Ts>
		struct trailing_type_at<Index, T, Ts...>
		{
			static_assert(Index < sizeof...(Ts) + 1, "Index out of bounds");
			using type = typename trailing_type_at<Index - 1, Ts...>::type;
		};

		template <typename BaseT, typename TopTrailingObj, typename... TrailingTs>
		struct TrailingObjectsImpl;

		template <typename BaseT, typename TopTrailingObj>
		struct TrailingObjectsImpl<BaseT, TopTrailingObj>
		{
			static constexpr size_t AdditionalSizeToAllocImpl(size_t sizeSoFar)
			{
				return sizeSoFar;
			}

			static constexpr size_t GetTrailingObjectsImpl(size_t offset, size_t index)
			{
				return offset;
			}
		};

		template <typename BaseT, typename TopTrailingObj, typename PrevT>
		struct TrailingObjectsImpl<BaseT, TopTrailingObj, PrevT>
		{
			static constexpr size_t AdditionalSizeToAllocImpl(size_t sizeSoFar, size_t count)
			{
				return AlignUp(sizeSoFar, alignof(PrevT)) + (count * sizeof(PrevT));
			}

			static constexpr size_t GetTrailingObjectsImpl(size_t offset, size_t index, size_t count)
			{
				offset = AlignUp(offset, alignof(PrevT));
				if (index == 0)
					return offset;
				return offset + (count * sizeof(PrevT));
			}
		};

		template <typename BaseT, typename TopTrailingObj, typename PrevT, typename NextT, typename... RestT>
		struct TrailingObjectsImpl<BaseT, TopTrailingObj, PrevT, NextT, RestT...> : public TrailingObjectsImpl<BaseT, TopTrailingObj, NextT, RestT...>
		{
			using ParentType = TrailingObjectsImpl<BaseT, TopTrailingObj, NextT, RestT...>;

			static constexpr size_t Offset(size_t offset, size_t count)
			{
				return offset + (count * sizeof(PrevT));
			}

			template <typename... Counts>
			static constexpr size_t AdditionalSizeToAllocImpl(size_t sizeSoFar, size_t count, Counts... moreCounts)
			{
				sizeSoFar = AlignUp(sizeSoFar, alignof(PrevT));
				return ParentType::AdditionalSizeToAllocImpl(Offset(sizeSoFar, count), moreCounts...);
			}

			template <typename... Counts>
			static constexpr size_t GetTrailingObjectsImpl(size_t offset, size_t index, size_t count, Counts... moreCounts)
			{
				offset = AlignUp(offset, alignof(PrevT));
				if (index == 0)
					return offset;
				auto next = Offset(offset, count);
				return ParentType::GetTrailingObjectsImpl(next, index - 1, moreCounts...);
			}
		};
	} // namespace TrailingObjectsInternal

	template <typename BaseT, typename... TrailingTs>
	class TrailingObjects : private TrailingObjectsInternal::TrailingObjectsImpl<BaseT, TrailingObjects<BaseT, TrailingTs...>, TrailingTs...>
	{
		using TrailingObjectsImpl = TrailingObjectsInternal::TrailingObjectsImpl<BaseT, TrailingObjects<BaseT, TrailingTs...>, TrailingTs...>;

		BaseT* GetSelf()
		{
			return static_cast<BaseT*>(this);
		}

		const BaseT* GetSelf() const
		{
			return static_cast<const BaseT*>(this);
		}

	public:
		static constexpr size_t ObjectTrailLength = sizeof...(TrailingTs);

		template <size_t Index>
		using trailing_type_at = typename TrailingObjectsInternal::trailing_type_at<Index, TrailingTs...>;

		template <size_t Index>
		using trailing_type_at_t = typename trailing_type_at<Index>::type;

		template <size_t Index, typename... Counts>
		inline const auto GetTrailingObjects(Counts... counts) const
		{
			static_assert(sizeof...(Counts) == ObjectTrailLength, "Counts arguments must match TrailingTs");
			using ElementType = trailing_type_at_t<Index>;
			auto offset = TrailingObjectsImpl::GetTrailingObjectsImpl(sizeof(BaseT), Index, counts...);
			auto ptr = reinterpret_cast<const ElementType*>(reinterpret_cast<const char*>(GetSelf()) + offset);
			return ptr;
		}

		template <size_t Index, typename... Counts>
		inline auto GetTrailingObjects(Counts... counts)
		{
			static_assert(sizeof...(Counts) == ObjectTrailLength, "Counts arguments must match TrailingTs");
			using ElementType = trailing_type_at_t<Index>;
			auto offset = TrailingObjectsImpl::GetTrailingObjectsImpl(sizeof(BaseT), Index, counts...);
			auto ptr = reinterpret_cast<ElementType*>(reinterpret_cast<char*>(GetSelf()) + offset);
			return ptr;
		}

		template <size_t Index, typename TCount, size_t... Is>
		inline auto GetTrailingObjectsPtrImpl(const TCount* counts, std::index_sequence<Is...>) const
		{
			return GetTrailingObjects<Index>(counts[Is]...);
		}

		template <size_t Index, typename TCount, size_t... Is>
		inline auto GetTrailingObjectsPtrImpl(const TCount* counts, std::index_sequence<Is...>)
		{
			return GetTrailingObjects<Index>(counts[Is]...);
		}

		template <size_t Index, typename TCount>
		inline const auto GetTrailingObjectsPtr(const TCount* counts) const
		{
			return GetTrailingObjectsPtrImpl<Index>(counts, std::make_index_sequence<sizeof...(TrailingTs)>{});
		}

		template <size_t Index, typename TCount>
		inline auto GetTrailingObjectsPtr(const TCount* counts)
		{
			return GetTrailingObjectsPtrImpl<Index>(counts, std::make_index_sequence<sizeof...(TrailingTs)>{});
		}

		template <typename... Counts>
		static constexpr size_t TotalSizeToAlloc(Counts... counts)
		{
			static_assert(sizeof...(Counts) == sizeof...(TrailingTs), "Counts arguments must match TrailingTs");
			return TrailingObjectsImpl::AdditionalSizeToAllocImpl(sizeof(BaseT), counts...);
		}

		template <typename... Counts>
		static constexpr size_t AdditionalSizeToAlloc(Counts... counts)
		{
			static_assert(sizeof...(Counts) == sizeof...(TrailingTs), "Counts arguments must match TrailingTs");
			return TotalSizeToAlloc(counts...) - sizeof(BaseT);
		}
	};

	template <typename Trail, typename T>
	struct TrailingObjStorage
	{
		constexpr TrailingObjStorage() = default;

		template <typename... Counts>
			requires(sizeof...(Counts) == Trail::ObjectTrailLength)
		constexpr TrailingObjStorage(Counts... counts) : counts{ static_cast<T>(counts)... }
		{
		}

		template <size_t Index>
		using trailing_type_at = Trail::template trailing_type_at<Index>;

		template <size_t Index>
		using trailing_type_at_t = typename trailing_type_at<Index>::type;

		HEXA_UTILS_NAMESPACE::Array<T, Trail::ObjectTrailLength> counts;

		template <size_t Index>
		inline T GetCount() const
		{
			return counts[Index];
		}

		template <size_t Index>
		inline auto GetTrailingObjects(Trail* self)
		{
			return self->template GetTrailingObjectsPtr<Index>(counts.data());
		}

		template <size_t Index>
		inline auto GetTrailingObjects(const Trail* self) const
		{
			return self->template GetTrailingObjectsPtr<Index>(counts.data());
		}

		template <size_t Index>
		inline auto GetTrailingSpan(Trail* self)
		{
			using T = trailing_type_at_t<Index>;
			return Span<T>{GetTrailingObjects<Index>(self), counts[Index]};
		}

		template <size_t Index>
		inline auto GetTrailingSpan(const Trail* self) const
		{
			using T = trailing_type_at_t<Index>;
			return Span<const T>{GetTrailingObjects<Index>(self), counts[Index]};
		}

		template <typename... Counts>
		inline void SetCounts(Counts... counts)
		{
			static_assert(sizeof...(Counts) == Trail::ObjectTrailLength, "Counts arguments must match TrailingTs");
			this->counts = Array<T, Trail::ObjectTrailLength>{ static_cast<T>(counts)... };
		}

		template <size_t Index>
		inline void SetCount(T count)
		{
			static_assert(Index < Trail::ObjectTrailLength, "Index out of range");
			this->counts[Index] = count;
		}

		template<size_t Index, typename Span>
		inline void Init(Trail* self, const Span& input)
		{
			SetCount<Index>(static_cast<T>(input.size()));
			auto span = GetTrailingSpan<Index>(self);
			std::uninitialized_move(input.begin(), input.end(), span.data());
		}

		template <typename... Spans>
		inline void InitializeMove(Trail* self, Spans&&... spans)
		{
			static_assert(sizeof...(Spans) == Trail::ObjectTrailLength, "Spans arguments must match TrailingTs");
			InitializeMoveImpl(self, std::make_index_sequence<Trail::ObjectTrailLength>{}, std::forward<Spans>(spans)...);
		}

		template <size_t... Is, typename... Spans>
		inline void InitializeMoveImpl(Trail* self, std::index_sequence<Is...>, Spans&&... spans)
		{
			(Init<Is>(self, spans), ...);
		}
	};

#define DEFINE_TRAILING_OBJ_SPAN_GETTER(name, idx, storage)                                                                                                                                            \
    inline auto name()                                                                                                                                                                                 \
    {                                                                                                                                                                                                  \
        return storage.GetTrailingSpan<idx>(this);                                                                                                                                                     \
    }                                                                                                                                                                                                  \
    inline auto name() const                                                                                                                                                                           \
    {                                                                                                                                                                                                  \
        return storage.GetTrailingSpan<idx>(this);                                                                                                                                                     \
    }
} // namespace HEXA_UTILS_NAMESPACE

#endif