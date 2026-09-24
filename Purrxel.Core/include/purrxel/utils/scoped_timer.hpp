#ifndef SCOPED_TIMER_HPP
#define SCOPED_TIMER_HPP

#include "pch/std.hpp"

namespace HXSL
{

    class ScopedTimer 
    {
    public:
        ScopedTimer(const std::string& name)
            : name(name), start(std::chrono::high_resolution_clock::now()) 
        {
        }

        ~ScopedTimer() 
        {
            auto end = std::chrono::high_resolution_clock::now();
            std::chrono::duration<double, std::milli> duration = end - start;
            std::cout << name << " took " << duration.count() << " ms\n";
        }

    private:
        std::string name;
        std::chrono::time_point<std::chrono::high_resolution_clock> start;
    };

#define PROFILE_SCOPE(name) ScopedTimer timer##__LINE__(name)
}

#endif