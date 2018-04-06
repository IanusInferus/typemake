#pragma once

#include <array>
#include <ostream>

namespace typemakesample
{
namespace math
{

struct Vector3d
{
public:
    std::array<double, 3> data;

    static double dot(Vector3d left, Vector3d right);
    static Vector3d cross(Vector3d left, Vector3d right);
    bool operator ==(Vector3d right);
    bool operator !=(Vector3d right);
};

std::ostream & operator <<(std::ostream & stream, const Vector3d & v);

}
}
