#ifndef EUCLIDEAN
#include "Spaces/Euclidean.hlsl"
#endif

static const float INFINITY = 1e20;

float UnionSdf(float a, float b)
{
    return min(a, b);
}
HitResult UnionHit(HitResult a, HitResult b)
{
    if(a.dist < b.dist)
    {
        return a;
    }
    else
    {
        return b;
    }
}


float SmoothUnionSdf(float a, float b, float k)
{
    float h = saturate(0.5f + 0.5f * (b - a) / k);
    h = h * h * (3.0f - 2.0f * h);
    return lerp(b, a, h) - k * h * (1.0f - h);
}
HitResult SmoothUnionHit(HitResult a, HitResult b, float k)
{
    //TODO: Smooth the normals and albedo as well
    float h = saturate(0.5f + 0.5f * (b.dist - a.dist) / k);
    h = h * h * (3.0f - 2.0f * h);
    HitResult result;
    if(h < 0.5f)
    {
        result = a;
    }
    else
    {
        result = b;
    }
    result.dist = SmoothUnionSdf(a.dist, b.dist, k);
    result.normal = normalize(lerp(b.normal, a.normal, h));
    result.albedo = lerp(b.albedo, a.albedo, h);
    return result;
}


float AllSdf()
{
    return -INFINITY;
}
HitResult AllHit()
{
    HitResult result;
    result.hit = false;
    result.position = float3(0.0f, 0.0f, 0.0f);
    result.dist = -INFINITY;
    result.normal = float3(0.0f, 0.0f, 0.0f);
    result.albedo = float4(0.0f, 0.0f, 0.0f, 0.0f);
    return result;
}


float NoneSdf()
{
    return INFINITY;
}
HitResult NoneHit()
{
    HitResult result;
    result.hit = false;
    result.position = float3(0.0f, 0.0f, 0.0f);
    result.dist = INFINITY;
    result.normal = float3(0.0f, 0.0f, 0.0f);
    result.albedo = float4(0.0f, 0.0f, 0.0f, 0.0f);
    return result;
}

float IntersectionSdf(float a, float b)
{
    return max(a, b);
}
HitResult IntersectionHit(HitResult a, HitResult b)
{
    if(a.dist > b.dist)
    {
        return a;
    }
    else
    {
        return b;
    }
}


void AddSdf(inout float a, float b)
{
    a = UnionSdf(a, b);
}
void AddHit(inout HitResult a, HitResult b)
{
    a = UnionHit(a, b);
}


void IntersectSdf(inout float a, float b)
{
    a = IntersectionSdf(a, b);
}
void IntersectHit(inout HitResult a, HitResult b)
{
    a = IntersectionHit(a, b);
}


float SphereSdf(float3 pos, float3 center, float radius)
{
    return length(pos - center) - radius;
}
float SphereSdf(float3 pos, float3 center, float radius, float4 _)
{
    return SphereSdf(pos, center, radius);
}
HitResult SphereHit(float3 pos, float3 center, float radius, float4 albedo)
{
    HitResult result;
    result.hit = true;
    result.position = pos;
    result.albedo = albedo;
    result.dist = SphereSdf(pos, center, radius);

    result.normal = normalize(pos - center);

    return result;
}


float PlaneSdf(float3 pos, float3 normal, float d)
{
    return dot(pos, normal) - d;
}
float PlaneSdf(float3 pos, float3 normal, float d, float4 _)
{
    return PlaneSdf(pos, normal, d);
}
HitResult PlaneHit(float3 pos, float3 normal, float d, float4 albedo)
{
    HitResult result;
    result.hit = true;
    result.position = pos;
    result.albedo = albedo;
    result.dist = PlaneSdf(pos, normal, d);

    result.normal = normal;

    return result;
}


float CubeISdf(float3 pos, float3 center, float3 size)
{
    float obj = AllSdf();
    pos -= center;
    IntersectSdf(obj, PlaneSdf(pos, float3(1, 0, 0), size.x));
    IntersectSdf(obj, PlaneSdf(pos, float3(-1, 0, 0), size.x));
    IntersectSdf(obj, PlaneSdf(pos, float3(0, 1, 0), size.y));
    IntersectSdf(obj, PlaneSdf(pos, float3(0, -1, 0), size.y));
    IntersectSdf(obj, PlaneSdf(pos, float3(0, 0, 1), size.z));
    IntersectSdf(obj, PlaneSdf(pos, float3(0, 0, -1), size.z));
    return obj;
}
float CubeISdf(float3 pos, float3 center, float3 size, float4 _)
{
    return CubeISdf(pos, center, size);
}
HitResult CubeIHit(float3 pos, float3 center, float3 size, float4 albedo)
{
    HitResult obj = AllHit();
    pos -= center;
    IntersectHit(obj, PlaneHit(pos, float3(1, 0, 0), size.x, albedo));
    IntersectHit(obj, PlaneHit(pos, float3(-1, 0, 0), size.x, albedo));
    IntersectHit(obj, PlaneHit(pos, float3(0, 1, 0), size.y, albedo));
    IntersectHit(obj, PlaneHit(pos, float3(0, -1, 0), size.y, albedo));
    IntersectHit(obj, PlaneHit(pos, float3(0, 0, 1), size.z, albedo));
    IntersectHit(obj, PlaneHit(pos, float3(0, 0, -1), size.z, albedo));
    return obj;
}