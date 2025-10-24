static const int NUM_SAMPLES = 16;

RWTexture2D<float4> outputTexture : register(u0);
RWTexture2D<float4> normalTexture : register(u1);
RWTexture2D<float4> positionTexture : register(u2);

float rand(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

float2 randOffset(float2 uv, int i)
{
    float ang = rand(uv + float2(i, 0)) * 6.28318530718; // 2 * PI
    float2 len = rand(uv + float2(0, i)) * 0.5;

    return float2(cos(ang), sin(ang)) * len;
}

[numthreads(16, 16, 1)]
void CSMain(uint3 DTid : SV_DispatchThreadID)
{
    return;
    float ao = 0;
    for(int i = 0; i < NUM_SAMPLES; i++)
    {
        //sample normal and position
        float3 position = positionTexture[DTid.xy].xyz;
        float3 normal = normalTexture[DTid.xy].xyz;

        //sample position at a random offset
        float2 offset = randOffset(DTid.xy, i) * 10;
        float3 samplePos = positionTexture[DTid.xy + offset].xyz;


        //calculate ao
        float3 dir = samplePos - position;
        float s = dot(normal, dir);
        ao += s * normalTexture[DTid.xy + offset].w;
    }
    ao /= NUM_SAMPLES;

    //normalize ao
    ao = pow(ao, 0.5);
    ao = saturate((ao - 0.05) / 0.95) * 0.5;

    //write the result to the output texture
    outputTexture[DTid.xy] = lerp(
        outputTexture[DTid.xy],
        float4(0, 0, 0, 1),
        ao
    );
}