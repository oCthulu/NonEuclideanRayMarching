using System.Text;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace SceneBuilding;



public abstract class Expression<T> {
    public abstract string BuildSource(SourceBuilder sb);

    public static implicit operator Expression<T>(T value) => new LiteralExpression<T>(value);
}

public class LiteralExpression<T> : Expression<T> {
    public T value;

    public LiteralExpression(T value){
        this.value = value;
    }

    public override string BuildSource(SourceBuilder sb){
        return sb.scene.GetTypeDescription<T>().BuildSource(value, sb);
    }
}

public class CodeExpression<T> : Expression<T> {
    public string code;

    public CodeExpression(string code){
        this.code = code;
    }

    public override string BuildSource(SourceBuilder sb){
        return code;
    }

    public static implicit operator string(CodeExpression<T> expr) => expr.code;
}

public class SourceBuilder{
    public class SubBuilder{
        public readonly StringBuilder source = new();
        HashSet<object> marks = new();

        public void Append(StringBuilder sb){
            source.Append(sb);
        }
        public void Append(string str){
            source.Append(str);
        }
        public void AppendLine(){
            source.AppendLine();
        }
        public void AppendLine(StringBuilder sb){
            Append(sb);
            AppendLine();
        }
        public void AppendLine(string str){
            Append(str);
            AppendLine();
        }

        public void Mark(object obj){
            marks.Add(obj);
        }
        public bool HasMark(object obj){
            return marks.Contains(obj);
        }
        public bool HasMarkAndMark(object obj){
            bool has = HasMark(obj);
            if(!has) Mark(obj);
            return has;
        }

        public void Mark<T>(){
            Mark(typeof(T));
        }
        public bool HasMark<T>(){
            return HasMark(typeof(T));
        }
        public bool HasMarkAndMark<T>(){
            return HasMarkAndMark(typeof(T));
        }
    }

    public readonly SceneBuilder scene;

    public SubBuilder global = new();
    public SubBuilder cbuffer = new();
    public SubBuilder? function;
    public SubBuilder? local;

    public SourceBuilder(SceneBuilder scene){
        this.scene = scene;
    }
}

public abstract class ObjectBuilder{
    protected int occuranceCount = 0;
    bool built;

    public abstract string BuildSdfSource(SourceBuilder sb);
    public abstract string BuildHitSource(SourceBuilder sb);

    public virtual void Prepare(SourceBuilder sb){
        if(built) throw new InvalidOperationException("Object already built.");
        occuranceCount++;
    }

    public virtual void FinalizeBuild(){
        built = true;
    }
}

public abstract class FunctionObjectBuilder<T> : ObjectBuilder{
    protected string? sdfVariableName;
    protected string? hitVariableName;

    public static void Require<TReq>(SourceBuilder sb) where TReq : FunctionObjectBuilder<T>, new(){
        if(!sb.global.HasMarkAndMark<TReq>()){
            var obj = new TReq();
            obj.BuildFunctions(sb);
        }
    }

    public override string BuildSdfSource(SourceBuilder sb)
    {
        if(!sb.global.HasMarkAndMark<T>()){
            BuildFunctions(sb);
        }
        return GetSdfExpression(sb);
    }
    public override string BuildHitSource(SourceBuilder sb)
    {
        if(!sb.global.HasMarkAndMark<T>()){
            BuildFunctions(sb);
        }
        return GetHitExpression(sb);
    }

    public abstract void BuildFunctions(SourceBuilder sb);
    public abstract string GetSdfExpression(SourceBuilder sb);
    public abstract string GetHitExpression(SourceBuilder sb);
}

public class SceneBuilder{
    readonly Dictionary<Type, TypeDescriptor> typeDescriptions = [];
    readonly SourceBuilder sb;

    readonly string spaceFile;
    readonly string rendererFile;

    private string? camTransformName;

    public ObjectBuilder? root;

    Action<ConstantBufferWriter>? onCbufferWrite;

    public SceneBuilder(string spaceFile = "Spaces/Euclidean.hlsl", string rendererFile = "Renderers/EuclideanLit.hlsl"){
        sb = new SourceBuilder(this);
        this.spaceFile = spaceFile;
        this.rendererFile = rendererFile;

        //Register built-in type descriptions
        RegisterTypeDescription<float, FloatDescriptor>();
        RegisterTypeDescription<Matrix, MatrixDescriptor>();
        RegisterTypeDescription<Vector3, Vector3Descriptor>();
        RegisterTypeDescription<Vector4, Vector4Descriptor>();
    }

