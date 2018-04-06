#pragma once
#include <string>
#include <memory>
#include <vector>
#include <sstream>
#include <stdexcept>

namespace typemakesample
{
namespace core
{

template<typename T>
std::string ToString(T value)
{
    std::stringstream s;
    s << value;
    return std::string(s.str());
}

bool EqualIgnoreCase(const std::string &l, const std::string &r);

}
}
