#include "typemakesample.h"

#ifdef TYPEMAKESAMPLE_USE_MODULE
import math;
#else
#   include "typemakesample/math/Vector.hpp"
#endif

using namespace typemakesample::math;

TYPEMAKESAMPLE_API void typemakesample_Vector3d__ctor(double x, double y, double z, /* OUT */ typemakesample_Vector3d * * Return)
{
    *Return = reinterpret_cast<typemakesample_Vector3d *>(new Vector3d{ x, y, z });
}

TYPEMAKESAMPLE_API void typemakesample_Vector3d__dtor(typemakesample_Vector3d * This)
{
    auto a = reinterpret_cast<Vector3d *>(This);
    delete a;
}

TYPEMAKESAMPLE_API double typemakesample_Vector3d_dot(typemakesample_Vector3d * left, typemakesample_Vector3d * right)
{
    auto a = reinterpret_cast<Vector3d *>(left);
    auto b = reinterpret_cast<Vector3d *>(right);
    return Vector3d::dot(*a, *b);
}
