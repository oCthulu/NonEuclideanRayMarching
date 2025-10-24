cbuffer SceneParameters : register(b1)
{
    float3 lightDirection;
};

float InvLerp(float a, float b, float t)
{
    return saturate((t - a) / (b - a));
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
        InvLerp(0.999, 0.9995, dot(dir, lightDirection))
    );
}

float4 GetColor(Ray ray EXTRA_PARAMS_DECL)
{
    HitResult hit = CastRay(ray EXTRA_PARAMS);
    if(hit.hit)
    {
        float3 normal = normalize(hit.normal);
        float directLight = max(dot(normal, lightDirection), 0.0);
        float skyLight = max(dot(normal, float3(0, 1, 0)), 0.0) * 0.5 + 0.5;
        float ambientLight = 0.1;
        return hit.albedo * dot(float3(directLight, skyLight, ambientLight), float3(0.7, 0.2, 0.1));
    }
    return Skybox(ray.direction);
}