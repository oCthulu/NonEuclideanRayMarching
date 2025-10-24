// ...existing code...

RWTexture2D<float4> outputTexture : register(u0);

[numthreads(16, 16, 1)]
void CS(uint3 DTid : SV_DispatchThreadID)
{
    float2 uv = DTid.xy / outputTexture.GetDimensions().y;
    outputTexture[DTid.xy] = float4(uv, 0.0f, 1.0f);
}
