#pragma once

#ifdef TYPEMAKESAMPLE_USE_MODULE
import std.core;
import std.memory;
#else
#   include <string>
#   include <memory>
#   include <vector>
#   include <sstream>
#   include <stdexcept>
#endif

namespace typemakesample
{
namespace core
{

TYPEMAKESAMPLE_EXPORT template<typename T>
std::string ToString(T value)
{
    std::stringstream s;
    s << value;
    return std::string(s.str());
}

TYPEMAKESAMPLE_EXPORT bool EqualIgnoreCase(const std::string & l, const std::string & r);

}
}
