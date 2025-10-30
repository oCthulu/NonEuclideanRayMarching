using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using SharpDX.Windows;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.DXGI.Resource;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX.Direct3D;
using SceneBuilding;
using Plane = SceneBuilding.Plane;
using System.ComponentModel;

namespace GpuHlslRayMarchingTest
{
    public partial class Form1 : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public static Form1 Instance { get; private set; } = null!;
        public event Action? OnRender;
        //common DX11 fields
        private Device device;
        private SwapChain swapChain;

        //textures and views
        private Texture2D backBuffer;
        private UnorderedAccessView backBufferUAV;

        //float startTime = Environment.TickCount / 1000.0f;
        int startTimeMillis = Environment.TickCount;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float Time {get; private set;} = 0;


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float DeltaTime => Time - prevTime;

        float prevTime;

        List<Scene> scenes = new();
        int currentSceneIndex = 0;
        int CurrentSceneIndex {
            get => currentSceneIndex;
            set {
                Current.Enabled = false;
                currentSceneIndex = value;
                Current.Enabled = true;
            }
        }

        Scene Current => scenes[CurrentSceneIndex];

        public Form1()
        {
            Instance = this;
            //setup swapchain, device, and main render target
            var swapChainDescription = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(ClientSize.Width, ClientSize.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.UnorderedAccess
            };

            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, swapChainDescription, out device, out swapChain);

            var includeHandler = new IncludeHandler();
            //main = new Scene(device, "Resources/Scenes/TestScene2.hlsl", "CSMain", "cs_5_0", includeHandler);

            {
                SceneBuilder sb = new();
                var root = new Sphere(1.0f, new Vector3(0, 0, 0), new Vector4(1,0,0,1));

                sb.root = root;
                sb.DefineCameraTransform<Matrix>("camTransform");

                Scene scene = sb.Build(device, includeHandler);
                new OrbitCamera(scene);
                scenes.Add(scene);
            }
            {
                SceneBuilder sb = new();
                var root = new SmoothUnion(0.5f,
                    new Sphere(1.0f, new Vector3(0, 0, 0), new Vector4(1,0,0,1)),
                    new Sphere(0.5f, sb.Expr(() => new Vector3(0, 0, 2f * MathF.Sin(Time))), new Vector4(0,1,0,1))
                );

                sb.root = root;
                sb.DefineCameraTransform<Matrix>("camTransform");

                Scene scene = sb.Build(device, includeHandler);
                new OrbitCamera(scene);
                scenes.Add(scene);
            }
            {
                SceneBuilder sb = new();
                var root = new Intersection(
                    new Sphere(1.0f, new Vector3(0, 0, 0), new Vector4(1,0,0,1)),
                    new Sphere(1.5f, sb.Expr(() => new Vector3(0, 0, 2f * MathF.Sin(Time))), new Vector4(1,0,0,1))
                );

                sb.root = root;
                sb.DefineCameraTransform<Matrix>("camTransform");
                
                Scene scene = sb.Build(device, includeHandler);
                new OrbitCamera(scene);
                scenes.Add(scene);
            }
            {
                SceneBuilder sb = new();
                var root = new Intersection(
                    new Sphere(2.0f, new Vector3(0, 0, 0), new Vector4(1,1,1,1)),
                    new Plane(new Vector3(0,1,0), 0, new Vector4(1,1,1,1)),
                    new Invert(
                        new SmoothUnion(0.5f,
                            new Sphere(1f, new Vector3(0, 0, 0), new Vector4(1,1,1,1)),
                            new Sphere(0.7f, sb.Expr(() => 3f * MathF.Sin(Time) * new Vector3(0, 0, 1)), new Vector4(1,1,1,1)),
                            new Sphere(0.7f, sb.Expr(() => (1.5f * MathF.Sin(Time/4) - 1.5f) * new Vector3(0, 1, 0)), new Vector4(1,1,1,1)),
                            new Sphere(0.7f, sb.Expr(() => 3f * MathF.Sin(Time/2) * new Vector3(1, 0, 0)), new Vector4(1,1,1,1))
                        )
                    )
                );

                sb.root = root;
                sb.DefineCameraTransform<Matrix>("camTransform");
                
                Scene scene = sb.Build(device, includeHandler);
                new OrbitCamera(scene);
                scenes.Add(scene);
            }


