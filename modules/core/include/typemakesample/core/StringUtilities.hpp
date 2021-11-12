#pragma once

#ifndef TYPEMAKESAMPLE_USE_MODULE
#include "typemakesample/core/StringUtilities.inc.hpp"
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
