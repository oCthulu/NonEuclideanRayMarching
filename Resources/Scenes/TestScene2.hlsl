#include "Spaces/Euclidean.hlsl"
#include "Shapes/Euclidean.hlsl"

cbuffer SceneConstants : register(b0)
{
    float4x4 camTransform;
};

//This is the best macro I could come up with to reduce code duplication (you cant use a #if inside a #define)
#define SCENE(t, Object) \
    Object obj = None##t(); \
    Add##t(obj, Sphere##t(p, float3(0, 0, 0), 1, float4(1, 0, 0, 1))); \
    Add##t(obj, CubeI##t(p, float3(0, -2, 0.0), float3(2, 2, 2), float4(0, 1, 0, 1))); \
    return obj;

// Scene SDF
float GetDistance(float3 p)
{
    SCENE(Sdf, float)
}

// Scene Hit
HitResult GetHit(float3 p)
{
    SCENE(Hit, HitResult)
}

Transform GetTransform()
{
    return camTransform;
}

#include "RayMarchingCore.hlsl"
#include "Renderers/EuclideanLit.hlsl"
#include "RayMarchingMain.hlsl"