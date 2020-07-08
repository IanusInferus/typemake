#include "typemakesample.h"

#include <cstdio>

int main(int argc, char ** argv)
{
    typemakesample_Vector3d * a = nullptr;
    typemakesample_Vector3d * b = nullptr;
    typemakesample_Vector3d__ctor(1, 0, 0, &a);
    typemakesample_Vector3d__ctor(1, 1, 0, &b);
    auto result = typemakesample_Vector3d_dot(a, b);
    std::printf("Vector3d::cross({{{1, 0, 0}}}, {{{0, 1, 0}}}) = %f\n", result);
    typemakesample_Vector3d__dtor(a);
    typemakesample_Vector3d__dtor(b);

    return 0;
}
