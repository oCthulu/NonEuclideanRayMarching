RWTexture2D<float4> outputTexture : register(u0);

[numthreads(16, 16, 1)]
void CSMain(uint3 DTid : SV_DispatchThreadID)
{
    uint2 dimensions;
    outputTexture.GetDimensions(dimensions.x, dimensions.y);
    float2 uv = DTid.xy / float2(dimensions.x, dimensions.y);
    float4 color = float4(uv, 0.5, 1.0); // Example: gradient based on position
    outputTexture[DTid.xy] = color;
}
