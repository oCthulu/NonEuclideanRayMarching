using System.Text;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace SceneBuilding;



public abstract class Expression<T> {
    public abstract string BuildSource(SourceBuilder sb);

    public static implicit operator Expression<T>(T value) => new LiteralExpression<T>(value);

    public virtual Expression<TTo> Apply<TTo>(Func<T, TTo> func) where TTo : unmanaged {
        throw new NotImplementedException($"Apply not valid for {GetType().Name}.");
    }
}

public class LiteralExpression<T> : Expression<T> {
    public T value;

    public LiteralExpression(T value){
        this.value = value;
    }

    public override string BuildSource(SourceBuilder sb){
        return sb.scene.GetTypeDescription<T>().BuildSource(value, sb);
    }

    public override Expression<TTo> Apply<TTo>(Func<T, TTo> func) 
    {
        return new LiteralExpression<TTo>(func(value));
    }
}

public class FunctionExpression<T> : Expression<T> where T : unmanaged {
    Func<T> func;
    string varName;
    SceneBuilder sceneBuilder;

    public FunctionExpression(Func<T> func, SceneBuilder sceneBuilder){
        this.func = func;
        this.sceneBuilder = sceneBuilder;

        varName = $"expr{Guid.NewGuid():N}";

        sceneBuilder.DefineParameter<T>(varName);

        sceneBuilder.onCbufferWrite += (cbw) => cbw.Write(varName, func());
    }

    public override string BuildSource(SourceBuilder sb){
        return varName;
    }

    public override Expression<TTo> Apply<TTo>(Func<T, TTo> func)
    {
        return new FunctionExpression<TTo>(() => func(this.func()), sceneBuilder);
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
    // public class SubBuilder{
    //     public readonly StringBuilder source = new();
    //     HashSet<object> marks = new();

    //     public void Append(StringBuilder sb){
    //         source.Append(sb);
    //     }
    //     public void Append(string str){
    //         source.Append(str);
    //     }
    //     public void AppendLine(){
    //         source.AppendLine();
    //     }
    //     public void AppendLine(StringBuilder sb){
    //         Append(sb);
    //         AppendLine();
    //     }
    //     public void AppendLine(string str){
    //         Append(str);
    //         AppendLine();
    //     }

    //     public void Mark(object obj){
    //         marks.Add(obj);
    //     }
    //     public bool HasMark(object obj){
    //         return marks.Contains(obj);
    //     }
    //     public bool HasMarkAndMark(object obj){
    //         bool has = HasMark(obj);
    //         if(!has) Mark(obj);
    //         return has;
    //     }

    //     public void Mark<T>(){
    //         Mark(typeof(T));
    //     }
    //     public bool HasMark<T>(){
    //         return HasMark(typeof(T));
    //     }
    //     public bool HasMarkAndMark<T>(){
    //         return HasMarkAndMark(typeof(T));
    //     }
    // }

    public readonly SceneBuilder scene;

    // public SubBuilder global = new();
    // public SubBuilder cbuffer = new();
    // public SubBuilder? function;
    // public SubBuilder? local;

    public SourceBuilder(SceneBuilder scene){
        this.scene = scene;
    }

    public SourceBuilder(SourceBuilder other){
        scene = other.scene;
    }

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
    public void AppendLine(string str)
    {
        Append(str);
        AppendLine();
    }

    public string NewVariableName(string prefix = "var")
    {
        return $"{prefix}{Guid.NewGuid():N}";
    }
}

public class MarkedSourceBuilder : SourceBuilder{
    HashSet<object> marks = new();

    public MarkedSourceBuilder(SourceBuilder other) : base(other){
    }

    public void Mark(object obj){
        marks.Add(obj);
    }
    public bool HasMark(object obj){
        return marks.Contains(obj);
    }
    public void MarkAndAppend(object obj, StringBuilder sb){
        if(!HasMark(obj)){
            Mark(obj);
            Append(sb);
        }
    }

