using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace SceneBuilding;


public interface ITypeDescriptor<T>{
    public static abstract string GetTypeName();
    public static abstract string BuildSource(T value, SourceBuilder sb);
}
public abstract class TypeDescriptor {
    public abstract string GetTypeName();
    public abstract string BuildSource(object value, SourceBuilder sb);
}
public abstract class TypeDescriptor<T> : TypeDescriptor {
    public abstract string BuildSource(T value, SourceBuilder sb);
    public override string BuildSource(object value, SourceBuilder sb) => BuildSource((T)value, sb);
}
public class TypeDescriptor<T, TDesc> : TypeDescriptor<T> where TDesc : ITypeDescriptor<T>, new() {
    public override string GetTypeName() => TDesc.GetTypeName();
    public override string BuildSource(T value, SourceBuilder sb) => TDesc.BuildSource(value, sb);
}


public class FloatDescriptor : ITypeDescriptor<float>{
    public static string GetTypeName() => "float";
    public static string BuildSource(float value, SourceBuilder sb) => value.ToString("G");
}

public class MatrixDescriptor : ITypeDescriptor<Matrix>{
    public static string GetTypeName() => "float4x4";
    public static string BuildSource(Matrix value, SourceBuilder sb){
        return $"float4x4({value.M11:G}, {value.M12:G}, {value.M13:G}, {value.M14:G}, {value.M21:G}, {value.M22:G}, {value.M23:G}, {value.M24:G}, {value.M31:G}, {value.M32:G}, {value.M33:G}, {value.M34:G}, {value.M41:G}, {value.M42:G}, {value.M43:G}, {value.M44:G})";
    }
}

public class Vector3Descriptor : ITypeDescriptor<Vector3>{
    public static string GetTypeName() => "float3";
    public static string BuildSource(Vector3 value, SourceBuilder sb){
        return $"float3({value.X:G}, {value.Y:G}, {value.Z:G})";
    }
}

public class Vector4Descriptor : ITypeDescriptor<Vector4>{
    public static string GetTypeName() => "float4";
    public static string BuildSource(Vector4 value, SourceBuilder sb){
        return $"float4({value.X:G}, {value.Y:G}, {value.Z:G}, {value.W:G})";
    }
}