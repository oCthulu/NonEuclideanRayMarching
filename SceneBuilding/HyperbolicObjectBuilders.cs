using SharpDX;
using SharpDX.D3DCompiler;

namespace SceneBuilding;

public static class HyperBuilderUtils {
    public static void IncludeHyperbolicUtils(SourceBuilder sb){
        sb.scene.global.MarkAndAppend("HyperbolicUtils.hlsl", """
            
            #ifndef HYPERBOLIC_UTILS_HLSL
            #include "HyperbolicUtils.hlsl"
            #endif

            """);
    }
}

class SphereH : ObjectBuilder {
    Expression<float> radius;
    Expression<Vector4> center;
    Expression<Vector4> albedo;

    public SphereH(Expression<float> radius, Expression<Vector4> center, Expression<Vector4> albedo){
        this.center = center;
        this.radius = radius;
        this.albedo = albedo;
    }

    public override string BuildSdfSource(SourceBuilder sb)
    {
        HyperBuilderUtils.IncludeHyperbolicUtils(sb);

        sb.scene.global.MarkAndAppend("SphereHSdf", """
            float SphereHSdf(Pos p, float4 center, float radius)
            {
                float coshDist = (-ldot(p, center));
                return acosh(coshDist) - radius;
            }
            """);
        return $"SphereHSdf(p, {center.BuildSource(sb)}, {radius.BuildSource(sb)})";
    }

    public override string BuildHitSource(SourceBuilder sb)
    {
        sb.scene.global.MarkAndAppend("SphereHHit", """
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
        return $"SphereHHit(p, {center.BuildSource(sb)}, {radius.BuildSource(sb)}, {albedo.BuildSource(sb)})";
    }

    // public override void BuildFunctions(SourceBuilder sb)
    // {
    //     sb.global.AppendLine("""
    //         float SphereHSdf(Pos p, float4 center, float radius)
    //         {
    //             float coshDist = p.w * center.w - dot(p.xyz, center.xyz);
    //             return log(coshDist + sqrt(coshDist * coshDist - 1.0)) - radius; //acosh(coshDist) - radius
    //         }

    //         HitResult SphereHHit(Pos p, float4 center, float radius, float4 albedo)
    //         {
    //             HitResult result;
    //             result.hit = true;
    //             result.position = p;
    //             result.dist = SphereHSdf(p, center, radius);
    //             result.normal = normalize(p - center * cosh(result.dist));
    //             result.albedo = albedo;
    //             return result;
    //         }
    //         """);
    // }

    // public override string GetSdfExpression(SourceBuilder sb) => $"SphereHSdf(p, {center.BuildSource(sb)}, {radius.BuildSource(sb)})";
    // public override string GetHitExpression(SourceBuilder sb) => $"SphereHHit(p, {center.BuildSource(sb)}, {radius.BuildSource(sb)}, {albedo.BuildSource(sb)})";
}

public class PlaneH : ObjectBuilder {
    Expression<Vector4> planeNormal;
    Expression<Vector4> albedo;
    Expression<float> dist;

    public PlaneH(Expression<Vector4> planeNormal, Expression<float> dist, Expression<Vector4> albedo){
        this.dist = dist;
        this.planeNormal = planeNormal;
        this.albedo = albedo;
    }

    public override string BuildSdfSource(SourceBuilder sb)
    {
        HyperBuilderUtils.IncludeHyperbolicUtils(sb);

        sb.scene.global.MarkAndAppend("PlaneHSdf", """
            float PlaneHSdf(Pos p, float dist, float4 planeNormal)
            {
                return asinh(ldot(planeNormal, p)) - dist;
            }
            """);
        return $"PlaneHSdf(p, {dist.BuildSource(sb)}, {planeNormal.BuildSource(sb)})";
    }

    public override string BuildHitSource(SourceBuilder sb)
    {
        sb.scene.global.MarkAndAppend("PlaneHHit", """
            HitResult PlaneHHit(Pos p, float4 planeNormal, float dist, float4 albedo)
            {
                HitResult result;
                result.hit = true;
                result.position = p;
                result.dist = PlaneHSdf(p, dist, planeNormal);
                result.normal = planeNormal;
                result.albedo = albedo;
                return result;
            }
            """);
        return $"PlaneHHit(p, {planeNormal.BuildSource(sb)}, {dist.BuildSource(sb)}, {albedo.BuildSource(sb)})";
    }
}

public class TransformH : TransformingObjectBuilder {
    Expression<Matrix> transform;
    Expression<Matrix> inverseTransform;
    public TransformH(Expression<Matrix> transform, ObjectBuilder child) : base(child){
        this.transform = transform;
        inverseTransform = transform.Apply(m => Matrix.Invert(m));
    }

    protected override void TransformInput(string inputVar, SourceBuilder sb)
    {
        sb.AppendLine($"{inputVar} = mul({transform.BuildSource(sb)}, {inputVar});");
    }

    protected override void TransformOutputSdf(string outputVar, SourceBuilder sb)
    {
        //do nothing
    }

    protected override void TransformOutputHit(string outputVar, SourceBuilder sb)
    {
        sb.AppendLine($"{outputVar}.position = mul({inverseTransform.BuildSource(sb)}, {outputVar}.position);");
        sb.AppendLine($"{outputVar}.normal = normalize(mul({inverseTransform.BuildSource(sb)}, float4({outputVar}.normal)));");
    }
}



public class ConstantH : ObjectBuilder {
    Expression<float> sdf;
    Expression<Vector4> albedo;
    Expression<Vector4> normal;

    public ConstantH(Expression<float> sdf,  Expression<Vector4> albedo, Expression<Vector4> normal){
        this.sdf = sdf;
        this.albedo = albedo;
        this.normal = normal;
    }

    public override string BuildSdfSource(SourceBuilder sb)
    {
        return sdf.BuildSource(sb);
    }

    public override string BuildHitSource(SourceBuilder sb)
    {
        sb.scene.global.MarkAndAppend("ConstantHHit", """
            HitResult ConstantHHit(Pos p, float dist, float4 albedo, float4 normal)
            {
                HitResult result;
                result.hit = true;
                result.position = p;
                result.dist = dist;
                result.normal = normal;
                result.albedo = albedo;
                return result;
            }
            """);
        return $"ConstantHHit(p, {sdf.BuildSource(sb)}, {albedo.BuildSource(sb)}, {normal.BuildSource(sb)})";
    }
}