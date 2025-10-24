#define EUCLIDEAN

//Types
#define Pos float3
#define Transform float4x4

struct Ray
{
    Pos origin;
    Pos direction;
};

struct HitResult
{
    bool hit;
    Pos position;
    float dist;
    float3 normal;
    float4 albedo;
};

//Methods
Pos GetOrigin(Ray r) { return r.origin; }

void Advance(inout Ray r, float distance) { r.origin += r.direction * distance; }

Ray TransformRay(Ray r, Transform t)
{
    Ray transformedRay;
    transformedRay.origin = mul(t, float4(r.origin, 1.0f)).xyz;
    transformedRay.direction = normalize(mul(t, float4(r.direction, 0.0f)).xyz);
    return transformedRay;
}

Ray GetRay(float2 uv)
{
    Ray ray;
    ray.origin = float3(0.0f, 0.0f, 0.0f); // Camera position
    ray.direction = normalize(float3(uv, 1.0f)); // Ray direction based on UV coordinates
    return ray;
}

HitResult NoHit()
{
    HitResult result;
    result.hit = false;
    result.position = float3(0.0f, 0.0f, 0.0f);
    result.dist = 1e20; // A large distance
    result.normal = float3(0.0f, 0.0f, 0.0f);
    result.albedo = float4(0.0f, 0.0f, 0.0f, 0.0f);
    return result;
}