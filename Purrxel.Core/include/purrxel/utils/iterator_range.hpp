#ifndef ITERATOR_RANGE_HPP
#define ITERATOR_RANGE_HPP

template <typename IteratorT>
class iterator_range 
{
    IteratorT Begin, End;
public:
    iterator_range(IteratorT begin, IteratorT end) : Begin(begin), End(end) 
    {
    }

    IteratorT begin() const { return Begin; }
    IteratorT end() const { return End; }
};


#endif