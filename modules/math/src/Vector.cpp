﻿#ifdef TYPEMAKESAMPLE_USE_MODULE
import math;
import std.core;
#else
#   include "typemakesample/math/Vector.hpp"
#endif

namespace typemakesample
{
namespace math
{

double Vector3d::dot(Vector3d left, Vector3d right)
{
    return left.data[0] * right.data[0] + left.data[1] * right.data[1] + left.data[2] * right.data[2];
}

Vector3d Vector3d::cross(Vector3d left, Vector3d right)
{
    return {{{left.data[1] * right.data[2] - left.data[2] * right.data[1], left.data[2] * right.data[0] - left.data[0] * right.data[2], left.data[0] * right.data[1] - left.data[1] * right.data[0]}}};
}

bool Vector3d::operator ==(Vector3d right)
{
    return (data[0] == right.data[0]) && (data[1] == right.data[1]) && (data[2] == right.data[2]);
}

bool Vector3d::operator !=(Vector3d right)
{
    return !(*this == right);
}

}
}