            {
                SceneBuilder sb = new("Spaces/Hyperbolic.hlsl", "Renderers/HyperbolicLit.hlsl");
                var root = new Union(
                    new SphereH(0.1f, HyperUtil.ToHyperbolic(new Vector3(0, 0, 0)), new Vector4(1,0,0,1)),
                    new SphereH(0.1f, HyperUtil.TranslationX(1).Column4, new Vector4(1,0,0,1)),
                    new Intersection(
                        new PlaneH(new Vector4(0,1,0,0), 0, new Vector4(1,1,1,1)),
                        new SphereH(2f, HyperUtil.Origin, new Vector4(1, 1, 1, 1))
                    )
                );

                sb.root = root;
                sb.DefineCameraTransform<Matrix>("camTransform");

                Scene scene = sb.Build(device, includeHandler);
                new OrbitCameraH(scene);
                scenes.Add(scene);
            }
            {
                SceneBuilder sb = new("Spaces/Hyperbolic.hlsl", "Renderers/HyperbolicLit.hlsl");
                var root = new Union(
                    HyperUtil.Tiling(
                        4,
                        5,
                        2,
                        (transform) => new TransformH(
                            transform,
                            new SphereH(0.2f, HyperUtil.TranslationY(0.2f).Column4, new Vector4(1, 0, 0, 1))
                        )
                    ),
                    new Intersection(
                        new PlaneH(new Vector4(0, 1, 0, 0), 0, new Vector4(1, 1, 1, 1)),
                        new SphereH(3.2f, HyperUtil.Origin, new Vector4(1, 1, 1, 1))
                    )
                );

                sb.root = root;
                sb.DefineCameraTransform<Matrix>("camTransform");

                Scene scene = sb.Build(device, includeHandler);
                new FirstPersonCameraH(scene, 1, 0.5f);
                scenes.Add(scene);
            }
            {
                SceneBuilder sb = new("Spaces/Hyperbolic.hlsl", "Renderers/HyperbolicLit.hlsl");

                int sides = 12;
                float interiorAngle = MathF.PI / 2;
                float vertDistPadding = 0.2f;
                float sideDist = HyperUtil.HyperTriSideLength(
                    interiorAngle / 2,
                    MathF.PI / sides,
                    MathF.PI / 2
                );
                float vertDist = HyperUtil.HyperTriSideLength(
                    MathF.PI / 2,
                    MathF.PI / sides,
                    interiorAngle / 2
                ) - vertDistPadding;

                var root = new Union(
                    HyperUtil.NGonPrism(
                        sides,
                        sideDist,
                        1f,
                        new Vector4(1, 1, 1, 1),
                        Matrix.RotationY(MathF.PI / sides)
                    ),
                    new SphereH(0.1f, (HyperUtil.TranslationZ(vertDist) * HyperUtil.TranslationY(0.1f)).Column4, new Vector4(1, 0, 0, 1)),
                    new SphereH(0.1f, (HyperUtil.TranslationZ(-vertDist) * HyperUtil.TranslationY(0.1f)).Column4, new Vector4(0, 0, 1, 1))
                );

                sb.root = root;
                sb.DefineCameraTransform<Matrix>("camTransform");

                Scene scene = sb.Build(device, includeHandler);
                new FirstPersonCameraH(scene, 1, 0.5f);
                scenes.Add(scene);
            }
            {
                SceneBuilder sb = new("Spaces/Hyperbolic.hlsl", "Renderers/HyperbolicLit.hlsl");

                var root = new Intersection(
                    new MixMatch(
                        new PlaneH(new Vector4(0, 1, 0, 0), 0, new Vector4()),
                        new Union(
                            HyperUtil.Tiling(
                                4,
                                5,
                                2,
                                0.1f,
                                0.05f,
                                new Vector4(1, 1, 1, 1)//,
                                // () => new SphereH(0.2f, HyperUtil.TranslationY(0.2f).Column4, new Vector4(
                                //     Vector3.Normalize(new Vector3(
                                //         (float)rand.NextDouble(),
                                //         (float)rand.NextDouble(),
                                //         (float)rand.NextDouble()
                                //     )),
                                //     1
                                // ))
                            ),
                            new ConstantH(
                                0.001f,
                                new Vector4(0.5f, 0.5f, 0.5f, 1),
                                new Vector4(0, 1, 0, 0)
                            )
                        )
                    ),
                    new SphereH(3.2f, HyperUtil.Origin, new Vector4(0.5f, 0.5f, 0.5f, 1))
                );

                sb.root = root;
                sb.DefineCameraTransform<Matrix>("camTransform");

                Scene scene = sb.Build(device, includeHandler);
                new FirstPersonCameraH(scene, 1, 0.5f);
                scenes.Add(scene);
            }
            {
                SceneBuilder sb = new("Spaces/Hyperbolic.hlsl", "Renderers/HyperbolicLit.hlsl");

                var root = new Intersection(
                    new MixMatch(
                        new PlaneH(new Vector4(0, 1, 0, 0), 0, new Vector4()),
                        new Union(
                            HyperUtil.Tiling(
                                5,
                                4,
                                2,
                                0.1f,
                                0.05f,
                                new Vector4(1, 1, 1, 1)//,
                                // () => new SphereH(0.2f, HyperUtil.TranslationY(0.2f).Column4, new Vector4(
                                //     Vector3.Normalize(new Vector3(
                                //         (float)rand.NextDouble(),
                                //         (float)rand.NextDouble(),
                                //         (float)rand.NextDouble()
                                //     )),
                                //     1
                                // ))
                            ),
                            new ConstantH(
                                0.001f,
                                new Vector4(0.5f, 0.5f, 0.5f, 1),
                                new Vector4(0, 1, 0, 0)
                            )
                        )
                    ),
                    new SphereH(3.2f, HyperUtil.Origin, new Vector4(0.5f, 0.5f, 0.5f, 1))
                );

                sb.root = root;
                sb.DefineCameraTransform<Matrix>("camTransform");

                Scene scene = sb.Build(device, includeHandler);
                new FirstPersonCameraH(scene, 1, 0.5f);
                scenes.Add(scene);
            }

