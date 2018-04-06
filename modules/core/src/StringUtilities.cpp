#include "typemakesample/core/StringUtilities.hpp"

#include <cctype>

namespace typemakesample
{
namespace core
{

bool EqualIgnoreCase(const std::string& l, const std::string& r)
{
    if (l.length() != r.length()) { return false; }
    for (size_t i = 0; i < l.length(); i += 1)
    {
        if (l[i] != r[i])
        {
            auto cl = std::tolower(l[i]);
            auto cr = std::tolower(r[i]);
            if (cl != cr)
            {
                return false;
            }
        }
    }
    return true;
}

}
}
