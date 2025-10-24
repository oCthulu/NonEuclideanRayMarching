using SharpDX;

namespace GpuHlslRayMarchingTest;
public static class HyperUtil{
    public static Vector4 ToHyperbolic(Vector3 v){
        float w = (float)Math.Sqrt(1 + v.LengthSquared());
        return new Vector4(v, w);
    }
}