#define HYPERBOLIC

//Types
#define Pos float4
#define Transform float4x4

#include "HyperbolicUtils.hlsl"

struct Ray
{
    Pos origin;
    Pos direction;
    float sinhDistanceTraveled;
    float coshDistanceTraveled;
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
    //return r.origin;


    return r.coshDistanceTraveled * r.origin + r.sinhDistanceTraveled * r.direction;
    //return cosh(r.distanceTraveled) * r.origin + sinh(r.distanceTraveled) * r.direction;
}

// For hyperbolic space, we store the sinh of the distance traveled, NOT the distance itself
void Advance(inout Ray r, float sinhDistance) {
    //float sinhDistance = sinh(distance);
    float coshDist = SinhToCosh(sinhDistance);

    // r.sinhDistanceTraveled = sinh(r.distanceTraveled);
    // r.coshDistanceTraveled = cosh(r.distanceTraveled);

    float oldSinh = r.sinhDistanceTraveled;
    float oldCosh = r.coshDistanceTraveled;

    r.sinhDistanceTraveled = oldCosh * sinhDistance + oldSinh * coshDist;
    r.coshDistanceTraveled = oldCosh * coshDist + oldSinh * sinhDistance;

    // r.origin = coshDist * r.origin + sinhDistance * r.direction;
    // r.direction = sinhDistance * r.origin + coshDist * r.direction;
}

Ray TransformRay(Ray r, Transform t)
{
    Ray transformedRay;
    transformedRay.origin = mul(t, r.origin);
    transformedRay.direction = mul(t, r.direction);
    transformedRay.sinhDistanceTraveled = r.sinhDistanceTraveled;
    transformedRay.coshDistanceTraveled = r.coshDistanceTraveled;
    return transformedRay;
}

Ray GetRay(float2 uv)
{
    Ray ray;
    ray.origin = float4(0, 0, 0, 1); // Camera position in hyperbolic space
    ray.direction = normalize(float4(uv.x, uv.y, 1, 0)); // Ray direction based on UV coordinates
    ray.sinhDistanceTraveled = 0;
    ray.coshDistanceTraveled = 1;

    return ray;
}

HitResult NoHit()
{
    HitResult result;
    result.hit = false;
    result.position = float4(0, 0, 0, 0);
    //this field is called dist instead of sinhDist for compatibility with euclidean Union and Intersection definitions
    result.dist = 1e20; // A large distance
    result.normal = float4(0, 0, 0, 0);
    result.albedo = float4(0, 0, 0, 0);
    return result;
}