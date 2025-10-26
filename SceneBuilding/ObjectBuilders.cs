using SharpDX;

namespace SceneBuilding;

public class Union : ObjectBuilder{
    List<ObjectBuilder> objects = new List<ObjectBuilder>();

    public Union(){
    }

    public Union(params ObjectBuilder[] objs){
        objects.AddRange(objs);
    }

    public void Add(ObjectBuilder obj){
        objects.Add(obj);
    }

    public override string BuildSdfSource(SourceBuilder sb)
    {
        sb.scene.global.MarkAndAppend("UnionSdf","""
            float UnionSdf(float d1, float d2) { return min(d1, d2); }
            """);
        return Util.AppendBinaryFunctionSequence<ObjectBuilder>(sb, "float", "UnionSdf", objects.ToArray(), o => o.BuildSdfSource(sb));

        //return Util.GetBinaryFunctionNest<ObjectBuilder>("UnionSdf", objects.ToArray(), o => o.BuildSdfSource(sb));
    }

    public override string BuildHitSource(SourceBuilder sb)
    {
        sb.scene.global.MarkAndAppend("UnionHit","""
            HitResult UnionHit(HitResult a, HitResult b)
            {
                if(a.dist < b.dist)
                {
                    return a;
                }
                else
                {
                    return b;
                }
            }
            """);

        return Util.AppendBinaryFunctionSequence<ObjectBuilder>(sb, "HitResult", "UnionHit", objects.ToArray(), o => o.BuildHitSource(sb));
    }
}

public class SmoothUnion : ObjectBuilder{
    List<ObjectBuilder> objects = new List<ObjectBuilder>();
    public Expression<float> k;

    public SmoothUnion(Expression<float> k){
        this.k = k;
    }

    public SmoothUnion(Expression<float> k, params ObjectBuilder[] objs) : this(k){
        objects.AddRange(objs);
    }

    public void Add(ObjectBuilder obj){
        objects.Add(obj);
    }

    public override string BuildSdfSource(SourceBuilder sb)
    {
        sb.scene.global.MarkAndAppend("SmoothUnionSdf","""
            float SmoothUnionSdf(float d1, float d2, float k)
            {
                float h = saturate(0.5 + 0.5 * (d2 - d1) / k);
                return lerp(d2, d1, h) - k * h * (1.0 - h);
            }
            """);

        return Util.GetBinaryFunctionNest<ObjectBuilder>("SmoothUnionSdf", objects.ToArray(), o => o.BuildSdfSource(sb), $", {k.BuildSource(sb)}");
    }

    public override string BuildHitSource(SourceBuilder sb)
    {
        sb.scene.global.MarkAndAppend("SmoothUnionHit","""
            HitResult SmoothUnionHit(HitResult a, HitResult b, float k)
            {
                float h = saturate(0.5f + 0.5f * (b.dist - a.dist) / k);
                h = h * h * (3.0f - 2.0f * h);
                HitResult result;
                if(h < 0.5f)
                {
                    result = a;
                }
                else
                {
                    result = b;
                }
                result.dist = SmoothUnionSdf(a.dist, b.dist, k);
                result.normal = normalize(lerp(b.normal, a.normal, h));
                result.albedo = lerp(b.albedo, a.albedo, h);
                return result;
            }
            """);

        return Util.GetBinaryFunctionNest<ObjectBuilder>("SmoothUnionHit", objects.ToArray(), o => o.BuildHitSource(sb), $", {k.BuildSource(sb)}");
    }



    // public override void BuildFunctions(SourceBuilder sb)
    // {
    //     sb.global.AppendLine("""
    //         float SmoothUnionSdf(float d1, float d2, float k)
    //         {
    //             float h = saturate(0.5 + 0.5 * (d2 - d1) / k);
    //             return lerp(d2, d1, h) - k * h * (1.0 - h);
    //         }
    //         HitResult SmoothUnionHit(HitResult a, HitResult b, float k)
    //         {
    //             float h = saturate(0.5f + 0.5f * (b.dist - a.dist) / k);
    //             h = h * h * (3.0f - 2.0f * h);
    //             HitResult result;
    //             if(h < 0.5f)
    //             {
    //                 result = a;
    //             }
    //             else
    //             {
    //                 result = b;
    //             }
    //             result.dist = SmoothUnionSdf(a.dist, b.dist, k);
    //             result.normal = normalize(lerp(b.normal, a.normal, h));
    //             result.albedo = lerp(b.albedo, a.albedo, h);
    //             return result;
    //         }
    //         """);
    // }

