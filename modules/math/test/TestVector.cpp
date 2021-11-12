#ifdef TYPEMAKESAMPLE_USE_MODULE
import math;
#else
#include "typemakesample/math/Vector.hpp"
#endif

#include <cassert>

using namespace typemakesample::math;

int main(int argc, char ** argv)
{
    assert(Vector3d::dot({{{0, 0, 0}}}, {{{0, 0, 0}}}) == 0);
    assert(Vector3d::dot({{{1, 0, 0}}}, {{{1, 0, 0}}}) == 1);
    assert(Vector3d::dot({{{0, -1, 0}}}, {{{0, -1, 0}}}) == 1);
    assert(Vector3d::dot({{{0, 0, 1}}}, {{{0, 0, -1}}}) == -1);
    assert(Vector3d::dot({{{2, 0, -2}}}, {{{2, 0, 2}}}) == 0);
    assert(Vector3d::cross({{{1, 0, 0}}}, {{{0, 1, 0}}}) == Vector3d({{{0, 0, 1}}}));

    return 0;
}
