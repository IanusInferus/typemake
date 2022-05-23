#pragma once

#ifdef TYPEMAKESAMPLE_USE_MODULE
import std.core;
#else
#   include <array>
#   include <ostream>
#endif

namespace typemakesample
{
namespace math
{

TYPEMAKESAMPLE_EXPORT struct Vector3d
{
public:
    std::array<double, 3> data;

    static double dot(Vector3d left, Vector3d right);
    static Vector3d cross(Vector3d left, Vector3d right);
    bool operator ==(Vector3d right);
    bool operator !=(Vector3d right);
};

TYPEMAKESAMPLE_EXPORT inline std::ostream & operator <<(std::ostream & stream, const Vector3d & v)
{
    return stream << "{" << v.data[0] << " " << v.data[1] << " " << v.data[2] << "}";
}

}
}
