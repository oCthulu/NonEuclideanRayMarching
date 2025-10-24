static const int MAX_MARCHING_STEPS = 100;
static const float MIN_DIST = 0.01;

//constant buffer
cbuffer Constants : register(b0)
{
    float4x4 camMatrix;
    float3 lightingDir;
    float directLightingWeight;
    float skyLightingWeight;
    float ambientLightingWeight;
};

RWTexture2D<float4> outputTexture : register(u0);

float InvLerp(float a, float b, float t)
{
    return saturate((t - a) / (b - a));
}

float SampleDistance(float3 pos)
{
    return length(pos) - 1;
}

bool CastRay(float3 pos, float3 dir, out float3 hitPos, out float3 hitNormal)
{
    hitPos = pos;
    hitNormal = float3(0, 0, 0);

    for (int i = 0; i < MAX_MARCHING_STEPS; i++)
    {
        float dist = SampleDistance(hitPos);
        if (dist < MIN_DIST)
        {
            // Hit
            //normal is the gradient of the distance field
            hitNormal = normalize(float3(
                SampleDistance(hitPos + float3(MIN_DIST, 0, 0)) - dist,
                SampleDistance(hitPos + float3(0, MIN_DIST, 0)) - dist,
                SampleDistance(hitPos + float3(0, 0, MIN_DIST)) - dist
            ));
            return true;
        }

        hitPos += dist * dir;
    }

    // No hit
    hitPos = float3(0, 0, 0);
    hitNormal = float3(0, 0, 0);
    return false;
}

float4 Skybox(float3 dir) {
    float t = dot(dir, float3(0, 1, 0));
    return lerp(
        //base skybox gradient
        lerp(
            //ground gradient
            lerp(
                float4(0.5, 0.5, 0.5, 1),   //down color
                float4(0.2, 0.2, 0.2, 1),   //horizoin color
                -t
            ),
            //sky gradient
            lerp(
                float4(0.8, 0.9, 1, 1),     //horizon color
                float4(0.3, 0.5, 1, 1),     //up color
                t
            ),
            InvLerp(-0.025, 0.025, t)
        ),
        //sun color
        float4(1,1,1,1),
        //sun direction
        InvLerp(0.999, 0.9995, dot(dir, lightingDir))
    );
}

[numthreads(16, 16, 1)]
void CSMain(uint3 DTid : SV_DispatchThreadID)
{
    uint2 dimensions;
    outputTexture.GetDimensions(dimensions.x, dimensions.y);

    //find the ray direction
    float2 uv = DTid.xy / float2(dimensions.x, dimensions.y);
    float aspect = (float)dimensions.x / dimensions.y;

    float3 rayDir = normalize(float3((uv.x * 2 - 1) * aspect, -(uv.y * 2 - 1), 1));
    float3 rayOrigin = float3(0, 0, 0);

    //transform the ray direction by the camera matrix
    rayDir = mul(float4(rayDir, 0), camMatrix);
    rayOrigin = mul(float4(rayOrigin, 1), camMatrix);

    float3 hitPos, hitNormal;
    float4 color;

    if(CastRay(rayOrigin, rayDir, hitPos, hitNormal)){
        float4 albedo = float4(1, 1, 1, 1); //todo: albedo

        //lighting
        float directLighting = saturate(dot(hitNormal, lightingDir));
        float skyLighting = (dot(hitNormal, float3(0,1,0)) + 1) * 0.5;

        float lighting = 
            directLighting * directLightingWeight +
            skyLighting * skyLightingWeight +
            ambientLightingWeight;

        color = albedo * lighting;
    }
    else{
        color = Skybox(rayDir);
    }
    color.a = 1;

    outputTexture[DTid.xy] = color;
}