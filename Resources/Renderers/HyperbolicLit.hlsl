#ifndef HYPERBOLIC_UTILS_HLSL
#include "HyperbolicUtils.hlsl"
#endif


float4 GetColor(Ray ray EXTRA_PARAMS_DECL)
{
    //will add to cbuffer later
    float4 lightDirection = normalize(float4(0, 1, -0.5, 0));

    HitResult hit = CastRay(ray EXTRA_PARAMS);
    if(hit.hit)
    {
        // float4 lightDirLocal = ParallelTransport(lightDirection, float4(0,0,0,1), hit.position);
        // float lightIntensity = saturate(ldot(hit.normal, lightDirLocal));

        float4 normalDirLocal = ParallelTransport(hit.normal, hit.position, float4(0,0,0,1));

        float directLight = saturate(dot(normalDirLocal, lightDirection));
        float skyLight = saturate(0.5f + 0.5f * normalDirLocal.y);
        float ambientLight = 1;

        float lightIntensity = dot(float3(directLight, skyLight, ambientLight), float3(0.7, 0.2, 0.1));

        return hit.albedo * lightIntensity;
    }
    return float4(0, 0, 0, 1);
}