    // public override string GetSdfExpression(SourceBuilder sb) => Util.GetBinaryFunctionNest<ObjectBuilder>("SmoothUnionSdf", objects.ToArray(), o => o.BuildSdfSource(sb), $", {k.BuildSource(sb)}");
    // public override string GetHitExpression(SourceBuilder sb) => Util.GetBinaryFunctionNest<ObjectBuilder>("SmoothUnionHit", objects.ToArray(), o => o.BuildHitSource(sb), $", {k.BuildSource(sb)}");
}

public class Intersection : ObjectBuilder{
    List<ObjectBuilder> objects = new List<ObjectBuilder>();

    public Intersection(){
    }

    public Intersection(params ObjectBuilder[] objs){
        objects.AddRange(objs);
    }

    public void Add(ObjectBuilder obj){
        objects.Add(obj);
    }

    public override string BuildSdfSource(SourceBuilder sb)
    {
        sb.scene.global.MarkAndAppend("IntersectionSdf","""
            float IntersectionSdf(float d1, float d2) { return max(d1, d2); }
            """);

        return Util.AppendBinaryFunctionSequence<ObjectBuilder>(sb, "float", "IntersectionSdf", objects.ToArray(), o => o.BuildSdfSource(sb));
    }

    public override string BuildHitSource(SourceBuilder sb)
    {
        sb.scene.global.MarkAndAppend("IntersectionHit","""
            HitResult IntersectionHit(HitResult a, HitResult b)
            {
                if(a.dist > b.dist)
                {
                    return a;
                }
                else
                {
                    return b;
                }
            }
            """);

        return Util.AppendBinaryFunctionSequence<ObjectBuilder>(sb, "HitResult", "IntersectionHit", objects.ToArray(), o => o.BuildHitSource(sb));
    }
}

//     public override void BuildFunctions(SourceBuilder sb)
//     {
//         sb.global.AppendLine("""
//             float IntersectionSdf(float d1, float d2) { return max(d1, d2); }
//             HitResult IntersectionHit(HitResult a, HitResult b)
//             {
//                 if(a.dist > b.dist)
//                 {
//                     return a;
//                 }
//                 else
//                 {
//                     return b;
//                 }
//             }
//             """);
//     }

//     public override string GetSdfExpression(SourceBuilder sb) => Util.GetBinaryFunctionNest<ObjectBuilder>("IntersectionSdf", objects.ToArray(), o => o.BuildSdfSource(sb));
//     public override string GetHitExpression(SourceBuilder sb) => Util.GetBinaryFunctionNest<ObjectBuilder>("IntersectionHit", objects.ToArray(), o => o.BuildHitSource(sb));
// }

public class Invert : ObjectBuilder{
    ObjectBuilder obj;

    public Invert(ObjectBuilder obj){
        this.obj = obj;
    }

    public override string BuildSdfSource(SourceBuilder sb)
    {
        sb.scene.global.MarkAndAppend("InvertSdf","""
            float InvertSdf(float d) { return -d; }
            """);

        return $"InvertSdf({obj.BuildSdfSource(sb)})";
    }

    public override string BuildHitSource(SourceBuilder sb)
    {
        sb.scene.global.MarkAndAppend("InvertHit","""
            HitResult InvertHit(HitResult a)
            {
                a.dist = InvertSdf(a.dist);
                a.normal = -a.normal;
                return a;
            }
            """);

        return $"InvertHit({obj.BuildHitSource(sb)})";
    }
}

//     public override void BuildFunctions(SourceBuilder sb)
//     {
//         sb.global.AppendLine("""
//             float InvertSdf(float d) { return -d; }
//             HitResult InvertHit(HitResult a)
//             {
//                 a.dist = InvertSdf(a.dist);
//                 a.normal = -a.normal;
//                 return a;
//             }
//             """);
//     }

//     public override string GetSdfExpression(SourceBuilder sb) => $"InvertSdf({obj.BuildSdfSource(sb)})";
//     public override string GetHitExpression(SourceBuilder sb) => $"InvertHit({obj.BuildHitSource(sb)})";


public class Sphere : ObjectBuilder{
    public Expression<float> radius;
    public Expression<Vector3> center;
    public Expression<Vector4> albedo;

    public Sphere(Expression<float> radius, Expression<Vector3> center, Expression<Vector4> albedo){
        this.radius = radius;
        this.center = center;
        this.albedo = albedo;
    }

