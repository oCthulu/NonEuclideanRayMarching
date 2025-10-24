using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

public ref struct ConstantBufferWriter : IDisposable{
    private int bufferIndex;
    private ComputeShader shader;
    private ConstantBuffer constantBuffer;
    private Buffer buffer;
    private byte[] data;

    public ConstantBufferWriter(ComputeShader shader, ShaderReflection shaderReflection, ref Buffer? buffer, int bufferIndex)
    {
        this.shader = shader;
        this.bufferIndex = bufferIndex;

        constantBuffer = shaderReflection.GetConstantBuffer(bufferIndex);
        buffer ??= new Buffer(shader.Device, new BufferDescription(){
            SizeInBytes = constantBuffer.Description.Size,
            BindFlags = BindFlags.ConstantBuffer,
            Usage = ResourceUsage.Default,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        });

        this.buffer = buffer;
        data = new byte[constantBuffer.Description.Size];
    }

    public readonly Span<byte> GetWriteableSpan(string name)
    {
        var member = constantBuffer.GetVariable(name) ?? throw new ArgumentException($"Member {name} not found in constant buffer.");
        
        var offset = member.Description.StartOffset;
        var size = member.Description.Size;
        if (offset + size > data.Length) throw new ArgumentException($"Member {name} exceeds constant buffer size.");

        return new Span<byte>(data, offset, size);
    }

    public readonly void Write<T>(string name, T value) where T : unmanaged{
        Span<byte> span = GetWriteableSpan(name);
        if (span.Length != Unsafe.SizeOf<T>()) throw new ArgumentException($"Data size {span.Length} does not match member size {Unsafe.SizeOf<T>()}.");
        MemoryMarshal.Write(span, in value);
    }

    public readonly void Write(string name, Span<byte> data)
    {
        Span<byte> span = GetWriteableSpan(name);
        if (data.Length != span.Length) throw new ArgumentException($"Data size {data.Length} does not match member size {span.Length}.");
        data.CopyTo(span);
    }

    public readonly void Dispose()
    {
        shader.Device.ImmediateContext.UpdateSubresource(data, buffer);
        shader.Device.ImmediateContext.ComputeShader.SetConstantBuffer(bufferIndex, buffer);
    }
}