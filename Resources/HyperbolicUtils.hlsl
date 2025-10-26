#define HYPERBOLIC_UTILS_HLSL

float ldot(float4 a, float4 b)
{
    return dot(a.xyz, b.xyz) - a.w * b.w;
}

float4 lnormalize(float4 v)
{
    float len2 = ldot(v, v);
    return v / sqrt(abs(len2));
}

float acosh(float x)
{
    return log(x + sqrt(x * x - 1));
}

float asinh(float x)
{
    return log(x + sqrt(x * x + 1));
}

float SinhToCosh(float sinhVal)
{
    return sqrt(1 + sinhVal * sinhVal);
}

float4 ParallelTransport(float4 v, float4 fromPos, float4 toPos)
{
    float cosTheta = -ldot(fromPos, toPos);
    float4 axis = normalize(toPos + cosTheta * fromPos);
    float sinhTheta = sqrt(cosTheta * cosTheta - 1);
    return v + (sinhTheta * ldot(v, axis) - (1 + cosTheta) * ldot(v, fromPos)) * (fromPos + toPos) / (1 + cosTheta);
}

float4 DirectionTo(float4 fromPos, float4 toPos)
{
    //project fromPos to the plane perpendicular to toPos
    float4 projected = toPos + ldot(fromPos, toPos) * fromPos;
    return projected / sqrt(ldot(projected, projected));
}

float3 GeodesicEndpoint(float4 startPos, float4 direction)
{
    float4 xi = startPos + direction;
    return xi.xyz / xi.w;
}