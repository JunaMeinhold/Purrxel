namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using Hexa.NET.D3DCompiler;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using VoxelEngine.Debugging;
    using VoxelEngine.IO;
    using D3DShaderMacro = Hexa.NET.D3DCommon.ShaderMacro;

    public static unsafe class ShaderCompiler
    {
        public static void Reflect<T>(ReadOnlySpan<byte> blob, out ComPtr<T> reflector) where T : unmanaged, IComObject<T>
        {
            fixed (byte* pData = blob)
            {
                D3DCompiler.Reflect(pData, (nuint)blob.Length, out reflector);
            }
        }

        public static bool Compile(byte* pSource, int sourceLen, ShaderMacro[] macros, string entryPoint, string sourceName, string basePath, string profile, out Blob? shaderBlob, out string? error)
        {
            shaderBlob = null;
            error = null;
            ShaderFlags flags = (ShaderFlags)(1 << 21);
#if DEBUG && !RELEASE && !SHADER_FORCE_OPTIMIZE
                flags |= ShaderFlags.Debug | ShaderFlags.SkipOptimization | ShaderFlags.DebugNameForSource;
#else
            flags |= ShaderFlags.OptimizationLevel2;
#endif

            var pMacros = macros.Length > 0 ? AllocT<D3DShaderMacro>(macros.Length + 1) : null;

            for (int i = 0; i < macros.Length; i++)
            {
                var macro = macros[i];
                var pName = macro.Name.ToUTF8Ptr();
                var pDef = macro.Definition.ToUTF8Ptr();
                pMacros[i] = new(pName, pDef);
            }
            if (pMacros != null)
            {
                pMacros[macros.Length].Name = null;
                pMacros[macros.Length].Definition = null;
            }

            byte* pEntryPoint = entryPoint.ToUTF8Ptr();
            byte* pSourceName = sourceName.ToUTF8Ptr();
            byte* pProfile = profile.ToUTF8Ptr();

            ID3D10Blob* vBlob;
            ID3D10Blob* vError;

            string systemInclude = Paths.CurrentShaderPath;

            IncludeHandler handler = new(Path.Combine(Paths.CurrentShaderPath, basePath) ?? string.Empty, systemInclude);
            ID3DInclude* include = (ID3DInclude*)Alloc(sizeof(ID3DInclude) + sizeof(nint));
            include->LpVtbl = (void**)Alloc(sizeof(nint) * 2);
            include->LpVtbl[0] = (void*)Marshal.GetFunctionPointerForDelegate(handler.Open);
            include->LpVtbl[1] = (void*)Marshal.GetFunctionPointerForDelegate(handler.Close);

            D3DCompiler.Compile(pSource, (nuint)sourceLen, pSourceName, pMacros, include, pEntryPoint, pProfile, (uint)flags, 0, &vBlob, &vError);

            Free(include->LpVtbl);
            Free(include);

            Free(pEntryPoint);
            Free(pSourceName);
            Free(pProfile);

            for (int i = 0; i < macros.Length; i++)
            {
                var macro = pMacros[i];
                Free(macro.Name);
                Free(macro.Definition);
            }

            Free(pMacros);

            if (vError != null)
            {
                error = ToStringFromUTF8((byte*)vError->GetBufferPointer());
                vError->Release();
            }

            if (vBlob == null)
            {
                return false;
            }

            shaderBlob = new(vBlob->GetBufferPointer(), vBlob->GetBufferSize(), copy: true);
            vBlob->Release();

            return true;
        }

        public static unsafe Blob GetInputSignature(Shader* shader)
        {
            ComPtr<ID3D10Blob> output = default;
            D3DCompiler.GetInputSignatureBlob(shader->Bytecode, shader->Length, output.GetAddressOf());
            Blob blob = new(output.GetBufferPointer(), output.GetBufferSize(), true);
            output.Release();
            return blob;
        }

        public static unsafe void Reflect<T>(Shader* blob, out ComPtr<T> reflector) where T : unmanaged, IComObject<T>
        {
            D3DCompiler.Reflect(blob->Bytecode, blob->Length, out reflector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Compile(byte* pSource, int sourceLen, ShaderMacro[] macros, string entry, string sourceName, string basePath, string profile, Shader** shader, out string? error)
        {
            Compile(pSource, sourceLen, macros, entry, sourceName, basePath, profile, out var shaderBlob, out error);
            if (shaderBlob != null)
            {
                Shader* pShader = AllocT<Shader>();
                pShader->Bytecode = AllocCopyT((byte*)shaderBlob.BufferPointer, (uint)shaderBlob.PointerSize);
                pShader->Length = shaderBlob.PointerSize;
                *shader = pShader;
            }

            if (error != null)
            {
                ImGuiConsole.Log(error);
            }
        }

        public static unsafe void GetShaderOrCompileFile(string entry, string path, string profile, ShaderMacro[] macros, Shader** shader, bool bypassCache = false)
        {
            string fullPath = Paths.CurrentShaderPath + path;
            string basePath = Path.GetDirectoryName(path)!;
            byte[] data = FileSystem.ReadAllBytes(fullPath);
            uint hash = CRC32.Crc32(data);
            Shader* pShader;
            if (bypassCache || !ShaderCache.GetShader(path, SourceLanguage.HLSL, hash, macros, &pShader, out _))
            {
                fixed (byte* pData = data)
                {
                    Compile(pData, data.Length, macros, entry, path, basePath, profile, &pShader, out _);
                }

                if (pShader == null)
                {
                    return;
                }

                ShaderCache.CacheShader(path, SourceLanguage.HLSL, hash, macros, Array.Empty<InputElementDescription>(), pShader);
            }
            *shader = pShader;
        }

        public static unsafe void GetShaderOrCompileFileWithInputSignature(string entry, string path, string profile, ShaderMacro[] macros, Shader** shader, out InputElementDescription[]? inputElements, out Blob signature, bool bypassCache = false)
        {
            string fullPath = Paths.CurrentShaderPath + path;
            string basePath = Path.GetDirectoryName(path)!;
            byte[] data = FileSystem.ReadAllBytes(fullPath);
            uint hash = CRC32.Crc32(data);
            Shader* pShader;
            if (bypassCache || !ShaderCache.GetShader(path, SourceLanguage.HLSL, hash, macros, &pShader, out inputElements))
            {
                fixed (byte* pData = data)
                {
                    Compile(pData, data.Length, macros, entry, path, basePath, profile, &pShader, out _);
                }

                signature = null;
                inputElements = null;
                if (pShader == null)
                {
                    return;
                }

                signature = GetInputSignature(pShader);
                inputElements = GetInputElementsFromSignature(pShader);
                ShaderCache.CacheShader(path, SourceLanguage.HLSL, hash, macros, inputElements, pShader);
            }
            *shader = pShader;
            signature = GetInputSignature(pShader);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe InputElementDescription[] GetInputElementsFromSignature(Shader* shader)
        {
            Reflect(shader->AsSpan(), out ComPtr<ID3D11ShaderReflection> reflection);
            ShaderDesc desc;
            reflection.GetDesc(&desc);

            var inputElements = new InputElementDescription[desc.InputParameters];
            for (uint i = 0; i < desc.InputParameters; i++)
            {
                SignatureParameterDesc parameterDesc;
                reflection.GetInputParameterDesc(i, &parameterDesc);

                InputElementDescription inputElement = new()
                {
                    SemanticName = ToStringFromUTF8(parameterDesc.SemanticName)!,
                    SemanticIndex = (int)parameterDesc.SemanticIndex,
                    Slot = 0,
                    AlignedByteOffset = -1,
                    Classification = InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                };

                if (parameterDesc.Mask == (byte)RegisterComponentMaskFlags.ComponentX)
                {
                    inputElement.Format = parameterDesc.ComponentType switch
                    {
                        RegisterComponentType.Uint32 => Format.R32Uint,
                        RegisterComponentType.Sint32 => Format.R32Sint,
                        RegisterComponentType.Float32 => Format.R32Float,
                        _ => Format.Unknown,
                    };
                }

                if (parameterDesc.Mask == (byte)(RegisterComponentMaskFlags.ComponentX | RegisterComponentMaskFlags.ComponentY))
                {
                    inputElement.Format = parameterDesc.ComponentType switch
                    {
                        RegisterComponentType.Uint32 => Format.R32G32Uint,
                        RegisterComponentType.Sint32 => Format.R32G32Sint,
                        RegisterComponentType.Float32 => Format.R32G32Float,
                        _ => Format.Unknown,
                    };
                }

                if (parameterDesc.Mask == (byte)(RegisterComponentMaskFlags.ComponentX | RegisterComponentMaskFlags.ComponentY | RegisterComponentMaskFlags.ComponentZ))
                {
                    inputElement.Format = parameterDesc.ComponentType switch
                    {
                        RegisterComponentType.Uint32 => Format.R32G32B32Uint,
                        RegisterComponentType.Sint32 => Format.R32G32B32Sint,
                        RegisterComponentType.Float32 => Format.R32G32B32Float,
                        _ => Format.Unknown,
                    };
                }

                if (parameterDesc.Mask == (byte)(RegisterComponentMaskFlags.ComponentX | RegisterComponentMaskFlags.ComponentY | RegisterComponentMaskFlags.ComponentZ | RegisterComponentMaskFlags.ComponentW))
                {
                    inputElement.Format = parameterDesc.ComponentType switch
                    {
                        RegisterComponentType.Uint32 => Format.R32G32B32A32Uint,
                        RegisterComponentType.Sint32 => Format.R32G32B32A32Sint,
                        RegisterComponentType.Float32 => Format.R32G32B32A32Float,
                        _ => Format.Unknown,
                    };
                }

                inputElements[i] = inputElement;
            }

            reflection.Release();
            return inputElements;
        }
    }
}