    public void MarkAndAppend(object obj, string str){
        if(!HasMark(obj)){
            Mark(obj);
            Append(str);
        }
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

public abstract class TransformingObjectBuilder : ObjectBuilder{
    protected ObjectBuilder child;

    public TransformingObjectBuilder(ObjectBuilder child){
        this.child = child;
    }

    public override void Prepare(SourceBuilder sb){
        base.Prepare(sb);
        child.Prepare(sb);
    }

    public override void FinalizeBuild(){
        base.FinalizeBuild();
        child.FinalizeBuild();
    }

    protected abstract void TransformInput(string inputVar, SourceBuilder sb);
    protected abstract void TransformOutputSdf(string outputVar, SourceBuilder sb);
    protected abstract void TransformOutputHit(string outputVar, SourceBuilder sb);

    public override string BuildSdfSource(SourceBuilder sb)
    {
        //backup the current position and apply the inverse transform
        string varName = $"localPos{Guid.NewGuid():N}";
        string exprName = $"localSdf{Guid.NewGuid():N}";

        sb.AppendLine($"Pos {varName} = p;");

        TransformInput("p", sb);

        // SourceBuilder local = new SourceBuilder(sb);
        // string sdfExpr = child.BuildSdfSource(local);
        // sb.AppendLine(local.source);
        //sb.AppendLine($"float {exprName} = {sdfExpr};");
        sb.AppendLine($"float {exprName} = {child.BuildSdfSource(sb)};");

        TransformOutputSdf(exprName, sb);

        sb.AppendLine($"p = {varName};"); //restore the original position

        return $"({exprName})";
    }

    public override string BuildHitSource(SourceBuilder sb)
    {
        //backup the current position and apply the inverse transform
        string varName = $"localPos{Guid.NewGuid():N}";
        string exprName = $"localHit{Guid.NewGuid():N}";

        sb.AppendLine($"Pos {varName} = p;");

        TransformInput("p", sb);

        SourceBuilder local = new SourceBuilder(sb);
        string hitExpr = child.BuildHitSource(local);
        sb.AppendLine(local.source);
        sb.AppendLine($"HitResult {exprName} = {hitExpr};");

        TransformOutputHit(exprName, sb);

        sb.AppendLine($"p = {varName};"); //restore the original position

        return $"({exprName})";
    }
}

// public abstract class FunctionObjectBuilder<T> : ObjectBuilder{
//     protected string? sdfVariableName;
//     protected string? hitVariableName;

//     public static void Require<TReq>(SourceBuilder sb) where TReq : FunctionObjectBuilder<T>, new(){
//         if(!sb.global.HasMarkAndMark<TReq>()){
//             var obj = new TReq();
//             obj.BuildFunctions(sb);
//         }
//     }

//     public override string BuildSdfSource(SourceBuilder sb)
//     {
//         if(!sb.global.HasMarkAndMark<T>()){
//             BuildFunctions(sb);
//         }
//         return GetSdfExpression(sb);
//     }
//     public override string BuildHitSource(SourceBuilder sb)
//     {
//         if(!sb.global.HasMarkAndMark<T>()){
//             BuildFunctions(sb);
//         }
//         return GetHitExpression(sb);
//     }

//     public abstract void BuildFunctions(SourceBuilder sb);
//     public abstract string GetSdfExpression(SourceBuilder sb);
//     public abstract string GetHitExpression(SourceBuilder sb);
// }

public class SceneBuilder{
    readonly Dictionary<Type, TypeDescriptor> typeDescriptions = [];
    readonly SourceBuilder main;
    public readonly MarkedSourceBuilder global;
    public readonly MarkedSourceBuilder cbuffer;

    readonly string spaceFile;
    readonly string rendererFile;

    private string? camTransformName;

    public ObjectBuilder? root;

    public Action<ConstantBufferWriter>? onCbufferWrite;

    public SceneBuilder(string spaceFile = "Spaces/Euclidean.hlsl", string rendererFile = "Renderers/EuclideanLit.hlsl"){
        main = new SourceBuilder(this);
        global = new MarkedSourceBuilder(main);
        cbuffer = new MarkedSourceBuilder(main);

        this.spaceFile = spaceFile;
        this.rendererFile = rendererFile;

        //Register built-in type descriptions
        RegisterTypeDescription<float, FloatDescriptor>();
        RegisterTypeDescription<Matrix, MatrixDescriptor>();
        RegisterTypeDescription<Vector2, Vector2Descriptor>();
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

    public void DefineParameter<T>(string name){
        if(!cbuffer.HasMark(name)){
            cbuffer.Mark(name);
            var typeDesc = GetTypeDescription<T>();
            cbuffer.AppendLine($"    {typeDesc.GetTypeName()} {name};");
        }
    }

    public Expression<T> Expr<T>(Func<T> func) where T : unmanaged{
        return new FunctionExpression<T>(func, this);
    }

    public void DefineCameraTransform<T>(string name){
        if(camTransformName != null) throw new InvalidOperationException("Camera transform already defined.");
        camTransformName = name;
        DefineParameter<T>(name);
    }

    public string BuildSource(){
        if(root == null) throw new InvalidOperationException("No root object defined.");

        root.Prepare(main);
        

        //TODO: perhaps have a stack to handle nested local builders?
        SourceBuilder sdf = new SourceBuilder(main);
        SourceBuilder hit = new SourceBuilder(main);

        string sdfSource = root.BuildSdfSource(sdf);
        string hitSource = root.BuildHitSource(hit);

        main.AppendLine($"#include \"{spaceFile}\"");
        if(cbuffer.source.Length > 0){
            main.AppendLine("cbuffer SceneParams : register(b0){");
            main.Append(cbuffer.source);
            main.AppendLine("}");
        }
        main.AppendLine();

        main.Append(global.source);
        main.AppendLine();

        main.AppendLine("float GetDistance(Pos p){");
        main.Append(sdf.source);
        main.Append($"return {sdfSource};");
        main.AppendLine("}");

        main.AppendLine();

        main.AppendLine("HitResult GetHit(Pos p){");
        main.Append(hit.source);
        main.Append($"return {hitSource};");
        main.AppendLine("}");
        main.AppendLine();

        if(camTransformName == null) throw new InvalidOperationException("Camera transform not defined.");
        main.AppendLine(
            $$"""
            Transform GetTransform()
            {
                return {{camTransformName}};
            }
            """
        );

        main.AppendLine($"#include \"RayMarchingCore.hlsl\"");
        main.AppendLine($"#include \"{rendererFile}\"");
        main.AppendLine($"#include \"RayMarchingMain.hlsl\"");

        root.FinalizeBuild();

        return main.source.ToString();
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