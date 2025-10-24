float4 GetColor(Ray ray EXTRA_PARAMS_DECL)
{
    HitResult hit = CastRay(ray EXTRA_PARAMS);
    if(hit.hit)
    {
        return hit.albedo;
    }
    return float4(0, 0, 0, 1);
}