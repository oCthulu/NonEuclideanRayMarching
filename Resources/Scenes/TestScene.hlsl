#include "Spaces/Euclidean.hlsl"
#include "Shapes/Euclidean.hlsl"

cbuffer SceneConstants : register(b0)
{
    float4x4 camTransform;
};

// Scene SDF
float GetDistance(float3 p)
{
    float obj = NoneSdf();
    AddSdf(obj, SphereSdf(p, float3(0, 0, 0), 1));
    AddSdf(obj, CubeISdf(p, float3(0, -2, 0), float3(2, 2, 2)));
    return obj;
}

// Scene Hit
HitResult GetHit(float3 p)
{
    HitResult obj = NoneHit();
    AddHit(obj, SphereHit(p, float3(0, 0, 0), 1, float4(1, 0, 0, 1)));
    AddHit(obj, CubeIHit(p, float3(0, -2, 0), float3(2, 2, 2), float4(0, 1, 0, 1)));
    return obj;
}

Transform GetTransform()
{
    return camTransform;
}

#include "RayMarchingCore.hlsl"
#include "Renderers/EuclideanLit.hlsl"
#include "RayMarchingMain.hlsl"