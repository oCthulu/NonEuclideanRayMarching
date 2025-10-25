#define HYPERBOLIC_UTILS_HLSL

float ldot(float4 a, float4 b)
{
    return dot(a.xyz, b.xyz) - a.w * b.w;
}

float acosh(float x)
{
    return log(x + sqrt(x * x - 1));
}

float asinh(float x)
{
    return log(x + sqrt(x * x + 1));
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
    float cosTheta = -ldot(fromPos, toPos);
    float4 dir = toPos + cosTheta * fromPos;
    float sinhDist = sqrt(cosTheta * cosTheta - 1);
    return dir / sinhDist;
}