// See RayMarchingCore.hlsl for important definitions and functions
// This file contains the main compute shader for ray marching
RWTexture2D<float4> outputTexture : register(u0);

[numthreads(16, 16, 1)]
void CSMain(uint3 DTid : SV_DispatchThreadID)
{
    // #ifdef PIXEL_CONTEXT
    // PIXEL_CONTEXT context = (PIXEL_CONTEXT)0;
    // #endif
    // #ifdef PIXEL_CONTEXT_INIT
    // PIXEL_CONTEXT_INIT(context, DTid);
    // #endif

    EXTRA_PARAMS_INIT

    uint2 dimensions;
    outputTexture.GetDimensions(dimensions.x, dimensions.y);
    float aspectRatio = dimensions.x / (float)dimensions.y;

    //find the ray direction
    float2 uv = DTid.xy / float2(dimensions.x, dimensions.y);
    uv -= 0.5f; // Center the UV coordinates
    uv *= 2.0f; // Scale to [-1, 1] range
    uv.x *= aspectRatio; // Adjust for aspect ratio
    uv.y *= -1.0f; // Flip Y axis to have the origin at the bottom-left

    Ray ray = GetRay(uv);
    Transform sceneTransform = GetTransform();
    ray = TransformRay(ray, sceneTransform);
    float4 color = GetColor(ray EXTRA_PARAMS);

    outputTexture[DTid.xy] = color;
    //outputTexture[DTid.xy] = float4(uv, 0, 1); // Debug: visualize UV coordinates
}