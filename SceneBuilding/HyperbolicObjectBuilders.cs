
using SharpDX;

namespace SceneBuilding;

class SphereH : FunctionObjectBuilder<SphereH> {
    Expression<float> radius;
    Expression<Vector4> center;
    Expression<Vector4> albedo;

    public SphereH(Expression<float> radius, Expression<Vector4> center, Expression<Vector4> albedo){
        this.center = center;
        this.radius = radius;
        this.albedo = albedo;
    }

    public override void BuildFunctions(SourceBuilder sb)
    {
        sb.global.AppendLine("""
            float SphereHSdf(Pos p, float4 center, float radius)
            {
                float coshDist = p.w * center.w - dot(p.xyz, center.xyz);
                return log(coshDist + sqrt(coshDist * coshDist - 1.0)) - radius; //acosh(coshDist) - radius
            }

            HitResult SphereHHit(Pos p, float4 center, float radius, float4 albedo)
            {
                HitResult result;
                result.hit = true;
                result.position = p;
                result.dist = SphereHSdf(p, center, radius);
                result.normal = normalize(p - center * cosh(result.dist));
                result.albedo = albedo;
                return result;
            }
            """);
    }

    public override string GetSdfExpression(SourceBuilder sb) => $"SphereHSdf(p, {center.BuildSource(sb)}, {radius.BuildSource(sb)})";
    public override string GetHitExpression(SourceBuilder sb) => $"SphereHHit(p, {center.BuildSource(sb)}, {radius.BuildSource(sb)}, {albedo.BuildSource(sb)})";
}