#include "typemakesample/core/StringUtilities.hpp"
#include "typemakesample/math/Vector.hpp"

#include <jni.h>
#include <string>

using namespace typemakesample::core;
using namespace typemakesample::math;

extern "C" JNIEXPORT jstring JNICALL Java_typemakesample_hello_MainActivity_stringFromJNI(JNIEnv *env, jobject /* this */)
{
    std::string hello = "Vector3d::cross({{{1, 0, 0}}}, {{{0, 1, 0}}}) = " + ToString(Vector3d::cross({{{1, 0, 0}}}, {{{0, 1, 0}}}));
    return env->NewStringUTF(hello.c_str());
}