    public override string BuildSdfSource(SourceBuilder sb)
    {
        sb.scene.global.MarkAndAppend("SphereSdf","""
            float SphereSdf(float3 p, float3 center, float r) { return length(p - center) - r; }
            """);

        return $"SphereSdf(p, {center.BuildSource(sb)}, {radius.BuildSource(sb)})";
    }

    public override string BuildHitSource(SourceBuilder sb)
    {
        sb.scene.global.MarkAndAppend("SphereHit","""
            HitResult SphereHit(float3 p, float3 center, float radius, float4 albedo)
            {
                HitResult result;
                result.hit = true;
                result.position = p;
                result.albedo = albedo;
                result.dist = SphereSdf(p, center, radius);
                result.normal = normalize(p - center);
                return result;
            }
            """);

        return $"SphereHit(p, {center.BuildSource(sb)}, {radius.BuildSource(sb)}, {albedo.BuildSource(sb)})";
    }

    // public override void BuildFunctions(SourceBuilder sb)
    // {
    //     sb.global.AppendLine("""
    //         float SphereSdf(float3 p, float3 center, float r) { return length(p - center) - r; }
    //         HitResult SphereHit(float3 p, float3 center, float radius, float4 albedo)
    //         {
    //             HitResult result;
    //             result.hit = true;
    //             result.position = p;
    //             result.albedo = albedo;
    //             result.dist = SphereSdf(p, center, radius);
    //             result.normal = normalize(p - center);
    //             return result;
    //         }
    //         """);
    // }

    // public override string GetSdfExpression(SourceBuilder sb) => $"SphereSdf(p, {center.BuildSource(sb)}, {radius.BuildSource(sb)})";
    // public override string GetHitExpression(SourceBuilder sb) => $"SphereHit(p, {center.BuildSource(sb)}, {radius.BuildSource(sb)}, {albedo.BuildSource(sb)})";
}

public class Plane : ObjectBuilder{
    public Expression<Vector3> normal;
    public Expression<float> d;
    public Expression<Vector4> albedo;

    public Plane(Expression<Vector3> normal, Expression<float> d, Expression<Vector4> albedo){
        this.normal = normal;
        this.d = d;
        this.albedo = albedo;
    }

    public override string BuildSdfSource(SourceBuilder sb)
    {
        sb.scene.global.MarkAndAppend("PlaneSdf","""
            float PlaneSdf(float3 p, float3 n, float d) { return dot(p, n) + d; }
            """);

        return $"PlaneSdf(p, {normal.BuildSource(sb)}, {d.BuildSource(sb)})";
    }

    public override string BuildHitSource(SourceBuilder sb)
    {
        sb.scene.global.MarkAndAppend("PlaneHit","""
            HitResult PlaneHit(float3 p, float3 n, float d, float4 albedo)
            {
                HitResult result;
                result.hit = true;
                result.position = p;
                result.albedo = albedo;
                result.dist = PlaneSdf(p, n, d);
                result.normal = n;
                return result;
            }
            """);

        return $"PlaneHit(p, {normal.BuildSource(sb)}, {d.BuildSource(sb)}, {albedo.BuildSource(sb)})";
    }

    // public override void BuildFunctions(SourceBuilder sb)
    // {
    //     sb.global.AppendLine("""
    //         float PlaneSdf(float3 p, float3 n, float d) { return dot(p, n) + d; }
    //         HitResult PlaneHit(float3 p, float3 n, float d, float4 albedo)
    //         {
    //             HitResult result;
    //             result.hit = true;
    //             result.position = p;
    //             result.albedo = albedo;
    //             result.dist = PlaneSdf(p, n, d);
    //             result.normal = n;
    //             return result;
    //         }
    //         """);
    // }

    // public override string GetSdfExpression(SourceBuilder sb) => $"PlaneSdf(p, {normal.BuildSource(sb)}, {d.BuildSource(sb)})";
    // public override string GetHitExpression(SourceBuilder sb) => $"PlaneHit(p, {normal.BuildSource(sb)}, {d.BuildSource(sb)}, {albedo.BuildSource(sb)})";
}



public class MixMatch : ObjectBuilder{
    public ObjectBuilder sdfObj;
    public ObjectBuilder hitObj;

    public MixMatch(ObjectBuilder sdfObj, ObjectBuilder hitObj){
        this.sdfObj = sdfObj;
        this.hitObj = hitObj;
    }

    public override string BuildSdfSource(SourceBuilder sb)
    {
        return sdfObj.BuildSdfSource(sb);
    }

    public override string BuildHitSource(SourceBuilder sb)
    {
        return hitObj.BuildHitSource(sb);
    }
}