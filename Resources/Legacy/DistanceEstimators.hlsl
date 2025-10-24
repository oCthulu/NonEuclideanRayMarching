float Sphere(float3 s, float3 pos, float radius)
{
    return length(pos - s) - radius;
}

float Cube(float3 s, float3 pos, float3 size)
{
    float3 d = abs(pos - s) - size;
    return length(max(d, 0.0));
}

float CubeI(float3 s, float3 pos, float3 size)
{
    float3 d = abs(pos - s) - size;
    return max(d.x, max(d.y, d.z));
}

float Capsule(float3 s, float3 start, float3 end, float radius)
{
    float3 d = end - start;
    float t = dot(s - start, d) / dot(d, d);
    t = clamp(t, 0.0, 1.0);
    float3 closest = start + t * d;
    return length(s - closest) - radius;
}

float SlicedSphereI(float3 s, float3 pos, float radius, float height)
{
    return max(Sphere(s, pos, radius), (abs(pos.y - s.y) - height));
}

float PlaneSigned(float3 s, float3 pos, float3 normal)
{
    return dot(pos - s, normal);
}

void Add(inout float dist, float d)
{
    dist = min(dist, d);
}

void Intersect(inout float dist, float d)
{
    dist = max(dist, d);
}