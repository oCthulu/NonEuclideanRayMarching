using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SharpDX.D3DCompiler;

namespace GpuHlslRayMarchingTest
{
    public static class DxcCompiler
    {
        // Use the modern DXC interfaces with correct GUIDs from dxcapi.h
        [ComImport]
        [Guid("4605C4CB-2019-492A-ADA4-65F20BB7D67F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDxcUtils
        {
            void CreateBlobFromBlob(IntPtr pBlob, uint offset, uint length, out IntPtr ppResult);
            void CreateBlobFromPinned(IntPtr pData, uint size, uint codePage, out IntPtr ppBlobEncoding);
            void CreateBlobWithEncodingFromPinned(IntPtr pText, uint size, uint codePage, out IntPtr pBlobEncoding);
            void CreateBlobWithEncodingOnHeapCopy(IntPtr pText, uint size, uint codePage, out IntPtr pBlobEncoding);
            void CreateBlobWithEncodingOnMalloc(IntPtr pText, IntPtr pIMalloc, uint size, uint codePage, out IntPtr pBlobEncoding);
            void CreateDefaultIncludeHandler(out IntPtr ppResult);
            void CreateReflection(ref DxcBuffer pData, ref Guid iid, out IntPtr ppvReflection);
            void BuildArguments([MarshalAs(UnmanagedType.LPWStr)] string pSourceName, [MarshalAs(UnmanagedType.LPWStr)] string pEntryPoint, [MarshalAs(UnmanagedType.LPWStr)] string pTargetProfile, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] pArguments, uint argCount, IntPtr pDefines, uint defineCount, out IntPtr ppArgs);
            void GetPDBContents(IntPtr pPDBBlob, out IntPtr ppHash, out IntPtr ppContainer);
        }

        [ComImport]
        [Guid("228B4687-5A6A-4730-900C-9702B2203F54")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDxcCompiler3
        {
            void Compile(ref DxcBuffer pSource, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] pArguments, uint argCount, IntPtr pIncludeHandler, ref Guid riid, out IntPtr ppResult);
            void Disassemble(ref DxcBuffer pObject, ref Guid riid, out IntPtr ppResult);
        }

        [ComImport]
        [Guid("58346CDA-DDE7-4497-9461-6F87AF5E0659")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDxcResult
        {
            void GetStatus(out int pStatus);
            void GetResult(out IntPtr ppResult);
            void GetErrorBuffer(out IntPtr ppErrors);
            void HasOutput(uint dxcOutKind);
            void GetOutput(uint dxcOutKind, ref Guid iid, out IntPtr ppvObject, out IntPtr ppOutputName);
            void GetNumOutputs(out uint pResult);
            void GetOutputByIndex(uint index, out uint pOutputType);
            void PrimaryOutput();
        }

        [ComImport]
        [Guid("8BA5FB08-5195-40e2-AC58-0D989C3A0102")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDxcBlob
        {
            IntPtr GetBufferPointer();
            nuint GetBufferSize();
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DxcBuffer
        {
            public IntPtr Ptr;
            public nuint Size;
            public uint Encoding;
        }

        // Correct CLSIDs from dxcapi.h
        private static readonly Guid CLSID_DxcUtils = new Guid("6245d6af-66e0-48fd-80b4-4d271796748c");
        private static readonly Guid CLSID_DxcCompiler = new Guid("73e22d93-e6ce-47f3-b5bf-f0664f39c1b0");

        [DllImport("dxcompiler.dll", EntryPoint = "DxcCreateInstance")]
        private static extern int DxcCreateInstance(ref Guid rclsid, ref Guid riid, out IntPtr ppv);

        public static ShaderBytecode CompileShader(string shaderSource, string entryPoint, string profile)
        {
            IntPtr compilerPtr = IntPtr.Zero;
            IntPtr sourcePtr = IntPtr.Zero;
            IntPtr resultPtr = IntPtr.Zero;
            
            try
            {
                // Create only the DXC compiler - we don't need utils for basic compilation
                var compilerGuid = typeof(IDxcCompiler3).GUID;
                var compilerClsid = CLSID_DxcCompiler;

                int hr = DxcCreateInstance(ref compilerClsid, ref compilerGuid, out compilerPtr);
                if (hr != 0) throw new Exception($"Failed to create DxcCompiler: 0x{hr:X}");

                var compiler = Marshal.GetObjectForIUnknown(compilerPtr) as IDxcCompiler3;
                if (compiler == null) throw new Exception("Failed to get IDxcCompiler3 interface");

                // Create source buffer directly
                var sourceBytes = Encoding.UTF8.GetBytes(shaderSource);
                sourcePtr = Marshal.AllocHGlobal(sourceBytes.Length);
                Marshal.Copy(sourceBytes, 0, sourcePtr, sourceBytes.Length);

                var sourceBuffer = new DxcBuffer
                {
                    Ptr = sourcePtr,
                    Size = (nuint)sourceBytes.Length,
                    Encoding = 65001 // UTF-8
                };

                // Compile arguments
                string[] args = {
                    "-E", entryPoint,
                    "-T", profile,
                    "-O3"
                };

                var resultGuid = typeof(IDxcResult).GUID;
                compiler.Compile(ref sourceBuffer, args, (uint)args.Length, IntPtr.Zero, ref resultGuid, out resultPtr);

                var result = Marshal.GetObjectForIUnknown(resultPtr) as IDxcResult;
                if (result == null) throw new Exception("Failed to get IDxcResult interface");

                // Check compilation status
                result.GetStatus(out int status);
                if (status != 0)
                {
                    string errorMessage = "DXC compilation failed";
                    
                    try
                    {
                        result.GetErrorBuffer(out IntPtr errorPtr);
                        if (errorPtr != IntPtr.Zero)
                        {
                            if (Marshal.GetObjectForIUnknown(errorPtr) is IDxcBlob errorBlob)
                            {
                                try
                                {
                                    IntPtr errorTextPtr = errorBlob.GetBufferPointer();
                                    nuint errorSize = errorBlob.GetBufferSize();

                                    if (errorTextPtr != IntPtr.Zero && errorSize > 0)
                                    {
                                        byte[] errorBytes = new byte[errorSize];
                                        Marshal.Copy(errorTextPtr, errorBytes, 0, (int)errorSize);

                                        // Try to decode as UTF-8, fallback to ASCII if that fails
                                        try
                                        {
                                            string errorText = Encoding.UTF8.GetString(errorBytes).TrimEnd('\0');
                                            if (!string.IsNullOrWhiteSpace(errorText))
                                            {
                                                errorMessage = $"DXC compilation failed:\n{errorText}";
                                            }
                                        }
                                        catch
                                        {
                                            string errorText = Encoding.ASCII.GetString(errorBytes).TrimEnd('\0');
                                            if (!string.IsNullOrWhiteSpace(errorText))
                                            {
                                                errorMessage = $"DXC compilation failed:\n{errorText}";
                                            }
                                        }
                                    }
                                }
                                finally
                                {
                                    Marshal.ReleaseComObject(errorBlob);
                                }
                            }
                            // Release the error buffer pointer
                            Marshal.Release(errorPtr);
                        }
                    }
                    catch (Exception ex)
                    {
                        //throw;
                        errorMessage = $"DXC compilation failed (status: 0x{status:X}, error retrieval failed: {ex.Message})";
                    }
                    
                    throw new Exception(errorMessage);
                }

                // Get compiled bytecode
                result.GetResult(out IntPtr bytecodePtr);
                var bytecodeBlob = Marshal.GetObjectForIUnknown(bytecodePtr) as IDxcBlob;
                if (bytecodeBlob == null) throw new Exception("Failed to get bytecode blob");
                
                IntPtr dataPtr = bytecodeBlob.GetBufferPointer();
                nuint dataSize = bytecodeBlob.GetBufferSize();
                
                // Copy bytecode to managed array
                byte[] bytecode = new byte[dataSize];
                Marshal.Copy(dataPtr, bytecode, 0, (int)dataSize);
                
                // Clean up COM objects
                Marshal.ReleaseComObject(bytecodeBlob);
                Marshal.ReleaseComObject(result);
                Marshal.ReleaseComObject(compiler);

                return new ShaderBytecode(bytecode);
            }
            catch (DllNotFoundException)
            {
                throw new Exception("dxcompiler.dll not found. Please install Windows 10 SDK or place dxcompiler.dll in your output directory.");
            }
            catch (COMException ex)
            {
                throw new Exception($"DXC COM error: 0x{ex.HResult:X} - {ex.Message}");
            }
            finally
            {
                // Clean up native memory
                if (sourcePtr != IntPtr.Zero) Marshal.FreeHGlobal(sourcePtr);
                if (compilerPtr != IntPtr.Zero) Marshal.Release(compilerPtr);
                if (resultPtr != IntPtr.Zero) Marshal.Release(resultPtr);
            }
        }

        // Fallback method using DXC process if COM interface fails
        public static ShaderBytecode CompileShaderProcess(string shaderSource, string entryPoint, string profile)
        {
            string tempShaderFile = Path.GetTempFileName() + ".hlsl";
            string tempOutputFile = Path.GetTempFileName() + ".cso";

            try
            {
                File.WriteAllText(tempShaderFile, shaderSource);

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dxc.exe",
                    Arguments = $"-T {profile} -E {entryPoint} -Fo \"{tempOutputFile}\" \"{tempShaderFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(startInfo))
                {
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        string error = process.StandardError.ReadToEnd();
                        throw new Exception($"DXC compilation failed: {error}");
                    }
                }

                byte[] bytecode = File.ReadAllBytes(tempOutputFile);
                return new ShaderBytecode(bytecode);
            }
            finally
            {
                if (File.Exists(tempShaderFile)) File.Delete(tempShaderFile);
                if (File.Exists(tempOutputFile)) File.Delete(tempOutputFile);
            }
        }
    }
}