            CurrentSceneIndex = 0;



            backBuffer = swapChain.GetBackBuffer<Texture2D>(0);
            backBufferUAV = new UnorderedAccessView(device, backBuffer);

            var context = device.ImmediateContext;
            context.Rasterizer.SetViewport(new Viewport(0, 0, ClientSize.Width, ClientSize.Height));

            InitializeComponent();

            //setup callbacks
            KeyDown += new KeyEventHandler(Form1_KeyDown);
            
            // Enable key events
            KeyPreview = true;
            
            RenderLoop.Run(this, RenderCallback);
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    CurrentSceneIndex = (CurrentSceneIndex - 1 + scenes.Count) % scenes.Count;
                    break;
                case Keys.Right:
                    CurrentSceneIndex = (CurrentSceneIndex + 1) % scenes.Count;
                    break;
            }
        }


        private void RenderCallback()
        {
            // Check if resources are still valid
            if (backBufferUAV?.IsDisposed != false)
                return;

            Time = (Environment.TickCount - startTimeMillis) / 1000.0f;

            OnRender?.Invoke();

            Vector3 lightingDir = new(0.5f, 1, -1);
            lightingDir.Normalize();

            Current.RenderTo(backBufferUAV,
                null,
                writer => {
                    writer.Write("lightDirection", lightingDir);
                }
            );

            swapChain.Present(1, PresentFlags.None);

            prevTime = Time;
        }

        protected override void OnResize(EventArgs e)
        {
            //resize the back buffer and swap chain
            backBuffer.Dispose();
            backBufferUAV.Dispose();
            swapChain.ResizeBuffers(1, ClientSize.Width, ClientSize.Height, Format.R8G8B8A8_UNorm, SwapChainFlags.None);
            
            backBuffer = swapChain.GetBackBuffer<Texture2D>(0);
            backBufferUAV = new UnorderedAccessView(device, backBuffer);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose of managed resources
                backBufferUAV?.Dispose();
                backBuffer?.Dispose();
                swapChain?.Dispose();
                device?.Dispose();
                foreach(var scene in scenes)
                    scene.Dispose();
            }
            base.Dispose(disposing);
        }

        // //common DX11 fields
        // private Device device;
        // private SwapChain swapChain;

        // //textures and views
        // private Texture2D backBuffer;
        // private UnorderedAccessView backBufferUAV;

        // //render shader fields
        // private ComputeShader shader;
        // private ShaderBytecode shaderBytecode;
        // private ShaderReflection shaderReflection;
        // private Buffer? renderParams;
        // private Buffer? sceneParams;
        
        // // IL operations buffer
        // private Buffer? operationsBuffer;
        // private UnorderedAccessView? operationsBufferUAV;

        // //"interesting" fields
        // private Vector2 camRotation;
        // private float logCamDistance = 1.3f;
        // private System.Drawing.Point lastMousePosition;

        // private float CamDistance => (float)Math.Exp(logCamDistance);

        // float startTime = Environment.TickCount / 1000.0f;

        // public Form1()
        // {
        //     //setup swapchain, device, and main render target
        //     var swapChainDescription = new SwapChainDescription()
        //     {
        //         BufferCount = 1,
        //         ModeDescription = new ModeDescription(ClientSize.Width, ClientSize.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
        //         IsWindowed = true,
        //         OutputHandle = Handle,
        //         SampleDescription = new SampleDescription(1, 0),
        //         SwapEffect = SwapEffect.Discard,
        //         Usage = Usage.UnorderedAccess
        //     };

        //     Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, swapChainDescription, out device, out swapChain);

        //     backBuffer = swapChain.GetBackBuffer<Texture2D>(0);
        //     backBufferUAV = new UnorderedAccessView(device, backBuffer);

        //     var context = device.ImmediateContext;
        //     context.Rasterizer.SetViewport(new Viewport(0, 0, ClientSize.Width, ClientSize.Height));

        //     //operations buffer - start empty
        //     // operationsBuffer = new Buffer(device, new BufferDescription
        //     // {
        //     //     SizeInBytes = Marshal.SizeOf<IlOperation>() * 1, //start with space for 1 operation
        //     //     BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
        //     //     CpuAccessFlags = CpuAccessFlags.None,
        //     //     Usage = ResourceUsage.Default,
        //     //     StructureByteStride = Marshal.SizeOf<IlOperation>(),
        //     //     OptionFlags = ResourceOptionFlags.BufferStructured
        //     // });
        //     // operationsBufferUAV = new UnorderedAccessView(device, operationsBuffer);

        //     // //setup other render buffers
        //     // normalBuffer = new Texture2D(device, new Texture2DDescription()
        //     // {
        //     //     Width = ClientSize.Width,
        //     //     Height = ClientSize.Height,
        //     //     MipLevels = 1,
        //     //     ArraySize = 1,
        //     //     Format = Format.R32G32B32A32_Float,
        //     //     SampleDescription = new SampleDescription(1, 0),
        //     //     Usage = ResourceUsage.Default,
        //     //     BindFlags = BindFlags.UnorderedAccess
        //     // });
        //     // normalBufferUAV = new UnorderedAccessView(device, normalBuffer);

        //     // positionBuffer = new Texture2D(device, new Texture2DDescription()
        //     // {
        //     //     Width = ClientSize.Width,
        //     //     Height = ClientSize.Height,
        //     //     MipLevels = 1,
        //     //     ArraySize = 1,
        //     //     Format = Format.R32G32B32A32_Float,
        //     //     SampleDescription = new SampleDescription(1, 0),
        //     //     Usage = ResourceUsage.Default,
        //     //     BindFlags = BindFlags.UnorderedAccess
        //     // });
        //     // positionBufferUAV = new UnorderedAccessView(device, positionBuffer);


        //     InitializeComponent();

        //     // Define the include handler
        //     var includeHandler = new IncludeHandler();

        //     // Debug: Try DXC without fallback to see exact errors
        //     // string shaderSource = File.ReadAllText("Resources/Scenes/IL.hlsl");
        //     // shaderBytecode = DxcCompiler.CompileShader(shaderSource, "CSMain", "cs_6_0");
        //     shaderBytecode = ShaderBytecode.CompileFromFile("Resources/Scenes/IL.hlsl", "CSMain", "cs_5_0", ShaderFlags.None, EffectFlags.None, null, includeHandler);

        //     shader = new ComputeShader(device, shaderBytecode);
        //     shaderReflection = new ShaderReflection(shaderBytecode);

        //     // Initialize IL operations
        //     //InitializeILOperations();

        //     // Compile the SSAO shader
        //     // ssaoShaderBytecode = ShaderBytecode.CompileFromFile("Resources/SSAO.hlsl", "CSMain", "cs_5_0", ShaderFlags.None, EffectFlags.None, null, includeHandler);
        //     // ssaoShader = new ComputeShader(device, ssaoShaderBytecode);
        //     // ssaoShaderReflection = new ShaderReflection(ssaoShaderBytecode);

        //     //setup callbacks
        //     MouseMove += new MouseEventHandler(Form1_MouseMove);
        //     MouseWheel += new MouseEventHandler(Form1_MouseWheel);
        //     //KeyDown += new KeyEventHandler(Form1_KeyDown);
            
        //     // Enable key events
        //     KeyPreview = true;
            
        //     RenderLoop.Run(this, RenderCallback);
        // }

        // private void Form1_MouseMove(object? sender, MouseEventArgs e)
        // {
        //     if (MouseButtons == MouseButtons.Left)
        //     {
        //         var deltaX = e.X - lastMousePosition.X;
        //         var deltaY = e.Y - lastMousePosition.Y;

        //         camRotation.X += deltaY * 0.01f; // Adjust sensitivity as needed
        //         camRotation.Y += deltaX * 0.01f; // Adjust sensitivity as needed
        //     }
        //     lastMousePosition = e.Location;
        // }

        // private void Form1_MouseWheel(object? sender, MouseEventArgs e)
        // {
        //     logCamDistance += e.Delta * 0.001f; // Adjust sensitivity as needed
        // }


        // private void RenderCallback()
        // {
        //     float time = Environment.TickCount / 1000.0f;
        //     time -= startTime;

        //     //setup camera view matrix
        //     Matrix cameraView = Matrix.Identity;
        //     cameraView *= Matrix.Translation(0, 0, -CamDistance);
        //     cameraView *= Matrix.RotationX(camRotation.X);
        //     cameraView *= Matrix.RotationY(camRotation.Y);
        //     //cameraView *= Matrix.Translation(0, 0.5f, 0);

        //     cameraView.Transpose();

        //     Vector3 lightingDir = new(0.5f, 1, -1);
        //     lightingDir.Normalize();

        //     var context = device.ImmediateContext;

        //     // using (var writer = new ConstantBufferWriter(renderShader, renderShaderReflection, ref renderShaderConstantBuffer, 0))
        //     // {
        //     //     writer.Write("camMatrix", cameraView);
        //     //     writer.Write("lightingDir", lightingDir);

        //     //     writer.Write("directLightingWeight", 0.7f);
        //     //     writer.Write("skyLightingWeight", 0.25f);
        //     //     writer.Write("ambientLightingWeight", 0.05f);
        //     // }

        //     using (var writer = new ConstantBufferWriter(shader, shaderReflection, ref renderParams, 0))
        //     {
        //         writer.Write("camTransform", cameraView);
        //     }
        //     using (var writer = new ConstantBufferWriter(shader, shaderReflection, ref sceneParams, 1))
        //     {
        //         writer.Write("lightDirection", lightingDir);
        //     }

        //     context.ComputeShader.Set(shader);
        //     context.ComputeShader.SetUnorderedAccessView(0, backBufferUAV);
            

        //     // SetOperations([
        //     //     Cube(Matrix.Translation(0, 0, 3), 3f, new Vector4(1, 0, 0, 1)),
        //     //     Sphere(Matrix.Translation(0, 0, 0), 1f, new Vector4(1, 0, 0, 1)),
        //     //     Sphere(Matrix.Translation(3*MathF.Sin(time), 0, 0), 0.8f, new Vector4(1, 0, 0, 1)),
        //     //     //Union()
        //     //     SmoothUnion(0.5f),
        //     //     Invert(),
        //     //     Intersection()
        //     // ]);
        //     SetOperations([
        //         Cube(Matrix.RotationY(time), 3f, new Vector4(1, 0, 0, 1)),
        //         // Sphere(Matrix.Translation(0, 0, 0), 1f, new Vector4(1, 0, 0, 1)),
        //         // Sphere(Matrix.Translation(3*MathF.Sin(time), 0, 0), 0.8f, new Vector4(1, 0, 0, 1)),
        //         // SmoothUnion(0.5f),
        //         // Invert(),
        //         // Intersection()
        //     ]);
        //     context.ComputeShader.SetUnorderedAccessView(1, operationsBufferUAV);

        //     int threadGroupX = (ClientSize.Width + 15) / 16;
        //     int threadGroupY = (ClientSize.Height + 15) / 16;
        //     context.Dispatch(threadGroupX, threadGroupY, 1);

        //     //switch to SSAO shader
        //     // context.ComputeShader.Set(ssaoShader);
        //     // context.Dispatch(threadGroupX, threadGroupY, 1);

        //     swapChain.Present(1, PresentFlags.None);
        // }

        // protected override void OnResize(EventArgs e)
        // {
        //     //resize the back buffer and swap chain
        //     backBuffer.Dispose();
        //     backBufferUAV.Dispose();
        //     swapChain.ResizeBuffers(1, ClientSize.Width, ClientSize.Height, Format.R8G8B8A8_UNorm, SwapChainFlags.None);
            
        //     backBuffer = swapChain.GetBackBuffer<Texture2D>(0);
        //     backBufferUAV = new UnorderedAccessView(device, backBuffer);

        //     // //resize the normal buffer
        //     // Texture2DDescription desc = normalBuffer.Description;
        //     // normalBuffer.Dispose();
        //     // normalBufferUAV.Dispose();
        //     // normalBuffer = new Texture2D(device, desc with {
        //     //     Width = ClientSize.Width,
        //     //     Height = ClientSize.Height
        //     // });
        //     // normalBufferUAV = new UnorderedAccessView(device, normalBuffer);

        //     // //resize the position buffer
        //     // desc = positionBuffer.Description;
        //     // positionBuffer.Dispose();
        //     // positionBufferUAV.Dispose();
        //     // positionBuffer = new Texture2D(device, desc with {
        //     //     Width = ClientSize.Width,
        //     //     Height = ClientSize.Height
        //     // });
        //     // positionBufferUAV = new UnorderedAccessView(device, positionBuffer);
        // }

        // protected override void Dispose(bool disposing)
        // {
        //     if (disposing)
        //     {
        //         // Dispose of managed resources
        //         operationsBuffer?.Dispose();
        //         operationsBufferUAV?.Dispose();
        //         renderParams?.Dispose();
        //         sceneParams?.Dispose();
        //         shader?.Dispose();
        //         shaderBytecode?.Dispose();
        //         shaderReflection?.Dispose();
        //         backBufferUAV?.Dispose();
        //         backBuffer?.Dispose();
        //         swapChain?.Dispose();
        //         device?.Dispose();
        //     }
        //     base.Dispose(disposing);
        // }

        // void SetOperations(IlOperation[] operations)
        // {
        //     //check if we need to resize the buffer
        //     int requiredSize = Marshal.SizeOf<IlOperation>() * operations.Length;
        //     if (operationsBuffer == null || operationsBuffer.Description.SizeInBytes < requiredSize)
        //     {
        //         operationsBuffer?.Dispose();
        //         operationsBufferUAV?.Dispose();

        //         operationsBuffer = new Buffer(device, new BufferDescription
        //         {
        //             SizeInBytes = requiredSize,
        //             BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
        //             CpuAccessFlags = CpuAccessFlags.None,
        //             Usage = ResourceUsage.Default,
        //             StructureByteStride = Marshal.SizeOf<IlOperation>(),
        //             OptionFlags = ResourceOptionFlags.BufferStructured
        //         });
        //         operationsBufferUAV = new UnorderedAccessView(device, operationsBuffer);
        //     }

        //     //upload new data
        //     device.ImmediateContext.UpdateSubresource(operations, operationsBuffer);
        // }


        // //TODO: Move to utility class
        // IlOperation Operation(int opcode, Matrix transform, Vector4 parameters, Vector4 albedo)
        // {
        //     Matrix t = transform;
        //     t.Invert();
        //     t.Transpose();
        //     return new IlOperation
        //     {
        //         Opcode = opcode,
        //         Transform = t,
        //         Parameters = parameters,
        //         Albedo = albedo
        //     };
        // }

        // IlOperation Sphere(Matrix transform, float radius, Vector4 albedo)
        // {
        //     return Operation(5, transform, new Vector4(0, 0, 0, radius), albedo);
        // }

        // IlOperation Plane(Matrix transform, Vector3 normal, float d, Vector4 albedo)
        // {
        //     return Operation(6, transform, new Vector4(normal, d), albedo);
        // }

        // IlOperation Cube(Matrix transform, float size, Vector4 albedo)
        // {
        //     return Operation(7, transform, new Vector4(0, 0, 0, size), albedo);
        // }

        // IlOperation Union()
        // {
        //     return Operation(0, Matrix.Identity, Vector4.Zero, Vector4.Zero);
        // }

        // IlOperation Invert()
        // {
        //     return Operation(2, Matrix.Identity, Vector4.Zero, Vector4.Zero);
        // }

        // IlOperation Intersection()
        // {
        //     return Operation(1, Matrix.Identity, Vector4.Zero, Vector4.Zero);
        // }

        // IlOperation SmoothUnion(float k)
        // {
        //     return Operation(8, Matrix.Identity, new Vector4(0, 0, 0, k), Vector4.Zero);
        // }



        // Include handler class for shader includes
        private class IncludeHandler : Include
        {
            public void Close(Stream stream)
            {
                stream.Close();
            }

            public Stream Open(IncludeType type, string fileName, Stream parentStream)
            {
                return new FileStream("Resources/" + fileName, FileMode.Open, FileAccess.Read);
            }

            public IDisposable? Shadow { get; set; }

            public void Dispose()
            {
                // No resources to dispose
            }
        }

        // private void InitializeILOperations()
        // {
        //     // Create a sample scene using the IL builder
        //     currentOperations = ILBuilder.CreateSmoothScene();
            
        //     // Create the operations buffer
        //     CreateOperationsBuffer();
        // }

        // private void CreateOperationsBuffer()
        // {
        //     // Dispose existing buffer if it exists
        //     operationsBuffer?.Dispose();
        //     operationsBufferUAV?.Dispose();

        //     if (currentOperations.Length > 0)
        //     {
        //         // Create structured buffer for operations
        //         var bufferDesc = new BufferDescription
        //         {
        //             SizeInBytes = System.Runtime.InteropServices.Marshal.SizeOf<IlOperation>() * currentOperations.Length,
        //             BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
        //             CpuAccessFlags = CpuAccessFlags.None,
        //             Usage = ResourceUsage.Default,
        //             StructureByteStride = System.Runtime.InteropServices.Marshal.SizeOf<IlOperation>(),
        //             OptionFlags = ResourceOptionFlags.BufferStructured
        //         };

        //         operationsBuffer = Buffer.Create(device, currentOperations, bufferDesc);
        //         operationsBufferUAV = new UnorderedAccessView(device, operationsBuffer);
        //     }
        // }

        // public void UpdateScene(IlOperation[] newOperations)
        // {
        //     currentOperations = newOperations;
        //     CreateOperationsBuffer();
        // }

        // // Example method to create different scenes
        // private void CreateExampleScene1()
        // {
        //     var operations = new ILBuilder()
        //         .Sphere(Vector3.Zero, 1.0f, new Vector4(1, 0, 0, 1))  // Red sphere
        //         .Sphere(new Vector3(1.5f, 0, 0), 0.8f, new Vector4(0, 1, 0, 1))  // Green sphere
        //         .Union()
        //         .ToArray();
            
        //     UpdateScene(operations);
        // }

        // private void CreateExampleScene2()
        // {
        //     var operations = ILBuilder.CreateBooleanScene();
        //     UpdateScene(operations);
        // }

        // private void CreateExampleScene3()
        // {
        //     var operations = ILBuilder.CreateSmoothScene();
        //     UpdateScene(operations);
        // }

        // private void Form1_KeyDown(object? sender, KeyEventArgs e)
        // {
        //     switch (e.KeyCode)
        //     {
        //         case Keys.D1:
        //             CreateExampleScene1();
        //             break;
        //         case Keys.D2:
        //             CreateExampleScene2();
        //             break;
        //         case Keys.D3:
        //             CreateExampleScene3();
        //             break;
        //         case Keys.R:
        //             // Reload shader
        //             ReloadShader();
        //             break;
        //     }
        // }

        // private void ReloadShader()
        // {
        //     try
        //     {
        //         shader?.Dispose();
        //         shaderBytecode?.Dispose();
        //         shaderReflection?.Dispose();

        //         var includeHandler = new IncludeHandler();
        //         shaderBytecode = ShaderBytecode.CompileFromFile("Resources/Scenes/IL.hlsl", "CSMain", "cs_5_0", ShaderFlags.None, EffectFlags.None, null, includeHandler);
        //         shader = new ComputeShader(device, shaderBytecode);
        //         shaderReflection = new ShaderReflection(shaderBytecode);
        //     }
        //     catch (Exception ex)
        //     {
        //         System.Diagnostics.Debug.WriteLine($"Failed to reload shader: {ex.Message}");
        //     }
        // }
    }
}