#ifndef HYPERBOLIC_UTILS_HLSL
#include "HyperbolicUtils.hlsl"
#endif


float4 Skybox(float3 dir)
{
    float t = 0.5f * (dir.y + 1.0f);
    return lerp(float4(0.1f, 0.2f, 0.5f, 1.0f), float4(0.7f, 0.8f, 1.0f, 1.0f), t);
}

float4 GetColor(Ray ray EXTRA_PARAMS_DECL)
{
    //will add to cbuffer later
    float4 lightDirection = normalize(float4(0, 1, -0.5, 0));

    HitResult hit = CastRay(ray EXTRA_PARAMS);
    if(hit.hit)
    {
        float4 lightDirLocal = ParallelTransport(lightDirection, float4(0,0,0,1), hit.position);
        float4 upDirLocal = ParallelTransport(float4(0,1,0,0), float4(0,0,0,1), hit.position);

        float directLight = saturate(ldot(hit.normal, lightDirLocal));
        float skyLight = saturate(0.5f + 0.5f * ldot(hit.normal, upDirLocal));
        float ambientLight = 1;

        float lightIntensity = dot(float3(directLight, skyLight, ambientLight), float3(0.7, 0.2, 0.1));

        return hit.albedo * lightIntensity;

        // float4 normalDirLocal = ParallelTransport(hit.normal, hit.position, float4(0,0,0,1));
        // normalDirLocal = normalize(normalDirLocal);

        // float directLight = saturate(dot(normalDirLocal, lightDirection));
        // float skyLight = saturate(0.5f + 0.5f * normalDirLocal.y);
        // float ambientLight = 1;

        // float lightIntensity = dot(float3(directLight, skyLight, ambientLight), float3(0.7, 0.2, 0.1));

        // return hit.albedo * lightIntensity;
    }
    return Skybox(GeodesicEndpoint(ray.origin, ray.direction));
}