    public void RegisterTypeDescription<T, TDesc>() where TDesc : ITypeDescriptor<T>, new(){
        typeDescriptions[typeof(T)] = new TypeDescriptor<T, TDesc>();
    }

    public TypeDescriptor<T> GetTypeDescription<T>(){
        if(typeDescriptions.TryGetValue(typeof(T), out var desc)){
            return (TypeDescriptor<T>)desc;
        }
        throw new ArgumentException($"No type description registered for type {typeof(T)}");
    }

    public Expression<T> DefineParameter<T>(string name){
        if(!sb.cbuffer.HasMark(name)){
            sb.cbuffer.Mark(name);
            var typeDesc = GetTypeDescription<T>();
            sb.cbuffer.AppendLine($"    {typeDesc.GetTypeName()} {name};");
        }
        return new CodeExpression<T>(name);
    }

    public Expression<T> Expr<T>(Func<T> func) where T : unmanaged{
        string name = $"expr{Guid.NewGuid():N}";
        var expr = DefineParameter<T>(name);
        onCbufferWrite += (cbw) => cbw.Write(name, func());
        return expr;
    }

    public Expression<T> DefineCameraTransform<T>(string name){
        if(camTransformName != null) throw new InvalidOperationException("Camera transform already defined.");
        camTransformName = name;
        return DefineParameter<T>(name);
    }

    public string BuildSource(){
        if(root == null) throw new InvalidOperationException("No root object defined.");

        root.Prepare(sb);
        
        SourceBuilder.SubBuilder sdfBuilder = new();
        SourceBuilder.SubBuilder hitBuilder = new();

        //TODO: perhaps have a stack to handle nested local builders?
        sb.function = sdfBuilder;
        sb.local = sb.function;
        sb.function.Append($"return {root.BuildSdfSource(sb)};");

        sb.function = hitBuilder;
        sb.local = sb.function;
        sb.function.Append($"return {root.BuildHitSource(sb)};");

        StringBuilder final = new();
        final.AppendLine($"#include \"{spaceFile}\"");
        if(sb.cbuffer.source.Length > 0){
            final.AppendLine("cbuffer SceneParams : register(b0){");
            final.Append(sb.cbuffer.source);
            final.AppendLine("}");
        }
        final.AppendLine();

        final.Append(sb.global.source);
        final.AppendLine();

        final.AppendLine($"float GetDistance(Pos p){{");
        final.Append(sdfBuilder.source);
        final.AppendLine("}");
        final.AppendLine();

        final.AppendLine($"HitResult GetHit(Pos p){{");
        final.Append(hitBuilder.source);
        final.AppendLine("}");
        final.AppendLine();

        if(camTransformName == null) throw new InvalidOperationException("Camera transform not defined.");

        final.AppendLine($$"""
Transform GetTransform()
{
    return {{camTransformName}};
}
""");

        final.AppendLine($"#include \"RayMarchingCore.hlsl\"");
        final.AppendLine($"#include \"{rendererFile}\"");
        final.AppendLine($"#include \"RayMarchingMain.hlsl\"");

        root.FinalizeBuild();

        return final.ToString();
    }

    public Scene Build(Device device, Include? includeHandler = null, string entryPoint = "CSMain", string profile = "cs_5_0"){
        var source = BuildSource();
        Console.WriteLine(source);
        var shaderByteCode = ShaderBytecode.Compile(source, entryPoint, profile, ShaderFlags.None, EffectFlags.None, null, includeHandler);
        Scene scene = new Scene(device, shaderByteCode);

        scene.writeSceneConstants += onCbufferWrite;

        return scene;
    }
}


// public abstract class Expression{
//     public abstract void AppendSource(StringBuilder sb);

//     public static implicit operator Expression(string value) => new LiteralExpression(value);
//     public static implicit operator Expression(int value) => new LiteralExpression(value.ToString());
//     public static implicit operator Expression(float value) => new LiteralExpression(value.ToString("G"));
// }

// public class LiteralExpression : Expression{
//     string value;
//     public LiteralExpression(string value){
//         this.value = value;
//     }

