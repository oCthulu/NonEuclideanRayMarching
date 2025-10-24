Texture2D<float4> inputTexture : register(t0);
SamplerState samplerState : register(s0);

struct PSInput
{
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD;
};

float4 PSMain(PSInput input) : SV_TARGET
{
    return float4(1,0,0,1); //inputTexture.Sample(samplerState, input.uv);
}
