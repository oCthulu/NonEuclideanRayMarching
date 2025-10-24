#define HYPERBOLIC

//Types
#define Pos float4
#define Transform float4x4

struct Ray
{
    Pos origin;
    Pos direction;
    float distanceTraveled;
};

struct HitResult
{
    bool hit;
    Pos position;
    float dist;
    float4 normal;
    float4 albedo;
};

//Methods
Pos GetOrigin(Ray r) { 
    return cosh(r.distanceTraveled) * r.origin + sinh(r.distanceTraveled) * r.direction;
}

void Advance(inout Ray r, float distance) {
    r.distanceTraveled += distance;
}

Ray TransformRay(Ray r, Transform t)
{
    Ray transformedRay;
    transformedRay.origin = mul(t, r.origin);
    transformedRay.direction = mul(t, r.direction);
    transformedRay.distanceTraveled = r.distanceTraveled; // Preserve distance traveled
    return transformedRay;
}

Ray GetRay(float2 uv)
{
    Ray ray;
    ray.origin = float4(0, 0, 0, 1); // Camera position in hyperbolic space
    ray.direction = normalize(float4(uv.x, uv.y, 1, 0)); // Ray direction based on UV coordinates
    ray.distanceTraveled = 0.0; // Initialize distance traveled
    return ray;
}

HitResult NoHit()
{
    HitResult result;
    result.hit = false;
    result.position = float4(0, 0, 0, 0);
    result.dist = 1e20; // A large distance
    result.normal = float4(0, 0, 0, 0);
    result.albedo = float4(0, 0, 0, 0);
    return result;
}