//     public override void AppendSource(StringBuilder sb){
//         sb.Append(value);
//     }

//     public static implicit operator LiteralExpression(string value) => new(value);
//     public static implicit operator LiteralExpression(int value) => new(value.ToString());
//     public static implicit operator LiteralExpression(float value) => new(value.ToString("G"));

//     public static implicit operator string(LiteralExpression expr) => expr.value;
// }

// public class SceneBuilder{
//     struct CbufferEntry{
//         public string type;
//         public string name;
//     }

//     public string spaceFile;
//     public string rendererFile;
//     public ObjectBuilder root;

//     List<CbufferEntry> cbuffer = [];

//     public SceneBuilder(ObjectBuilder root, string spaceFile = "Spaces/Euclidean.hlsl", string rendererFile = "Renderers/EuclideanLit.hlsl"){
//         this.root = root;
//         this.spaceFile = spaceFile;
//         this.rendererFile = rendererFile;
//     }

//     public void DefineParameter(string type, string name, out Expression expr){
//         cbuffer.Add(new CbufferEntry(){type=type, name=name});
//         expr = new LiteralExpression(name);
//     }

//     public StringBuilder BuildSource(){
//         StringBuilder sb = new();
//         sb.AppendLine($"#include \"{spaceFile}\"");

//         if(cbuffer.Count > 0){
//             sb.AppendLine("cbuffer SceneParams : register(b1){");
//             foreach(var line in cbuffer){
//                 sb.AppendLine($"    {line.type} {line.name};");
//             }
//             sb.AppendLine("}");
//         }

//         sb.AppendLine($"float GetDistance(Pos p){{");
//         sb.Append(BuildSdfSource());
//         sb.AppendLine("}");

//         sb.AppendLine($"HitResult GetHit(Pos p, Dir d){{");
//         sb.Append(BuildHitSource());
//         sb.AppendLine("}");

//         sb.AppendLine($"#include \"RayMarchingCore.hlsl\"");
//         sb.AppendLine($"#include \"{rendererFile}\"");
//         sb.AppendLine($"#include \"RayMarchingMain.hlsl\"");

//         return sb;
//     }

//     public StringBuilder BuildSdfSource(){
//         var sb = new SourceBuilder();
//         var ctx = new BuildContext();

//         root.PrepareCtx(ctx);
//         root.BuildSdfSource(sb, ctx);

//         var final = new StringBuilder();
//         final.Append(sb.presource);
//         final.AppendLine();
//         final.Append("return ");
//         final.Append(sb.source);
//         return final;
//     }

//     public StringBuilder BuildHitSource(){
//         var sb = new SourceBuilder();
//         var ctx = new BuildContext();

//         root.PrepareCtx(ctx);
//         root.BuildHitSource(sb, ctx);

//         var final = new StringBuilder();
//         final.Append(sb.presource);
//         final.AppendLine();
//         final.Append("return ");
//         final.Append(sb.source);
//         return final;
//     }
// }

// public abstract class Sphere : ObjectBuilder{
//     public Expression center;
//     public Expression radius;
//     public Expression color;

//     public Sphere(Expression center, Expression radius, Expression color){
//         this.center = center;
//         this.radius = radius;
//         this.color = color;
//     }

//     public override void BuildSdfSource(SourceBuilder sb, BuildContext context){
//         var ctx = context.GetContext(this);
//         if(ctx.sdfVariableName == null){
//             ctx.sdfVariableName = "v" + Guid.NewGuid().ToString("N");
//             sb.AppendPresource(new StringBuilder($"float {ctx.sdfVariableName}(Pos p){{ return SphereSDF(p, {center}, {radius}); }}\n"));
//         }
//         sb.AppendSource(new StringBuilder($"{ctx.sdfVariableName}(p)"));
//     }

//     public override void BuildHitSource(SourceBuilder sb, BuildContext context){
//         var ctx = context.GetContext(this);
//         if(ctx.hitVariableName == null){
//             ctx.hitVariableName = $"sphereHit{context.GetContext(this).occuranceCount}";
//             sb.AppendPresource(new StringBuilder($"HitResult {ctx.hitVariableName}(Pos p){{ return SphereHit(p, {center}, {radius}, {color}); }}\n"));
//         }
//         sb.AppendSource(new StringBuilder($"{ctx.hitVariableName}(p)"));
//     }
// }