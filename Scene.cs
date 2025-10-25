using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using SharpDX.Windows;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.DXGI.Resource;
using SharpDX.Direct3D;
using Color = SharpDX.Color;

public class Scene : IDisposable{
    public bool Enabled {get => enabled; set { 
        enabled = value; 
        if (enabled) OnEnabled?.Invoke(); else OnDisabled?.Invoke();
        OnEnableChanged?.Invoke();
    }}
    public event Action<ConstantBufferWriter>? writeSceneConstants = null;
    public event Action<ConstantBufferWriter>? writeRenderConstants = null;

    public event Action? OnEnableChanged;
    public event Action? OnEnabled;
    public event Action? OnDisabled;

    bool enabled;

    Device device;
    ComputeShader shader;
    ShaderReflection reflection;

    Buffer? renderParams;
    Buffer? sceneParams;

    public Scene(Device device, ShaderBytecode shaderByteCode){
        this.device = device;

        shader = new ComputeShader(device, shaderByteCode);
        reflection = new ShaderReflection(shaderByteCode);
    }

    public Scene(Device device, string fileName, string entryPoint = "CSMain", string profile = "cs_5_0", Include? includeHandler = null){
        this.device = device;

        var shaderByteCode = ShaderBytecode.CompileFromFile(fileName, entryPoint, profile, ShaderFlags.None, EffectFlags.None, null, includeHandler);
        shader = new ComputeShader(device, shaderByteCode);
        reflection = new ShaderReflection(shaderByteCode);
    }

    public void RenderTo(UnorderedAccessView target, Action<ConstantBufferWriter>? writeExtraSceneConstants = null, Action<ConstantBufferWriter>? writeExtraRenderConstants = null){
        var targetTex = target.ResourceAs<Texture2D>();
        var context = device.ImmediateContext;

        using (var writer = new ConstantBufferWriter(shader, reflection, ref sceneParams, 0))
        {
            writeSceneConstants?.Invoke(writer);
            writeExtraSceneConstants?.Invoke(writer);
        }
        
        if(reflection.Description.ConstantBuffers > 1) {
            using var writer = new ConstantBufferWriter(shader, reflection, ref renderParams, 1);
            writeRenderConstants?.Invoke(writer);
            writeExtraRenderConstants?.Invoke(writer);
        }

        context.ComputeShader.Set(shader);
        context.ComputeShader.SetUnorderedAccessView(0, target);

        int threadGroupX = (targetTex.Description.Width + 15) / 16;
        int threadGroupY = (targetTex.Description.Height + 15) / 16;
        context.Dispatch(threadGroupX, threadGroupY, 1);

        context.ComputeShader.SetUnorderedAccessView(0, null);
        context.ComputeShader.Set(null);
        
        targetTex.Dispose();
    }

    public void Dispose(){
        renderParams?.Dispose();
        sceneParams?.Dispose();
        shader.Dispose();
        reflection.Dispose();
    }
}

