/*
The following types will be aliased in another file using #define directives, and the methods will be defined there as well.
Depending on their definitions, they will define the curvature and other properties of the space.

Types (from space file):
    Pos - Represents a position in space (float3 in euclidean coordinates)
    Ray - Represents a ray in space (struct in euclidean coordinates)
    Transform - Represents a transformation in space (float4x4 in euclidean coordinates)
    HitResult - Represents the result of a ray hitting an object (struct in euclidean coordinates)

Types (from scene file):
    PIXEL_CONTEXT (optional) - Represents the context for a pixel (struct)

Methods (from space file):
    Pos GetOrigin(Ray r) - Returns the origin of the ray
    void Advance(inout Ray r, float distance) - Advances the ray by a specified distance
    Ray TransformRay(Ray r, Transform t) - Transforms the ray by a transformation matrix
    Ray GetRay(float2 uv) - Returns a ray based on UV coordinates
    GetColor(HitResult hit) - Returns the color for a hit result
    HitResult NoHit() - Returns a no-hit result

Methods (from scene file):
    Transform GetTransform() - Returns the transformation matrix for the scene
    float GetDistance(Pos p) - Returns the distance from a point to the scene
    HitResult GetHit(Pos p) - Returns the hit result for a point in the scene

Methods (from renderer file):
    float4 GetColor(Ray ray) - Returns the color for a ray by casting it into the scene
    
Methods (from this file):
    HitResult CastRay(Ray ray) - Casts a ray into the scene and returns the hit result
*/

#ifndef MAX_DISTANCE
#define MAX_DISTANCE 100.0f
#endif

#ifndef MAX_STEPS
#define MAX_STEPS 100
#endif

#ifndef EPSILON
#define EPSILON 0.001f
#endif

#ifndef EXTRA_PARAMS_DECL
#define EXTRA_PARAMS_DECL
#endif
#ifndef EXTRA_PARAMS
#define EXTRA_PARAMS
#endif
#ifndef EXTRA_PARAMS_INIT
#define EXTRA_PARAMS_INIT
#endif

HitResult CastRay(Ray ray EXTRA_PARAMS_DECL)
{
    // March the ray through the scene
    float distance = 0.0f;
    int steps = 0;
    while (distance < MAX_DISTANCE && steps < MAX_STEPS)
    {
        Pos origin = GetOrigin(ray);
        // Get the distance to the scene from the current position
        float sceneDistance = GetDistance(origin EXTRA_PARAMS);

        // If we are close enough to the surface, we hit something
        if (sceneDistance < EPSILON)
        {
            return GetHit(origin EXTRA_PARAMS);
        }

        // Advance the ray by the distance to the surface
        Advance(ray, sceneDistance);
        distance += sceneDistance;
        steps++;
    }

    return NoHit();
}