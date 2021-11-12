#ifdef TYPEMAKESAMPLE_USE_MODULE
import core;
import math;
#include "typemakesample/core/StringUtilities.inc.hpp"
#include "typemakesample/math/Vector.inc.hpp"
#else
#include "typemakesample/core/StringUtilities.hpp"
#include "typemakesample/math/Vector.hpp"
#endif

#include <cstdio>

using namespace typemakesample::core;
using namespace typemakesample::math;

int main(int argc, char ** argv)
{
    std::printf("Vector3d::cross({{{1, 0, 0}}}, {{{0, 1, 0}}}) = %s\n", ToString(Vector3d::cross({{{1, 0, 0}}}, {{{0, 1, 0}}})).c_str());

    return 0;
}
