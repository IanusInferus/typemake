#ifndef __TYPEMAKESAMPLE_H__
#define __TYPEMAKESAMPLE_H__

#ifdef _MSC_VER
#   ifdef TYPEMAKESAMPLE_BUILD
#       ifdef TYPEMAKESAMPLE_DYNAMIC
#           define TYPEMAKESAMPLE_API __declspec(dllexport)
#       else
#           define TYPEMAKESAMPLE_API
#       endif
#   else
#       define TYPEMAKESAMPLE_API
#   endif
#elif defined(__GNUC__) && __GNUC__ >= 4
#   ifdef TYPEMAKESAMPLE_BUILD
#       define TYPEMAKESAMPLE_API __attribute__ ((visibility("default")))
#   else
#       define TYPEMAKESAMPLE_API
#   endif
#else
#   define TYPEMAKESAMPLE_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef struct { char _placeHolder_; } typemakesample_Vector3d;

TYPEMAKESAMPLE_API void typemakesample_Vector3d__ctor(double x, double y, double z, /* OUT */ typemakesample_Vector3d * * Return);
TYPEMAKESAMPLE_API void typemakesample_Vector3d__dtor(typemakesample_Vector3d * This);
TYPEMAKESAMPLE_API double typemakesample_Vector3d_dot(typemakesample_Vector3d * left, typemakesample_Vector3d * right);

#ifdef __cplusplus
}
#endif

#endif
